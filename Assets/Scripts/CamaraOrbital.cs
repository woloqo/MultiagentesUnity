using UnityEngine;

public class CamaraOrbital : MonoBehaviour
{
    public float velocidad = 10f;

    private Vector3 centro = new Vector3(7, 0, 7);

    void Start()
    {
        transform.LookAt(centro);
    }

    void Update()
    {
        transform.RotateAround(
            centro,
            Vector3.up,
            velocidad * Time.deltaTime
        );

        transform.LookAt(centro);
    }
}
