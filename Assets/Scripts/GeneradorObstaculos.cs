using UnityEngine;

public class GeneradorObstaculos : MonoBehaviour
{
    public GameObject prefabObstaculo;
    public TextAsset archivoPosiciones;

    [ContextMenu("Generar obstaculos")]
    public void Generar()
    {
        if (prefabObstaculo == null || archivoPosiciones == null)
        {
            Debug.LogWarning("Falta asignar el Prefab o el archivo de posiciones.");
            return;
        }

        foreach (string linea in archivoPosiciones.text.Split('\n'))
        {
            string[] datos = linea.Trim().Split(',');

            if (datos.Length != 2 ||
                !int.TryParse(datos[0], out int fila) ||
                !int.TryParse(datos[1], out int columna))
                continue;

            if (fila < 0 || fila >= 15 ||
                columna < 0 || columna >= 15)
                continue;

            GameObject obstaculo = Instantiate(
                prefabObstaculo,
                new Vector3(columna, 0.55f, fila),
                Quaternion.identity,
                transform
            );

            obstaculo.name = $"Obstaculo_{fila}_{columna}";
        }
    }
}
