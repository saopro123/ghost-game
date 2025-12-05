using UnityEngine;
using System.Collections;

public class ShopCat : MonoBehaviour
{
    [Header("== Cài Đặt Di Chuyển ==")]
    public Vector2 stopPosition = new Vector2(8f, 0f);
    public float moveDuration = 2f;

    // Tham chiếu Player (Giữ để ShopMenu có thể tìm Player thông qua ShopCat nếu cần thiết)
    [HideInInspector] public Player player;

    // 🆕 THAM CHIẾU LEVEL MANAGER
    private LevelManager levelManager;

    void Start()
    {
        // Khóa tất cả ràng buộc
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Tìm Player và LevelManager
        player = FindAnyObjectByType<Player>();
        levelManager = FindAnyObjectByType<LevelManager>(); // 🆕 Tìm LevelManager

        // Bắt đầu di chuyển vào
        StartCoroutine(EntryRoutine());
    }

    IEnumerator EntryRoutine()
    {
        // Di chuyển Mèo đến vị trí dừng
        Transform catTransform = transform;
        float timer = 0f;

        Vector3 startPos = catTransform.position;
        Vector3 targetPos = stopPosition;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            catTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        catTransform.position = targetPos;

        Debug.Log("Shop Cat arrived at its position.");
        // Mèo Shop đã dừng lại, LevelManager đã mở Menu
    }

    // HÀM MỚI: Được gọi bởi LevelManager khi người chơi đóng shop
    public void StartExit()
    {
        StartCoroutine(ExitRoutine());
    }

    // ShopCat.cs

    IEnumerator ExitRoutine()
    {
        Debug.Log("Shop Cat is leaving.");

        // Di chuyển ra ngoài màn hình (Giữ nguyên logic di chuyển)
        Transform catTransform = transform;
        float timer = 0f;
        // ... (logic tính exitPos và exitDuration) ...
        float exitDuration = moveDuration * 1.5f;
        Vector3 startPos = catTransform.position;
        Vector3 exitPos = new Vector3(-12f, startPos.y, startPos.z);


        while (timer < exitDuration)
        {
            timer += Time.deltaTime;
            float t = timer / exitDuration;
            catTransform.position = Vector3.Lerp(startPos, exitPos, t);
            yield return null;
        }
         if (levelManager != null)
          {
             levelManager.ResumeGameAfterShop(); 
             Debug.Log("Shop Cat called ResumeGameAfterShop. Spawn should restart.");
          }

        // Phá hủy Mèo Shop sau khi rời khỏi màn hình
        Destroy(gameObject);
    }
    public void PurchaseUpgrade(string upgradeType)
    {
        // Logic này đã được chuyển sang ShopMenu.cs và Player.cs
        Debug.LogWarning("ShopCat.PurchaseUpgrade đang bị gọi, nên gọi trực tiếp Player từ ShopMenu.");
    }
}