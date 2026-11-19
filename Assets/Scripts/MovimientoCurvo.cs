using System.Collections;
using UnityEngine;

public class MovimientoCurvo : MonoBehaviour
{
    public float duracionGiro = 1f;
    public float duracionMovimiento = 3f;

    void Start()
    {
        StartCoroutine(GirarYAvanzar());
    }

    IEnumerator GirarYAvanzar()
    {
        Quaternion rotacionInicial = transform.rotation;
        Quaternion rotacionFinal = Quaternion.Euler(0, 270, 0);

        float tiempo = 0f;

        // Primero gira hacia la derecha
        while (tiempo < duracionGiro)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracionGiro);

            transform.rotation = Quaternion.Slerp(
                rotacionInicial,
                rotacionFinal,
                t
            );

            yield return null;
        }

        transform.rotation = rotacionFinal;

        // Despues avanza hacia la derecha
        Vector3 inicio = transform.position;
        Vector3 destino = inicio + new Vector3(1, 0, 0);

        Vector3 control = (inicio + destino) / 2f
                          + new Vector3(0, 0, 0.15f);

        tiempo = 0f;

        while (tiempo < duracionMovimiento)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / duracionMovimiento
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position =
                Mathf.Pow(1f - t, 2) * inicio +
                2f * (1f - t) * t * control +
                Mathf.Pow(t, 2) * destino;

            yield return null;
        }

        transform.position = destino;
    }
}
