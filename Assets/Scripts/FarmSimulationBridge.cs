using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class FarmGridCellData
{
    public int row;
    public int col;
}

[Serializable]
public class FarmStationData
{
    public FarmGridCellData gas;
    public FarmGridCellData seeds;
}

[Serializable]
public class FarmFieldData
{
    public int rows;
    public int columns;
    public string[] coordinate_order;
    public FarmGridCellData[] obstacles;
    public FarmStationData stations;
    public int sowable_count;
}

[Serializable]
public class FarmTractorData
{
    public int id;
    public int row;
    public int col;
    public string direction;
    public float seeds;
    public float fuel;
    public string mode;
}

[Serializable]
public class FarmMetricsData
{
    public float coverage_pct;
    public int collisions;
    public int rejected_actions;
}

[Serializable]
public class FarmScenarioData
{
    public string architecture;
    public float density;
    public string start_mode;
    // Legacy/compatibility aliases emitted by the Python bridge.
    public float obstacle_density;
    public int seed;
    public string initial_positions;
    public bool q_learning;
}

[Serializable]
public class FarmResetRequestData
{
    public float density;
    public int seed;
    public string start_mode;
    public bool q_learning;
    public string architecture;
}

[Serializable]
public class FarmErrorData
{
    public string error;
}

[Serializable]
public class FarmSnapshotData
{
    public int schema_version;
    public FarmScenarioData scenario;
    public int tick;
    public bool complete;
    public bool finished;
    public FarmFieldData field;
    public FarmGridCellData[] planted;
    public FarmTractorData[] tractors;
    public FarmMetricsData metrics;
}

/// <summary>
/// Presentation-only client for the live Python AgentPy bridge.
/// Add this component to ConexionPython; Python remains the simulation authority.
/// </summary>
public class FarmSimulationBridge : MonoBehaviour
{
    [Header("Python bridge")]
    public string serverUrl = "http://127.0.0.1:8000";
    public float secondsPerTick = 0.8f;
    public bool autoAdvance = true;

    [Header("Scene references (auto-filled from ComunicacionPython when available)")]
    public Transform tractor1;
    public Transform tractor2;
    public Transform tractor3;
    public GameObject obstaclePrefab;
    public Material plantedMaterial;
    // The tractor model's hood points along its local +X axis.
    public float headingOffsetY = -90f;

    private FarmSnapshotData current;
    private bool firstSnapshot = true;
    private bool fieldSynchronized;
    private bool requestNextTick;
    private bool requestFailed;
    private bool requestInProgress;
    private Coroutine sessionCoroutine;
    private FarmResetRequestData pendingReset;
    private bool externalUiActive;
    private string connectionMessage = "Elige una opción para consultar el episodio activo de Python.";
    private float requestedDensity = 0.10f;
    private string requestedSeed = "0";
    private int requestedStartMode;
    private int requestedArchitecture;
    private bool requestedQLearning;
    private GeneradorObstaculos obstacleGenerator;
    private ControlSiembra sowingController;

    public FarmSnapshotData CurrentSnapshot => current;
    public string StatusMessage => connectionMessage;
    public bool IsAutoAdvancing => autoAdvance;
    public bool IsFinished => current != null && current.finished;
    public float TicksPerSecond => 1f / Mathf.Max(0.05f, secondsPerTick);

    public void SetExternalUIActive(bool active)
    {
        externalUiActive = active;
    }

    private void Awake()
    {
        ComunicacionPython demoBridge = GetComponent<ComunicacionPython>();
        if (demoBridge != null)
        {
            demoBridge.StopAllCoroutines();
            demoBridge.enabled = false;
            tractor1 = tractor1 != null ? tractor1 : demoBridge.tractor1;
            tractor2 = tractor2 != null ? tractor2 : demoBridge.tractor2;
            tractor3 = tractor3 != null ? tractor3 : demoBridge.tractor3;
        }

        obstacleGenerator = FindAnyObjectByType<GeneradorObstaculos>();
        sowingController = FindAnyObjectByType<ControlSiembra>();
        if (obstaclePrefab == null && obstacleGenerator != null)
            obstaclePrefab = obstacleGenerator.prefabObstaculo;
        if (plantedMaterial == null && sowingController != null)
            plantedMaterial = sowingController.materialSembrado;

        DesactivarMovimientoLocal(tractor1);
        DesactivarMovimientoLocal(tractor2);
        DesactivarMovimientoLocal(tractor3);
    }

