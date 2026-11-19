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
            {
                Debug.LogError("No se recibieron datos de tractores");
                yield break;
            }

            foreach (DatosTractor datos in estado.tractores)
            {
                Transform tractor = null;

                if (datos.id == 1) tractor = tractor1;
                if (datos.id == 2) tractor = tractor2;
                if (datos.id == 3) tractor = tractor3;

                if (tractor != null)
                {
                    tractor.position = new Vector3(
                        datos.x,
                        tractor.position.y,
                        datos.z
                    );

                    Debug.Log(
                        $"Tractor {datos.id}: X={datos.x}, Z={datos.z}"
                    );
                }
            }
        }
    }
}
