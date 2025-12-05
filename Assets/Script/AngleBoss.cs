using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AngelBoss : Enemy
{
    [Header("== BOSS ATTACK SETTINGS ==")]

    // --- Đòn 1 & 2: Bắn Đạn ---
    [Tooltip("Prefab đạn của Boss.")]
    public GameObject bossBulletPrefab;
    [Tooltip("Tốc độ đạn Boss.")]
    public float bossBulletSpeed = 7f;

    [Tooltip("Thời gian hồi chiêu giữa các phát bắn thường (giây).")]
    public float baseAttackRate = 1.2f;

    [Tooltip("Số lượng viên đạn trong 1 chùm (hình quạt).")]
    public int fanBulletCount = 7;
    [Tooltip("Tổng góc lan rộng của chùm đạn (độ).")]
    public float fanSpreadAngle = 60f;
    [Tooltip("Thời gian hồi chiêu của đòn Fan Shot.")]
    public float fanAttackRate = 4.5f;
    private float fanAttackTimer;

    // --- Đòn 3: Beam NGA NGẪU NHIÊN ---
    [Tooltip("Chiều dài tối đa của Beam (đơn vị Unity).")]
    public float beamLength = 20f;
    [Tooltip("Khoảng Y tối thiểu và tối đa Beam có thể xuất hiện.")]
    public Vector2 beamYRange = new Vector2(-5f, 5f);
    [Tooltip("Số lượng Beam bắn ra cùng lúc.")]
    public int numRandomBeams = 3;
    [Tooltip("Thời gian chờ sau cảnh báo trước khi bắn Beam (giây).")]
    public float beamWindupTime = 1.0f;
    [Tooltip("Thời gian Beam gây sát thương tồn tại (giây).")]
    public float beamDuration = 2.0f;
    [Tooltip("Thời gian hồi chiêu của đòn Beam.")]
    public float beamCooldownTime = 15.0f;
    [Tooltip("Sát thương gây ra bởi Beam.")]
    public int beamDamage = 100;

    [Header("== Cài Đặt Hiển Thị Beam ==")]
    [Tooltip("Material dùng cho Beam (Additive/Unlit).")]
    public Material beamMaterial;
    [Tooltip("Thời gian Beam TĂNG độ rộng (làm Beam mượt mà hơn).")]
    public float beamGrowTime = 0.15f;
    [Tooltip("Độ rộng tối đa của Beam Cảnh báo (Đỏ).")]
    public float maxWarningWidth = 0.4f;
    [Tooltip("Độ rộng tối đa của Beam Gây Sát Thương (Vàng).")]
    public float maxDamageWidth = 0.6f;
    [Tooltip("Độ mờ của Beam Cảnh báo (0.0 là tàng hình, 1.0 là đầy đủ).")]
    [Range(0f, 1f)]
    public float warningBeamOpacity = 0.3f;
    [Tooltip("Sorting Layer cho các hiệu ứng Beam (ví dụ: FX_OVERLAY).")]
    public string beamSortingLayerName = "FX_OVERLAY";
    [Tooltip("Thứ tự hiển thị (càng cao càng nổi).")]
    public int beamSortingOrder = 10;

    [Header("== Cài Đặt Raycast ==")]
    [Tooltip("Layer mà Beam có thể gây sát thương (thường là Player).")]
    public LayerMask playerLayer;

    // Các tham chiếu cho Raycast Visuals
    private LineRenderer[] warningLineRenderers;
    private LineRenderer[] beamLineRenderers;
    private float beamCooldownTimer;

    private float[] selectedYPositions;


    protected override void Start()
    {
        // Khởi tạo cơ bản (KHÔNG gọi base.Start() để tránh tự hủy)
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // 🆕 THAY THẾ: Bắt đầu Coroutine để Khởi tạo LineRenderers
        StartCoroutine(DelayedInitializationRoutine());

        beamCooldownTimer = beamCooldownTime;
        fanAttackTimer = fanAttackRate;

        InvokeRepeating("BaseAttack", 0.5f, baseAttackRate);

        contactDamage = 0;
    }

    // 🆕 COROUTINE: Điều phối việc khởi tạo LineRenderers để tránh đứng hình
    IEnumerator DelayedInitializationRoutine()
    {
        // Thực hiện khởi tạo LineRenderers chia nhỏ
        yield return StartCoroutine(InitializeLineRenderersRoutine());

        // Đợi thêm 1 frame trước khi tiếp tục
        yield return null;

        // Các logic khác cần chạy sau khi khởi tạo xong (nếu có)
        Debug.Log("Angel Boss Initialization complete (Line Renderers are ready).");
    }

    // 🆕 COROUTINE MỚI: Khởi tạo Line Renderers trong nhiều Frame
    IEnumerator InitializeLineRenderersRoutine()
    {
        warningLineRenderers = new LineRenderer[numRandomBeams];
        beamLineRenderers = new LineRenderer[numRandomBeams];

        Color warningColor = Color.red;
        warningColor.a = warningBeamOpacity;

        Color damageColor = Color.yellow;
        damageColor.a = 1.0f;

        for (int i = 0; i < numRandomBeams; i++)
        {
            // LineRenderer cho Warning (Đỏ)
            GameObject warningObj = new GameObject($"BossWarningRay_{i}");
            warningObj.transform.SetParent(transform);
            warningLineRenderers[i] = warningObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(warningLineRenderers[i], warningColor, 5);
            warningLineRenderers[i].enabled = false; // Đảm bảo ban đầu bị tắt

            yield return null; // Dừng lại 1 frame

            // LineRenderer cho Beam (Vàng)
            GameObject beamObj = new GameObject($"BossDamageBeam_{i}");
            beamObj.transform.SetParent(transform);
            beamLineRenderers[i] = beamObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(beamLineRenderers[i], damageColor, 6);
            beamLineRenderers[i].enabled = false; // Đảm bảo ban đầu bị tắt

            yield return null; // Dừng lại 1 frame
        }
    }


    protected override void FixedUpdate()
    {
        Move();

        if (playerTarget != null)
        {
            UpdateFanShotTimer();
            UpdateBeamTimer();
        }
    }

    protected override void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null && contactDamage > 0)
            {
                player.TakeDamage(contactDamage);
            }
        }
    }

    // --- Cấu hình LineRenderer ---
    private void ConfigureLineRenderer(LineRenderer lr, Color color, int order)
    {
        lr.startWidth = 0f;
        lr.endWidth = 0f;

        if (beamMaterial != null)
        {
            lr.material = beamMaterial;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        lr.startColor = color;
        lr.endColor = color;

        lr.useWorldSpace = true;
        lr.positionCount = 2;

        lr.sortingLayerName = beamSortingLayerName;
        lr.sortingOrder = order;
    }

    // --- Đòn 1: Base Attack ---
    void BaseAttack()
    {
        if (playerTarget == null || bossBulletPrefab == null) return;
        Vector2 directionToPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        GameObject bulletObj = Instantiate(bossBulletPrefab, transform.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.SetDirection(directionToPlayer, bossBulletSpeed);
        }
    }

    // --- Đòn 2: Fan Shot ---
    void UpdateFanShotTimer()
    {
        fanAttackTimer -= Time.fixedDeltaTime;
        if (fanAttackTimer <= 0f)
        {
            ShootFanShot();
            fanAttackTimer = fanAttackRate;
        }
    }

    void ShootFanShot()
    {
        if (playerTarget == null || bossBulletPrefab == null) return;
        Vector2 baseDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        float angleStep = fanSpreadAngle / (fanBulletCount - 1);
        float startAngle = baseAngle - (fanSpreadAngle / 2f);

        for (int i = 0; i < fanBulletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector2 direction = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );

            GameObject bulletObj = Instantiate(bossBulletPrefab, transform.position, Quaternion.identity);
            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.SetDirection(direction, bossBulletSpeed);
            }
        }
    }

    // --- Đòn 3: Beam NGA NGẪU NHIÊN ---
    void UpdateBeamTimer()
    {
        beamCooldownTimer -= Time.fixedDeltaTime;

        if (beamCooldownTimer <= 0f)
        {
            // Kiểm tra xem Line Renderers đã được khởi tạo xong chưa
            if (warningLineRenderers != null && warningLineRenderers.Length > 0 && warningLineRenderers[0] != null)
            {
                StartCoroutine(BeamAttackRoutine());
                beamCooldownTimer = beamCooldownTime;
            }
        }
    }

    IEnumerator BeamAttackRoutine()
    {
        selectedYPositions = new float[numRandomBeams];
        for (int i = 0; i < numRandomBeams; i++)
        {
            selectedYPositions[i] = UnityEngine.Random.Range(beamYRange.x, beamYRange.y);
        }

        float bossXPosition = transform.position.x;
        Vector2 beamDirection = Vector2.left;

        // 1. Hiển thị Cảnh báo (Beam Đỏ - Grow In)
        for (int i = 0; i < numRandomBeams; i++)
        {
            Vector2 startPosition = new Vector2(bossXPosition, selectedYPositions[i]);
            Vector2 endPosition = startPosition + beamDirection * beamLength;

            warningLineRenderers[i].SetPosition(0, startPosition);
            warningLineRenderers[i].SetPosition(1, endPosition);
            warningLineRenderers[i].enabled = true;

            StartCoroutine(GrowBeamWidth(warningLineRenderers[i], maxWarningWidth, beamGrowTime));
        }

        // 2. Chờ Windup (1 giây)
        yield return new WaitForSeconds(beamWindupTime);

        // 3. Bắn Beam Sát Thương (Beam Vàng - Grow In & Tắt Beam Đỏ)
        for (int i = 0; i < numRandomBeams; i++)
        {
            // Tắt Beam Đỏ
            // Đảm bảo không gọi StopCoroutine trên coroutine đang chạy GrowBeamWidth
            warningLineRenderers[i].enabled = false;

            Vector2 startPosition = new Vector2(bossXPosition, selectedYPositions[i]);
            Vector2 endPosition = startPosition + beamDirection * beamLength;

            // Kích hoạt Beam Vàng
            beamLineRenderers[i].SetPosition(0, startPosition);
            beamLineRenderers[i].SetPosition(1, endPosition);
            beamLineRenderers[i].enabled = true;

            StartCoroutine(GrowBeamWidth(beamLineRenderers[i], maxDamageWidth, beamGrowTime));

            // Gây sát thương
            PerformBeamDamage(startPosition, beamDirection);
        }

        // 4. Chờ thời gian Beam tồn tại (2 giây)
        yield return new WaitForSeconds(beamDuration);

        // 5. Vô hiệu hóa Beam Sát Thương (Shrink Out)
        for (int i = 0; i < numRandomBeams; i++)
        {
            StartCoroutine(GrowBeamWidth(beamLineRenderers[i], 0f, 0.1f));
        }
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < numRandomBeams; i++)
        {
            // Đảm bảo đã thu nhỏ trước khi tắt
            beamLineRenderers[i].enabled = false;
        }
    }

    // Coroutine tạo hiệu ứng Beam mượt mà
    IEnumerator GrowBeamWidth(LineRenderer lr, float targetWidth, float duration)
    {
        float timer = 0f;
        float startWidth = lr.startWidth;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            float newWidth = Mathf.Lerp(startWidth, targetWidth, progress);
            lr.startWidth = newWidth;
            lr.endWidth = newWidth;

            yield return null;
        }
        lr.startWidth = targetWidth;
        lr.endWidth = targetWidth;
    }

    // Hàm gây sát thương Beam
    void PerformBeamDamage(Vector2 startPos, Vector2 direction)
    {
        // Sử dụng playerLayer để chỉ Raycast vào Player Layer
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, beamLength, playerLayer);

        // Debug.DrawRay(startPos, direction * beamLength, Color.red, 1f); // Dùng để kiểm tra Raycast trong Scene view

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                Player player = hit.collider.GetComponent<Player>();
                if (player != null)
                {
                    player.TakeDamage(beamDamage);
                }
            }
        }
    }

    // --- Xử lý khi Boss chết (Override Die) ---
    protected override void Die()
    {
        CancelInvoke("BaseAttack");
        StopAllCoroutines();

        // Tắt tất cả các Line Renderers đang hoạt động
        if (warningLineRenderers != null)
        {
            foreach (var lr in warningLineRenderers)
            {
                if (lr != null) lr.enabled = false;
            }
        }
        if (beamLineRenderers != null)
        {
            foreach (var lr in beamLineRenderers)
            {
                if (lr != null) lr.enabled = false;
            }
        }

        Debug.Log("Angel Boss Defeated!");

        // Gọi hàm Die gốc của Enemy để xử lý việc hủy/hoàn thành
        base.Die();
    }
}