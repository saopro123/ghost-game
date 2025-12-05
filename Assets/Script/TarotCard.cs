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
            yield return new WaitForSeconds(2.5f);
            EndTarotEvent();
        }
        else
        {
            yield break; // Game dừng ở Game Over/Win
        }
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

    // --- XỬ LÝ HIỆU ỨNG CỦA LÁ BÀI ĐÃ CHỌN ---
    private void ActivateEffect(CardType card)
    {
        // ... (Logic giữ nguyên) ...
        if (player == null || levelManager == null) return;
        switch (card)
        {
            case CardType.TheFool: player.FullHeal(); player.DecreaseMaxHP(foolMaxHPDebuff); break;
            case CardType.TheChariot: player.TryIncreaseMaxHP(chariotMaxHPBuff); break;
            case CardType.Death: levelManager.ActivateGameOver(); break;
            case CardType.TheDevil: player.IncreaseTarotBonusDamage(devilBonusDamage); player.IncreaseTarotDamageTakenMultiplier(devilDamageTakenMultiplier); break;
            case CardType.TheWorld: levelManager.ActivateGameWin(); break;
            case CardType.TheHangedMan: levelManager.SkipToNextBoss(); break;
            case CardType.WheelOfFortune:
                if (Random.value < 0.5f) levelManager.ActivateGameWin();
                else levelManager.ActivateGameOver();
                break;
        }
    }

    // --- KẾT THÚC SỰ KIỆN ---
    private void EndTarotEvent()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        if (astrologerCat != null) astrologerCat.StartExitRoutine();

        if (levelManager != null) levelManager.ResumeGameAfterTarot();

        Destroy(gameObject);
    }
}