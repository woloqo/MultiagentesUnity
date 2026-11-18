using UnityEngine;

public class MovimientoCurvo : MonoBehaviour
{
    public float duracion = 3f;

    private Vector3 inicio;
    private Vector3 control;
    private Vector3 destino;
    private float tiempo = 0f;

    void Start()
    {
        inicio = transform.position;
        destino = inicio + new Vector3(0, 0, 1);
        control = inicio + new Vector3(0.5f, 0, 0.5f);
    }

    void Update()
    {
        if (tiempo >= duracion)
            return;

        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / duracion);

        transform.position =
            Mathf.Pow(1 - t, 2) * inicio +
            2 * (1 - t) * t * control +
            Mathf.Pow(t, 2) * destino;
    }
}
