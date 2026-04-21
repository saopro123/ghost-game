using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class BlessingMenu : MonoBehaviour
{
    public static BlessingMenu Instance;

    [Header("== UI References ==")]
    // blessingCanvas bây giờ chính là gameObject này
    public BlessingUIHandler[] cardSlots;
    public List<BlessingData> allBlessings;

    [Header("== Synergy Tracking ==")]
    public static int divineCount = 0;
    public static int devilCount = 0;
    public static int mysticCount = 0;

    [Header("== Animation Settings ==")]
    public float fadeDuration = 0.5f;
    private CanvasGroup canvasGroup;
    public bool IsShowing { get; private set; } = false;
    private void Awake()
    {
        Instance = this;

        // --- RESET SYNERGY COUNTS ---
        divineCount = 0;
        devilCount = 0;
        mysticCount = 0;

        // Lấy CanvasGroup từ chính nó
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // KHỞI TẠO: Tàng hình thay vì SetActive(false)
        HideMenuImmediately();
    }

    // Hàm ẩn menu lập tức mà không tắt GameObject
    private void HideMenuImmediately()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ShowBlessingSelection()
    {
        // Vì Object luôn Active nên Coroutine này sẽ chạy được!
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        IsShowing = true;
        // 1. Chuẩn bị dữ liệu
        List<BlessingData> shuffled = new List<BlessingData>(allBlessings);
        for (int i = 0; i < shuffled.Count; i++)
        {
            BlessingData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        for (int i = 0; i < 3; i++) cardSlots[i].Setup(shuffled[i]);

        // 2. Dừng game
        Time.timeScale = 0f;
        if (GameMenuManager.Instance != null) GameMenuManager.Instance.PauseGameForEvent();

        // 3. Hiện hình từ từ
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void OnBlessingSelected(BlessingData data)
    {
        Player.Instance.ApplyBlessing(data.id);

        if (data.type == BlessingData.BlessingType.Divine) divineCount++;
        else if (data.type == BlessingData.BlessingType.Devil) devilCount++;
        else if (data.type == BlessingData.BlessingType.Mystic) mysticCount++;

        CheckSynergies();
        StartCoroutine(FadeOutAndCloseRoutine());
    }

    IEnumerator FadeOutAndCloseRoutine()
    {
        IsShowing = false; // Đánh dấu là đã bắt đầu ẩn đi
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        Time.timeScale = 1f;
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ResumeGameAfterShop();
        }
    }

    void CheckSynergies()
    {
        if (devilCount == 3) Player.Instance.ActivateDevilSynergy();
        if (divineCount == 3) Player.Instance.ActivateDivineSynergy();
        if (mysticCount == 3) Player.Instance.ActivateMysticSynergy();
    }
}