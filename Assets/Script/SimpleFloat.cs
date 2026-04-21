using UnityEngine;

public class SimpleFloat : MonoBehaviour
{
    public float speed = 2f;
    public float amount = 0.5f;
    private Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        // Tự động bay lên xuống nhẹ nhàng theo hình sin
        transform.position = startPos + new Vector3(0, Mathf.Sin(Time.unscaledTime * speed) * amount, 0);
    }
}