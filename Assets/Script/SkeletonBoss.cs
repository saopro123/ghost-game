using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SkeletonBoss : Enemy
{
    [Header("== BOSS ATTACK SETTINGS ==")]

    // --- Đòn 1: Xương (Bone Attack) ---
    [Tooltip("Prefab của cục xương (phải có script Bone.cs).")]
    public GameObject bonePrefab;
    [Tooltip("Tần suất tạo xương (giây/lần).")]
    public float boneSpawnInterval = 1f;
    [Tooltip("Tốc độ di chuyển ngang của xương.")]
    public float boneSpeed = 7f;
    [Tooltip("Khoảng cách dọc giữa các cục xương (ví dụ: 1f).")]
    public float boneSpawnSpacing = 1f;
    [Tooltip("Khoảng Y tối thiểu và tối đa để spawn xương.")]
    public Vector2 boneYRange = new Vector2(-5f, 5f);
    private float boneTimer;

    // --- Đòn 2: Laser Beam (Tái sử dụng từ AngelBoss) ---
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
    public float beamAttackRate = 10f; // Mỗi 10 giây
    [Tooltip("Sát thương gây ra bởi Beam.")]
    public int beamDamage = 100;

    // --- Cài đặt Hiển thị Beam & Raycast ---
    [Header("== Cài Đặt Hiển Thị Beam ==")]
    public Material beamMaterial;
    public float beamGrowTime = 0.15f;
    public float maxWarningWidth = 0.4f;
    public float maxDamageWidth = 0.6f;
    [Range(0f, 1f)]
    public float warningBeamOpacity = 0.3f;
    public string beamSortingLayerName = "FX_OVERLAY";
    public int beamSortingOrder = 10;
    [Header("== Cài Đặt Raycast ==")]
    public LayerMask playerLayer;

    // Các tham chiếu cho Raycast Visuals
    private LineRenderer[] warningLineRenderers;
    private LineRenderer[] beamLineRenderers;
    private float beamTimer;
    private float[] selectedYPositions;


    protected override void Start()
    {
        // Khởi tạo cơ bản, không gọi base.Start() để không tự hủy và di chuyển
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
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // Boss đứng yên sau khi di chuyển
        }

        // 🆕 SỬA: Bắt đầu Coroutine để Khởi tạo LineRenderers
        StartCoroutine(DelayedInitializationRoutine());

        beamTimer = beamAttackRate;
        boneTimer = boneSpawnInterval;

        contactDamage = 0; // Boss không gây sát thương khi va chạm vật lý
    }

    // 🆕 COROUTINE: Điều phối việc khởi tạo LineRenderers để tránh đứng hình
    IEnumerator DelayedInitializationRoutine()
    {
        // Thực hiện khởi tạo LineRenderers chia nhỏ
        yield return StartCoroutine(InitializeLineRenderersRoutine());

        // Đợi thêm 1 frame trước khi tiếp tục
        yield return null;

        Debug.Log("Skeleton Boss Initialization complete (Line Renderers are ready).");
    }

    protected override void FixedUpdate()
    {
        // Boss sẽ đứng yên sau khi đến vị trí, nên chỉ cần cập nhật timers
        UpdateAttackTimers();
    }

    // Ghi đè Move để Boss luôn đứng yên sau khi đến vị trí
    protected override void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void UpdateAttackTimers()
    {
        // 1. Logic Xương
        boneTimer -= Time.fixedDeltaTime;
        if (boneTimer <= 0f)
        {
            PerformBoneAttack();
            boneTimer = boneSpawnInterval;
        }

        // 2. Logic Beam
        beamTimer -= Time.fixedDeltaTime;
        if (beamTimer <= 0f)
        {
            // Kiểm tra xem Line Renderers đã được khởi tạo xong chưa
            if (warningLineRenderers != null && warningLineRenderers.Length > 0 && warningLineRenderers[0] != null)
            {
                StartCoroutine(BeamAttackRoutine());
                beamTimer = beamAttackRate;
            }
        }
    }

    // --- Đòn 1: Xương đi ngang (Bone Attack) ---
    void PerformBoneAttack()
    {
        if (bonePrefab == null) return;

        // 1. Chọn loại Xương ngẫu nhiên (Vàng HOẶC Hồng)
        // 0.0 -> 0.5 là Hồng (Damage), 0.5 -> 1.0 là Vàng (Heal)
        bool isHealingBone = Random.value > 0.5f;

        // 2. Chọn vị trí Y ngẫu nhiên trong dải đã thiết lập
        float randomY = Random.Range(boneYRange.x, boneYRange.y);

        // Vị trí X ngoài màn hình (Giả định LevelManager spawn ở X=12)
        float spawnX = 12f;

        Vector3 spawnPos = new Vector3(spawnX, randomY, 0);

        // 3. Tạo cục xương đơn lẻ
        GameObject boneObj = Instantiate(bonePrefab, spawnPos, Quaternion.identity);

        // 4. Gán loại Xương và Tốc độ
        Bone boneScript = boneObj.GetComponent<Bone>();
        if (boneScript != null)
        {
            // LƯU Ý: BoneScript cần có hàm Initialize(bool isHealing, float speed)
            boneScript.Initialize(isHealingBone, boneSpeed);
        }
    }

    #region Beam Logic (Tái sử dụng và Tối ưu hóa)

    // 🆕 COROUTINE: Chia việc khởi tạo Line Renderer ra nhiều Frame
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
            warningLineRenderers[i].enabled = false; // Tắt khi khởi tạo

            yield return null; // Dừng lại 1 frame

            // LineRenderer cho Beam (Vàng)
            GameObject beamObj = new GameObject($"BossDamageBeam_{i}");
            beamObj.transform.SetParent(transform);
            beamLineRenderers[i] = beamObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(beamLineRenderers[i], damageColor, 6);
            beamLineRenderers[i].enabled = false; // Tắt khi khởi tạo

            yield return null; // Dừng lại 1 frame
        }
    }


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

    IEnumerator BeamAttackRoutine()
    {
        // ... (Logic Beam giữ nguyên) ...
        selectedYPositions = new float[numRandomBeams];
        for (int i = 0; i < numRandomBeams; i++)
        {
            selectedYPositions[i] = UnityEngine.Random.Range(beamYRange.x, beamYRange.y);
        }

        float bossXPosition = transform.position.x;
        Vector2 beamDirection = Vector2.left;

        // 1. Hiển thị Cảnh báo
        for (int i = 0; i < numRandomBeams; i++)
        {
            Vector2 startPosition = new Vector2(bossXPosition, selectedYPositions[i]);
            Vector2 endPosition = startPosition + beamDirection * beamLength;

            warningLineRenderers[i].SetPosition(0, startPosition);
            warningLineRenderers[i].SetPosition(1, endPosition);
            warningLineRenderers[i].enabled = true;

            StartCoroutine(GrowBeamWidth(warningLineRenderers[i], maxWarningWidth, beamGrowTime));
        }

        // 2. Chờ Windup
        yield return new WaitForSeconds(beamWindupTime);

        // 3. Bắn Beam Sát Thương
        for (int i = 0; i < numRandomBeams; i++)
        {
            warningLineRenderers[i].enabled = false;

            Vector2 startPosition = new Vector2(bossXPosition, selectedYPositions[i]);
            Vector2 endPosition = startPosition + beamDirection * beamLength;

            beamLineRenderers[i].SetPosition(0, startPosition);
            beamLineRenderers[i].SetPosition(1, endPosition);
            beamLineRenderers[i].enabled = true;

            StartCoroutine(GrowBeamWidth(beamLineRenderers[i], maxDamageWidth, beamGrowTime));

            // Gây sát thương
            PerformBeamDamage(startPosition, beamDirection);
        }

        // 4. Chờ thời gian Beam tồn tại
        yield return new WaitForSeconds(beamDuration);

        // 5. Vô hiệu hóa Beam Sát Thương
        for (int i = 0; i < numRandomBeams; i++)
        {
            StartCoroutine(GrowBeamWidth(beamLineRenderers[i], 0f, 0.1f));
        }
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < numRandomBeams; i++)
        {
            beamLineRenderers[i].enabled = false;
        }
    }

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

    void PerformBeamDamage(Vector2 startPos, Vector2 direction)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, beamLength, playerLayer);

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

    #endregion

    // --- Xử lý khi Boss chết ---
    protected override void Die()
    {
        CancelInvoke();
        StopAllCoroutines();

        // Tắt tất cả các Line Renderers
        if (warningLineRenderers != null)
            foreach (var lr in warningLineRenderers)
                if (lr != null) lr.enabled = false;
        if (beamLineRenderers != null)
            foreach (var lr in beamLineRenderers)
                if (lr != null) lr.enabled = false;

        Debug.Log("Skeleton Boss Defeated!");

        base.Die();
    }

    // Ghi đè hàm va chạm để không tự hủy khi chạm Player (vì contactDamage = 0)
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
}