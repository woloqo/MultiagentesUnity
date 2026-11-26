using UnityEngine;

public class MovimientoAmbiente : MonoBehaviour
{
    public bool giroContinuo;
    public float velocidad = 18f;
    public float amplitud = 4f;
    private Quaternion inicial;
    private float fase;
    void Awake() { inicial = transform.localRotation; fase = transform.position.x * 0.37f + transform.position.z * 0.19f; }
    void Update()
    {
        if (giroContinuo) transform.Rotate(Vector3.forward, velocidad * Time.deltaTime, Space.Self);
        else transform.localRotation = inicial * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.2f + fase) * amplitud);
    }
}
