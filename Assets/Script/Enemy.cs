using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Đảm bảo tất cả kẻ địch có các component cần thiết
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    // -- Thuộc tính cơ bản --
    [Header("Base Stats")]
    public int maxHealth = 1;
    [HideInInspector] public int currentHealth;
    public int contactDamage = 10;

    // BIẾN MỚI: Đánh dấu đây là Boss hay Quái thường
    [Tooltip("Đánh dấu đây là Boss. Boss sẽ được LevelManager trao thưởng Gold.")]
    public bool isBoss = false;

    // -- Movement --
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float yTrackingMultiplier = 0.5f;   // Hệ số làm chậm theo dõi Y

    // -- Hiệu Ứng Hình Ảnh --
    [Header("Hiệu Ứng Hình Ảnh")]
    public SpriteRenderer spriteRenderer;
    public Color hitFlashColor1 = Color.white;  // Màu nháy 1
    public Color hitFlashColor2 = Color.yellow; // Màu nháy 2
    public float hitFlashDuration = 0.05f;      // Thời gian của mỗi lần nháy
    public int hitFlashCount = 1;               // Số lần nháy
    public float deathFadeDuration = 1.0f;       // 🆕 Thời gian mờ dần khi chết

    protected Rigidbody2D rb;
    protected Transform playerTarget;
    private Coroutine flashCoroutine;
    private Coroutine deathCoroutine; // 🆕 Tham chiếu Coroutine chết
    private bool isDying = false;     // 🆕 Trạng thái chết để tránh lỗi

    // THAM CHIẾU MỚI
    protected LevelManager levelManager;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        currentHealth = maxHealth;

        // Tìm LevelManager và Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        levelManager = FindAnyObjectByType<LevelManager>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tự hủy sau 8s nếu bay khỏi màn hình
        if (!isBoss)
        {
            Destroy(gameObject, 8f);
        }
    }

    protected virtual void FixedUpdate()
    {
        // 🆕 Không di chuyển nếu đang chết
        if (!isDying)
        {
            Move();
        }
    }

    // Phương thức di chuyển: Luôn đi sang trái, cố gắng theo dõi vị trí Y của Player
    protected virtual void Move()
    {
        if (playerTarget == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.left * moveSpeed;
            return;
        }

        float directionY = playerTarget.position.y - transform.position.y;
        float verticalMovement = Mathf.Sign(directionY);

        float velocityY = verticalMovement * moveSpeed * yTrackingMultiplier;

        Vector2 finalVelocity = new Vector2(-moveSpeed, velocityY);

        if (rb != null)
        {
            rb.linearVelocity = finalVelocity;
        }
    }

    // Phương thức nhận sát thương từ đạn của Player
    public virtual void TakeDamage(int damageAmount)
    {
        if (isDying) return; // Không nhận sát thương nếu đang chết

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (spriteRenderer != null)
            {
                // DỪNG COROUTINE CŨ VÀ KHỞI ĐỘNG CÁI MỚI
                if (flashCoroutine != null)
                {
                    StopCoroutine(flashCoroutine);
                }
                flashCoroutine = StartCoroutine(HitFlashRoutine(hitFlashCount));
            }
        }
    }

    // Coroutine tạo hiệu ứng nhấp nháy
    IEnumerator HitFlashRoutine(int count)
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < count; i++)
        {
            spriteRenderer.color = hitFlashColor2;
            yield return new WaitForSeconds(hitFlashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(hitFlashDuration);
        }

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    // Xử lý va chạm vật lý với Player 
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(contactDamage);
            }
            // Kẻ địch thông thường tự hủy khi chạm Player
            if (!isBoss)
            {
                Die();
            }
        }
    }

    // --- LOGIC CHẾT ĐÃ CẬP NHẬT ---
    protected virtual void Die()
    {
        if (isDying) return;
        isDying = true;

        // 1. GỌI HÀM CẤP GOLD CHO QUÁI THƯỜNG
        if (levelManager != null && !isBoss)
        {
            Debug.Log($"[GOLD CHECK] Regular enemy died. isBoss: {isBoss}. Gold Reward: 1");
            levelManager.OnEnemyDefeated(isBoss); // isBoss = False
        }

        // 2. Kích hoạt hiệu ứng mờ dần
        deathCoroutine = StartCoroutine(DeathRoutine(deathFadeDuration));
    }

    // 🆕 Coroutine Chết (Fade Out)
    IEnumerator DeathRoutine(float duration)
    {
        // A. Vô hiệu hóa vật lý và tương tác ngay lập tức
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            // ✅ SỬA LỖI TẠI ĐÂY: Dùng bodyType thay cho isKinematic
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Tắt tất cả các collider
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // B. Hiệu ứng mờ dần (Fade Out)
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
        }

        // C. Hủy đối tượng/component cuối cùng
        if (!isBoss)
        {
            // Quái thường: Hủy toàn bộ GameObject
            Destroy(gameObject);
        }
        else
        {
            // Boss: Hủy component Enemy, báo hiệu cho LevelManager
            Destroy(this);
            // LevelManager sẽ chịu trách nhiệm hủy GameObject Boss sau khi sự kiện kết thúc.
        }
    }
}