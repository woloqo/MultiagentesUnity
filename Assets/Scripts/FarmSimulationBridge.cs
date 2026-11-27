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
    public bool q_learning;
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
    private string connectionMessage = "Conectando con Python...";
    private GeneradorObstaculos obstacleGenerator;
    private ControlSiembra sowingController;

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

    private void Start()
    {
        StartCoroutine(ConsumirSimulacion());
    }

    private IEnumerator ConsumirSimulacion()
    {
        yield return StartCoroutine(Consultar("GET", "/state"));
        while (current != null && !current.finished)
        {
            if (autoAdvance)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, secondsPerTick));
            }
            else
            {
                while (!requestNextTick)
                    yield return null;
                requestNextTick = false;
            }

            if (current != null && !current.finished)
                yield return StartCoroutine(Consultar("POST", "/step"));
        }

        if (current != null && current.finished)
            connectionMessage = current.complete
                ? "Simulación completada en tick " + current.tick
                : "Límite de ticks alcanzado en " + current.tick;
    }

    private IEnumerator Consultar(string method, string route)
    {
        string url = serverUrl.TrimEnd('/') + route;
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (method == "POST")
            {
                request.uploadHandler = new UploadHandlerRaw(new byte[0]);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                connectionMessage = "Error Python: " + request.error;
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
                Debug.LogError(connectionMessage);
                yield break;
            }

            if (snapshot == null || snapshot.field == null || snapshot.tractors == null)
            {
                connectionMessage = "Snapshot incompleto recibido desde Python.";
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
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 1f, 2.5f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        var title = new GUIStyle(GUI.skin.box) { fontSize = 17, fontStyle = FontStyle.Bold };
        var label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        var button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        int count = current != null && current.tractors != null ? current.tractors.Length : 0;
        GUI.Box(new Rect(12, 12, 430, 141 + count * 25), "Simulación Python", title);
        GUI.Label(new Rect(24, 40, 405, 25), connectionMessage, label);
        if (current == null)
        {
            GUI.matrix = previousMatrix;
            return;
        }

        string coverage = current.metrics != null ? current.metrics.coverage_pct.ToString("F1") + "%" : "—";
        GUI.Label(new Rect(24, 65, 405, 25), "Tick " + current.tick + " · sembrado " + coverage, label);
        string learning = current.scenario == null ? "no informado" : (current.scenario.q_learning ? "activado" : "desactivado");
        GUI.Label(new Rect(24, 90, 405, 25), "Q-learning: " + learning + " · Navegación: A*", label);
        if (current.tractors != null)
        {
            for (int i = 0; i < current.tractors.Length; i++)
            {
                FarmTractorData tractor = current.tractors[i];
                GUI.Label(new Rect(24, 115 + i * 25, 405, 25),
                    "T" + tractor.id + " · " + tractor.mode + " · semillas " + tractor.seeds.ToString("F0") + " · combustible " + tractor.fuel.ToString("F0"), label);
            }
        }

        float buttonY = 120 + count * 25;
        if (GUI.Button(new Rect(24, buttonY, 175, 30), autoAdvance ? "Pausar" : "Autoavanzar", button))
            autoAdvance = !autoAdvance;
        if (GUI.Button(new Rect(209, buttonY, 175, 30), "Siguiente tick", button) && !autoAdvance)
            requestNextTick = true;
        GUI.matrix = previousMatrix;
    }
}
