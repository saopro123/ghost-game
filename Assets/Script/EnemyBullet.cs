using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Cài Đặt Đạn Kẻ Địch")]
    [Tooltip("Tốc độ bay của viên đạn (đơn vị/giây)")]
    public float speed = 7f; // Giờ đây chỉ là giá trị mặc định, có thể bị ghi đè

    [Tooltip("Sát thương gây ra cho Player (ví dụ: 25 Purification)")]
    public int damage = 25;

    [Tooltip("Thời gian tự hủy")]
    public float lifetime = 4f;

    private Rigidbody2D rb;
    private Vector2 moveDirection; // Hướng bay của đạn

    void Awake() // Dùng Awake thay vì Start để đảm bảo rb được gán trước khi SetDirection có thể được gọi
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Nếu không có hướng cụ thể được thiết lập từ bên ngoài, mặc định bay sang trái
        if (moveDirection == Vector2.zero)
        {
            moveDirection = Vector2.left;
        }
        rb.linearVelocity = moveDirection * speed;

        Destroy(gameObject, lifetime);
    }

    // Phương thức để Boss thiết lập hướng và tốc độ cho đạn
    public void SetDirection(Vector2 direction, float bulletSpeed)
    {
        moveDirection = direction.normalized; // Đảm bảo hướng được chuẩn hóa
        speed = bulletSpeed; // Ghi đè tốc độ mặc định

        // Nếu đã Start rồi, cập nhật vận tốc ngay lập tức
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Kiểm tra Tag: Chỉ tương tác với Player
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                // Gọi hàm TakeDamage() của Player
                player.TakeDamage(damage);
            }

            // 2. Tự hủy đạn sau khi gây sát thương
            Destroy(gameObject);
        }
    }
}