using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal"); // A/D hoặc mũi tên
        float v = Input.GetAxis("Vertical");   // W/S hoặc mũi tên

        transform.Translate(h * moveSpeed * Time.deltaTime,
                            v * moveSpeed * Time.deltaTime, 0);
    }
}