    public void ConnectToCurrentEpisode(bool startAutomatically)
    {
        if (sessionCoroutine != null)
            return;

        autoAdvance = startAutomatically;
        requestNextTick = false;
        connectionMessage = "Consultando el episodio activo de Python...";
        sessionCoroutine = StartCoroutine(ConsumirSimulacion());
    }

    public void StartConfiguredEpisode(float density, int seed, string startMode, bool qLearning, string architecture, bool startAutomatically)
    {
        if (density < 0f || density >= 0.5f)
        {
            connectionMessage = "La densidad debe estar entre 0 y menos de 0.50.";
            return;
        }
        if (startMode != "fixed" && startMode != "random")
        {
            connectionMessage = "start_mode debe ser fixed o random.";
            return;
        }
        if (architecture != "improved" && architecture != "baseline")
        {
            connectionMessage = "architecture debe ser improved o baseline.";
            return;
        }
        if (qLearning && architecture != "improved")
        {
            connectionMessage = "Q-learning solo está disponible con improved.";
            return;
        }

        FarmResetRequestData reset = new FarmResetRequestData
        {
            density = density,
            seed = seed,
            start_mode = startMode,
            q_learning = qLearning,
            architecture = architecture
        };

        autoAdvance = startAutomatically;
        requestNextTick = false;
        if (sessionCoroutine != null)
        {
            if (requestInProgress)
            {
                // Let the current HTTP request finish before replacing its episode.
                pendingReset = reset;
                connectionMessage = "Reinicio solicitado; esperando la petición actual...";
                return;
            }

            // The playback coroutine may be waiting for its next visual tick.
            StopCoroutine(sessionCoroutine);
            sessionCoroutine = null;
        }

        pendingReset = null;
        connectionMessage = "Iniciando episodio en Python...";
        sessionCoroutine = StartCoroutine(ResetearYConsumir(reset));
    }

    public void PausePlayback()
    {
        autoAdvance = false;
        requestNextTick = false;
    }

    public void ResumePlayback()
    {
        if (current != null && !current.finished)
            autoAdvance = true;
    }

    public void RequestSingleTick()
    {
        if (current != null && !current.finished && !autoAdvance)
            requestNextTick = true;
    }

    public void SetTicksPerSecond(float ticksPerSecond)
    {
        float clamped = Mathf.Clamp(ticksPerSecond, 0.25f, 5f);
        secondsPerTick = 1f / clamped;
    }

    private IEnumerator ConsumirSimulacion()
    {
        yield return StartCoroutine(Consultar("GET", "/state"));
        if (requestFailed || current == null)
        {
            if (TryTakePendingReset(out FarmResetRequestData failedReadReset))
            {
                requestFailed = false;
                yield return StartCoroutine(ResetearYConsumir(failedReadReset));
                yield break;
            }

            sessionCoroutine = null;
            yield break;
        }

        if (TryTakePendingReset(out FarmResetRequestData pendingReadReset))
        {
            yield return StartCoroutine(ResetearYConsumir(pendingReadReset));
            yield break;
        }

        yield return StartCoroutine(AvanzarHastaFin());
    }

    private IEnumerator ResetearYConsumir(FarmResetRequestData reset)
    {
        current = null;
        firstSnapshot = true;
        fieldSynchronized = false;
        requestNextTick = false;
        string body = JsonUtility.ToJson(reset);
        yield return StartCoroutine(Consultar("POST", "/reset", body));
        if (requestFailed || current == null)
        {
            pendingReset = null;
            sessionCoroutine = null;
            yield break;
        }

        if (TryTakePendingReset(out FarmResetRequestData pendingPostReset))
        {
            yield return StartCoroutine(ResetearYConsumir(pendingPostReset));
            yield break;
        }

        // The reset response already contains tick 0; this read confirms that state
        // retrieval itself is non-advancing before the first POST /step.
        yield return StartCoroutine(Consultar("GET", "/state"));
        if (requestFailed)
        {
            pendingReset = null;
            sessionCoroutine = null;
            yield break;
        }

        if (TryTakePendingReset(out FarmResetRequestData pendingStateReset))
        {
            yield return StartCoroutine(ResetearYConsumir(pendingStateReset));
            yield break;
        }

        yield return StartCoroutine(AvanzarHastaFin());
    }

