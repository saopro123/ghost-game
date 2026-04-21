using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopMenu : MonoBehaviour
{
    private Player player;
    private LevelManager levelManager;
    private UIManager uiManager;

    [Header("== Cài Đặt Giá Cơ Bản ==")]
    public Text goldDisplay;
    public int basePrice = 70;
    public int priceIncreasePerPurchase = 30;

    [Header("== Quy Tắc 7 Shard ==")]
    [Tooltip("Text hiển thị số lượng Shard đã mua (ví dụ: Shards: 0/7)")]
    public Text shardCounterText;
    private static int totalShardsBought = 0; // Lưu tổng số shard đã mua toàn game
    private const int MAX_SHARDS = 7;

    [Header("== Mua Blessing ==")]
    public int blessingPrice = 100;
    public Text blessingPriceText; // Hiển thị giá 100 gold lên nút
    public static int bonusShardsCount = 0;

    private static Dictionary<string, int> purchaseCount = new Dictionary<string, int>()
    {
        {"ATK_UP", 0}, {"MAX_HP_UP", 0}, {"AMOUNT_UP", 0}, {"FIRE_RATE_UP", 0}, {"LUCK_UP", 0}
    };

    private Dictionary<string, int> maxPurchases = new Dictionary<string, int>()
    {
        {"ATK_UP", 7}, {"MAX_HP_UP", 4}, {"AMOUNT_UP", 5}, {"FIRE_RATE_UP", 4}, {"LUCK_UP", 4}
    };
    public static ShopMenu Instance;

    void Awake()
    {
        Instance = this;

        // --- RESET DỮ LIỆU KHI NEW GAME ---
        totalShardsBought = 0;
        bonusShardsCount = 0;

        // Reset toàn bộ số lượng mua của từng món về 0
        // Cần tạo một danh sách tạm để tránh lỗi "Collection was modified"
        List<string> keys = new List<string>(purchaseCount.Keys);
        foreach (string key in keys)
        {
            purchaseCount[key] = 0;
        }

        Debug.Log("Shop Data Reset for New Game");
    }
    public void Initialize(ShopCat cat)
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        player = FindAnyObjectByType<Player>();
        uiManager = UIManager.Instance;

        if (levelManager == null || player == null || uiManager == null) return;

        UpdateGoldDisplay();
        UpdateShardDisplay();
        UpdateAllPriceTexts(); // Thêm dòng này để hiện giá ngay khi mở shop

        if (blessingPriceText != null)
        {
            int currentBlessingPrice = player.hasFateDiscount ? Mathf.RoundToInt(blessingPrice * 0.7f) : blessingPrice;
            blessingPriceText.text = currentBlessingPrice.ToString() + "G";
        }
    }

    private void UpdateGoldDisplay()
    {
        if (goldDisplay != null) goldDisplay.text = player.totalGold.ToString();
    }

    private void UpdateShardDisplay()
    {
        if (shardCounterText != null)
            shardCounterText.text = "Shards: " + totalShardsBought + "/" + MAX_SHARDS;
    }

    private int GetCurrentPrice(string upgradeKey)
    {
        // Lấy TỔNG số shard đã mua từ trước đến giờ để tính giá chung
        // Ví dụ: đã mua 1 lần ATK và 1 lần HP -> tổng là 2.
        // Lần mua thứ 3 (bất kỳ món nào) sẽ tính dựa trên số 2 này.
        int globalCount = totalShardsBought;

        int currentPrice = basePrice + (globalCount * priceIncreasePerPurchase);

        // Áp dụng giảm giá 30% nếu có thẻ Fate's Discount (ID 11)
        if (player != null && player.hasFateDiscount)
        {
            return Mathf.RoundToInt(currentPrice * 0.7f);
        }

        return currentPrice;
    }

    private bool TryPurchase(string upgradeKey, int healAmountIfMaxed)
    {
        // 1. Kiểm tra giới hạn TỔNG 7 SHARD trước
        if (totalShardsBought >= MAX_SHARDS && !player.isMysticSynergy)
        {
            Debug.Log("Đã đạt giới hạn 7 Shard toàn game! Chỉ hồi HP.");
            player.Heal(healAmountIfMaxed);
            if (uiManager != null) uiManager.UpdatePurificationMeter(player.currentPurification, player.maxPurification);
            return false;
        }

        int price = GetCurrentPrice(upgradeKey);

        // 2. Kiểm tra giới hạn riêng của từng món
        if (purchaseCount[upgradeKey] >= maxPurchases[upgradeKey])
        {
            player.Heal(healAmountIfMaxed);
            return false;
        }

        // 3. Kiểm tra tiền
        if (player.totalGold < price) return false;

        // THỰC HIỆN MUA
        player.totalGold -= price;
        purchaseCount[upgradeKey]++;
        totalShardsBought++; // Tăng tổng số shard

        UpdateGoldDisplay();
        UpdateShardDisplay();
        UpdateAllPriceTexts();
        if (uiManager != null) uiManager.UpdateGoldDisplay(player.totalGold);

        return true;
    }

    // --- CÁC NÚT BẤM ---
    public void OnBuyAttackUp() { if (TryPurchase("ATK_UP", 25)) player.IncreaseDamage(5); }
    public void OnBuyMaxHpUp() { if (TryPurchase("MAX_HP_UP", 25)) player.TryIncreaseMaxHP(25); }
    public void OnBuyLuckUp() { if (TryPurchase("LUCK_UP", 25)) player.IncreaseLuck(0.1f); }
    public void OnBuyFireRateUp() { if (TryPurchase("FIRE_RATE_UP", 25)) player.IncreaseFireRate(0.02f); }
    public void OnBuyAmountUp() { if (TryPurchase("AMOUNT_UP", 25)) player.IncreaseProjectileAmount(1); }

    // --- NÚT MUA BLESSING MỚI ---
    public void OnBuyBlessing()
    {
        int currentBlessingPrice = player.hasFateDiscount ? Mathf.RoundToInt(blessingPrice * 0.7f) : blessingPrice;

        if (player.totalGold >= currentBlessingPrice)
        {
            player.totalGold -= currentBlessingPrice;
            UpdateGoldDisplay();

            // 1. Tắt UI Shop ngay lập tức
            // 2. Gọi HideShopMenu và bảo nó KHÔNG ĐƯỢC Resume game (false)
            levelManager.HideShopMenu(false);

            // 3. Kiểm tra an toàn trước khi gọi Blessing
            if (BlessingMenu.Instance != null)
            {
                BlessingMenu.Instance.ShowBlessingSelection();
            }
            else
            {
                Debug.LogError("BlessingMenu Instance is NULL! Check if the object is active in Hierarchy.");
                // Nếu lỗi thì phải cứu vãn bằng cách resume game
                levelManager.ResumeGameAfterShop();
            }
        }
    }

    public void OnCloseButtonClicked()
    {
        if (levelManager != null) levelManager.HideShopMenu();
    }
    // Hàm tặng Shard ngẫu nhiên (Dùng cho sự kiện Xanh lá)
    public void AddRandomShard(bool isBonus)
    {
        // Lấy danh sách các loại nâng cấp
        List<string> keys = new List<string>(purchaseCount.Keys);
        string randomKey = keys[Random.Range(0, keys.Count)];

        // Tăng chỉ số trong dữ liệu Shop
        purchaseCount[randomKey]++;

        if (!isBonus) totalShardsBought++;

        // Áp dụng chỉ số đó vào Player ngay lập tức
        if (player == null) player = Player.Instance;

        if (randomKey == "ATK_UP") player.IncreaseDamage(5);
        else if (randomKey == "MAX_HP_UP") player.TryIncreaseMaxHP(25);
        else if (randomKey == "LUCK_UP") player.IncreaseLuck(0.1f);
        else if (randomKey == "FIRE_RATE_UP") player.IncreaseFireRate(0.02f);
        else if (randomKey == "AMOUNT_UP") player.IncreaseProjectileAmount(1);

        UpdateShardDisplay();
    }

    // Hàm trung gian để áp dụng chỉ số từ Key sang Player
    public void ApplyStatToPlayer(string upgradeKey)
    {
        // Đảm bảo player không null
        if (player == null) player = Player.Instance;
        if (player == null) return;

        switch (upgradeKey)
        {
            case "ATK_UP": player.IncreaseDamage(5); break;
            case "MAX_HP_UP": player.TryIncreaseMaxHP(25); break;
            case "LUCK_UP": player.IncreaseLuck(0.1f); break;
            case "FIRE_RATE_UP": player.IncreaseFireRate(0.02f); break;
            case "AMOUNT_UP": player.IncreaseProjectileAmount(1); break;
        }
    }

    // Dùng cho Xúc xắc 1 & 2 (Wheel of Fortune)
    public void DiceChaosReset()
    {
        // 1. Tính tổng shard hiện có và cộng thêm 2
        int totalToDistribute = totalShardsBought + 2;
        totalToDistribute = Mathf.Min(totalToDistribute, 7); // Giới hạn 7 (hoặc tùy bạn)

        // 2. Reset Player về chỉ số gốc (để tính lại từ đầu, tránh cộng dồn lỗi)
        player.ResetStatsToBase();

        // 3. Xóa sạch dictionary shard hiện tại
        List<string> keys = new List<string>(purchaseCount.Keys);
        foreach (string k in keys) purchaseCount[k] = 0;

        // 4. Chia lại ngẫu nhiên
        for (int i = 0; i < totalToDistribute; i++)
        {
            string rKey = keys[Random.Range(0, keys.Count)];
            purchaseCount[rKey]++;
            ApplyStatToPlayer(rKey); // Áp dụng chỉ số mới
        }

        totalShardsBought = totalToDistribute;
        UpdateShardDisplay();
    }

    public void DeathTarotPenalty()
    {
        // Nếu chưa mua cái gì thì không trừ, tránh lỗi logic
        if (totalShardsBought <= 0)
        {
            Debug.Log("No shards to lose!");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            if (totalShardsBought <= 0) break;

            List<string> ownedKeys = new List<string>();
            foreach (var pair in purchaseCount)
            {
                if (pair.Value > 0) ownedKeys.Add(pair.Key);
            }

            if (ownedKeys.Count > 0)
            {
                string rKey = ownedKeys[Random.Range(0, ownedKeys.Count)];
                purchaseCount[rKey]--;
                totalShardsBought--;
            }
        }

        RefreshPlayerStats();
    }

    private void RefreshPlayerStats()
    {
        // Đảm bảo player không null trước khi dùng
        if (player == null) player = Player.Instance;
        if (player == null) return; // Nếu vẫn null thì thoát để tránh crash

        player.ResetStatsToBase();
        foreach (var pair in purchaseCount)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                ApplyStatToPlayer(pair.Key);
            }
        }
    }
    [Header("== Price Display Text (Legacy Text) ==")]
    public Text atkPriceText;
    public Text hpPriceText;
    public Text luckPriceText;
    public Text fireRatePriceText;
    public Text amountPriceText;

    // Hàm cập nhật toàn bộ chữ hiển thị giá trên UI
    private void UpdateAllPriceTexts()
    {
        atkPriceText.text = GetPriceDisplay("ATK_UP");
        hpPriceText.text = GetPriceDisplay("MAX_HP_UP");
        luckPriceText.text = GetPriceDisplay("LUCK_UP");
        fireRatePriceText.text = GetPriceDisplay("FIRE_RATE_UP");
        amountPriceText.text = GetPriceDisplay("AMOUNT_UP");
    }
    private string GetPriceDisplay(string key)
    {
        // Kiểm tra nếu đã đạt giới hạn mua của món đó
        if (purchaseCount[key] >= maxPurchases[key])
        {
            return "MAX";
        }

        // Nếu chưa đạt giới hạn, lấy giá hiện tại
        return GetCurrentPrice(key).ToString() + "G";
    }
}