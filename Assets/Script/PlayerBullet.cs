using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerBullet : MonoBehaviour
{
    [Header("Cài Đặt Đạn")]
    [Tooltip("Tốc độ bay của viên đạn")]
    public float speed = 10f;
    [Tooltip("Sát thương gây ra cho kẻ địch")]
    public int damage = 1;
    [Tooltip("Thời gian tự hủy")]
    public float lifetime = 3f;

    [Header("Hiệu Ứng Hình Ảnh")]
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.yellow;
    public float flashDuration = 0.05f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Thiết lập vận tốc ban đầu (bay sang phải)
        if (rb != null)
        {
            rb.linearVelocity = Vector2.right * speed;
        }

        Destroy(gameObject, lifetime);

        if (spriteRenderer != null)
        {
            StartCoroutine(BulletFlashRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra Tag: Đạn chỉ tương tác với vật thể có Tag "Enemy"
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null)
            {
                // Gọi hàm TakeDamage() của Enemy (hoặc Boss)
                enemy.TakeDamage(damage);
            }

            // Tự hủy đạn sau khi va chạm
            Destroy(gameObject);
        }
    }

    // Coroutine tạo hiệu ứng nhấp nháy liên tục
    IEnumerator BulletFlashRoutine()
    {
        Color originalColor = spriteRenderer.color;

        while (true)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }
}