using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TarotCard : MonoBehaviour
{
    // CÁC THAM CHIẾU CẦN THIẾT
    private LevelManager levelManager;
    private Player player;
    private AstrologerCat astrologerCat;
    private Collider2D cardCollider; // Tham chiếu đến Collider của chính lá bài

    // ĐỊNH NGHĨA CÁC LÁ BÀI TAROT
    public enum CardType
    {
        TheFool,
        TheChariot,
        TheDevil,
        WheelOfFortune,
        TheHangedMan,
        TheWorld,
        Death
    }

    [System.Serializable]
    public class CardData
    {
        public CardType type;
        [Tooltip("Tỷ trọng để random lá bài này (tổng nên là 100).")]
        public int weight;
        [Tooltip("Sprite hiển thị khi lá bài đã lật.")]
        public Sprite faceSprite;
        public string cardName;
    }

    [Header("== Cài Đặt Lá Bài ==")]
    [Tooltip("Sprite mặc định khi lá bài chưa được lật (Mặt sau).")]
    public Sprite backSprite;
    public List<CardData> allCardsData = new List<CardData>()
    {
        new CardData { type = CardType.TheFool, weight = 15, cardName = "The Fool" },
        new CardData { type = CardType.TheChariot, weight = 20, cardName = "The Chariot" },
        new CardData { type = CardType.TheDevil, weight = 15, cardName = "The Devil" },
        new CardData { type = CardType.TheHangedMan, weight = 15, cardName = "The Hanged Man" },
        new CardData { type = CardType.WheelOfFortune, weight = 10, cardName = "Wheel of Fortune" },
        new CardData { type = CardType.TheWorld, weight = 5, cardName = "The World" },
        new CardData { type = CardType.Death, weight = 20, cardName = "Death" }
    };

    [Header("== Thông Số Hiệu Ứng ==")]
    public int foolMaxHPDebuff = 25;
    public int chariotMaxHPBuff = 25;
    public int devilBonusDamage = 100;
    public float devilDamageTakenMultiplier = 1.0f;

    [Header("== Cài Đặt Hiển Thị ==")]
    public SpriteRenderer cardRenderer;
    public Vector3 displayPosition = Vector3.zero;

    private CardType selectedCardType;
    private CardData selectedCardData;
    private bool isReadyToFlip = false;

    // --- KHỞI TẠO ---
    public void Initialize(LevelManager lm, Player p)
    {
        levelManager = lm;
        player = p;

        // 🆕 THÊM KIỂM TRA TẠI ĐÂY
        cardCollider = GetComponent<Collider2D>();
        astrologerCat = FindAnyObjectByType<AstrologerCat>();

        // Nếu bất kỳ tham chiếu bắt buộc nào bị thiếu, dừng lại ngay lập tức
        if (cardRenderer == null) { Debug.LogError("Lỗi khởi tạo TarotCard: Card Renderer bị thiếu!"); return; }
        if (backSprite == null) { Debug.LogError("Lỗi khởi tạo TarotCard: Back Sprite bị thiếu!"); return; }
        if (cardCollider == null) { Debug.LogError("Lỗi khởi tạo TarotCard: Collider2D bị thiếu trên đối tượng gốc!"); return; }


        StartCoroutine(SetupCardRoutine());
    }

    // 🆕 HÀM MỚI: Bắt Input thô và Raycast thủ công
    void Update()
    {
        if (!isReadyToFlip) return;

        bool inputBegan = false;
        Vector3 inputPosition = Vector3.zero;

        // Bắt Input Click (PC)
        if (Input.GetMouseButtonDown(0))
        {
            inputBegan = true;
            inputPosition = Input.mousePosition;
        }
        // Bắt Input Chạm (Mobile)
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputBegan = true;
            inputPosition = Input.GetTouch(0).position;
        }

        if (inputBegan)
        {
            // Chuyển vị trí click/chạm từ Screen Space sang World Space
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(inputPosition);

            // Thực hiện kiểm tra va chạm (Raycast) tại World Point đó
            if (cardCollider != null && cardCollider.OverlapPoint(worldPoint))
            {
                // Input trúng vào Collider của lá bài
                Debug.Log("Tarot Card Clicked (Manual Raycast)!");
                isReadyToFlip = false;
                StartCoroutine(FlipAndActivateRoutine());
            }
        }
    }


    // --- QUY TRÌNH HIỂN THỊ LÁ BÀI ---
    private IEnumerator SetupCardRoutine()
    {
        yield return new WaitForSeconds(2f); // Chờ Mèo bay vào

        selectedCardData = RandomlySelectCard();
        selectedCardType = selectedCardData.type;

        if (cardRenderer != null && backSprite != null && cardCollider != null)
        {
            transform.position = displayPosition;
            cardRenderer.sprite = backSprite;
            cardRenderer.gameObject.SetActive(true);
            cardCollider.enabled = true; // Kích hoạt Collider để bắt Raycast thủ công
        }
        else
        {
            Debug.LogError("Thiếu CardRenderer, BackSprite, hoặc Collider2D!");
            ActivateEffect(selectedCardType);
            yield break;
        }

        isReadyToFlip = true;
    }

    // 🛑 ĐÃ XÓA: Hàm OnMouseDown() bị lỗi khi Time.timeScale = 0f
    /*
    private void OnMouseDown()
    {
        if (isReadyToFlip)
        {
            isReadyToFlip = false; 
            StartCoroutine(FlipAndActivateRoutine());
        }
    }
    */

    // --- QUY TRÌNH LẬT BÀI VÀ KÍCH HOẠT ---
    private IEnumerator FlipAndActivateRoutine()
    {
        // 1. Lật lá bài (Thay Sprite)
        if (cardRenderer != null)
        {
            if (selectedCardData.faceSprite != null) // 🆕 Bổ sung kiểm tra an toàn
            {
                cardRenderer.sprite = selectedCardData.faceSprite;
                Debug.Log($"Lá bài đã lật: {selectedCardData.cardName}");
            }
            else
            {
                Debug.LogError($"LỖI SPRITE: Face Sprite cho lá {selectedCardData.cardName} bị thiếu!");
                // Giữ nguyên Sprite mặt sau và tiếp tục để tránh dừng game
            }
        }

        yield return new WaitForSeconds(1.5f);

        // 2. Kích hoạt hiệu ứng
        ActivateEffect(selectedCardType);

        // 3. Chờ và Kết thúc sự kiện (trừ khi Game Over/Win)
        if (selectedCardType != CardType.Death && selectedCardType != CardType.TheWorld)
        {
            yield return new WaitForSeconds(5f);
            EndTarotEvent();
        }
        else
        {
            yield break; // Game dừng ở Game Over/Win
        }
        yield return new WaitForSecondsRealtime(1.5f);

        // 2. Kích hoạt hiệu ứng
        ActivateEffect(selectedCardType);

        // 3. CHỈ KẾT THÚC Ở ĐÂY NẾU KHÔNG PHẢI XÚC XẮC VÀ THẮNG/THUA
        if (selectedCardType != CardType.WheelOfFortune &&
            selectedCardType != CardType.TheWorld &&
            selectedCardType != CardType.Death)
        {
            yield return new WaitForSecondsRealtime(5f);
            EndTarotEvent();
        }
        // Nếu là WheelOfFortune: Nó sẽ KHÔNG chạy xuống đây, 
        // mà sẽ đợi hàm RollDiceRoutine tự gọi EndTarotEvent khi quay xong.
    }

    // --- CHỌN LÁ BÀI NGẪU NHIÊN DỰA TRÊN TỶ TRỌNG ---
    private CardData RandomlySelectCard()
    {
        // ... (Logic giữ nguyên) ...
        int totalWeight = 0;
        foreach (var card in allCardsData) totalWeight += card.weight;
        int randomNumber = Random.Range(0, totalWeight);
        int runningTotal = 0;
        foreach (var card in allCardsData)
        {
            runningTotal += card.weight;
            if (randomNumber < runningTotal) return card;
        }
        return allCardsData[0];
    }

    // --- KẾT THÚC SỰ KIỆN ---
    private void EndTarotEvent()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        if (astrologerCat != null) astrologerCat.StartExitRoutine();

        // SỬA DÒNG NÀY: Dùng levelManager (viết thường) và ResumeGameAfterShop
        if (levelManager != null)
        {
            levelManager.ResumeGameAfterShop();
        }

        Destroy(gameObject);
    }
    [Header("== Cài Đặt Xúc Xắc (Wheel of Fortune) ==")]
    public Sprite[] diceFaces; // Kéo 6 sprite mặt xúc xắc vào đây (từ 1 đến 6)
    public float rollDuration = 3f;

    private void ActivateEffect(CardType card)
    {
        if (player == null || levelManager == null) return;

        switch (card)
        {
            case CardType.TheFool:
                player.FullHeal();
                player.DecreaseMaxHP(foolMaxHPDebuff);
                EndTarotEvent();
                break;

            case CardType.TheChariot:
                player.TryIncreaseMaxHP(chariotMaxHPBuff);
                EndTarotEvent();
                break;

            case CardType.TheDevil:
                player.IncreaseTarotBonusDamage(devilBonusDamage);
                player.IncreaseTarotDamageTakenMultiplier(devilDamageTakenMultiplier);
                EndTarotEvent();
                break;

            case CardType.TheHangedMan:
                levelManager.SkipToNextBoss();
                EndTarotEvent();
                break;

            case CardType.TheWorld:
                levelManager.ActivateGameWin();
                break;

            case CardType.Death:
                // MỚI: Mất 3 Shard thay vì thua game
                Debug.Log("TAROT: Death - Losing 3 Stat Shards!");
                if (ShopMenu.Instance != null) ShopMenu.Instance.DeathTarotPenalty();
                EndTarotEvent();
                break;

            case CardType.WheelOfFortune:
                // MỚI: Gọi Coroutine quay xúc xắc
                StartCoroutine(RollDiceRoutine());
                break;
        }
    }

    IEnumerator RollDiceRoutine()
    {
        Debug.Log("DICE: Roll started..."); // Kiểm tra xem coroutine có chạy không
        int finalResult = 0;
        float startTime = Time.unscaledTime; // Dùng thời gian thực không phụ thuộc TimeScale
        float currentInterval = 0.05f;

        // Vòng lặp xoay xúc xắc
        while (Time.unscaledTime - startTime < rollDuration)
        {
            finalResult = Random.Range(1, 7); // Random từ 1 đến 6

            if (cardRenderer != null && diceFaces != null && diceFaces.Length >= 6)
            {
                cardRenderer.sprite = diceFaces[finalResult - 1];
            }
            else
            {
                Debug.LogError("DICE ERROR: Missing Sprite Renderer hoặc Dice Faces chưa gán đủ 6 mặt!");
                yield break; // Thoát nếu thiếu asset
            }

            yield return new WaitForSecondsRealtime(currentInterval);

            // Làm chậm dần tốc độ xoay
            currentInterval = Mathf.Min(currentInterval + 0.03f, 0.5f);
        }

        Debug.Log("DICE: Stopped at number " + finalResult); // Xem kết quả cuối cùng

        // 2. Kích hoạt hiệu ứng
        ApplyDiceEffect(finalResult);

        // 3. Chờ 1.5 giây để người chơi kịp nhìn con số cuối cùng
        Debug.Log("DICE: Waiting to end event...");
        yield return new WaitForSecondsRealtime(5f);
        Debug.Log("DICE: Event Ended. Game Resumed.");
    }

    void ApplyDiceEffect(int result)
    {
        Debug.Log("APPLYING DICE EFFECT: " + result);

        switch (result)
        {
            case 1:
            case 2:
                Debug.Log("Effect: Chaos Shard Reset");
                if (ShopMenu.Instance != null) ShopMenu.Instance.DiceChaosReset();
                else Debug.LogError("ShopMenu Instance is NULL!");
                break;

            case 3:
            case 4:
                Debug.Log("Effect: V-Split Shot");
                player.isVSplitShot = true;
                break;

            case 5:
            case 6:
                Debug.Log("Effect: 100 Gold & Temp DMG");
                player.AddGold(100);
                player.StartCoroutine(player.TempDamageBuffRoutine(5, 150f));
                break;
        }
    }
}