    private IEnumerator AvanzarHastaFin()
    {
        while (current != null && !current.finished)
        {
            if (TryTakePendingReset(out FarmResetRequestData pendingStepReset))
            {
                yield return StartCoroutine(ResetearYConsumir(pendingStepReset));
                yield break;
            }

            if (autoAdvance)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, secondsPerTick));
            }
            else
            {
                while (!requestNextTick && pendingReset == null)
                    yield return null;
                if (pendingReset != null)
                    continue;
                requestNextTick = false;
            }

            if (current != null && !current.finished)
            {
                yield return StartCoroutine(Consultar("POST", "/step", "{}"));
                if (requestFailed)
                    break;
            }
        }

        if (TryTakePendingReset(out FarmResetRequestData pendingFinishReset))
        {
            yield return StartCoroutine(ResetearYConsumir(pendingFinishReset));
            yield break;
        }

        if (current != null && current.finished)
            connectionMessage = current.complete
                ? "Simulación completada en tick " + current.tick
                : "Límite de ticks alcanzado en " + current.tick;

        sessionCoroutine = null;
    }

    private bool TryTakePendingReset(out FarmResetRequestData reset)
    {
        reset = pendingReset;
        pendingReset = null;
        return reset != null;
    }

    private IEnumerator Consultar(string method, string route, string jsonBody = null)
    {
        requestFailed = false;
        string url = serverUrl.TrimEnd('/') + route;
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (method == "POST")
            {
                byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonBody ?? "{}");
                request.uploadHandler = new UploadHandlerRaw(body);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            requestInProgress = true;
            yield return request.SendWebRequest();
            requestInProgress = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                string details = request.downloadHandler != null ? request.downloadHandler.text : "";
                FarmErrorData apiError = null;
                if (!string.IsNullOrEmpty(details))
                {
                    try { apiError = JsonUtility.FromJson<FarmErrorData>(details); }
                    catch (Exception) { }
                }
                connectionMessage = "Error Python: " +
                    (apiError != null && !string.IsNullOrEmpty(apiError.error) ? apiError.error : request.error);
                requestFailed = true;
                Debug.LogError(connectionMessage + " (" + url + ")");
                yield break;
            }

            FarmSnapshotData snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<FarmSnapshotData>(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                connectionMessage = "JSON inválido: " + exception.Message;
                requestFailed = true;
                Debug.LogError(connectionMessage);
                yield break;
            }

            if (snapshot == null || snapshot.field == null || snapshot.tractors == null)
            {
                connectionMessage = "Snapshot incompleto recibido desde Python.";
                requestFailed = true;
                Debug.LogError(connectionMessage);
                yield break;
            }

            current = snapshot;
            connectionMessage = "Conectado a Python";
            AplicarSnapshot(snapshot);
        }
    }

    private void AplicarSnapshot(FarmSnapshotData snapshot)
    {
        SincronizarCampo(snapshot.field);
        if (snapshot.planted != null)
        {
            foreach (FarmGridCellData cell in snapshot.planted)
                PintarCelda(cell.row, cell.col);
        }

        if (snapshot.tractors != null)
        {
            foreach (FarmTractorData tractor in snapshot.tractors)
            {
                Transform target = ObtenerTractor(tractor.id);
                if (target == null)
                    continue;

                Vector3 destination = new Vector3(tractor.col, target.position.y, tractor.row);
                Quaternion rotation = RotacionDe(tractor.direction, target.rotation);
                if (firstSnapshot)
                {
                    target.position = destination;
                    target.rotation = rotation;
                }
                else if ((target.position - destination).sqrMagnitude > 0.001f)
                {
                    StartCoroutine(MoverConCurva(target, destination, rotation));
                }
                else
                {
                    target.rotation = rotation;
                }
            }
        }

        firstSnapshot = false;
    }

    private void SincronizarCampo(FarmFieldData field)
    {
        if (field == null || fieldSynchronized)
            return;

        HashSet<string> desired = new HashSet<string>();
        if (field.obstacles != null)
        {
            foreach (FarmGridCellData cell in field.obstacles)
                desired.Add(NombreObstaculo(cell.row, cell.col));
        }

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in allTransforms)
        {
            if (!item.name.StartsWith("Obstaculo_", StringComparison.Ordinal))
                continue;

            item.gameObject.SetActive(false);
        }

        foreach (string obstacleName in desired)
        {
            if (!TryParseObstacleName(obstacleName, out int row, out int col))
                continue;
            FarmObstacleVisuals.CreateObstacle(row, col, obstaclePrefab, obstacleGenerator != null ? obstacleGenerator.transform : null);
        }

        PosicionarEstacion("Gasolinera", "gas", field.stations != null ? field.stations.gas : null);
        PosicionarEstacion("EstacionSemillas", "seeds", field.stations != null ? field.stations.seeds : null);
        fieldSynchronized = true;
    }

    private void PosicionarEstacion(string objectName, string kind, FarmGridCellData cell)
    {
        if (cell == null)
            return;

        GameObject station = null;
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name == objectName && item.gameObject.scene.IsValid())
            {
                station = item.gameObject;
                break;
            }
        }
        if (station != null) station.SetActive(false);
        FarmObstacleVisuals.CreateStation(kind, cell.row, cell.col);
    }

    private void PintarCelda(int row, int col)
    {
        GameObject cell = GameObject.Find("Celda_" + row + "_" + col);
        if (cell == null)
            return;

        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer != null && plantedMaterial != null)
            renderer.sharedMaterial = plantedMaterial;
    }

    private IEnumerator MoverConCurva(Transform tractor, Vector3 destination, Quaternion finalRotation)
    {
        Vector3 start = tractor.position;
        Quaternion initialRotation = tractor.rotation;
        float duration = Mathf.Max(0.05f, secondsPerTick * 0.8f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            // Turn first, then move in the direction the tractor is facing.
            tractor.rotation = Quaternion.Slerp(initialRotation, finalRotation, Mathf.Clamp01(t * 2f));
            tractor.position = Vector3.Lerp(start, destination, Mathf.Clamp01(t * 2f - 1f));
            yield return null;
        }

        tractor.position = destination;
        tractor.rotation = finalRotation;
    }

    private Quaternion RotacionDe(string direction, Quaternion fallback)
    {
        Vector3 forward;
        switch (direction)
        {
            case "N": forward = Vector3.back; break;
            case "S": forward = Vector3.forward; break;
            case "E": forward = Vector3.right; break;
            case "O": forward = Vector3.left; break;
            default: return fallback;
        }

        return Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, headingOffsetY, 0f);
    }

    private Transform ObtenerTractor(int id)
    {
        if (id == 1) return tractor1;
        if (id == 2) return tractor2;
        if (id == 3) return tractor3;
        return null;
    }

    private static string NombreObstaculo(int row, int col)
    {
        return "Obstaculo_" + row + "_" + col;
    }

    private static bool TryParseObstacleName(string name, out int row, out int col)
    {
        row = 0;
        col = 0;
        string[] parts = name.Split('_');
        return parts.Length == 3 && parts[0] == "Obstaculo" &&
            int.TryParse(parts[1], out row) && int.TryParse(parts[2], out col);
    }

    private static void DesactivarMovimientoLocal(Transform tractor)
    {
        if (tractor == null)
            return;

        MovimientoTractor straight = tractor.GetComponent<MovimientoTractor>();
        if (straight != null)
            straight.enabled = false;

        MovimientoCurvo curved = tractor.GetComponent<MovimientoCurvo>();
        if (curved != null)
            curved.enabled = false;
    }

    private void OnGUI()
    {
        if (externalUiActive)
            return;

        if (current == null)
            DibujarConfiguracion();
        else
            DibujarEstado();
    }

    private void DibujarConfiguracion()
    {
        const float x = 12f;
        const float y = 12f;
        const float width = 370f;
        GUI.Box(new Rect(x, y, width, 386f), "Configurar episodio Python");
        GUI.Label(new Rect(x + 16f, y + 32f, width - 32f, 22f),
            "Densidad de obstáculos: " + requestedDensity.ToString("F2"));
        requestedDensity = GUI.HorizontalSlider(
            new Rect(x + 16f, y + 58f, width - 32f, 20f), requestedDensity, 0f, 0.49f);

        GUI.Label(new Rect(x + 16f, y + 86f, 118f, 24f), "Semilla:");
        requestedSeed = GUI.TextField(new Rect(x + 136f, y + 84f, 90f, 26f), requestedSeed);

        GUI.Label(new Rect(x + 16f, y + 119f, 140f, 22f), "Posiciones iniciales:");
        requestedStartMode = GUI.SelectionGrid(
            new Rect(x + 16f, y + 143f, width - 32f, 30f), requestedStartMode,
            new[] { "Fijas", "Aleatorias" }, 2);

        GUI.Label(new Rect(x + 16f, y + 181f, 140f, 22f), "Arquitectura:");
        requestedArchitecture = GUI.SelectionGrid(
            new Rect(x + 16f, y + 205f, width - 32f, 30f), requestedArchitecture,
            new[] { "Improved", "Baseline" }, 2);
        if (requestedArchitecture == 1)
            requestedQLearning = false;
        GUI.enabled = requestedArchitecture == 0;
        requestedQLearning = GUI.Toggle(new Rect(x + 16f, y + 242f, width - 32f, 24f),
            requestedQLearning, "Evaluar Q-learning (requiere política cargada)");
        GUI.enabled = true;

        GUI.Label(new Rect(x + 16f, y + 273f, width - 32f, 38f), connectionMessage);
        bool seedValid = int.TryParse(requestedSeed, out int seed);
        GUI.enabled = sessionCoroutine == null && seedValid;
        if (GUI.Button(new Rect(x + 16f, y + 325f, width - 32f, 36f), "Iniciar simulación"))
        {
            StartConfiguredEpisode(requestedDensity, seed,
                requestedStartMode == 0 ? "fixed" : "random",
                requestedQLearning,
                requestedArchitecture == 0 ? "improved" : "baseline",
                true);
        }
        GUI.enabled = true;
    }

    private void DibujarEstado()
    {
        const float x = 12f;
        const float y = 12f;
        const float width = 350f;
        float height = 170f + (current.tractors != null ? current.tractors.Length * 20f : 0f);
        GUI.Box(new Rect(x, y, width, height), "Episodio multiagente");
        GUI.Label(new Rect(x + 14f, y + 28f, width - 28f, 22f), connectionMessage);

        string scenarioText = "";
        if (current.scenario != null)
        {
            float density = current.scenario.density > 0f
                ? current.scenario.density : current.scenario.obstacle_density;
            string starts = !string.IsNullOrEmpty(current.scenario.start_mode)
                ? current.scenario.start_mode : current.scenario.initial_positions;
            scenarioText = " · " + current.scenario.architecture + " · " + density.ToString("P0") +
                " · seed " + current.scenario.seed + " · " + starts;
        }

        string coverage = current.metrics != null
            ? current.metrics.coverage_pct.ToString("F1") + "%" : "—";
        GUI.Label(new Rect(x + 14f, y + 50f, width - 28f, 22f),
            "Tick " + current.tick + " · sembrado " + coverage + scenarioText);

        if (current.tractors != null)
        {
            for (int i = 0; i < current.tractors.Length; i++)
            {
                FarmTractorData tractor = current.tractors[i];
                GUI.Label(new Rect(x + 14f, y + 74f + i * 20f, width - 28f, 22f),
                    "T" + tractor.id + " · " + tractor.mode + " · semillas " + tractor.seeds.ToString("F0") +
                    " · combustible " + tractor.fuel.ToString("F0"));
            }
        }

        float controlsY = y + 80f + (current.tractors != null ? current.tractors.Length * 20f : 0f);
        if (!current.finished)
        {
            if (autoAdvance)
            {
                if (GUI.Button(new Rect(x + 14f, controlsY, 145f, 28f), "Pausar"))
                    PausePlayback();
            }
            else
            {
                if (GUI.Button(new Rect(x + 14f, controlsY, 105f, 28f), "Continuar"))
                    ResumePlayback();
                if (GUI.Button(new Rect(x + 126f, controlsY, 145f, 28f), "Siguiente tick"))
                    RequestSingleTick();
            }
        }
        else if (GUI.Button(new Rect(x + 14f, controlsY, width - 28f, 28f), "Nuevo episodio"))
        {
            current = null;
            firstSnapshot = true;
            fieldSynchronized = false;
            requestFailed = false;
            connectionMessage = "Configura el siguiente episodio.";
        }
    }

}
