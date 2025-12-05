using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("== Cài Đặt Quái Thường ==")]
    [Tooltip("Danh sách Prefab của các loại quái thường.")]
    public GameObject[] regularEnemyPrefabs;
    [Tooltip("Tỷ lệ spawn quái thường BAN ĐẦU (quái/giây).")]
    public float initialSpawnRate = 1.5f; // Tỷ lệ spawn ban đầu

    // 🆕 CÀI ĐẶT TĂNG TỶ LỆ SPAWN
    [Header("== Cài Đặt Tăng Tỷ Lệ Spawn ==")]
    [Tooltip("Tỷ lệ spawn TỐI ĐA có thể đạt được.")]
    public float maxSpawnRate = 5.0f;
    [Tooltip("Thời gian (giây) để tỷ lệ spawn đạt mức tối đa.")]
    public float timeToReachMaxRate = 180f; // Đạt max rate sau 3 phút

    [Header("== Cài Đặt Boss Tuần Tự ==")]
    [Tooltip("Danh sách Prefab của các Boss, sẽ xuất hiện tuần tự.")]
    public GameObject[] bossPrefabs;
    [Tooltip("Thời gian giữa các lần spawn Boss (giây).")]
    public float bossInterval = 180f;
    [Tooltip("Vị trí Boss sẽ bay đến và dừng lại (Tọa độ X, Y trong màn hình).")]
    public Vector2 bossStopPosition = new Vector2(8f, 0f);

    // BIẾN MỚI CHO TAROT
    [Tooltip("Vị trí Mèo Tarot sẽ dừng lại (thường giống Boss).")]
    public Vector2 catStopPosition = new Vector2(8f, 0f);

    [Header("== Cài Đặt Sự Kiện Sau Boss ==")]
    [Tooltip("Prefab Mèo Bán Hàng.")]
    public GameObject shopCatPrefab;
    [Tooltip("Prefab Mèo Chiêm Tinh.")]
    public GameObject astrologerCatPrefab;
    [Tooltip("Canvas Shop Menu (Đã có sẵn trong Scene và bị Disable).")]
    public GameObject shopMenuCanvas;
    [Tooltip("Tỉ lệ Mèo Chiêm Tinh xuất hiện (0.1 = 10%).")]
    [Range(0f, 1f)]
    public float astrologerChance = 0.1f;

    // PREFAB TAROT CARD
    [Header("== Cài Đặt Tarot ==")]
    [Tooltip("Prefab của Tarot Card Manager/Object (dùng để sinh ngẫu nhiên và hiển thị lá bài).")]
    public GameObject tarotCardPrefab;

    [Header("== Cài Đặt Khu Vực Spawn ==")]
    [Tooltip("Khoảng Y tối thiểu và tối đa để spawn quái thường.")]
    public Vector2 spawnYRange = new Vector2(-5f, 5f);

    // ==========================================================
    // ** BIẾN TỐI ƯU HÓA **
    // ==========================================================
    private List<GameObject> preInstantiatedBosses = new List<GameObject>();
    // ==========================================================

    private float bossTimer;
    private bool isBossActive = false;
    private Coroutine regularSpawnCoroutine;
    private Coroutine spawnRateIncreaseCoroutine;
    private float currentSpawnRate;
    private int currentBossIndex = 0;

    // BIẾN TRẠNG THÁI
    private bool isHandlingTarotEvent = false;

    private ShopCat activeShopCat;
    private Player player;
    private GameMenuManager gameMenuManager;


    void Start()
    {
        // 1. TẠO TRƯỚC TẤT CẢ BOSS (PRE-INSTANTIATION)
        PreInstantiateAllBosses();

        // 2. KHỞI TẠO CƠ BẢN
        currentSpawnRate = initialSpawnRate;
        bossTimer = bossInterval;

        // 3. BẮT ĐẦU ROUTINE GAME
        regularSpawnCoroutine = StartCoroutine(RegularEnemySpawnRoutine());
        spawnRateIncreaseCoroutine = StartCoroutine(SpawnRateIncreaseRoutine());

        // 4. THIẾT LẬP CÁC THAM CHIẾU
        player = FindAnyObjectByType<Player>();
        if (player == null) Debug.LogError("Player object not found! Gold system will fail.");

        gameMenuManager = GameMenuManager.Instance;
        if (gameMenuManager == null) Debug.LogError("GameMenuManager instance not found!");

        if (shopMenuCanvas != null)
        {
            shopMenuCanvas.SetActive(false);
        }
    }

    // ==========================================================
    // ** LOGIC TẠO TRƯỚC (PRE-INSTANTIATION) **
    // Tác vụ nặng Instantiation được thực hiện ở đây (chỉ 1 lần lúc tải Scene)
    // ==========================================================
    private void PreInstantiateAllBosses()
    {
        if (bossPrefabs.Length == 0) return;

        Debug.Log($"Pre-instantiating {bossPrefabs.Length} bosses...");

        Vector3 offScreenPos = new Vector3(1000f, 1000f, 0f); // Vị trí ẩn

        foreach (GameObject bossPrefab in bossPrefabs)
        {
            if (bossPrefab != null)
            {
                // Instantiate boss và đặt nó ở vị trí ẩn
                GameObject bossObj = Instantiate(bossPrefab, offScreenPos, Quaternion.identity);

                // Ẩn đối tượng boss hoàn toàn
                bossObj.SetActive(false);

                // Đảm bảo script Enemy được tắt để tránh lỗi null reference khi không active
                Enemy bossEnemyScript = bossObj.GetComponent<Enemy>();
                if (bossEnemyScript != null)
                {
                    bossEnemyScript.enabled = false;
                }

                preInstantiatedBosses.Add(bossObj);
            }
            else
            {
                Debug.LogError("Boss Prefab bị thiếu trong danh sách!");
            }
        }

        Debug.Log("Pre-instantiation completed. Bosses are ready in pool.");
    }


    void Update()
    {
        if (gameMenuManager == null || GameMenuManager.CurrentState != GameMenuManager.GameState.Playing || isHandlingTarotEvent)
        {
            return;
        }

        // Chỉ chạy timer nếu chưa có Boss hoạt động VÀ chưa hết danh sách Boss
        if (isBossActive || currentBossIndex >= preInstantiatedBosses.Count) return;

        bossTimer -= Time.deltaTime;

        if (bossTimer <= 0f)
        {
            StartCoroutine(BossSpawnRoutine());
            bossTimer = bossInterval; // Reset timer cho Boss tiếp theo
        }
    }

    // ==========================================================
    // ** LOGIC TĂNG DẦN TỶ LỆ SPAWN (Độ Khó) **
    // (Giữ nguyên)
    // ==========================================================
    IEnumerator SpawnRateIncreaseRoutine()
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (currentSpawnRate < maxSpawnRate)
        {
            if (GameMenuManager.CurrentState == GameMenuManager.GameState.Playing && !isBossActive && !isHandlingTarotEvent)
            {
                elapsedTime = Time.time - startTime;

                float t = Mathf.Clamp01(elapsedTime / timeToReachMaxRate);
                currentSpawnRate = Mathf.Lerp(initialSpawnRate, maxSpawnRate, t);
            }
            yield return null;
        }

        currentSpawnRate = maxSpawnRate;
    }


    // --- LOGIC SPAWN QUÁI THƯỜNG ---
    // (Giữ nguyên)
    IEnumerator RegularEnemySpawnRoutine()
    {
        while (true)
        {
            if (!isBossActive && !isHandlingTarotEvent && GameMenuManager.CurrentState == GameMenuManager.GameState.Playing)
            {
                SpawnRegularEnemy();
            }

            yield return new WaitForSeconds(1f / currentSpawnRate);
        }
    }

    void SpawnRegularEnemy()
    {
        if (regularEnemyPrefabs.Length == 0) return;

        GameObject enemyToSpawn = regularEnemyPrefabs[Random.Range(0, regularEnemyPrefabs.Length)];

        float spawnX = 12f;
        float spawnY = Random.Range(spawnYRange.x, spawnYRange.y);

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
    }

    // --- LOGIC SPAWN BOSS TUẦN TỰ (ĐÃ TỐI ƯU) ---
    IEnumerator BossSpawnRoutine()
    {
        if (currentBossIndex >= preInstantiatedBosses.Count) // Kiểm tra Boss trong Pool
        {
            Debug.Log("Đã hoàn thành tất cả Boss trong danh sách!");
            HandleGameWin();
            yield break;
        }

        // 🆕 LẤY BOSS TỪ POOL (KHÔNG CẦN INSTANTIATE)
        GameObject bossObj = preInstantiatedBosses[currentBossIndex];

        // Cập nhật Index cho Boss tiếp theo
        currentBossIndex++;

        if (regularSpawnCoroutine != null)
        {
            StopCoroutine(regularSpawnCoroutine);
        }
        isBossActive = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossMusic();
        }

        Debug.Log($"BOSS ALERT! Activating Boss Index {currentBossIndex - 1}: {bossObj.name}.");

        float spawnX = 12f;
        Vector3 initialBossPosition = new Vector3(spawnX, bossStopPosition.y, 0f);

        // 🆕 KÍCH HOẠT BOSS VÀ VỊ TRÍ BAN ĐẦU
        bossObj.transform.position = initialBossPosition;
        bossObj.SetActive(true);
        Enemy bossEnemyScript = bossObj.GetComponent<Enemy>();
        if (bossEnemyScript != null)
        {
            bossEnemyScript.enabled = true; // Bật lại script Enemy
        }

        Transform bossTransform = bossObj.transform;
        float moveDuration = 2f;
        float timer = 0f;
        Vector3 startPos = initialBossPosition;
        Vector3 targetPos = bossStopPosition;

        // Boss fly-in animation
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            bossTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        bossTransform.position = targetPos;

        Debug.Log("Boss reached its stop position. Attacking starts now.");

        // Chờ Boss bị tiêu diệt
        while (bossEnemyScript != null && bossObj.activeInHierarchy) // Chờ đợi boss bị hủy hoặc bị tắt
        {
            yield return null;
        }

        Debug.Log("BOSS DEFEATED: LevelManager manually granting 50 Gold.");
        OnEnemyDefeated(true);

        // 🆕 Sau khi boss chết, chúng ta chỉ cần set nó inactive, không cần Destroy
        // Mặc dù trong trường hợp Boss, Destroy cũng không gây khựng vì Instantiate đã được trả phí,
        // nhưng cách này sạch sẽ hơn nếu sau này bạn muốn giữ Boss lại để hiển thị thống kê.
        if (bossObj != null)
        {
            bossObj.SetActive(false);
            // Thiết lập lại vị trí ẩn
            bossObj.transform.position = new Vector3(1000f, 1000f, 0f);
            if (bossEnemyScript != null) bossEnemyScript.enabled = false;
        }

        HandlePostBossEvent();
    }

    // --- XỬ LÝ SỰ KIỆN SAU KHI BOSS CHẾT ---
    // (Giữ nguyên)
    void HandlePostBossEvent()
    {
        float spawnX = 12f;
        Vector3 initialEventPos = new Vector3(spawnX, catStopPosition.y, 0f);

        if (Random.value <= astrologerChance)
        {
            if (astrologerCatPrefab != null && tarotCardPrefab != null)
            {
                Debug.Log("Astrologer Cat is coming (10%)! Starting Tarot Event.");

                Instantiate(astrologerCatPrefab, initialEventPos, Quaternion.identity);

                GameObject tarotObj = Instantiate(tarotCardPrefab, Vector3.zero, Quaternion.identity);
                TarotCard tarotScript = tarotObj.GetComponent<TarotCard>();

                if (tarotScript != null)
                {
                    tarotScript.Initialize(this, player);
                }
                else
                {
                    Debug.LogError("TarotCard script not found on prefab!");
                }

                isHandlingTarotEvent = true;
            }
            else
            {
                Debug.LogWarning("Astrologer Cat HOẶC Tarot Card Prefab bị thiếu! Tiếp tục game.");
                ResumeGameAfterShop();
            }
        }
        else
        {
            if (shopCatPrefab != null)
            {
                Debug.Log("Shop Cat is coming (90%)! Showing Shop Menu.");
                GameObject catObj = Instantiate(shopCatPrefab, initialEventPos, Quaternion.identity);

                activeShopCat = catObj.GetComponent<ShopCat>();

                ShowShopMenu();
            }
            else
            {
                Debug.LogWarning("Shop Cat Prefab bị thiếu! Tiếp tục game.");
                ResumeGameAfterShop();
            }
        }
    }

    public void OnEnemyDefeated(bool isBoss)
    {
        if (player == null) return;

        int baseGoldReward = isBoss ? 50 : 1;

        player.AddGold(baseGoldReward);
    }

    // --- HÀM GỌI LẠI SAU KHI SỰ KIỆN KẾT THÚC ---
    // (Giữ nguyên)
    public void ResumeGameAfterShop()
    {
        isBossActive = false;
        isHandlingTarotEvent = false;

        activeShopCat = null;

        Debug.Log("Sự kiện kết thúc. Resuming regular enemy spawn.");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMusic();
        }

        if (currentBossIndex < preInstantiatedBosses.Count)
        {
            regularSpawnCoroutine = StartCoroutine(RegularEnemySpawnRoutine());

            if (gameMenuManager != null)
            {
                gameMenuManager.ResumeGame();
            }
        }
        else
        {
            Debug.Log("Tất cả Boss đã bị đánh bại. Cấp độ hoàn thành.");
        }
    }

    // ==========================================================
    // ** HÀM HỖ TRỢ CHO TAROT (Gọi từ TarotCard.cs) **
    // (Giữ nguyên)
    // ==========================================================

    public void SkipToNextBoss()
    {
        Debug.Log("TAROT: The Hanged Man - Bỏ qua thời gian chờ, chuẩn bị Boss tiếp theo ngay lập tức.");
        bossTimer = 0.01f;
        ResumeGameAfterTarot();
    }

    public void ActivateGameWin()
    {
        Debug.Log("TAROT: The World - Kích hoạt chiến thắng ngay lập lập tức.");
        if (regularSpawnCoroutine != null) StopCoroutine(regularSpawnCoroutine);
        HandleGameWin();
    }

    public void ActivateGameOver()
    {
        Debug.Log("TAROT: Death - Kích hoạt thua cuộc ngay lập tức.");
        if (gameMenuManager != null)
        {
            gameMenuManager.GameOver();
        }
    }

    public void ResumeGameAfterTarot()
    {
        ResumeGameAfterShop();
    }

    // ==========================================================
    // ** LOGIC ẨN/HIỆN SHOP MENU (ĐÃ BỎ PAUSE/RESUME) **
    // (Giữ nguyên)
    // ==========================================================
    public void ShowShopMenu()
    {
        if (shopMenuCanvas != null)
        {
            shopMenuCanvas.SetActive(true);

            ShopMenu menuScript = shopMenuCanvas.GetComponent<ShopMenu>();
            if (menuScript != null)
            {
                menuScript.Initialize(null);
            }
        }
    }

    public void HideShopMenu()
    {
        if (shopMenuCanvas != null)
        {
            shopMenuCanvas.SetActive(false);

            if (activeShopCat != null)
            {
                activeShopCat.StartExit();
            }
            else
            {
                ResumeGameAfterShop();
            }
        }
    }

    void HandleGameWin()
    {
        Debug.Log("TẤT CẢ BOSS ĐÃ BỊ ĐÁNH BẠI! GAME WIN!");

        if (regularSpawnCoroutine != null) StopCoroutine(regularSpawnCoroutine);

        if (gameMenuManager != null)
        {
            gameMenuManager.GameWin();
        }
    }
}