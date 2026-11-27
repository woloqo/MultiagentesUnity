using UnityEngine;

/// <summary>
/// Runtime configuration and monitoring UI for the Python-controlled farm.
/// Configuration and restart requests go through the Python HTTP bridge. This
/// component never creates simulation state locally.
/// </summary>
[RequireComponent(typeof(FarmSimulationBridge))]
public class FarmSimulationUI : MonoBehaviour
{
    private const float MinTicksPerSecond = 0.25f;
    private const float MaxTicksPerSecond = 5f;

    private FarmSimulationBridge bridge;
    private bool showingConfiguration = true;
    private int selectedDensityIndex = 2;
    private bool randomInitialPositions;
    private string seedInput = "0";
    private bool qLearningEnabled = true;
    private bool stylesReady;

    private GUIStyle panelStyle;
    private GUIStyle cardStyle;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle secondaryStyle;
    private GUIStyle valueStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle buttonStyle;
    private GUIStyle primaryButtonStyle;

    private Texture2D coverageTrack;
    private Texture2D coverageFill;

    private readonly float[] obstacleDensities = { 0f, 0.05f, 0.10f };

    private void Awake()
    {
        bridge = GetComponent<FarmSimulationBridge>();
        if (bridge != null)
            bridge.SetExternalUIActive(true);
    }

    private void OnDestroy()
    {
        if (bridge != null)
            bridge.SetExternalUIActive(false);
    }

