using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class DatosTractor
{
    public int id;
    public int x;
    public int z;
}

[Serializable]
public class EstadoTractores
{
    public DatosTractor[] tractores;
}

public class ComunicacionPython : MonoBehaviour
{
    public Transform tractor1;
    public Transform tractor2;
    public Transform tractor3;

    public float duracionMovimiento = 0.8f;

    private Vector3[] destinos = new Vector3[3];
    private bool[] inicializados = new bool[3];
    private Coroutine[] movimientos = new Coroutine[3];

    void Start()
    {
        StartCoroutine(ActualizarPosiciones());
    }

    IEnumerator ActualizarPosiciones()
    {
        while (true)
        {
            yield return StartCoroutine(ObtenerDatos());
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator ObtenerDatos()
    {
        using (UnityWebRequest request =
            UnityWebRequest.Get("http://127.0.0.1:8000/estado"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            EstadoTractores estado =
                JsonUtility.FromJson<EstadoTractores>(
                    request.downloadHandler.text
                );

            if (estado == null || estado.tractores == null)
                yield break;

            foreach (DatosTractor datos in estado.tractores)
            {
                Transform tractor = null;

                if (datos.id == 1) tractor = tractor1;
                if (datos.id == 2) tractor = tractor2;
                if (datos.id == 3) tractor = tractor3;

                if (tractor == null)
                    continue;

                int indice = datos.id - 1;

                Vector3 nuevoDestino = new Vector3(
                    datos.x,
                    tractor.position.y,
                    datos.z
                );

                if (!inicializados[indice])
                {
                    tractor.position = nuevoDestino;
                    destinos[indice] = nuevoDestino;
                    inicializados[indice] = true;
                    continue;
                }

                Vector3 cambio = nuevoDestino - destinos[indice];

                if (cambio.sqrMagnitude < 0.001f)
                    continue;

                if (Mathf.Abs(cambio.x) + Mathf.Abs(cambio.z) > 1.01f)
                {
                    Debug.LogWarning("Enviar movimientos de una celda");
                    continue;
                }

                if (GameObject.Find(
                    $"Obstaculo_{datos.z}_{datos.x}") != null)
                {
                    Debug.LogWarning("Destino ocupado por una roca");
                    continue;
                }

                destinos[indice] = nuevoDestino;

                if (movimientos[indice] != null)
                    StopCoroutine(movimientos[indice]);

                movimientos[indice] = StartCoroutine(
                    MoverConCurva(tractor, nuevoDestino)
                );
            }
        }
    }

    IEnumerator MoverConCurva(Transform tractor, Vector3 destino)
    {
        Vector3 inicio = tractor.position;
        Vector3 direccion = destino - inicio;

        Vector3 perpendicular = new Vector3(
            -direccion.z, 0, direccion.x
        ).normalized;

        Vector3 control = (inicio + destino) / 2f
                          + perpendicular * 0.15f;

        float tiempo = 0f;

        while (tiempo < duracionMovimiento)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(duracionMovimiento, 0.01f)
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            tractor.position =
                Mathf.Pow(1f - t, 2) * inicio +
                2f * (1f - t) * t * control +
                Mathf.Pow(t, 2) * destino;

            yield return null;
        }

        tractor.position = destino;
    }
}
