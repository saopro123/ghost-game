using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuperAngelBoss : Enemy
{
    [Header("== PHASE 1: JUDGMENT (Angel & Skeleton) ==")]
    public GameObject bulletPrefab;
    public GameObject bonePrefab;
    public float baseAttackRate = 1f;
    public float boneSpawnRate = 1.5f;

    [Header("== PHASE 2: PUNISHMENT (Beams) ==")]
    public Material beamMaterial;
    public int numBeams = 5;
    public float beamCooldown = 8f;

    [Header("== PHASE 3: REVELATION (Minions/Vulnerability) ==")]
    public GameObject minionPrefab;
    public Transform[] minionSpawnPoints;
    public float vulnerableDuration = 8f;
    private bool isInvulnerable = false;

    // Beam Visuals
    private LineRenderer[] warningLines;
    private LineRenderer[] damageLines;
    private float attackTimer;
    private float beamTimer;

    protected override void Start()
    {
        // Khởi tạo Boss
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        isBoss = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        // Khởi tạo LineRenderers cho Beams (từ AngelBoss)
        StartCoroutine(InitBeamsRoutine());

        attackTimer = baseAttackRate;
        beamTimer = beamCooldown;

        // Bắt đầu chu kỳ bắn quạt và xương
        InvokeRepeating("FanShot", 2f, 3f);
        InvokeRepeating("SpawnDivineBones", 1f, boneSpawnRate);
    }

    IEnumerator InitBeamsRoutine()
    {
        warningLines = new LineRenderer[numBeams];
        damageLines = new LineRenderer[numBeams];
        for (int i = 0; i < numBeams; i++)
        {
            warningLines[i] = CreateBeam(new Color(1, 0, 0, 0.3f), 5);
            damageLines[i] = CreateBeam(Color.yellow, 6);
            yield return null;
        }
    }

    LineRenderer CreateBeam(Color col, int order)
    {
        GameObject obj = new GameObject("SuperBeam");
        obj.transform.SetParent(transform);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = beamMaterial != null ? beamMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = col;
        lr.startWidth = lr.endWidth = 0;
        lr.positionCount = 2;
        lr.sortingLayerName = "FX_OVERLAY";
        lr.sortingOrder = order;
        lr.enabled = false;
        return lr;
    }

    protected override void FixedUpdate()
    {
        // Super Boss đứng yên ở vị trí chỉ định
        rb.linearVelocity = Vector2.zero;

        if (playerTarget == null || isDying) return;

        // Cập nhật timer cho đòn Beam
        beamTimer -= Time.fixedDeltaTime;
        if (beamTimer <= 0)
        {
            StartCoroutine(SuperBeamRoutine());
            beamTimer = beamCooldown;
        }

        // Cơ chế Vulnerable Phase (Tự động kích hoạt mỗi khi mất 25% máu)
        CheckPhaseTransition();
    }

    #region Attacks (Tái sử dụng logic)

    void FanShot()
    {
        if (isInvulnerable) return;
        float angleStep = 180f / 10;
        for (int i = 0; i < 11; i++)
        {
            float angle = -90f + (i * angleStep);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            b.GetComponent<EnemyBullet>().SetDirection(dir, 8f);
        }
    }

    void SpawnDivineBones()
    {
        if (isInvulnerable) return;
        float randomY = Random.Range(-4.5f, 4.5f);
        GameObject b = Instantiate(bonePrefab, new Vector3(12, randomY, 0), Quaternion.identity);
        b.GetComponent<Bone>().Initialize(Random.value > 0.5f, 7f); // Yellow or Pink
    }

    IEnumerator SuperBeamRoutine()
    {
        float[] yPositions = new float[numBeams];
        for (int i = 0; i < numBeams; i++)
        {
            yPositions[i] = Random.Range(-4.5f, 4.5f);
            warningLines[i].SetPosition(0, new Vector2(12, yPositions[i]));
            warningLines[i].SetPosition(1, new Vector2(-12, yPositions[i]));
            warningLines[i].enabled = true;
            StartCoroutine(GrowWidth(warningLines[i], 0.3f, 0.2f));
        }

        yield return new WaitForSeconds(1.2f);

        for (int i = 0; i < numBeams; i++)
        {
            warningLines[i].enabled = false;
            damageLines[i].SetPosition(0, new Vector2(12, yPositions[i]));
            damageLines[i].SetPosition(1, new Vector2(-12, yPositions[i]));
            damageLines[i].enabled = true;
            StartCoroutine(GrowWidth(damageLines[i], 0.6f, 0.1f));

            // Raycast gây sát thương
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(12, yPositions[i]), Vector2.left, 24f, LayerMask.GetMask("Player"));
            if (hit.collider != null) hit.collider.GetComponent<Player>().TakeDamage(50);
        }

        yield return new WaitForSeconds(1f);
        for (int i = 0; i < numBeams; i++) damageLines[i].enabled = false;
    }

    IEnumerator GrowWidth(LineRenderer lr, float target, float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0, target, t / time);
            yield return null;
        }
    }
    #endregion

    #region Phase Control (Từ Egyptian Cat)

    private float lastPhaseHealth;
    void CheckPhaseTransition()
    {
        // Cứ mỗi khi mất 30% máu, Boss sẽ triệu hồi Minions và trở nên bất tử
        if (!isInvulnerable && currentHealth < maxHealth * 0.7f && lastPhaseHealth == 0)
        {
            StartVulnerableChallenge();
            lastPhaseHealth = currentHealth;
        }
    }

    void StartVulnerableChallenge()
    {
        isInvulnerable = true;
        spriteRenderer.color = Color.gray; // Báo hiệu bất tử
        Debug.Log("Super Boss: Invulnerable! Destroy minions to damage me!");

        // Triệu hồi 2 minions bảo vệ
        for (int i = 0; i < minionSpawnPoints.Length; i++)
        {
            GameObject m = Instantiate(minionPrefab, minionSpawnPoints[i].position, Quaternion.identity);
            m.GetComponent<VengefulCatMinion>().BossController = null; // Độc lập
        }

        StartCoroutine(VulnerabilityTimer());
    }

    IEnumerator VulnerabilityTimer()
    {
        yield return new WaitForSeconds(vulnerableDuration);
        isInvulnerable = false;
        spriteRenderer.color = Color.white;
    }

    public override void TakeDamage(int damageAmount, bool isFromExplosion = false)
    {
        if (isInvulnerable)
        {
            // Nháy xanh báo hiệu có khiên
            spriteRenderer.color = Color.cyan;
            Invoke("ResetColor", 0.1f);
            return;
        }
        base.TakeDamage(damageAmount, isFromExplosion);
    }

    void ResetColor() => spriteRenderer.color = isInvulnerable ? Color.gray : Color.white;

    #endregion

    protected override void Die()
    {
        CancelInvoke();
        StopAllCoroutines();
        // Super Boss không dùng Die của Enemy để tránh mờ dần ngay
        // Nó sẽ được LevelManager xử lý để chạy Hidden Ending
        gameObject.SetActive(false);
    }
}