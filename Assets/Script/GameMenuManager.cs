using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices; // THÊM THƯ VIỆN NÀY

public class GameMenuManager : MonoBehaviour
{
    // ==========================================================
    // ** CẦU NỐI JAVASCRIPT (CHỈ CHẠY TRÊN WEBGL) **
    // ==========================================================
#if !UNITY_EDITOR && UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void SaveScoreToWeb(int score);
#endif

    public enum GameState { MainMenu, Playing, Paused, GameOver, GameWin }
    public static GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("== UI Canvas References ==")]
    public GameObject mainMenuCanvas;
    public GameObject pauseMenuCanvas;
    public GameObject gameOverCanvas;
    public GameObject gameWinCanvas;

    [Header("== Transition Settings ==")]
    [Tooltip("Thời gian menu biến mất từ từ")]
    public float fadeDuration = 0.4f;
    private bool isTransitioning = false;

    public static GameMenuManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetState(GameState.MainMenu);
    }

    void Update()
    {
        if (isTransitioning) return;

        bool isInputDetected = false;
        if (Input.GetMouseButtonDown(0)) isInputDetected = true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) isInputDetected = true;

        if (isInputDetected)
        {
            if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.GameWin)
            {
                HandleScreenTap();
            }
            else if (CurrentState == GameState.Paused)
            {
                bool isShopActive = ShopMenu.Instance != null && ShopMenu.Instance.gameObject.activeInHierarchy;
                bool isBlessingActive = BlessingMenu.Instance != null && BlessingMenu.Instance.IsShowing;

                if (!isShopActive && !isBlessingActive)
                {
                    HandleScreenTap();
                }
            }
        }
    }

    public void HandleScreenTap()
    {
        switch (CurrentState)
        {
            case GameState.MainMenu:
                StartCoroutine(FadeAndAction(mainMenuCanvas, () => StartGame()));
                break;
            case GameState.Paused:
                StartCoroutine(FadeAndAction(pauseMenuCanvas, () => ResumeGame()));
                break;
            case GameState.GameOver:
            case GameState.GameWin:
                GameObject activeCanvas = (CurrentState == GameState.GameOver) ? gameOverCanvas : gameWinCanvas;
                StartCoroutine(FadeAndAction(activeCanvas, () => RestartGame()));
                break;
        }
    }

    IEnumerator FadeAndAction(GameObject canvasObj, System.Action action)
    {
        isTransitioning = true;
        CanvasGroup cg = canvasObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
        }
        action.Invoke();
        isTransitioning = false;
    }

    public void StartGame() { SetState(GameState.Playing); }

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void PauseGame() { if (CurrentState == GameState.Playing) SetState(GameState.Paused); }

    public void ResumeGame() { if (CurrentState == GameState.Paused) SetState(GameState.Playing); }

    // --- Cập nhật GameOver để gửi điểm ---
    public void GameOver()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            SetState(GameState.GameOver);
            SendFinalScoreToWeb(); // Gửi điểm số
        }
    }

    // --- Cập nhật GameWin để gửi điểm ---
    public void GameWin()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            SetState(GameState.GameWin);
            SendFinalScoreToWeb(); // Gửi điểm số
        }
    }

    // HÀM BỔ TRỢ: Lấy điểm từ Player và gửi sang JS
    private void SendFinalScoreToWeb()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        if (Player.Instance != null)
        {
            int score = Player.Instance.totalGold; // Lấy tổng vàng làm điểm
            SaveScoreToWeb(score);
            Debug.Log("Score sent to React: " + score);
        }
#endif
    }

    public void PauseGameForEvent()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        if (mainMenuCanvas != null) SetupCanvas(mainMenuCanvas, newState == GameState.MainMenu);
        if (pauseMenuCanvas != null) SetupCanvas(pauseMenuCanvas, newState == GameState.Paused);
        if (gameOverCanvas != null) SetupCanvas(gameOverCanvas, newState == GameState.GameOver);
        if (gameWinCanvas != null) SetupCanvas(gameWinCanvas, newState == GameState.GameWin);

        if (AudioManager.Instance != null)
        {
            if (newState == GameState.GameOver || newState == GameState.GameWin) AudioManager.Instance.StopMusic();
            else if (newState == GameState.Playing) AudioManager.Instance.PlayMainMusic();
        }
        Time.timeScale = (newState == GameState.Playing) ? 1f : 0f;
    }

    void SetupCanvas(GameObject go, bool active)
    {
        go.SetActive(active);
        if (active)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }
}