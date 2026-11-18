using UnityEngine;

public class GeneradorTerreno : MonoBehaviour
{
    [ContextMenu("Crear cuadricula 15x15")]
    public void CrearCuadricula()
    {
        for (int fila = 0; fila < 15; fila++)
        {
            for (int columna = 0; columna < 15; columna++)
            {
                GameObject celda =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

                celda.name = $"Celda_{fila}_{columna}";
                celda.transform.SetParent(transform, false);

                celda.transform.localPosition =
                    new Vector3(columna, 0.025f, fila);

                celda.transform.localScale =
                    new Vector3(0.96f, 0.05f, 0.96f);
            }
        }
    }
}