    private void OnGUI()
    {
        if (bridge == null)
            bridge = GetComponent<FarmSimulationBridge>();
        if (bridge == null)
            return;

        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(
            Mathf.Min(Screen.width / 1280f, Screen.height / 720f),
            0.65f,
            1.35f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        if (showingConfiguration)
            DrawConfiguration(Screen.width / scale, Screen.height / scale);
        else if (bridge.CurrentSnapshot == null)
            DrawConnecting(Screen.width / scale, Screen.height / scale);
        else
            DrawDashboard();

        GUI.matrix = previousMatrix;
    }

    private void DrawConfiguration(float screenWidth, float screenHeight)
    {
        float width = Mathf.Min(700f, screenWidth - 32f);
        float height = Mathf.Min(620f, screenHeight - 28f);
        float x = (screenWidth - width) * 0.5f;
        float y = (screenHeight - height) * 0.5f;
        GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);

        float left = x + 28f;
        float contentWidth = width - 56f;
        GUI.Label(new Rect(left, y + 22f, contentWidth - 16f, 34f),
            "Configurar episodio", titleStyle);
        GUI.Label(new Rect(left, y + 58f, contentWidth, 48f),
            "Python genera el campo y decide cada acción. Al iniciar, recibe esta selección y devuelve el estado real del tick 0.",
            bodyStyle);

        GUI.Label(new Rect(left, y + 118f, contentWidth, 24f),
            "DENSIDAD DE OBSTÁCULOS", headingStyle);
        float gap = 10f;
        float choiceWidth = (contentWidth - 2f * gap) / 3f;
        string[] densityLabels = { "0 %", "5 %", "10 %" };
        for (int i = 0; i < densityLabels.Length; i++)
        {
            int choice = i;
            GUIStyle style = selectedDensityIndex == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(left + i * (choiceWidth + gap), y + 148f, choiceWidth, 42f),
                    densityLabels[i], style))
                selectedDensityIndex = choice;
        }

        GUI.Label(new Rect(left, y + 207f, contentWidth, 24f),
            "POSICIONES INICIALES", headingStyle);
        float modeWidth = (contentWidth - gap) / 2f;
        if (GUI.Button(new Rect(left, y + 237f, modeWidth, 42f),
                "Fijas", randomInitialPositions ? buttonStyle : selectedButtonStyle))
            randomInitialPositions = false;
        if (GUI.Button(new Rect(left + modeWidth + gap, y + 237f, modeWidth, 42f),
                "Aleatorias", randomInitialPositions ? selectedButtonStyle : buttonStyle))
            randomInitialPositions = true;

        GUI.Label(new Rect(left, y + 295f, 150f, 24f), "SEMILLA", headingStyle);
        seedInput = GUI.TextField(new Rect(left + 150f, y + 291f, 150f, 32f), seedInput);
        GUI.Label(new Rect(left + 315f, y + 295f, 165f, 24f),
            "A* + Blackboard", secondaryStyle);
        qLearningEnabled = GUI.Toggle(
            new Rect(left, y + 329f, contentWidth, 28f),
            qLearningEnabled,
            "Usar Q-learning (requiere las tablas cargadas en Python)");

        GUI.Box(new Rect(left, y + 378f, contentWidth, 72f), GUIContent.none, cardStyle);
        GUI.Label(new Rect(left + 14f, y + 388f, contentWidth - 28f, 54f),
            "Iniciar crea un episodio nuevo en Python con densidad, semilla e inicios seleccionados. Conectar solo lee el episodio que ya está activo; si terminó, usa Reiniciar simulación.",
            secondaryStyle);

        bool validSeed = int.TryParse(seedInput, out int selectedSeed);
        GUI.enabled = validSeed;
        if (GUI.Button(new Rect(left, y + 462f, contentWidth, 44f),
                validSeed ? "Iniciar episodio con esta selección" : "Escribe una semilla entera",
                primaryButtonStyle))
        {
            showingConfiguration = false;
            bridge.StartConfiguredEpisode(
                obstacleDensities[selectedDensityIndex],
                selectedSeed,
                randomInitialPositions ? "random" : "fixed",
                qLearningEnabled,
                "improved",
                true);
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(left, y + 516f, contentWidth, 44f),
                "Conectar con el episodio activo de Python", buttonStyle))
        {
            showingConfiguration = false;
            bridge.ConnectToCurrentEpisode(true);
        }

        string selection = "Selección local: " +
            obstacleDensities[selectedDensityIndex].ToString("P0") +
            " · semilla " + seedInput +
            " · posiciones " + (randomInitialPositions ? "aleatorias" : "fijas") +
            " · Q-learning " + (qLearningEnabled ? "activo" : "inactivo");
        GUI.Label(new Rect(left, y + 571f, contentWidth, 24f), selection, secondaryStyle);
    }

    private void DrawConnecting(float screenWidth, float screenHeight)
    {
        const float width = 520f;
        const float height = 180f;
        float x = (screenWidth - width) * 0.5f;
        float y = (screenHeight - height) * 0.5f;
        GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
        GUI.Label(new Rect(x + 24f, y + 20f, width - 48f, 34f),
            "Conexión con Python", headingStyle);
        GUI.Label(new Rect(x + 24f, y + 62f, width - 48f, 48f),
            bridge.StatusMessage, bodyStyle);
        if (GUI.Button(new Rect(x + 24f, y + 118f, width - 48f, 40f),
                "Volver a configuración", buttonStyle))
            showingConfiguration = true;
    }

    private void DrawDashboard()
    {
        FarmSnapshotData snapshot = bridge.CurrentSnapshot;
        const float x = 18f;
        const float y = 18f;
        const float width = 500f;
        const float height = 652f;
        GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);

        GUI.Label(new Rect(x + 16f, y + 12f, 310f, 30f),
            "Episodio Python", titleStyle);
        if (GUI.Button(new Rect(x + width - 118f, y + 14f, 102f, 30f),
                "Ajustes", buttonStyle))
        {
            bridge.PausePlayback();
            showingConfiguration = true;
        }

        string state = snapshot.finished
            ? (snapshot.complete ? "Completado" : "Límite de ticks")
            : (bridge.IsAutoAdvancing ? "Reproduciendo" : "En pausa");
        GUI.Label(new Rect(x + 18f, y + 45f, width - 36f, 24f),
            state + " · " + bridge.StatusMessage, secondaryStyle);

        DrawMetricCard(new Rect(x + 18f, y + 77f, 220f, 66f),
            "TICK", snapshot.tick.ToString(), null);
        string coverage = snapshot.metrics != null
            ? snapshot.metrics.coverage_pct.ToString("F1") + "%"
            : "No informado";
        DrawMetricCard(new Rect(x + 248f, y + 77f, 234f, 66f),
            "COBERTURA", coverage, snapshot.metrics);

        DrawScenario(snapshot, x + 18f, y + 153f, width - 36f);
        DrawField(snapshot, x + 18f, y + 219f, width - 36f);

        GUI.Label(new Rect(x + 18f, y + 272f, width - 36f, 22f),
            "TRACTORES", headingStyle);
        if (snapshot.tractors != null)
        {
            for (int i = 0; i < snapshot.tractors.Length; i++)
                DrawTractor(snapshot.tractors[i],
                    new Rect(x + 18f, y + 298f + i * 60f, width - 36f, 54f));
        }

        DrawExtraMetrics(snapshot, x + 18f, y + 482f, width - 36f);
        DrawPlayback(x + 18f, y + 535f, width - 36f);
    }

    private void DrawScenario(FarmSnapshotData snapshot, float x, float y, float width)
    {
        GUI.Box(new Rect(x, y, width, 58f), GUIContent.none, cardStyle);
        GUI.Label(new Rect(x + 12f, y + 7f, width - 24f, 20f),
            "ESCENARIO", secondaryStyle);

        if (snapshot.scenario == null)
        {
            GUI.Label(new Rect(x + 12f, y + 29f, width - 24f, 22f),
                "Datos del escenario no informados", bodyStyle);
            return;
        }

        float densityValue = snapshot.scenario.density > 0f
            ? snapshot.scenario.density
            : snapshot.scenario.obstacle_density;
        string density = densityValue.ToString("P0");
        string architecture = string.IsNullOrEmpty(snapshot.scenario.architecture)
            ? "Arquitectura no informada"
            : snapshot.scenario.architecture;
        string learning = "Q-learning " +
            (snapshot.scenario.q_learning ? "activo" : "inactivo");
        string scenarioLine = density + " obstáculos · semilla " +
            snapshot.scenario.seed + " · " + architecture + " · " + learning;
        string startMode = !string.IsNullOrEmpty(snapshot.scenario.start_mode)
            ? snapshot.scenario.start_mode
            : snapshot.scenario.initial_positions;
        if (!string.IsNullOrEmpty(startMode))
            scenarioLine += " · inicios " + startMode;
        GUI.Label(new Rect(x + 12f, y + 29f, width - 24f, 22f),
            scenarioLine, bodyStyle);
    }

    private void DrawField(FarmSnapshotData snapshot, float x, float y, float width)
    {
        if (snapshot.field == null)
            return;

        int obstacleCount = snapshot.field.obstacles != null
            ? snapshot.field.obstacles.Length
            : 0;
        string fieldLine = "Campo " + snapshot.field.rows + " × " +
            snapshot.field.columns + " · obstáculos " + obstacleCount +
            " · celdas sembrables " + snapshot.field.sowable_count;
        GUI.Label(new Rect(x, y, width, 28f), fieldLine, bodyStyle);
    }

    private void DrawTractor(FarmTractorData tractor, Rect rect)
    {
        GUI.Box(rect, GUIContent.none, cardStyle);
        string mode = string.IsNullOrEmpty(tractor.mode) ? "Estado no informado" : tractor.mode;
        string heading = "T" + tractor.id + "  ·  " + mode;
        string direction = string.IsNullOrEmpty(tractor.direction)
            ? string.Empty
            : " · dirección " + tractor.direction;
        GUI.Label(new Rect(rect.x + 12f, rect.y + 5f, rect.width - 24f, 21f),
            heading, headingStyle);
        string values = "Fila " + tractor.row + ", columna " + tractor.col + direction +
            "    ·    semillas " + tractor.seeds.ToString("F0") +
            "    ·    combustible " + tractor.fuel.ToString("F1");
        GUI.Label(new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, 20f),
            values, secondaryStyle);
    }

    private void DrawExtraMetrics(FarmSnapshotData snapshot, float x, float y, float width)
    {
        if (snapshot.metrics == null)
            return;

        GUI.Box(new Rect(x, y, width, 42f), GUIContent.none, cardStyle);
        GUI.Label(new Rect(x + 12f, y + 10f, width - 24f, 22f),
            "Colisiones " + snapshot.metrics.collisions +
            "    ·    acciones rechazadas " + snapshot.metrics.rejected_actions,
            bodyStyle);
    }

    private void DrawMetricCard(Rect rect, string label, string value, FarmMetricsData metrics)
    {
        GUI.Box(rect, GUIContent.none, cardStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 18f),
            label, secondaryStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 25f, rect.width - 24f, 32f),
            value, valueStyle);

        if (metrics != null && label == "COBERTURA")
        {
            float ratio = Mathf.Clamp01(metrics.coverage_pct / 100f);
            Rect track = new Rect(rect.x + 12f, rect.y + rect.height - 7f,
                rect.width - 24f, 3f);
            GUI.DrawTexture(track, coverageTrack);
            GUI.DrawTexture(new Rect(track.x, track.y, track.width * ratio, track.height),
                coverageFill);
        }
    }

    private void DrawPlayback(float x, float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 22f), "REPRODUCCIÓN", headingStyle);
        bool canAdvance = !bridge.IsFinished;
        GUI.enabled = canAdvance;
        float buttonWidth = (width - 12f) / 3f;
        if (GUI.Button(new Rect(x, y + 26f, buttonWidth, 34f),
                bridge.IsAutoAdvancing ? "Pausar" : "Reanudar",
                bridge.IsAutoAdvancing ? buttonStyle : selectedButtonStyle))
        {
            if (bridge.IsAutoAdvancing)
                bridge.PausePlayback();
            else
                bridge.ResumePlayback();
        }

        GUI.enabled = canAdvance && !bridge.IsAutoAdvancing;
        if (GUI.Button(new Rect(x + buttonWidth + 6f, y + 26f, buttonWidth, 34f),
                "Avanzar un tick", buttonStyle))
            bridge.RequestSingleTick();

        GUI.enabled = true;
        if (GUI.Button(new Rect(x + 2f * (buttonWidth + 6f), y + 26f, buttonWidth, 34f),
                "Reiniciar", primaryButtonStyle))
        {
            RestartCurrentEpisode(bridge.CurrentSnapshot);
        }
        GUI.enabled = true;

        float speed = Mathf.Clamp(bridge.TicksPerSecond, MinTicksPerSecond, MaxTicksPerSecond);
        GUI.Label(new Rect(x, y + 68f, width, 20f),
            "Velocidad visual · " + speed.ToString("F2") + " ticks/s",
            bodyStyle);
        GUI.Label(new Rect(x, y + 91f, 48f, 18f), "Lenta", secondaryStyle);
        GUI.Label(new Rect(x + width - 58f, y + 91f, 58f, 18f),
            "Rápida", secondaryStyle);
        float adjustedSpeed = GUI.HorizontalSlider(
            new Rect(x + 52f, y + 94f, width - 108f, 18f),
            speed, MinTicksPerSecond, MaxTicksPerSecond);
        if (!Mathf.Approximately(speed, adjustedSpeed))
            bridge.SetTicksPerSecond(adjustedSpeed);
    }

    private void RestartCurrentEpisode(FarmSnapshotData snapshot)
    {
        FarmScenarioData scenario = snapshot != null ? snapshot.scenario : null;
        if (scenario == null)
        {
            bridge.StartConfiguredEpisode(
                obstacleDensities[selectedDensityIndex],
                int.TryParse(seedInput, out int selectedSeed) ? selectedSeed : 0,
                randomInitialPositions ? "random" : "fixed",
                qLearningEnabled,
                "improved",
                true);
            return;
        }

        float density = scenario.density > 0f
            ? scenario.density
            : scenario.obstacle_density;
        string startMode = !string.IsNullOrEmpty(scenario.start_mode)
            ? scenario.start_mode
            : scenario.initial_positions;
        if (string.IsNullOrEmpty(startMode))
            startMode = "fixed";

        string architecture = string.IsNullOrEmpty(scenario.architecture)
            ? "improved"
            : scenario.architecture;
        showingConfiguration = false;
        bridge.StartConfiguredEpisode(
            density,
            scenario.seed,
            startMode,
            scenario.q_learning,
            architecture,
            true);
    }

    private void EnsureStyles()
    {
        if (stylesReady)
            return;

        panelStyle = CreateBoxStyle(new Color(0.045f, 0.085f, 0.065f, 0.97f), 18, 10);
        cardStyle = CreateBoxStyle(new Color(0.075f, 0.13f, 0.10f, 0.98f), 12, 6);
        titleStyle = CreateLabelStyle(23, FontStyle.Bold, new Color(0.96f, 0.95f, 0.87f));
        headingStyle = CreateLabelStyle(15, FontStyle.Bold, new Color(0.82f, 0.88f, 0.80f));
        bodyStyle = CreateLabelStyle(14, FontStyle.Normal, new Color(0.93f, 0.94f, 0.88f));
        secondaryStyle = CreateLabelStyle(13, FontStyle.Normal, new Color(0.68f, 0.77f, 0.69f));
        valueStyle = CreateLabelStyle(24, FontStyle.Bold, new Color(0.96f, 0.78f, 0.34f));
        buttonStyle = CreateButtonStyle(
            new Color(0.13f, 0.22f, 0.17f),
            new Color(0.19f, 0.31f, 0.23f),
            new Color(0.10f, 0.18f, 0.14f));
        selectedButtonStyle = CreateButtonStyle(
            new Color(0.20f, 0.43f, 0.29f),
            new Color(0.25f, 0.51f, 0.35f),
            new Color(0.15f, 0.34f, 0.22f));
        primaryButtonStyle = CreateButtonStyle(
            new Color(0.67f, 0.43f, 0.13f),
            new Color(0.78f, 0.53f, 0.18f),
            new Color(0.52f, 0.32f, 0.09f));
        coverageTrack = MakeTexture(new Color(0.18f, 0.25f, 0.19f));
        coverageFill = MakeTexture(new Color(0.31f, 0.70f, 0.43f));
        stylesReady = true;
    }

    private static GUIStyle CreateBoxStyle(Color color, int horizontalPadding, int verticalPadding)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(horizontalPadding, horizontalPadding,
                verticalPadding, verticalPadding)
        };
        Texture2D texture = MakeTexture(color);
        style.normal.background = texture;
        style.onNormal.background = texture;
        return style;
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color color)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            normal = { textColor = color },
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };
    }

    private static GUIStyle CreateButtonStyle(Color normalColor, Color hoverColor, Color activeColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(8, 8, 5, 5)
        };
        style.normal.textColor = new Color(0.95f, 0.95f, 0.88f);
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Color.white;
        style.normal.background = MakeTexture(normalColor);
        style.hover.background = MakeTexture(hoverColor);
        style.active.background = MakeTexture(activeColor);
        style.focused.background = MakeTexture(hoverColor);
        style.onNormal.background = style.normal.background;
        style.onHover.background = style.hover.background;
        style.onActive.background = style.active.background;
        style.onFocused.background = style.focused.background;
        return style;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
