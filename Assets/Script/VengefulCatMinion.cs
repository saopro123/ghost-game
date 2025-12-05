using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public enum MinionType
{
    BulletShooter,      // Mèo 0: Bắn đạn thường (Aimed Shot)
    LaserAimer,         // Mèo 1: Chiếu Laser Aim theo Player (Laser Beam)
    BoneSpawner,        // Mèo 2: Tạo 5 đoạn bone Vàng/Hồng (Bone Waves)
    FanShotShooter      // Mèo 3: Bắn 10 viên đạn lan (Spread Shot)
}
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class VengefulCatMinion : Enemy
{
    // 🆕 THUỘC TÍNH MỚI: BossController sẽ gán tham chiếu vào đây sau khi Instantiate.
    public EgyptianCatBoss BossController { get; set; }

    [Header("== Minion Type & State ==")]
    public MinionType type;
    private bool isReadyToAttack = false; // Chỉ tấn công khi Boss kích hoạt

    [Header("== Cài Đặt Chung Attack ==")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 7f;
    public GameObject bonePrefab;

    [Header("== Cài Đặt Laser Aim (Type 1) ==")]
    public float aimTime = 2.0f;
    public float postAimDelay = 0.5f;
    public float laserFireTime = 0.5f;
    public int laserDamage = 50;
    public LayerMask playerLayer;
    private float laserLength = 20f;

    // --- CÀI ĐẶT LINE RENDERER ---
    [Header("== Cài Đặt Hiển Thị Laser (Type 1) ==")]
    public Material beamMaterial;
    public float maxWarningWidth = 0.4f;
    public float maxDamageWidth = 0.6f;
    public float beamGrowTime = 0.15f;
    public string beamSortingLayerName = "FX_OVERLAY";

    private LineRenderer warningLineRenderer;
    private LineRenderer damageLineRenderer;

    [Header("== Cài Đặt Bone Spawner (Type 2) ==")]
    public float boneSpawnDelay = 1.0f;
    public int boneCount = 5;
    public float boneSpeed = 7f;
    public Vector2 boneYRange = new Vector2(-5f, 5f);

    [Header("== Cài Đặt Fan Shot (Type 3) ==")]
    public int fanBulletCount = 10;
    public float fanSpreadAngle = 90f;


    protected override void Start()
    {
        // GHI ĐÈ Start() và KHÔNG GỌI base.Start() để tránh lệnh tự hủy.

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
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Khởi tạo Line Renderer cho đòn Laser
        InitializeLineRenderers();
    }

    private void InitializeLineRenderers()
    {
        // LineRenderer cho Warning (Màu đỏ mờ)
        GameObject warningObj = new GameObject($"MinionWarningRay_{type}");
        warningObj.transform.SetParent(transform);
        warningLineRenderer = warningObj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(warningLineRenderer, new Color(1f, 0.2f, 0.2f, 0.4f), 5);

        // LineRenderer cho Sát thương (Màu vàng sáng)
        GameObject beamObj = new GameObject($"MinionDamageBeam_{type}");
        beamObj.transform.SetParent(transform);
        damageLineRenderer = beamObj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(damageLineRenderer, new Color(1f, 1f, 0f, 1f), 6);

        warningLineRenderer.enabled = false;
        damageLineRenderer.enabled = false;
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


    protected override void FixedUpdate()
    {
        // Minion không tự tấn công lặp lại
    }

    protected override void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void InitializeMinion(MinionType newType)
    {
        type = newType;
        // Logic đổi màu/sprite dựa trên type nếu cần
    }

    // Hàm Boss gọi để kích hoạt Minion tấn công
    public void SetAttackActive(bool isActive)
    {
        isReadyToAttack = isActive;
        if (isActive)
        {
            PerformAttack();
        }
    }

    // Hàm Minion gọi sau khi MỘT đòn tấn công hoàn thành
    public void OnAttackComplete()
    {
        isReadyToAttack = false;

        // Thông báo cho Boss để chuyển lượt
        if (BossController != null)
        {
            BossController.MinionFinishedAttack();
        }
    }

    void PerformAttack()
    {
        if (playerTarget == null)
        {
            OnAttackComplete();
            return;
        }

        switch (type)
        {
            case MinionType.BulletShooter:
                ShootSimpleBullet();
                break;
            case MinionType.LaserAimer:
                StartCoroutine(LaserAimAttackRoutine());
                return; // Coroutine sẽ gọi OnAttackComplete()

            case MinionType.BoneSpawner:
                StartCoroutine(BoneSpawnAttackRoutine());
                return; // Coroutine sẽ gọi OnAttackComplete()

            case MinionType.FanShotShooter:
                ShootFanShot();
                break;
        }

        // Chỉ những đòn tấn công KHÔNG dùng Coroutine mới gọi ngay lập tức
        OnAttackComplete();
    }

    void ShootSimpleBullet()
    {
        if (bulletPrefab == null) return;
        Vector2 directionToPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.SetDirection(directionToPlayer, bulletSpeed);
        }
    }

    void ShootFanShot()
    {
        if (bulletPrefab == null) return;
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

            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.SetDirection(direction, bulletSpeed);
            }
        }
    }

    IEnumerator BoneSpawnAttackRoutine()
    {
        if (bonePrefab == null)
        {
            OnAttackComplete();
            yield break;
        }

        // Giả định xương luôn spawn bên phải màn hình (X=12)
        float spawnX = 12f;

        for (int i = 0; i < boneCount; i++)
        {
            // Tái sử dụng logic xương vàng/hồng từ Skeleton Boss
            bool isYellowBone = Random.value > 0.5f;
            float randomY = Random.Range(boneYRange.x, boneYRange.y);

            Vector3 spawnPos = new Vector3(spawnX, randomY, 0);
            GameObject boneObj = Instantiate(bonePrefab, spawnPos, Quaternion.identity);

            Bone boneScript = boneObj.GetComponent<Bone>();
            if (boneScript != null)
            {
                // BoneScript.Initialize(isYellowBone, boneSpeed)
                boneScript.Initialize(isYellowBone, boneSpeed);
            }

            yield return new WaitForSeconds(boneSpawnDelay);
        }

        OnAttackComplete();
    }

    IEnumerator LaserAimAttackRoutine()
    {
        if (warningLineRenderer == null || damageLineRenderer == null || playerTarget == null)
        {
            OnAttackComplete();
            yield break;
        }

        damageLineRenderer.enabled = false;
        warningLineRenderer.enabled = true;

        Vector3 startPosition = transform.position;
        float timer = 0f;

        StartCoroutine(GrowBeamWidth(warningLineRenderer, maxWarningWidth, beamGrowTime));

        // --- Giai đoạn 1: AIM theo Player (Lock-on) ---
        while (timer < aimTime)
        {
            Vector3 direction = (playerTarget.position - startPosition).normalized;

            // Dùng Raycast để định vị điểm cuối laser (nếu chạm Player hoặc vật cản)
            RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, laserLength, playerLayer);
            Vector3 endPosition = hit.collider != null ? hit.point : startPosition + direction * laserLength;

            warningLineRenderer.SetPosition(0, startPosition);
            warningLineRenderer.SetPosition(1, endPosition);

            timer += Time.deltaTime;
            yield return null;
        }

        // --- Giai đoạn 2: Dừng AIM và chờ đợi ---
        // Lấy hướng cuối cùng để bắn
        Vector3 finalDirection = (playerTarget.position - startPosition).normalized;
        RaycastHit2D finalHit = Physics2D.Raycast(startPosition, finalDirection, laserLength, playerLayer);
        Vector3 finalBeamEnd = finalHit.collider != null ? finalHit.point : startPosition + finalDirection * laserLength;

        // Khóa vị trí cảnh báo cuối cùng
        warningLineRenderer.SetPosition(1, finalBeamEnd);

        // Giảm độ rộng cảnh báo xuống 0 (Hiệu ứng biến mất)
        StartCoroutine(GrowBeamWidth(warningLineRenderer, 0f, 0.1f));

        yield return new WaitForSeconds(postAimDelay);

        // --- Giai đoạn 3: Bắn Laser Sát Thương ---
        warningLineRenderer.enabled = false;

        damageLineRenderer.SetPosition(0, startPosition);
        damageLineRenderer.SetPosition(1, finalBeamEnd);
        damageLineRenderer.enabled = true;

        // Tăng độ rộng Laser Sát thương
        StartCoroutine(GrowBeamWidth(damageLineRenderer, maxDamageWidth, beamGrowTime));

        // Thực hiện sát thương tức thì
        PerformLaserDamage(startPosition, finalDirection);

        yield return new WaitForSeconds(laserFireTime);

        // Tắt Laser Sát Thương
        StartCoroutine(GrowBeamWidth(damageLineRenderer, 0f, 0.1f));
        yield return new WaitForSeconds(0.1f);
        damageLineRenderer.enabled = false;

        OnAttackComplete();
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


    void PerformLaserDamage(Vector2 startPos, Vector2 direction)
    {
        // Kiểm tra xem tia Laser có trúng Player không
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, laserLength, playerLayer);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(laserDamage);
            }
        }
    }

    protected override void Die()
    {
        StopAllCoroutines();

        // 🆕 Hủy các Line Renderer GameObject khi Minion chết để dọn dẹp Scene
        if (warningLineRenderer != null) Destroy(warningLineRenderer.gameObject);
        if (damageLineRenderer != null) Destroy(damageLineRenderer.gameObject);

        // Báo cho Boss biết đã chết
        if (BossController != null)
        {
            BossController.CheckMinionStatus();
        }

        base.Die();
    }
}