using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    // Cài Đặt Di Chuyển
    [Header("Cài Đặt Di Chuyển")]
    [Tooltip("Tốc độ di chuyển và tốc độ phản hồi đến điểm chạm")]
    public float moveSpeed = 5f;
    [Tooltip("Giới hạn vị trí Y tối đa và tối thiểu trong World Space")]
    public float maxYBoundary = 4.5f;
    public float minYBoundary = -4.5f;

    // ==========================================================
    // ** THUỘC TÍNH SHOP CÓ THỂ NÂNG CẤP **
    // ==========================================================
    [Header("== Chỉ Số Nâng Cấp ==")]
    [Tooltip("Máu tối đa ban đầu")]
    public int baseMaxPurification = 100;
    public int maxPurification { get; private set; } // Max HP hiện tại
    public int currentPurification { get; private set; }

    [Tooltip("Sát thương cơ bản của một viên đạn")]
    public int damagePerBullet = 10;

    [Tooltip("Thời gian giữa các lần bắn (giảm là tăng tốc độ bắn)")]
    public float fireRate = 0.2f;

    [Tooltip("Số lượng viên đạn bắn ra trong một lần (Amount Up)")]
    public int projectileAmount = 1;

    [Tooltip("Chỉ số may mắn (ảnh hưởng đến drop, ngẫu nhiên)")]
    public float luck = 0f;

    [Tooltip("Offset (khoảng cách) giữa các viên đạn khi bắn Amount > 1")]
    public float projectileOffset = 0.3f;

    // ==========================================================
    // ** THUỘC TÍNH TAROT **
    // ==========================================================
    [Header("== Chỉ Số Hiệu Ứng Tarot ==")]
    [Tooltip("Sát thương cộng thêm gây ra từ hiệu ứng Tarot (The Devil).")]
    public int tarotBonusDamage = 0;
    [Tooltip("Hệ số nhân sát thương nhận vào (nhận 100% + giá trị này). Ví dụ: 1.0f là nhận gấp đôi.")]
    public float tarotDamageTakenMultiplier = 0f; // Mặc định 0, nghĩa là nhận 1x (100%)


    // --- LOGIC TIỀN TỆ VÀ LUCK ---
    [Header("== Tiền Tệ & May Mắn ==")]
    [Tooltip("Tổng Gold người chơi đang có")]
    public int totalGold = 0;
    [Tooltip("Hệ số nhân Gold (mặc định 1.0f)")]
    public float goldMultiplier = 1.0f; // Mặc định là 1x
    // Số lần mua Luck đã được thực hiện (dùng cho ShopMenu kiểm tra giới hạn)
    [HideInInspector] public int currentLuckUpgrades = 0;
    // ----------------------------

    [Header("== Giới Hạn HP Tăng Cường ==")]
    [Tooltip("Số HP cơ bản cho mỗi vòng Halo.")]
    private const int PURIFICATION_PER_HALO = 25; // 4 vòng * 25 HP = 100 HP
    [Tooltip("Số vòng Halo tối đa có thể mua thêm.")]
    public int maxUpgradableHalos = 4; // Tối đa 4 vòng mua thêm (100 -> 200 HP)
    [HideInInspector] public int currentUpgradedHalos = 0; // Số vòng đã mua
    // ==========================================================


    // Cài Đặt Bắn Đạn
    [Header("Cài Đặt Bắn Đạn")]
    [Tooltip("Prefab của viên đạn người chơi")]
    public GameObject bulletPrefab;
    [Tooltip("Điểm xuất phát của đạn (Transform con của Player)")]
    public Transform firePoint;

    // Cài đặt Purification & Hiệu Ứng
    [Header("Cài đặt Purification & Hiệu Ứng")]
    public SpriteRenderer spriteRenderer;

    // Cài đặt cho hiệu ứng nháy sáng khi trúng đòn
    public Color hitFlashColor = new Color(1f, 0.7f, 0.8f, 1f); // Màu hồng nhạt
    public float hitFlashDuration = 0.15f;
    public int hitFlashCount = 1;

    // Biến cho Power Up (sẽ dùng sau)
    [HideInInspector] public GameObject currentBulletPrefab;

    private float nextFireTime;
    private float targetY;
    private Rigidbody2D rb;
    private UIManager uiManager;
    public bool tookDamageInEvent = false;
    public bool isVSplitShot = false;

    // THAM CHIẾU MỚI: Game Menu Manager
    private GameMenuManager gameMenuManager;
    public static Player Instance; // Khai báo biến Instance tĩnh
    [Header("== Devil Synergy Settings ==")]
    public int devilLives = 0;
    public float currentDevilDmgBonus = 0f; // 0.5f tương đương 50%
    private int devilKillCounter = 0;
    private const int KILLS_FOR_LIFE = 10;
    private Coroutine explosiveRoutine;
    private Coroutine invincibilityRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // --- RESET STATIC VARS ---
        enemyBulletSpeedMultiplier = 1f;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetY = transform.position.y;

        // Khởi tạo chỉ số ban đầu
        maxPurification = baseMaxPurification;
        currentPurification = maxPurification;
        currentBulletPrefab = bulletPrefab;

        // KẾT NỐI UI MANAGER
        uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager object not found! Health bar won't work.");
        }
        else
        {
            // Cập nhật UI ngay khi bắt đầu
            uiManager.UpdatePurificationMeter(currentPurification, maxPurification);
            // Cập nhật Gold ban đầu
            uiManager.UpdateGoldDisplay(totalGold);
        }

        // KẾT NỐI GAME MENU MANAGER
        gameMenuManager = GameMenuManager.Instance;
        if (gameMenuManager == null)
        {
            Debug.LogError("GameMenuManager instance not found! Game Over won't work.");
        }


        if (rb == null) Debug.LogError("Rigidbody2D không được tìm thấy!");
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer không được tìm thấy!");

        UpdateGhostAlpha();
    }

    private void Update()
    {
        // 🛑 QUAN TRỌNG: KHÔNG CHẠY LOGIC INPUT NẾU GAME KHÔNG Ở TRẠNG THÁI PLAYING
        if (gameMenuManager != null && GameMenuManager.CurrentState != GameMenuManager.GameState.Playing)
        {
            // Cho phép tạm dừng game (dù không ở trạng thái Playing)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (GameMenuManager.CurrentState == GameMenuManager.GameState.Playing)
                {
                    gameMenuManager.PauseGame();
                }
            }

            // Nếu không phải trạng thái Playing, thoát khỏi Update
            return;
        }


        // Xử lý Input (Touch / Keyboard)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(touch.position);
            targetY = touchWorldPos.y;
        }
        else
        {
            float verticalInput = Input.GetAxis("Vertical");
            if (verticalInput != 0)
            {
                targetY += verticalInput * Time.deltaTime * moveSpeed;
            }
        }
        targetY = Mathf.Clamp(targetY, minYBoundary, maxYBoundary);
        // đoạn code này sẽ tự gán các chỉ số cần thiết.
        if (isDevilSynergy && currentDevilDmgBonus == 0)
        {
            currentDevilDmgBonus = 0.5f;
            if (devilLives == 0) devilLives = 1;
        }
    }

    private void FixedUpdate()
    {
        // 🛑 QUAN TRỌNG: KHÔNG CHẠY LOGIC FIXED UPDATE NẾU GAME KHÔNG Ở TRẠNG THÁI PLAYING
        if (gameMenuManager != null && GameMenuManager.CurrentState != GameMenuManager.GameState.Playing)
        {
            return;
        }

        MoveToTarget();

        // Logic Bắn Tự Động
        if (Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void MoveToTarget()
    {
        float moveDistance = targetY - transform.position.y;
        Vector2 currentVelocity = rb.linearVelocity;

        currentVelocity.y = moveDistance * moveSpeed * 2f;

        rb.linearVelocity = currentVelocity;
    }

    private void Shoot()
    {
        if (currentBulletPrefab == null || firePoint == null) return;

        // 1. Tính toán sát thương cơ bản
        float baseOutput = damagePerBullet + tarotBonusDamage;
        if (isMysticSynergy) baseOutput += (totalGold / 50);

        // 2. Áp dụng Devil Synergy Bonus
        float devilBonus = baseOutput * currentDevilDmgBonus;
        float currentOutput = baseOutput + devilBonus;

        // 3. Holy Cross (X2 nếu máu dưới 30%)
        if (hasHolyCross && currentPurification < maxPurification * 0.3f)
        {
            currentOutput *= 2;
            // Debug.Log("<color=yellow>HOLY CROSS X2!</color>");
        }

        int finalBulletDamage = Mathf.RoundToInt(currentOutput);

        // IN RA CONSOLE ĐỂ KIỂM TRA
        // Debug.Log("Final DMG: " + finalBulletDamage + " (Base: " + baseOutput + ", Devil Bonus: " + devilBonus + ")");

        // --- THỰC HIỆN BẮN ---
        if (isVSplitShot)
        {
            SpawnBulletWithAngle(-45f, finalBulletDamage);
            SpawnBulletWithAngle(45f, finalBulletDamage);
        }
        else
        {
            float totalOffset = (projectileAmount - 1) * projectileOffset;
            float startOffset = -totalOffset / 2f;

            for (int i = 0; i < projectileAmount; i++)
            {
                float currentYOffset = startOffset + i * projectileOffset;
                Vector3 spawnPosition = firePoint.position;
                spawnPosition.y += currentYOffset;
                SpawnBulletWithAngle(0f, finalBulletDamage, spawnPosition);
            }
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerShootSFX();
    }

    // ==========================================================
    // ** CHỨC NĂNG NÂNG CẤP (Dành cho Shop/Tarot) **
    // ==========================================================

    public void IncreaseDamage(int amount)
    {
        damagePerBullet += amount;
        Debug.Log($"Damage tăng: {damagePerBullet}");
    }

    // HÀM MỚI: Dùng cho The Devil
    public void IncreaseTarotBonusDamage(int amount)
    {
        tarotBonusDamage += amount;
        Debug.Log($"Tarot Bonus Damage tăng: {tarotBonusDamage}");
    }

    // HÀM MỚI: Dùng cho The Devil
    public void IncreaseTarotDamageTakenMultiplier(float amount)
    {
        tarotDamageTakenMultiplier += amount;
        Debug.Log($"Tarot Damage Taken Multiplier tăng: {tarotDamageTakenMultiplier}");
    }

    // HÀM MỚI: The Fool - Giảm Max HP
    // Giảm Max HP (Dùng cho lá bài The Fool hoặc trừng phạt)
    public void DecreaseMaxHP(int amount)
    {
        maxPurification -= amount;
        // Tối thiểu là 25 HP (1 tim)
        if (maxPurification < 25) maxPurification = 25;

        // Nếu máu hiện tại vượt quá mức trần mới -> cắt bớt
        if (currentPurification > maxPurification) currentPurification = maxPurification;

        RefreshHPUI();
        Debug.Log($"Max HP decreased to: {maxPurification}");
    }


    // Hàm mới: Tăng Max HP hoặc chỉ hồi máu nếu đã đạt giới hạn 8 Halo
    public bool TryIncreaseMaxHP(int amount)
    {
        if (isDevilSynergy) { damagePerBullet += 10; return false; }

        // Giới hạn tối đa 10 tim (250 HP)
        if (maxPurification >= 250)
        {
            Heal(amount);
            return false;
        }

        maxPurification += amount;
        currentPurification += amount; // Tăng bình chứa thì đổ đầy thêm luôn

        RefreshHPUI();
        return true;
    }


    public void IncreaseFireRate(float amount) // amount là giá trị TỐC ĐỘ BẮN (delay giảm)
    {
        fireRate -= amount;
        fireRate = Mathf.Max(0.05f, fireRate); // Đảm bảo fire rate không quá nhanh
        Debug.Log($"Fire Rate (Delay) giảm: {fireRate}");
    }

    public void IncreaseProjectileAmount(int amount)
    {
        projectileAmount += amount;
        Debug.Log($"Projectile Amount tăng: {projectileAmount}");
    }

    // SỬA HÀM TĂNG LUCK: Tăng chỉ số Luck và Gold Multiplier
    public void IncreaseLuck(float amount)
    {
        // Tăng chỉ số Luck (chỉ số này có thể dùng cho các logic khác)
        luck += amount;

        // Tăng hệ số nhân Gold (mỗi lần mua +25% = 0.25f)
        goldMultiplier += 0.25f; // Tăng 0.25 (25%) cho mỗi lần mua
        currentLuckUpgrades++;

        // Đảm bảo hệ số nhân không vượt quá 2.0f (1.0f ban đầu + 4 lần mua * 0.25f)
        goldMultiplier = Mathf.Min(2.0f, goldMultiplier);

        Debug.Log($"Luck tăng: {luck}. Gold Multiplier hiện tại: {goldMultiplier}x");
    }

    // HÀM MỚI: Thêm Gold
    // Hàm này được gọi bởi LevelManager khi kẻ địch chết
    public void AddGold(int baseGold)
    {
        // Áp dụng hệ số nhân (chỉ lấy phần số nguyên)
        // Số tiền nhận được = Gold Cơ Bản * Gold Multiplier
        int finalGold = Mathf.RoundToInt(baseGold * goldMultiplier);

        // Đảm bảo Gold luôn là số nguyên dương khi thêm vào
        totalGold += Mathf.Max(0, finalGold);

        // Cập nhật UI
        if (uiManager != null)
        {
            uiManager.UpdateGoldDisplay(totalGold);
        }
    }

    // ==========================================================
    // ** CHỨC NĂNG CƠ BẢN **
    // ==========================================================

    public void TakeDamage(int damageAmount)
    {
        if (isInvincible) return;
        tookDamageInEvent = true;

        if (hasShield)
        {
            hasShield = false;
            StartCoroutine(IFrameRoutine(1f));
            RefreshHPUI(); // Để cập nhật lại màu Ghost Alpha
            return;
        }

        float damageMultiplier = 1f + tarotDamageTakenMultiplier;
        int finalDamage = Mathf.RoundToInt(damageAmount * damageMultiplier);
        currentPurification -= finalDamage;

        if (currentPurification <= 0) Die();
        else
        {
            if (spriteRenderer != null) StartCoroutine(HitFlashRoutine(hitFlashCount));
            RefreshHPUI();
        }
    }

    // HÀM ĐẶC BIỆT DÙNG CHO THE FOOL (Hồi máu 100%)
    public void FullHeal()
    {
        currentPurification = maxPurification;
        RefreshHPUI();
    }

    public void Heal(int amount)
    {
        currentPurification += amount;
        currentPurification = Mathf.Min(maxPurification, currentPurification);

        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification, maxPurification);
        }

        UpdateGhostAlpha();
    }

    // Thuộc tính kiểm tra xem Player có đang di chuyển không.
    public bool IsMoving
    {
        get
        {
            return Mathf.Abs(rb.linearVelocity.y) > 0.01f;
        }
    }

    // HÀM: Cập nhật độ trong suốt (Alpha) của hồn ma
    private void UpdateGhostAlpha()
    {
        if (spriteRenderer == null) return;

        float healthRatio = (float)currentPurification / maxPurification;
        float minAlpha = 0.3f;
        float maxAlpha = 1.0f;
        float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, healthRatio);

        Color color = spriteRenderer.color;
        color.a = newAlpha;
        spriteRenderer.color = color;
    }

    // Coroutine hiệu ứng nháy khi Player trúng đòn
    IEnumerator HitFlashRoutine(int count)
    {
        Color originalColor = spriteRenderer.color;
        float originalAlpha = originalColor.a;

        for (int i = 0; i < count; i++)
        {
            Color flashColorWithAlpha = hitFlashColor;
            flashColorWithAlpha.a = originalAlpha;

            spriteRenderer.color = flashColorWithAlpha;
            yield return new WaitForSeconds(hitFlashDuration);

            Color currentColorWithAlpha = originalColor;
            currentColorWithAlpha.a = originalAlpha;

            spriteRenderer.color = currentColorWithAlpha;
            yield return new WaitForSeconds(hitFlashDuration);
        }

        UpdateGhostAlpha();
    }

    // SỬA HÀM QUAN TRỌNG: Thay thế Destroy/Set Active bằng gọi GameMenuManager
    private void Die()
    {
        if (isDevilSynergy && devilLives > 0)
        {
            devilLives--;
            currentDevilDmgBonus = Mathf.Max(0, currentDevilDmgBonus - 0.10f); // Giảm 10% bonus
            currentPurification = 1; // Trở lại với 1 HP
            gameObject.SetActive(true); // Đảm bảo player vẫn active
            StartCoroutine(IFrameRoutine(2f)); // Cho 2s bất tử để chạy
            RefreshHPUI();
            Debug.Log("Devil Revive! Lives left: " + devilLives);
            return;
        }

        Debug.Log("Game Over!");
        gameObject.SetActive(false);
        if (gameMenuManager != null) gameMenuManager.GameOver();
    }
    [Header("== Blessing States ==")]
    public GameObject explosionPrefab;
    public GameObject spectralBuddyPrefab; // Nếu bạn làm đệ tử
    private bool hasShield = false;
    private bool isInvincible = false;
    private int killCountForHeal = 0;
    public bool hasExplosionOnDeath = false;
    public bool hasHolyCross = false;
    public static float enemyBulletSpeedMultiplier = 1f;
    public bool isDevilSynergy = false; // Thêm dòng này vào đầu class Player
    public bool isMysticSynergy = false; // Thêm luôn cái này cho Synergy Mystic
    public bool isDivineSynergy = false;
    public bool hasFateDiscount = false;
    // --- HÀM CHÍNH ĐỂ APPLY 12 BLESSING ---
    public void ApplyBlessing(int id)
    {
        switch (id)
        {
            // --- DIVINE ---
            case 1: StartCoroutine(ShieldRegenRoutine()); break;
            case 2: killCountForHeal = 0; break; // Logic nằm trong AddKill()
            case 3: moveSpeed *= 1.2f; transform.localScale *= 0.85f; break;
            case 4: hasHolyCross = true; break;

            // --- DEVIL ---
            case 5: damagePerBullet += 30; DecreaseMaxHP(25); break;
            case 6: hasExplosionOnDeath = true; break; // Logic nằm trong Enemy.Die()
            case 7: projectileAmount += 2; fireRate += 0.1f; break;
            case 8: fireRate *= 0.6f; tarotDamageTakenMultiplier += 0.25f; break;

            // --- MYSTIC ---
            case 9: goldMultiplier += 0.5f; break;
            case 10: Instantiate(spectralBuddyPrefab, transform.position, Quaternion.identity); break;
            case 11: hasFateDiscount = true; break;
            case 12: enemyBulletSpeedMultiplier = 0.75f; break;
        }
    }

    // Logic cho Shield (ID 1)
    IEnumerator ShieldRegenRoutine()
    {
        while (true)
        {
            if (!hasShield)
            {
                yield return new WaitForSeconds(30f);
                hasShield = true;
                spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 1f); // Xanh nhạt
            }
            yield return null;
        }
    }

    // Ghi đè TakeDamage để xử lý Shield và I-Frame
    public void NewTakeDamage(int damage)
    {
        if (isInvincible) return;

        if (hasShield)
        {
            hasShield = false;
            UpdateGhostAlpha(); // Trở lại màu bình thường
            StartCoroutine(IFrameRoutine(1f)); // 1s bất tử
            return;
        }

        // Tính sát thương dựa trên Holy Cross (ID 4)
        int finalDmg = (currentPurification < maxPurification * 0.3f) ? damage : damage;
        // Nếu bạn muốn ID 4 tăng sát thương của BẠN, hãy check trong hàm Shoot()

        TakeDamage(finalDmg); // Gọi hàm TakeDamage gốc
    }

    IEnumerator IFrameRoutine(float duration)
    {
        isInvincible = true;
        float timer = 0;
        while (timer < duration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // Nhấp nháy
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        spriteRenderer.enabled = true;
        isInvincible = false;
    }

    public void AddKill()
    {
        killCountForHeal++;
        if (killCountForHeal >= 15) { Heal(5); killCountForHeal = 0; }

        // Logic riêng cho Devil Synergy
        if (isDevilSynergy)
        {
            devilKillCounter++;
            if (devilKillCounter >= KILLS_FOR_LIFE)
            {
                devilKillCounter = 0;

                if (devilLives < 3)
                {
                    devilLives++;
                    currentDevilDmgBonus = Mathf.Min(0.5f, currentDevilDmgBonus + 0.05f);
                    Debug.Log("Devil Life Gained! Bonus: " + currentDevilDmgBonus);
                }
                else
                {
                    // Đủ mạng rồi thì chỉ cộng bonus đến max 50%
                    currentDevilDmgBonus = Mathf.Min(0.5f, currentDevilDmgBonus + 0.05f);
                }
            }
        }
    }
    public void ActivateDevilSynergy()
    {
        isDevilSynergy = true;
        maxPurification = 25; // Để 1 tim (25HP) cho đỡ quá khó so với 1HP
        currentPurification = 25;
        devilLives = 1; // Cho sẵn 1 mạng khi kích hoạt
        currentDevilDmgBonus = 0.5f; // Bắt đầu với 50% bonus
        RefreshHPUI();
    }
    public void ActivateDivineSynergy()
    {
        maxPurification += 100;
        FullHeal();
        // Giảm thời gian hồi shield (nếu bạn dùng biến float cho cooldown shield)
        // Ví dụ: shieldCooldown = 10f; 
        Debug.Log("DIVINE SYNERGY: Max HP +100 & Fast Shield Regen!");
    }
    public void ActivateMysticSynergy()
    {
        isMysticSynergy = true;
        // Tự động kích hoạt hút tiền (Fate's Magnet)
        Debug.Log("MYSTIC SYNERGY: Shop limit removed & Damage scales with Gold!");
    }
    public void ResetStatsToBase()
    {
        damagePerBullet = 10;
        fireRate = 0.2f;
        projectileAmount = 1;
        luck = 0f;
        goldMultiplier = 1.0f;
        // Lưu ý: Không reset Max HP vì nó liên quan đến vòng Halo UI, 
        // trừ khi bạn muốn làm cực kỳ chi tiết. Tạm thời giữ nguyên HP.
    }
    void SpawnBulletWithAngle(float angle, int dmg, Vector3? customPos = null)
    {
        Vector3 pos = customPos ?? firePoint.position;
        GameObject bulletObj = Instantiate(currentBulletPrefab, pos, Quaternion.Euler(0, 0, angle));
        PlayerBullet bulletScript = bulletObj.GetComponent<PlayerBullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = dmg;

            // NẾU DMG > 20, phóng to viên đạn cho dễ nhìn
            if (dmg > 20) bulletObj.transform.localScale *= 1.5f;
            // NẾU có Devil Synergy, đổi đạn sang màu đỏ
            if (isDevilSynergy) bulletObj.GetComponent<SpriteRenderer>().color = Color.red;
        }
    }
    public void PenaltyHalfHealth()
    {
        currentPurification /= 2;
        if (currentPurification < 1) currentPurification = 1;
        RefreshHPUI();
    }
    // Trong Player.cs
    public IEnumerator TempDamageBuffRoutine(int amount, float duration)
    {
        damagePerBullet += amount;
        Debug.Log($"Temp Buff: +{amount} DMG for {duration}s");

        yield return new WaitForSeconds(duration);

        damagePerBullet -= amount;
        Debug.Log("Temp Buff expired.");
    }
    public void RefreshHPUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification, maxPurification);
        }
        UpdateGhostAlpha();
    }
    [Header("== Pickup Settings ==")]
    public bool isExplosiveRoundsActive = false;
    private int blessingShardCount = 0;

    public void AddBlessingShard()
    {
        blessingShardCount++;
        Debug.Log("Blessing Shards: " + blessingShardCount + "/3");
        if (blessingShardCount >= 3)
        {
            blessingShardCount = 0;
            BlessingMenu.Instance.ShowBlessingSelection();
        }
    }

    public void ActivateExplosiveRounds(float duration)
    {
        // Nếu đang có hiệu ứng thì dừng cái cũ ngay lập tức
        if (explosiveRoutine != null) StopCoroutine(explosiveRoutine);
        // Bắt đầu cái mới (Thời gian sẽ tính lại từ đầu)
        explosiveRoutine = StartCoroutine(ExplosiveRoundsRoutine(duration));
    }

    IEnumerator ExplosiveRoundsRoutine(float duration)
    {
        isExplosiveRoundsActive = true;
        yield return new WaitForSeconds(duration);
        isExplosiveRoundsActive = false;
        explosiveRoutine = null; // Giải phóng biến
    }

    public void ActivatePowerInvincibility(float duration)
    {
        if (invincibilityRoutine != null) StopCoroutine(invincibilityRoutine);
        invincibilityRoutine = StartCoroutine(PowerInvincibilityRoutine(duration));
    }

    IEnumerator PowerInvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float timer = 0;
        while (timer < duration)
        {
            spriteRenderer.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 5, 1));
            timer += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = Color.white;
        UpdateGhostAlpha();
        isInvincible = false;
        invincibilityRoutine = null; // Giải phóng biến
    }
}
