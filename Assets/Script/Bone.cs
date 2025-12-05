using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bone : MonoBehaviour
{
    private bool isYellowBone; // True: Vàng (Damage khi Moving), False: Hồng (Damage khi Idle)
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Header("== Cài Đặt Hình Ảnh & Sát Thương ==")]
    public Color yellowColor = Color.yellow;
    public Color pinkColor = Color.magenta;
    public int damageAmount = 30;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); // Lấy SpriteRenderer

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Đảm bảo SpriteRenderer được gán (có thể bạn sẽ gán từ Inspector)
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
    }

    // Hàm được gọi từ SkeletonBoss để khởi tạo
    public void Initialize(bool yellowBone, float speed)
    {
        isYellowBone = yellowBone;

        // **LOGIC ĐỔI MÀU Ở ĐÂY**
        if (sr != null)
        {
            sr.color = isYellowBone ? yellowColor : pinkColor;
        }
        else
        {
            Debug.LogWarning("Bone has no SpriteRenderer component to change color on!", this);
        }

        // Bắt đầu di chuyển sang trái
        if (rb != null)
        {
            rb.linearVelocity = Vector2.left * speed;
        }

        // Tự hủy sau 8 giây
        Destroy(gameObject, 8f);
    }

    // Logic gây sát thương (Yêu cầu Collider phải là Is Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                bool playerIsMoving = player.IsMoving;

                bool shouldDamage = false;

                if (isYellowBone)
                {
                    // Xương Vàng (Yellow): Gây DMG khi Player DI CHUYỂN
                    if (playerIsMoving)
                    {
                        shouldDamage = true;
                    }
                }
                else // Xương Hồng (Pink)
                {
                    // Xương Hồng (Pink): Gây DMG khi Player ĐỨNG YÊN
                    if (!playerIsMoving)
                    {
                        shouldDamage = true;
                    }
                }

                if (shouldDamage)
                {
                    player.TakeDamage(damageAmount);
                }

                // Cục xương tự hủy sau khi chạm Player
                Destroy(gameObject);
            }
        }
    }
}