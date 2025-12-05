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

    // THAM CHIẾU MỚI: Game Menu Manager
    private GameMenuManager gameMenuManager;


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
            uiManager.UpdatePurificationMeter(currentPurification);
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
        if (currentBulletPrefab != null && firePoint != null)
        {
            // Tính toán khoảng cách (offset) giữa các viên đạn
            float totalOffset = (projectileAmount - 1) * projectileOffset;
            float startOffset = -totalOffset / 2f;

            // Tính toán sát thương tổng (Damage cơ bản + Damage Tarot)
            // LƯU Ý: Nếu viên đạn có script riêng (PlayerBullet), bạn cần gán giá trị này trong đó.
            int totalDamage = damagePerBullet + tarotBonusDamage;

            for (int i = 0; i < projectileAmount; i++)
            {
                float currentYOffset = startOffset + i * projectileOffset;

                // Vị trí spawn của viên đạn
                Vector3 spawnPosition = firePoint.position;
                spawnPosition.y += currentYOffset;

                // Tạo đạn
                GameObject bulletObj = Instantiate(currentBulletPrefab, spawnPosition, Quaternion.identity);

                // Gán sát thương cho viên đạn 
                // Ví dụ: 
                // PlayerBullet bulletScript = bulletObj.GetComponent<PlayerBullet>();
                // if (bulletScript != null) bulletScript.damage = totalDamage; 
            }

            // 🆕 PHÁT ÂM THANH BẮN ĐẠN!
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerShootSFX();
            }
        }
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
    public void DecreaseMaxHP(int amount)
    {
        // 1. Giảm Max Purification
        maxPurification -= amount;
        maxPurification = Mathf.Max(baseMaxPurification, maxPurification); // Đảm bảo không thấp hơn HP ban đầu

        // 2. Giảm số vòng Halo đã mua nếu cần
        int reducedHalos = amount / PURIFICATION_PER_HALO;
        currentUpgradedHalos = Mathf.Max(0, currentUpgradedHalos - reducedHalos);

        // 3. Đảm bảo máu hiện tại không vượt quá max mới
        currentPurification = Mathf.Min(currentPurification, maxPurification);

        // 4. Cập nhật UI và Alpha
        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification);
        }
        UpdateGhostAlpha();
        Debug.Log($"Max HP giảm: {maxPurification}");
    }


    // Hàm mới: Tăng Max HP hoặc chỉ hồi máu nếu đã đạt giới hạn 8 Halo
    public bool TryIncreaseMaxHP(int amount)
    {
        // Kiểm tra xem đã đạt giới hạn vòng Halo mua thêm chưa (4 vòng)
        if (currentUpgradedHalos >= maxUpgradableHalos)
        {
            // Đã đạt giới hạn Max HP, chỉ hồi máu chứ không tăng Max HP
            Heal(amount);
            Debug.Log("Max HP đã đạt giới hạn (200 HP). Chỉ hồi máu.");
            return false; // Báo hiệu không tăng Max HP
        }

        // Đảm bảo lượng tăng thêm là 25 (hoặc bội số của PURIFICATION_PER_HALO)
        if (amount != PURIFICATION_PER_HALO)
        {
            Debug.LogWarning($"Lượng HP tăng thêm ({amount}) không khớp với giá trị Halo ({PURIFICATION_PER_HALO})!");
        }

        // Nếu chưa đạt giới hạn, tiến hành tăng Max HP
        maxPurification += amount;
        currentUpgradedHalos++;

        // Hồi máu tương ứng với lượng máu tối đa tăng thêm
        currentPurification += amount;
        currentPurification = Mathf.Min(maxPurification, currentPurification);

        // Cần báo cho UIManager cập nhật cả Max HP (số vòng Halo) và Current HP
        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification);
        }

        UpdateGhostAlpha();
        Debug.Log($"Max HP tăng: {maxPurification} (Vòng đã mua: {currentUpgradedHalos}/{maxUpgradableHalos})");
        return true; // Báo hiệu đã tăng Max HP thành công
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
        // TÍNH TOÁN SÁT THƯƠNG THỰC TẾ (Bao gồm hệ số nhân Tarot)
        float damageMultiplier = 1f + tarotDamageTakenMultiplier;
        int finalDamage = Mathf.RoundToInt(damageAmount * damageMultiplier);

        currentPurification -= finalDamage;
        currentPurification = Mathf.Max(0, currentPurification); // Đảm bảo không âm

        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification);
        }

        if (spriteRenderer != null)
        {
            StartCoroutine(HitFlashRoutine(hitFlashCount));
        }

        UpdateGhostAlpha();

        if (currentPurification <= 0)
        {
            Die();
        }
    }

    // HÀM ĐẶC BIỆT DÙNG CHO THE FOOL (Hồi máu 100%)
    public void FullHeal()
    {
        currentPurification = maxPurification;
        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification);
        }
        UpdateGhostAlpha();
        Debug.Log("Player đã được hồi máu đầy đủ.");
    }

    public void Heal(int amount)
    {
        currentPurification += amount;
        currentPurification = Mathf.Min(maxPurification, currentPurification);

        if (uiManager != null)
        {
            uiManager.UpdatePurificationMeter(currentPurification);
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
        Debug.Log("Game Over! Player Purification hết.");

        // 1. Tắt đối tượng Player (để nó không tương tác nữa)
        gameObject.SetActive(false);

        // 2. Gọi Game Over
        if (gameMenuManager != null)
        {
            gameMenuManager.GameOver();
        }
        else
        {
            // Trường hợp lỗi (Không tìm thấy Manager)
            Debug.LogError("GameMenuManager not found! Cannot show Game Over screen.");
        }
    }
}