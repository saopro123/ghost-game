using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static GameMenuManager;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("== References ==")]
    private Player player;
    private GameMenuManager gameMenuManager;
    public UnityEngine.UI.Image backgroundUI;

    [Header("== Enemy Prefabs ==")]
    public GameObject[] regularEnemyPrefabs;
    public GameObject[] bossPrefabs;
    public GameObject frostEnemyPrefab;
    public GameObject shielderPrefab;
    public GameObject goldenCoinPrefab;
    public GameObject catGhostPrefab;

    [Header("== Story Prefabs ==")]
    public GameObject angelNPCPrefab;
    public GameObject superAngelBossPrefab;
    public GameObject heavenGatePrefab;

    [Header("== Spawn Settings ==")]
    public float initialSpawnRate = 1.5f;
    public float maxSpawnRate = 5.0f;
    public float timeToReachMaxRate = 180f;
    public Vector2 spawnYRange = new Vector2(-4f, 4f);
    public float bossInterval = 180f;
    public Vector2 bossStopPosition = new Vector2(8f, 0f);
    public Vector2 catStopPosition = new Vector2(8f, 0f);

    [Header("== Environment Colors ==")]
    public float timeBetweenEvents = 90f;
    public Color colorNormal = Color.white;
    public Color colorYellow = new Color(1f, 0.9f, 0.4f, 0.2f);
    public Color colorRed = new Color(1f, 0.4f, 0.4f, 0.2f);
    public Color colorGreen = new Color(0.4f, 1f, 0.4f, 0.2f);

    [Header("== Rates & Chances ==")]
    [Range(0f, 1f)] public float astrologerChance = 0.1f;
    public GameObject shopCatPrefab;
    public GameObject astrologerCatPrefab;
    public GameObject shopMenuCanvas;
    public GameObject tarotCardPrefab;

    // Internal States
    private float currentSpawnRate;
    private float bossTimer;
    private float eventTimer;
    private float frostEnemyTimer = 60f;
    private float gameTimer = 0f;
    private int currentBossIndex = 0;
    private bool isBossActive = false;
    private bool isInEvent = false;
    private bool isStoryPlaying = false;
    private bool isHandlingTarotEvent = false;
    private ShopCat activeShopCat;
    private Coroutine regularSpawnCoroutine;
    private List<GameObject> preInstantiatedBosses = new List<GameObject>();
    private GameObject[] originalEnemyPrefabs;

    void Awake() { Instance = this; }

    void Start()
    {
        PreInstantiateAllBosses();
        currentSpawnRate = initialSpawnRate;
        bossTimer = bossInterval;
        eventTimer = timeBetweenEvents;

        player = Player.Instance;
        gameMenuManager = GameMenuManager.Instance;

        if (shopMenuCanvas != null) shopMenuCanvas.SetActive(false);

        regularSpawnCoroutine = StartCoroutine(RegularEnemySpawnRoutine());
        StartCoroutine(SpawnRateIncreaseRoutine());

        // Bắt đầu lời dẫn đầu game
        StartCoroutine(PlayIntroStory());
    }

    void Update()
    {
        // 1. Chặn toàn bộ timer nếu game pause hoặc đang diễn cốt truyện/hội thoại
        if (isStoryPlaying || Time.timeScale == 0 || gameMenuManager == null ||
            GameMenuManager.CurrentState != GameMenuManager.GameState.Playing || isHandlingTarotEvent)
            return;

        // 2. Nếu có Shielder Mini-boss, đóng băng tiến trình game
        if (Enemy.activeShielders > 0) return;

        gameTimer += Time.deltaTime;

        // 3. LOGIC ĐẾM GIỜ SỰ KIỆN MÔI TRƯỜNG (PHẢI CÓ ĐOẠN NÀY)
        if (!isBossActive && !isInEvent)
        {
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0)
            {
                eventTimer = timeBetweenEvents; // Reset ngay để chặn gọi trùng
                StartCoroutine(EnvironmentEventRoutine());
            }
        }

        // 4. LOGIC ĐẾM GIỜ BOSS (CHỈ GIỮ 1 ĐOẠN NÀY)
        if (!isBossActive && !isInEvent && currentBossIndex < preInstantiatedBosses.Count)
        {
            bossTimer -= Time.deltaTime;
            if (bossTimer <= 0f)
            {
                bossTimer = bossInterval; // Reset ngay
                StartCoroutine(BossSpawnRoutine());
            }
        }

        // 5. Frost Enemy Timer
        if (!isBossActive && !isInEvent)
        {
            frostEnemyTimer -= Time.deltaTime;
            if (frostEnemyTimer <= 0) { SpawnFrostEnemy(); frostEnemyTimer = 60f; }
        }
    }

    #region Story Logic
    IEnumerator PlayIntroStory()
    {
        isStoryPlaying = true;
        yield return new WaitForSeconds(1f);

        GameObject angel = Instantiate(angelNPCPrefab, new Vector3(8, 0, 0), Quaternion.identity);
        string[] introLines = {"Ngươi đã tỉnh lại rồi sao, linh hồn nhỏ bé?",
            "Ngươi đã chết, nhưng hồ sơ của ngươi thật rắc rối...",
            "Ngươi không đủ tốt để lên Thiên Đàng, cũng chẳng đủ xấu để xuống Địa Ngục.",
            "Ta sẽ cho ngươi một cơ hội. Vượt qua thử thách này để bay lên phía trên.",
            "Đừng nhìn xuống. Ánh sáng sẽ dẫn lối." };
        DialogueManager.Instance.StartDialogue("Thiên Thần", introLines, () => {
            // Gọi hàm mờ dần thay vì Destroy thẳng
            StartCoroutine(FadeOutAndDestroy(angel, 1f));

            isStoryPlaying = false;
            Time.timeScale = 1f; // Đảm bảo game chạy tiếp sau Intro
        });
    }
    void HandleGameEnding()
    {
        isStoryPlaying = true;
        bool isFirstWin = PlayerPrefs.GetInt("BeatenOnce", 0) == 0;
        if (isFirstWin) StartCoroutine(TrollEndingRoutine());
        else StartCoroutine(SuperBossEndingRoutine());
    }

    IEnumerator TrollEndingRoutine()
    {
        GameObject angel = Instantiate(angelNPCPrefab, catStopPosition, Quaternion.identity);
        string[] lines = {
            "Thật ấn tượng! Ngươi thực sự đã vượt qua tất cả.",
            "Ngươi xứng đáng bước qua cổng Thiên Đàng.",
            "...",
            "Ta đùa đấy.",
            "Ngươi thực sự nghĩ một kẻ như ngươi có thể lên đây sao?"
        };
        DialogueManager.Instance.StartDialogue("Thiên Thần", lines, () => {
            PlayerPrefs.SetInt("BeatenOnce", 1);
            player.TakeDamage(9999);
        });
        yield return null;
    }

    IEnumerator SuperBossEndingRoutine()
    {
        GameObject angel = Instantiate(angelNPCPrefab, catStopPosition, Quaternion.identity);
        string[] lines = {
        "Lại là ngươi? Ngươi vẫn chưa bỏ cuộc sao?",
        "Lần này ta sẽ không nương tay.",
        "Hãy đối mặt với cơn thịnh nộ của Thiên Đường!"
    };

        DialogueManager.Instance.StartDialogue("Thiên Thần", lines, () => {
            // 1. Xóa NPC Thiên thần cũ
            Destroy(angel);

            // 2. TẠO BOSS
            GameObject sb = Instantiate(superAngelBossPrefab, catStopPosition, Quaternion.identity);

            // 3. CẬP NHẬT TRẠNG THÁI (QUAN TRỌNG)
            isStoryPlaying = false; // Tắt cờ hội thoại để Update() chạy tiếp
            isBossActive = true;    // Đánh dấu boss đang hoạt động
            Time.timeScale = 1f;    // Trả lại thời gian để Boss có thể tấn công và Player có thể bắn

            // 4. Theo dõi Boss chết
            StartCoroutine(WaitAndTriggerHiddenEnding(sb));

            Debug.Log("Super Boss Spawned and Game Resumed!");
        });
        yield return null;
    }

    IEnumerator WaitAndTriggerHiddenEnding(GameObject bossObj)
    {
        while (bossObj != null && bossObj.activeInHierarchy) yield return null;
        Instantiate(heavenGatePrefab, Vector3.zero, Quaternion.identity);
        yield return new WaitForSeconds(2f);
        GameObject angel = Instantiate(angelNPCPrefab, new Vector3(2, 0, 0), Quaternion.identity);
        string[] lines = { "Hạ gục được cả hộ vệ tối cao sao...", "Cánh cổng kia... đúng là dành cho ngươi...", "Nhưng ta mới là người quyết định ai được vào.", "Mơ đẹp nhé." };
        DialogueManager.Instance.StartDialogue("Thiên Thần", lines, () => { player.TakeDamage(9999); });
    }
    #endregion

    #region Spawning System
    private void PreInstantiateAllBosses()
    {
        foreach (GameObject prefab in bossPrefabs)
        {
            GameObject b = Instantiate(prefab, new Vector3(1000, 1000, 0), Quaternion.identity);
            b.SetActive(false);
            preInstantiatedBosses.Add(b);
        }
    }

    IEnumerator RegularEnemySpawnRoutine()
    {
        while (true)
        {
            if (Time.timeScale == 0) { yield return null; continue; }
            if (!isBossActive && !isHandlingTarotEvent && !isStoryPlaying && GameMenuManager.CurrentState == GameState.Playing)
                SpawnRegularEnemy();
            yield return new WaitForSeconds(1f / currentSpawnRate);
        }
    }

    void SpawnRegularEnemy()
    {
        // Shielder Mini-boss hiếm (Chỉ trong 3p đầu)
        if (gameTimer < 180f && Enemy.activeShielders == 0 && !isBossActive && !isInEvent && Random.value <= 0.001f)
        {
            Instantiate(shielderPrefab, new Vector3(12, 0, 0), Quaternion.identity);
        }

        if (regularEnemyPrefabs.Length == 0) return;
        GameObject p = regularEnemyPrefabs[Random.Range(0, regularEnemyPrefabs.Length)];
        Instantiate(p, new Vector3(12, Random.Range(spawnYRange.x, spawnYRange.y), 0), Quaternion.identity);
    }

    IEnumerator BossSpawnRoutine()
    {
        isBossActive = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBossMusic();

        // 1. Lấy Boss từ Pool
        GameObject bossObj = preInstantiatedBosses[currentBossIndex];

        // KHAI BÁO BIẾN NÀY ĐỂ HẾT LỖI ĐỎ
        Enemy bossEnemyScript = bossObj.GetComponent<Enemy>();

        currentBossIndex++;
        bossObj.transform.position = new Vector3(12, bossStopPosition.y, 0);
        bossObj.SetActive(true);

        if (bossEnemyScript != null)
        {
            bossEnemyScript.enabled = true;
            // Chắc chắn là boss mới chưa bị đánh dấu là đã chết từ game trước
            // (Nếu bạn có dùng Reset logic bên Enemy)
        }

        // 2. Hiệu ứng Boss bay vào
        float t = 0;
        Vector3 start = bossObj.transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime / 2f;
            bossObj.transform.position = Vector3.Lerp(start, bossStopPosition, t);
            yield return null;
        }

        // 3. CHỜ BOSS CHẾT (Dựa vào biến isDead trong script Enemy)
        while (bossEnemyScript != null && !bossEnemyScript.isDead)
        {
            yield return null;
        }

        Debug.Log("BOSS CONFIRMED DEAD! Moving to events...");
        OnEnemyDefeated(true);

        // 4. Tắt object để dọn dẹp
        bossObj.SetActive(false);

        // 5. Kiểm tra Ending hoặc PostBossEvent
        if (currentBossIndex >= preInstantiatedBosses.Count) HandleGameEnding();
        else HandlePostBossEvent();
    }
    #endregion

    #region Environment Events
    IEnumerator EnvironmentEventRoutine()
    {
        isInEvent = true;
        int type = Random.Range(0, 3); // 0: Vàng, 1: Đỏ, 2: Xanh
        Color targetColor = colorNormal;
        float duration = 10f;

        if (type == 0) { targetColor = colorYellow; duration = 30f; }
        else if (type == 1) { targetColor = colorRed; duration = 10f; }
        else { targetColor = colorGreen; duration = 10f; }

        // 1. HIỆU ỨNG BẮT ĐẦU
        yield return StartCoroutine(LerpBackground(targetColor, 2f));
        player.tookDamageInEvent = false;

        float originalRate = currentSpawnRate;
        originalEnemyPrefabs = regularEnemyPrefabs;

        if (type == 0) { regularEnemyPrefabs = new GameObject[] { goldenCoinPrefab }; currentSpawnRate += 0.2f; }
        else if (type == 1) { regularEnemyPrefabs = new GameObject[] { catGhostPrefab }; currentSpawnRate *= 2f; }
        else if (type == 2) { Enemy.globalSpeedMultiplier = 2.5f; currentSpawnRate *= 2f; }

        // 2. VÒNG LẶP THỜI GIAN SỰ KIỆN (Dùng chung while để an toàn)
        float localTimer = 0;
        bool eventAborted = false;

        while (localTimer < duration)
        {
            if (Time.timeScale > 0) // Chỉ đếm khi game đang chạy
            {
                localTimer += Time.deltaTime;
                if (type == 2 && player.tookDamageInEvent)
                {
                    player.PenaltyHalfHealth();
                    if (ShopMenu.Instance != null) ShopMenu.Instance.AddRandomShard(false);
                    eventAborted = true;
                    break;
                }
            }
            yield return null;
        }

        // 3. TRAO THƯỞNG
        if (!eventAborted)
        {
            if (type == 0 && !player.tookDamageInEvent) BlessingMenu.Instance.ShowBlessingSelection();
            else if (type == 1 && player.gameObject.activeInHierarchy) { player.AddGold(50); ShowShopMenu(); }
            else if (type == 2 && !player.tookDamageInEvent) if (ShopMenu.Instance != null) ShopMenu.Instance.AddRandomShard(true);
        }

        // 4. DỌN DẸP
        regularEnemyPrefabs = originalEnemyPrefabs;
        currentSpawnRate = originalRate;
        Enemy.globalSpeedMultiplier = 1f;

        yield return StartCoroutine(LerpBackground(colorNormal, 2f));
        eventTimer = timeBetweenEvents;
        isInEvent = false;
        Debug.Log($"Sự kiện {type} kết thúc.");
    }
    IEnumerator LerpBackground(Color target, float time)
    {
        if (backgroundUI == null) yield break;
        Color start = backgroundUI.color; float t = 0;
        while (t < 1f) { t += Time.deltaTime / time; backgroundUI.color = Color.Lerp(start, target, t); yield return null; }
    }
    #endregion

    #region UI & Helpers
    public void ShowShopMenu()
    {
        if (shopMenuCanvas == null) return;
        shopMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
        if (gameMenuManager != null) gameMenuManager.PauseGameForEvent();
        ShopMenu.Instance.Initialize(null);
    }

    public void HideShopMenu(bool shouldResume = true)
    {
        if (shopMenuCanvas == null) return;
        shopMenuCanvas.SetActive(false);
        if (activeShopCat != null) activeShopCat.StartExit(false);
        if (shouldResume) ResumeGameAfterShop();
    }

    public void ResumeGameAfterShop()
    {
        isBossActive = false; isHandlingTarotEvent = false; isInEvent = false;
        Time.timeScale = 1f;
        if (gameMenuManager != null) gameMenuManager.ResumeGame();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMainMusic();
    }

    public void OnEnemyDefeated(bool boss) { player.AddGold(boss ? 50 : 1); }
    public void SkipToNextBoss() { bossTimer = 0.01f; ResumeGameAfterShop(); }
    public void ActivateGameWin() { HandleGameWin(); }
    public void ActivateGameOver() { if (gameMenuManager != null) gameMenuManager.GameOver(); }
    void HandleGameWin() { if (gameMenuManager != null) gameMenuManager.GameWin(); }
    public void SpawnFrostEnemy() { Instantiate(frostEnemyPrefab, new Vector3(12, Random.Range(spawnYRange.x, spawnYRange.y), 0), Quaternion.identity); }
    public IEnumerator FrostBlastEffect() { /* Logic cũ của bạn */ yield return null; }
    #endregion

    IEnumerator SpawnRateIncreaseRoutine()
    {
        float start = Time.time;
        while (currentSpawnRate < maxSpawnRate)
        {
            if (Time.timeScale > 0 && !isBossActive && !isInEvent)
            {
                currentSpawnRate = Mathf.Lerp(initialSpawnRate, maxSpawnRate, (Time.time - start) / timeToReachMaxRate);
            }
            yield return null;
        }
    }
    // --- HÀM XỬ LÝ SỰ KIỆN SAU KHI BOSS CHẾT (ĐÃ BỔ SUNG) ---
    void HandlePostBossEvent()
    {
        float spawnX = 12f;
        Vector3 initialEventPos = new Vector3(spawnX, catStopPosition.y, 0f);
        float rand = Random.value;

        // 1. SỰ KIỆN TAROT (10%)
        if (rand <= astrologerChance)
        {
            if (astrologerCatPrefab != null && tarotCardPrefab != null)
            {
                Debug.Log("Astrologer Cat incoming!");
                Instantiate(astrologerCatPrefab, initialEventPos, Quaternion.identity);
                GameObject tarotObj = Instantiate(tarotCardPrefab, Vector3.zero, Quaternion.identity);
                TarotCard tarotScript = tarotObj.GetComponent<TarotCard>();
                if (tarotScript != null) tarotScript.Initialize(this, player);
                isHandlingTarotEvent = true;
            }
            else ResumeGameAfterShop();
        }
        // 2. SỰ KIỆN BLESSING (45%)
        else if (rand <= 0.55f)
        {
            Debug.Log("Blessing Event triggered!");
            isHandlingTarotEvent = true;
            if (BlessingMenu.Instance != null)
            {
                BlessingMenu.Instance.ShowBlessingSelection();
            }
            else ResumeGameAfterShop();
        }
        // 3. SỰ KIỆN SHOP (45%)
        else
        {
            if (shopCatPrefab != null)
            {
                Debug.Log("Shop Cat incoming!");
                GameObject catObj = Instantiate(shopCatPrefab, initialEventPos, Quaternion.identity);
                activeShopCat = catObj.GetComponent<ShopCat>();
                ShowShopMenu();
            }
            else ResumeGameAfterShop();
        }
    }
    IEnumerator FadeOutAndDestroy(GameObject target, float duration)
    {
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color startColor = sr.color;
            float timer = 0;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / duration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
        Destroy(target);
    }
}