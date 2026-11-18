using UnityEngine;

public class ControlSiembra : MonoBehaviour
{
    public Material materialSembrado;

    public void SembrarCelda(int fila, int columna)
    {
        string nombre = $"Celda_{fila}_{columna}";

        Transform celda = transform.Find(nombre);

        if (celda != null)
        {
            Renderer rendererCelda = celda.GetComponent<Renderer>();

            rendererCelda.sharedMaterial = materialSembrado;
        }
    }

    [ContextMenu("Probar siembra")]
    public void ProbarSiembra()
    {
        SembrarCelda(7, 7);
    }
}
