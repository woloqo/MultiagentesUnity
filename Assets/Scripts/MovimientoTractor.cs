using UnityEngine;

public class MovimientoTractor : MonoBehaviour
{
    public float velocidad = 1.5f;
    public int cantidadPasos = 3;
    public ControlSiembra controlSiembra;

    private Vector3 destino;
    private int pasosRealizados = 0;

    void Start()
    {
        SiguienteDestino();
    }

    void Update()
    {
        if (pasosRealizados >= cantidadPasos)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destino,
            velocidad * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, destino) < 0.01f)
        {
            int fila = Mathf.RoundToInt(destino.z);
            int columna = Mathf.RoundToInt(destino.x);

            if (controlSiembra != null)
            {
                controlSiembra.SembrarCelda(fila, columna);
            }

            pasosRealizados++;

            if (pasosRealizados < cantidadPasos)
            {
                SiguienteDestino();
            }
        }
    }

    void SiguienteDestino()
    {
        Vector3 siguiente = transform.position + new Vector3(0, 0, 1);

        int fila = Mathf.RoundToInt(siguiente.z);
        int columna = Mathf.RoundToInt(siguiente.x);

        GameObject obstaculo = GameObject.Find(
            $"Obstaculo_{fila}_{columna}"
        );

        if (obstaculo != null)
        {
            Debug.Log("Tractor detenido por obstaculo");
            pasosRealizados = cantidadPasos;
            return;
        }

        destino = siguiente;
    }
}
