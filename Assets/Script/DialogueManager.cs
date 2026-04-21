using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text nameText;
    private Queue<string> sentences = new Queue<string>();
    private System.Action onComplete;
    private CanvasGroup canvasGroup;
    public float fadeDuration = 0.5f;
    private bool isTyping = false;
    private string currentFullSentence = "";
    private float nextClickDelay = 0.3f; // Khoảng chờ nhỏ giữa các câu để tránh double click
    private float lastClickTime;
    void Awake()
    {
        Instance = this;

        // 1. Tìm CanvasGroup ngay trên chính đối tượng gắn script này (là cái Canvas của bạn)
        canvasGroup = GetComponent<CanvasGroup>();

        // 2. Nếu trên chính nó không có, thì mới tìm ở cái Panel đã gán
        if (canvasGroup == null && dialoguePanel != null)
        {
            canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        }

        // 3. Nếu vẫn không có nữa thì tự thêm mới để không bao giờ bị Null
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Tạm thời ẩn panel đi lúc bắt đầu
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
    void Update()
    {
        if (!dialoguePanel.activeInHierarchy) return;

        // Chỉ nhận click khi bảng đang hiện rõ (Alpha = 1)
        if (canvasGroup != null && canvasGroup.alpha < 0.9f) return;

        bool inputDetected = Input.GetMouseButtonDown(0) ||
                             (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (inputDetected)
        {
            DisplayNext();
        }
    }

    public void StartDialogue(string name, string[] lines, System.Action callback = null)
    {
        // 1. GỌI NHẠC HỘI THOẠI TẠI ĐÂY
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDialogueMusic();
        }

        nameText.text = name;
        onComplete = callback;

        // Reset Alpha và hiện Panel
        canvasGroup.alpha = 1f;
        dialoguePanel.SetActive(true);
        Time.timeScale = 0f;

        sentences.Clear();
        foreach (string line in lines) sentences.Enqueue(line);
        DisplayNext();
    }

    void EndDialogue()
    {
        // Mờ dần panel
        StartCoroutine(FadeOutRoutine());

        // TRẢ LẠI NHẠC NỀN CHÍNH
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMusic();
        }
    }

    IEnumerator FadeOutRoutine()
    {
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        dialoguePanel.SetActive(false);

        // QUAN TRỌNG: Trả lại nhạc và thời gian
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMainMusic();

        if (onComplete != null) onComplete.Invoke();
        else Time.timeScale = 1f; // Chỉ chạy lại game nếu không có sự kiện nối tiếp
    }
    private Coroutine typeRoutine;

    public void DisplayNext()
    {
        // Nếu đang chạy chữ mà người chơi click -> Hiện hết câu luôn
        if (isTyping)
        {
            CompleteSentence();
            return;
        }

        // Chặn việc chuyển câu quá nhanh (tránh bấm nhầm)
        if (Time.unscaledTime - lastClickTime < nextClickDelay) return;

        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        lastClickTime = Time.unscaledTime;
        string nextSentence = sentences.Dequeue();
        currentFullSentence = nextSentence; // Lưu lại câu đầy đủ

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeSentence(nextSentence));
    }
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            // Phát âm thanh nhẹ mỗi khi hiện chữ (nếu muốn)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayNextDialogueSFX();

            yield return new WaitForSecondsRealtime(0.03f);
        }

        isTyping = false;
    }

    // Hàm để hiện toàn bộ câu ngay lập tức
    void CompleteSentence()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        dialogueText.text = currentFullSentence;
        isTyping = false;
        lastClickTime = Time.unscaledTime; // Reset lại thời gian chờ cho câu kế tiếp
    }
}