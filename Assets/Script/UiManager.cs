using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    // THÊM SINGLETON STATIC
    public static UIManager Instance;

    [Header("Halo Icons (8 Vòng)")]
    [Tooltip("Gán 8 Image Component của Halo vào đây theo thứ tự 1 đến 8")]
    public Image[] haloIcons; // Phải gán 8 phần tử trong Inspector

    [Header("Sprites")]
    [Tooltip("Sprite khi Halo đầy (Full Purification)")]
    public Sprite fullHaloSprite;
    [Tooltip("Sprite khi Halo rỗng (Mất Purification)")]
    public Sprite emptyHaloSprite;

    [Header("== Gold Display ==")]
    [Tooltip("Gán Text Component (Legacy) để hiển thị Gold")]
    public Text goldText; // Sử dụng UnityEngine.UI.Text

    // Giá trị máu đại diện cho 1 icon (25 HP)
    private const int PURIFICATION_PER_ICON = 25;
    private const int BASE_HALO_COUNT = 5; // Bắt đầu với 4 vòng
    private Player player;

    void Awake()
    {
        // KHỞI TẠO SINGLETON
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        player = FindAnyObjectByType<Player>();
        if (player == null)
        {
            Debug.LogError("Player object not found in the scene!");
        }

        // Đảm bảo mảng đủ 8 phần tử (Logic kiểm tra giữ nguyên)

        // Khởi tạo trạng thái ban đầu (hiển thị 4 vòng, 4 vòng còn lại ẩn)
        InitializeHaloDisplay();

        // Cập nhật UI ngay khi bắt đầu game
        if (player != null)
        {
            UpdatePurificationMeter(player.currentPurification);
            UpdateGoldDisplay(player.totalGold);
        }
    }

    // Hàm khởi tạo: Đảm bảo 4 vòng đầu hiển thị, 4 vòng sau ẩn đi (hoặc mờ đi)
    void InitializeHaloDisplay()
    {
        // 4 vòng đầu luôn hiển thị
        for (int i = 0; i < BASE_HALO_COUNT; i++)
        {
            if (i < haloIcons.Length)
            {
                haloIcons[i].gameObject.SetActive(true);
            }
        }

        // 4 vòng sau ẩn đi (chỉ active khi được mua)
        for (int i = BASE_HALO_COUNT; i < haloIcons.Length; i++)
        {
            haloIcons[i].gameObject.SetActive(false); // Ẩn hoàn toàn 4 icon chưa mua
        }
    }

    // Hàm public được gọi bởi Player mỗi khi máu thay đổi (bao gồm cả tăng Max HP)
    public void UpdatePurificationMeter(int currentHealth)
    {
        if (player == null) return;

        // 1. Tính toán tổng số vòng Halo đang được hiển thị
        int totalDisplayedHalos = BASE_HALO_COUNT + player.currentUpgradedHalos;

        // Đảm bảo không vượt quá giới hạn mảng
        totalDisplayedHalos = Mathf.Min(totalDisplayedHalos, haloIcons.Length);

        // 2. Kích hoạt/Ẩn các Halo Icon dựa trên Max HP đã mua
        for (int i = 0; i < haloIcons.Length; i++)
        {
            if (haloIcons[i] != null)
            {
                if (i < totalDisplayedHalos)
                {
                    haloIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    haloIcons[i].gameObject.SetActive(false);
                }
            }
        }

        // 3. Cập nhật Sprite (Đầy/Rỗng)
        int fullIcons = currentHealth / PURIFICATION_PER_ICON;

        for (int i = 0; i < totalDisplayedHalos; i++)
        {
            if (i < fullIcons)
            {
                // Icon đang đầy (Full Halo)
                haloIcons[i].sprite = fullHaloSprite;
            }
            else
            {
                // Icon đang rỗng (Empty Halo)
                haloIcons[i].sprite = emptyHaloSprite;
            }
        }
    }

    // ==========================================================
    // ** CHỨC NĂNG: HIỂN THỊ GOLD **
    // ==========================================================

    // Hàm public được gọi bởi Player/Shop mỗi khi nhận/trừ Gold
    public void UpdateGoldDisplay(int currentGold)
    {
        if (goldText != null)
        {
            // Hiển thị số Gold (luôn là số nguyên)
            goldText.text = currentGold.ToString();
        }
    }
}