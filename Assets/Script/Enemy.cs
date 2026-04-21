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
    protected Coroutine flashCoroutine;
    protected Coroutine deathCoroutine; // 🆕 Tham chiếu Coroutine chết
    protected bool isDying = false;     // 🆕 Trạng thái chết để tránh lỗi
    public static float enemyHealthMultiplier = 1f;
    public static float globalSpeedMultiplier = 1f; // Mặc định là 1x
    public GameObject[] pickupPrefabs;
    // THAM CHIẾU MỚI
    protected LevelManager levelManager;
    protected bool killedByExplosion = false; // Biến tạm để check nguyên nhân chết
    public static int activeShielders = 0;
    public bool isDead { get; private set; } = false; // Biến để LevelManager kiểm tra

    protected virtual void Start()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length <= 1)
        {
            // Chỉ reset khi con quái đầu tiên của scene xuất hiện
            enemyHealthMultiplier = 1f;
            globalSpeedMultiplier = 1f;
            activeShielders = 0;
        }

        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        currentHealth = Mathf.RoundToInt(maxHealth * enemyHealthMultiplier);

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

        float velocityY = verticalMovement * moveSpeed * globalSpeedMultiplier * yTrackingMultiplier;

        Vector2 finalVelocity = new Vector2(-moveSpeed, velocityY);

        if (rb != null)
        {
            rb.linearVelocity = finalVelocity;
        }
    }

    // Phương thức nhận sát thương từ đạn của Player
    public virtual void TakeDamage(int damageAmount, bool isFromExplosion = false)
    {
        if (isDying) return;

        if (activeShielders > 0 && GetType().Name != "ShieldProviderEnemy")
        {
            StartCoroutine(ShieldFlashEffect());
            return;
        }

        currentHealth -= damageAmount;

        // Gán nguyên nhân trúng đòn cuối cùng
        killedByExplosion = isFromExplosion;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Hiệu ứng nháy sáng...
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine(hitFlashCount));
            }
        }
    }

    // Coroutine tạo hiệu ứng nhấp nháy
    protected IEnumerator HitFlashRoutine(int count)
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
        isDead = true; // Xác nhận đã chết ngay lập tức

        if (playerTarget != null)
        {
            Player p = Player.Instance;
            p.AddKill();

            if (p.hasExplosionOnDeath)
            {
                // QUAN TRỌNG: Chết do đạn (false) -> 100% nổ. Chết do nổ (true) -> 10% nổ.
                float explodeChance = killedByExplosion ? 0.1f : 1.0f;

                if (Random.value <= explodeChance)
                {
                    GameObject expObj = Instantiate(p.explosionPrefab, transform.position, Quaternion.identity);
                    SoulExplosion expScript = expObj.GetComponent<SoulExplosion>();
                    if (expScript != null)
                    {
                        // Sát thương nổ = Player DMG x 2
                        expScript.explosionDamage = p.damagePerBullet * 2;
                    }
                }
            }
        }

        // Reset lại biến để con quái sau (nếu dùng Object Pool) không bị dính logic cũ
        killedByExplosion = false;

        if (Random.value <= 0.2f) SpawnRandomPickup();
        if (levelManager != null && !isBoss) levelManager.OnEnemyDefeated(isBoss);
        deathCoroutine = StartCoroutine(DeathRoutine(deathFadeDuration));
        if (!isBoss)
        {
            deathCoroutine = StartCoroutine(DeathRoutine(deathFadeDuration));
        }
        else
        {
            // Boss thì tắt ngay hoặc chạy hiệu ứng mờ nhưng không được để LevelManager kẹt
            StartCoroutine(BossDeathSequence());
        }
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
    protected void SpawnRandomPickup()
    {
        float rand = Random.value;
        int index = 0;

        if (rand <= 0.1f) index = 3; // 10% của 10% rơi ra Shard (Rất hiếm)
        else index = Random.Range(0, 3); // Còn lại là Heal, Explosive, Invincibility

        if (pickupPrefabs.Length > index)
        {
            Instantiate(pickupPrefabs[index], transform.position, Quaternion.identity);
        }
    }
    IEnumerator ShieldFlashEffect()
    {
        spriteRenderer.color = Color.cyan; // Hoặc dùng màu cầu vồng
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
    IEnumerator BossDeathSequence()
    {
        // Chạy hiệu ứng mờ dần (tùy chọn)
        yield return StartCoroutine(DeathRoutine(deathFadeDuration));
        gameObject.SetActive(false); // Tắt Boss
    }
}