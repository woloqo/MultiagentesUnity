using UnityEngine;

public class CamaraOrbital : MonoBehaviour
{
    public float velocidad = 4.5f;

    private Vector3 centro = new Vector3(8f, 0f, 9.5f);

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
