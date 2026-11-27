using UnityEngine;

// Camera views only: simulation state and tractor decisions remain in Python.
public class FarmCameraViews : MonoBehaviour
{
    public Vector3 fieldCenter = new Vector3(7f, 0f, 7f);
    public float overheadHeight = 24f;
    public float overheadSize = 9f;
    public Vector3 cabOffset = new Vector3(-2.5f, 1.5f, 0f);
    public float cabFieldOfView = 60f;
    public float orbitalFieldOfView = 43f;

    private enum View { Orbital, Panoramica, Tractor1, Tractor2, Tractor3 }
    private View view = View.Orbital;
    private Camera cameraComponent;
    private CamaraOrbital orbital;
    private FarmSimulationBridge bridge;
    private Vector3 orbitalPosition;
    private Quaternion orbitalRotation;
    private float orbitalFov;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        orbital = GetComponent<CamaraOrbital>();
        bridge = FindAnyObjectByType<FarmSimulationBridge>();
        orbitalPosition = transform.position;
        orbitalRotation = transform.rotation;
        orbitalFov = orbitalFieldOfView;
        cameraComponent.fieldOfView = orbitalFov;
    }

    private void LateUpdate()
    {
        if (view == View.Orbital) return;
        if (view == View.Panoramica)
        {
            transform.position = fieldCenter + Vector3.up * overheadHeight;
            transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            return;
        }
        if (bridge == null) bridge = FindAnyObjectByType<FarmSimulationBridge>();
        if (bridge == null) return;
        Transform tractor = view == View.Tractor1 ? bridge.tractor1 : view == View.Tractor2 ? bridge.tractor2 : bridge.tractor3;
        if (tractor == null) return;
        transform.position = tractor.position + tractor.rotation * cabOffset;
        Vector3 target = tractor.position + tractor.right * 0.7f + Vector3.up * 0.35f;
        transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
    }

    private void Select(View selected)
    {
        view = selected;
        if (orbital != null) orbital.enabled = selected == View.Orbital;
        cameraComponent.orthographic = selected == View.Panoramica;
        if (selected == View.Panoramica) cameraComponent.orthographicSize = overheadSize;
        else cameraComponent.fieldOfView = selected == View.Orbital ? orbitalFov : cabFieldOfView;
        if (selected == View.Orbital)
        {
            transform.position = orbitalPosition;
            transform.rotation = orbitalRotation;
        }
    }

    private void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 1f, 2.5f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        const float width = 210f;
        float x = Mathf.Max(0f, Screen.width / scale - width - 12f);
        var title = new GUIStyle(GUI.skin.box) { fontSize = 17, fontStyle = FontStyle.Bold };
        var button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        var label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        GUI.Box(new Rect(x, 12f, width, 198f), "Vista de cámara", title);
        if (GUI.Button(new Rect(x + 10f, 42f, 190f, 30f), "Orbital", button)) Select(View.Orbital);
        if (GUI.Button(new Rect(x + 10f, 77f, 190f, 30f), "Panorámica", button)) Select(View.Panoramica);
        if (GUI.Button(new Rect(x + 10f, 112f, 60f, 30f), "T1", button)) Select(View.Tractor1);
        if (GUI.Button(new Rect(x + 75f, 112f, 60f, 30f), "T2", button)) Select(View.Tractor2);
        if (GUI.Button(new Rect(x + 140f, 112f, 60f, 30f), "T3", button)) Select(View.Tractor3);
        GUI.Label(new Rect(x + 10f, 155f, 190f, 30f), "Actual: " + view, label);
        GUI.matrix = previousMatrix;
    }
}
