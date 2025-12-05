using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameMenuManager : MonoBehaviour
{
    // Enum định nghĩa các trạng thái game có thể có
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        GameWin
    }

    // Trạng thái game hiện tại
    public static GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("== UI Canvas References ==")]
    [Tooltip("Gán Canvas Main Menu vào đây")]
    public GameObject mainMenuCanvas;
    [Tooltip("Gán Canvas Pause Menu vào đây")]
    public GameObject pauseMenuCanvas;
    [Tooltip("Gán Canvas Game Over vào đây")]
    public GameObject gameOverCanvas;
    [Tooltip("Gán Canvas Game Win vào đây")]
    public GameObject gameWinCanvas;

    // Tham chiếu tĩnh để dễ dàng gọi từ các script khác
    public static GameMenuManager Instance { get; private set; }

    // Đã loại bỏ biến tĩnh "shouldStartPlaying"

    void Awake()
    {
        // Thiết lập Singleton
        if (Instance == null)
        {
            Instance = this;
            // 🛑 ĐÃ XÓA: DontDestroyOnLoad(gameObject);
            // Manager này sẽ bị hủy khi Scene bị tải lại (nhưng chúng ta không tải lại nữa!)
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Bắt đầu game ở trạng thái Main Menu, đảm bảo Time.timeScale = 0f
        SetState(GameState.MainMenu);
    }

    // Logic bắt input thô (Raw Input)
    void Update()
    {
        bool isInputDetected = false;

        // PC/Editor Click (Chuột trái)
        if (Input.GetMouseButtonDown(0))
        {
            isInputDetected = true;
        }

        // Mobile Touch (Chạm đầu tiên)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            isInputDetected = true;
        }

        if (isInputDetected)
        {
            HandleScreenTap();
        }
    }

    // ==========================================================
    // ** LOGIC QUẢN LÝ TRẠNG THÁI **
    // ==========================================================

    private void SetState(GameState newState)
    {
        CurrentState = newState;

        // 1. Ẩn tất cả Canvas Menu
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (gameWinCanvas != null) gameWinCanvas.SetActive(false);

        // 2. Xử lý logic theo từng trạng thái mới
        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
                Time.timeScale = 0f; // Dừng game
                Debug.Log("Game State: Main Menu");
                break;

            case GameState.Playing:
                // 🛑 QUAN TRỌNG: Chỉ cần đặt TimeScale = 1f và tất cả Canvas Menu đã bị ẩn.
                Time.timeScale = 1f;
                Debug.Log("Game State: Playing");
                break;

            case GameState.Paused:
                if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(true);
                Time.timeScale = 0f; // Dừng game
                Debug.Log("Game State: Paused");
                break;

            case GameState.GameOver:
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                Time.timeScale = 0f; // Dừng game
                Debug.Log("Game State: Game Over");
                break;

            case GameState.GameWin:
                if (gameWinCanvas != null) gameWinCanvas.SetActive(true);
                Time.timeScale = 0f; // Dừng game
                Debug.Log("Game State: Game Win");
                break;
        }
    }

    // ==========================================================
    // ** CÁC HÀM XỬ LÝ SỰ KIỆN CHÍNH **
    // ==========================================================

    public void HandleScreenTap()
    {
        switch (CurrentState)
        {
            // Khi đang ở MainMenu, chuyển thẳng sang Playing
            case GameState.MainMenu:
                StartGame();
                break;

            case GameState.Paused:
                ResumeGame();
                break;

            case GameState.GameOver:
            case GameState.GameWin:
                RestartGame();
                break;
        }
    }

    public void StartGame()
    {
        // 🛑 ĐƠN GIẢN HÓA: Chỉ cần chuyển trạng thái
        SetState(GameState.Playing);
    }

    public void RestartGame()
    {
        // 🛑 LƯU Ý: Nếu không tải lại Scene, bạn phải thêm logic Reset game tại đây.
        // Ví dụ: Player.Instance.ResetStats(); LevelManager.Instance.ResetLevel();
        // Hiện tại, nó chỉ ẩn UI và tiếp tục game (vì game time đã dừng ở Game Over/Win).

        // 🛑 Để Restart đúng nghĩa, bạn CẦN TẢI LẠI SCENE. 
        // Nếu không muốn tải lại, bạn phải tự code hàm Reset cho mọi thứ (Player, Enemies, Score, etc.)

        // GIỮ PHƯƠNG PHÁP TẢI LẠI CHO RESTART
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // Khi Scene tải lại, Awake() sẽ chạy và đưa game về MainMenu, chờ StartGame()

        // ⚠️ Lưu ý: Nếu bạn dùng StartGame() ở đây, bạn sẽ không reset được Scene!
        // Để đơn giản, ta vẫn dùng Scene Load ở đây.
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            SetState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    public void GameOver()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            SetState(GameState.GameOver);
        }
    }

    public void GameWin()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            SetState(GameState.GameWin);
        }
    }
    // --- Thêm vào GameMenuManager.cs ---

    // HÀM MỚI: Tạm dừng game nhưng KHÔNG hiển thị Pause Menu
    public void PauseGameForEvent()
    {
        // Chỉ tạm dừng nếu game đang chạy
        if (CurrentState == GameState.Playing)
        {
            // Chuyển sang trạng thái Paused, nhưng không gọi SetState
            // để tránh việc SetState() hiển thị pauseMenuCanvas

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("Game State: Paused for Event (No UI)");
        }
    }
}