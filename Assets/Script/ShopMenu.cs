using UnityEngine;
using UnityEngine.UI; // Dùng cho Text (Legacy)
using System.Collections.Generic;

public class ShopMenu : MonoBehaviour
{
    private Player player;
    private LevelManager levelManager;
    private UIManager uiManager;

    [Header("== Cài Đặt Giá Cơ Bản ==")]
    [Tooltip("Text Component (Legacy) hiển thị số Gold của người chơi")]
    public Text goldDisplay;

    public int basePrice = 70; // Giá mua lần đầu
    public int priceIncreasePerPurchase = 30; // Giá tăng thêm sau mỗi lần mua

    // Dictionary TĨNH (static): Lưu số lần mua của mỗi vật phẩm. 
    private static Dictionary<string, int> purchaseCount = new Dictionary<string, int>()
    {
        {"ATK_UP", 0},
        {"MAX_HP_UP", 0},
        {"AMOUNT_UP", 0},
        {"FIRE_RATE_UP", 0},
        {"LUCK_UP", 0}
    };

    // Giới hạn mua của mỗi vật phẩm
    private Dictionary<string, int> maxPurchases = new Dictionary<string, int>()
    {
        {"ATK_UP", 7},
        {"MAX_HP_UP", 4},
        {"AMOUNT_UP", 5},
        {"FIRE_RATE_UP", 4},
        {"LUCK_UP", 4}
    };


    public void Initialize(ShopCat cat)
    {
        // TÌM CÁC THAM CHIẾU CẦN THIẾT
        levelManager = FindAnyObjectByType<LevelManager>();
        player = FindAnyObjectByType<Player>();
        uiManager = UIManager.Instance; // Lấy Singleton

        if (levelManager == null || player == null || uiManager == null)
        {
            Debug.LogError("LevelManager, Player HOẶC UIManager bị thiếu!");
            // Gọi HideShopMenu để dọn dẹp (nếu có thể)
            if (levelManager != null) levelManager.HideShopMenu();
            else gameObject.SetActive(false);
            return;
        }

        // Hiển thị Gold ngay khi mở shop
        UpdateGoldDisplay();

        // 🛑 ĐÃ BỎ: KHÔNG DỪNG GAME
        Debug.Log("Shop Menu Initialized.");
    }

    // Cập nhật hiển thị Gold (chỉ cho Shop UI)
    private void UpdateGoldDisplay()
    {
        if (goldDisplay != null)
        {
            goldDisplay.text = player.totalGold.ToString();
        }
    }

    // Tính giá hiện tại của một vật phẩm
    private int GetCurrentPrice(string upgradeKey)
    {
        int count = purchaseCount[upgradeKey];
        return basePrice + (count * priceIncreasePerPurchase);
    }

    // Xử lý logic mua chung: Kiểm tra tiền, kiểm tra giới hạn, trừ tiền và tăng số lần mua
    private bool TryPurchase(string upgradeKey, int healAmountIfMaxed)
    {
        int price = GetCurrentPrice(upgradeKey);
        int max = maxPurchases[upgradeKey];
        bool isMaxed = purchaseCount[upgradeKey] >= max;

        // 1. Kiểm tra giới hạn mua (Nếu đã đạt max)
        if (isMaxed)
        {
            Debug.Log($"Đã đạt giới hạn mua {upgradeKey} ({max} lần). Chỉ hồi HP.");

            player.Heal(healAmountIfMaxed);

            // CẬP NHẬT PURIFICATION CHÍNH SAU KHI HEAL
            if (uiManager != null) uiManager.UpdatePurificationMeter(player.currentPurification);

            // Cập nhật lại Gold Display (chỉ để refresh)
            UpdateGoldDisplay();
            return false;
        }

        // 2. Kiểm tra tiền (Chỉ kiểm tra nếu chưa đạt giới hạn)
        if (player.totalGold < price)
        {
            Debug.LogWarning($"Không đủ tiền mua {upgradeKey}. Cần: {price}, Hiện có: {player.totalGold}");
            return false;
        }

        // 3. Mua thành công: Trừ tiền, Tăng số lần mua, Cập nhật UI

        // TRỪ TIỀN
        player.totalGold -= price;

        // TĂNG SỐ LẦN MUA
        purchaseCount[upgradeKey]++;

        // Cập nhật UI Shop
        UpdateGoldDisplay();

        // 🛑 CẬP NHẬT UI CHÍNH! (Sửa lỗi đồng bộ Gold)
        if (uiManager != null)
        {
            uiManager.UpdateGoldDisplay(player.totalGold);
        }

        return true;
    }

    // --- CÁC HÀM GÁN VÀO NÚT BUY ---

    public void OnBuyAttackUp()
    {
        if (TryPurchase("ATK_UP", 25))
        {
            player.IncreaseDamage(5);
        }
    }

    public void OnBuyMaxHpUp()
    {
        if (TryPurchase("MAX_HP_UP", 25))
        {
            player.TryIncreaseMaxHP(25);
        }
    }

    public void OnBuyLuckUp()
    {
        if (TryPurchase("LUCK_UP", 25))
        {
            player.IncreaseLuck(0.1f);
        }
    }

    public void OnBuyFireRateUp()
    {
        if (TryPurchase("FIRE_RATE_UP", 25))
        {
            player.IncreaseFireRate(0.02f);
        }
    }

    public void OnBuyAmountUp()
    {
        if (TryPurchase("AMOUNT_UP", 25))
        {
            player.IncreaseProjectileAmount(1);
        }
    }

    // Gán vào nút Close
    public void OnCloseButtonClicked()
    {
        if (levelManager != null)
        {
            // LevelManager sẽ lo việc hủy Mèo Shop và gọi ResumeGameAfterShop()
            levelManager.HideShopMenu();
        }
        else
        {
            // Trường hợp lỗi (Không tìm thấy LevelManager)
            gameObject.SetActive(false);
            Debug.LogWarning("Không tìm thấy LevelManager. Chỉ ẩn menu.");
        }
    }
}