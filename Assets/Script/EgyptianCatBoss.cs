using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EgyptianCatBoss : Enemy
{
    [Header("== BOSS PHASE CONTROL ==")]
    public GameObject minionPrefab;
    // Chắc chắn mảng này có đủ 4 phần tử
    public Transform[] minionSpawnPoints;

    [Tooltip("Khoảng thời gian (giây) giữa lần tấn công của mỗi Minion.")]
    public float activationDelay = 2f;

    [Tooltip("Thời gian (giây) Boss dễ bị tấn công sau khi 4 Minion bị hạ.")]
    public float vulnerableTime = 10f;

    [Header("== Boss Visuals/Feedback ==")]
    public Color vulnerableColor = Color.white;
    private Color originalColor;


    private List<VengefulCatMinion> activeMinions = new List<VengefulCatMinion>();
    private bool isVulnerable = false;
    private float vulnerableTimer;

    // >> BIẾN CHO QUẢN LÝ LƯỢT ĐÁNH <<
    private int currentMinionIndex = -1;
    private bool isAttackRotating = false;
    // -------------------------------------


    protected override void Start()
    {
        // 1. Khởi tạo các biến được thừa kế
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        // 🆕 AN TOÀN HƠN: Lưu màu gốc ngay lập tức
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        // 2. Thiết lập ràng buộc
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Bắt đầu trận đấu bằng trạng thái Bảo vệ
        StartGuardPhase();
    }

    protected override void FixedUpdate()
    {
        if (isVulnerable)
        {
            vulnerableTimer -= Time.fixedDeltaTime;
            if (vulnerableTimer <= 0)
            {
                StartGuardPhase();
            }
        }
    }

    protected override void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ==========================================================
    // --- PHASE CONTROL ---
    // ==========================================================

    void StartGuardPhase()
    {
        Debug.Log("Boss: Kích hoạt Guard Phase. Summoning Minions...");
        isVulnerable = false;

        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        StopAllCoroutines();

        // Bắt đầu Summon
        StartCoroutine(SummonMinionsRoutine());
    }

    void StartVulnerablePhase()
    {
        Debug.Log("Boss: Kích hoạt Vulnerable Phase. Tấn công đi!");
        isVulnerable = true;
        vulnerableTimer = vulnerableTime;

        if (spriteRenderer != null) spriteRenderer.color = vulnerableColor;
    }

    IEnumerator SummonMinionsRoutine()
    {
        if (minionSpawnPoints.Length < 4)
        {
            Debug.LogError("Cần ít nhất 4 điểm spawn cho Minion!");
            yield break;
        }

        activeMinions.Clear();

        // 1. Summon 4 Minions và gán loại
        for (int i = 0; i < 4; i++)
        {
            if (i >= minionSpawnPoints.Length) break;

            GameObject minionObj = Instantiate(minionPrefab, minionSpawnPoints[i].position, Quaternion.identity);
            VengefulCatMinion minion = minionObj.GetComponent<VengefulCatMinion>();

            if (minion != null)
            {
                // Gán tham chiếu Boss
                minion.BossController = this;

                // Gán loại Minion dựa trên index (đảm bảo gán từ 0-3 tương ứng với Enum)
                minion.InitializeMinion((MinionType)i);
                activeMinions.Add(minion);
            }
            yield return null;
        }

        // 2. Bắt đầu luân phiên tấn công
        StartAttackRotation();
    }

    void StartAttackRotation()
    {
        // 🆕 TÍNH AN TOÀN: Dọn dẹp danh sách trước khi bắt đầu
        activeMinions.RemoveAll(minion => minion == null);
        if (activeMinions.Count == 0)
        {
            StartVulnerablePhase(); // Nếu không có Minion nào, chuyển sang dễ tổn thương luôn
            return;
        }

        isAttackRotating = true;
        currentMinionIndex = 0;

        // Bắt đầu luân phiên
        StartCoroutine(AttackRotationRoutine());
    }

    // Hàm Minion gọi khi tấn công xong (để chuyển lượt)
    public void MinionFinishedAttack()
    {
        if (!isAttackRotating) return;

        // Tăng index
        currentMinionIndex++;

        // Quay lại Minion đầu tiên sau khi hết lượt
        if (currentMinionIndex >= activeMinions.Count)
        {
            currentMinionIndex = 0;
        }

        // Kích hoạt Minion tiếp theo sau delay (bắt đầu Coroutine mới)
        StartCoroutine(AttackRotationRoutine());
    }

    IEnumerator AttackRotationRoutine()
    {
        // Delay giữa các lần tấn công
        yield return new WaitForSeconds(activationDelay);

        // 🆕 TÍNH AN TOÀN: Đảm bảo index nằm trong phạm vi. Nếu không, DỌN DẸP và BẮT ĐẦU LẠI
        if (currentMinionIndex < 0 || currentMinionIndex >= activeMinions.Count)
        {
            CheckMinionStatus();
            yield break;
        }

        VengefulCatMinion nextMinion = activeMinions[currentMinionIndex];

        if (nextMinion != null)
        {
            // Kích hoạt Minion để nó tấn công 1 lần
            nextMinion.SetAttackActive(true);
        }
        else
        {
            // Nếu Minion đã chết (vị trí null), dọn dẹp và chuyển sang Minion tiếp theo
            Debug.Log($"Minion tại vị trí {currentMinionIndex} đã chết. Chuyển lượt.");
            CheckMinionStatus();
        }
    }

    // 🆕 HÀM ĐƯỢC TỐI ƯU HÓA: Chỉ dùng để xử lý cái chết của Minion
    public void CheckMinionStatus()
    {
        // 1. Dọn dẹp danh sách Minion để chỉ giữ lại những con còn sống
        activeMinions.RemoveAll(minion => minion == null);

        if (activeMinions.Count == 0 && !isVulnerable)
        {
            // 2. TẤT CẢ CHẾT: Ngừng luân phiên và chuyển sang dễ tổn thương
            isAttackRotating = false;
            StopAllCoroutines();
            StartVulnerablePhase();
        }
        else if (activeMinions.Count > 0 && isAttackRotating)
        {
            // 3. CHẾT GIỮA CHU KỲ: Bắt đầu lại rotation để đảm bảo index đúng
            StopAllCoroutines();
            StartAttackRotation();
        }
    }

    // ==========================================================
    // --- DAMAGE CONTROL (QUAN TRỌNG) ---
    // ==========================================================

    public override void TakeDamage(int damageAmount)
    {
        // 🆕 Gán Feedback khi bị tấn công trong trạng thái dễ tổn thương
        if (isVulnerable)
        {
            // Ví dụ: Nháy sáng khi bị tấn công
            StartCoroutine(FlashDamage(Color.red, 0.1f));
            base.TakeDamage(damageAmount);
        }
        // else: Boss không nhận sát thương
    }

    // 🆕 HÀM MỚI: Feedback hình ảnh khi nhận sát thương
    IEnumerator FlashDamage(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;

        Color baseColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);

        // Trở lại màu bình thường (vulnerableColor hoặc originalColor)
        spriteRenderer.color = isVulnerable ? vulnerableColor : originalColor;
    }


    protected override void Die()
    {
        Debug.Log("Egyptian Cat Boss Defeated!");
        StopAllCoroutines();

        // Dọn dẹp tất cả Minion còn sót lại
        foreach (var minion in activeMinions)
        {
            if (minion != null)
            {
                Destroy(minion.gameObject);
            }
        }

        base.Die();
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        // Giữ nguyên: Boss không gây sát thương khi va chạm
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null && contactDamage > 0)
            {
                player.TakeDamage(contactDamage);
            }
        }
    }
}