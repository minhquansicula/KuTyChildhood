using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager — Singleton quản lý trạng thái game xuyên suốt.
/// Tồn tại qua các scene nhờ DontDestroyOnLoad.
/// 
/// Chức năng:
/// - Theo dõi GameState hiện tại (MainMenu, Act1, Act2, Act3)
/// - Quản lý tiền (currency) thông qua CurrencyManager
/// - Điều phối chuyển scene
/// - Cung cấp truy cập global qua Instance
/// </summary>
public class GameManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static GameManager Instance { get; private set; }

    // ========== STATE ==========
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

    // ========== EVENTS ==========
    /// <summary>Gọi khi GameState thay đổi. Param: newState</summary>
    public System.Action<GameState> OnGameStateChanged;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        // Singleton pattern: chỉ giữ 1 instance duy nhất
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo cursor cho menu
        SetCursorState(true);
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Thay đổi trạng thái game và thông báo cho tất cả listeners.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");

        // Cấu hình cursor theo state
        switch (newState)
        {
            case GameState.MainMenu:
                SetCursorState(true); // Hiện cursor ở menu
                break;
            case GameState.Act1_RealWorld:
            case GameState.Act2_MemoryWorld:
                SetCursorState(false); // Ẩn cursor khi chơi (FPS)
                break;
            case GameState.Act3_Ending:
                SetCursorState(true); // Hiện cursor ở ending
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Bắt đầu game mới — reset dữ liệu và load Act1.
    /// </summary>
    public void StartNewGame()
    {
        // Reset tất cả hệ thống
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.ResetCurrency();

        if (MemoryCollectionManager.Instance != null)
            MemoryCollectionManager.Instance.ResetMemories();

        SetGameState(GameState.Act1_RealWorld);
        SceneLoader.Instance?.LoadScene("Act1_RealWorld");
    }

    /// <summary>
    /// Chuyển sang Act2 (thế giới ký ức) — gọi khi chạm vật kỷ niệm ở Act1.
    /// </summary>
    public void EnterMemoryWorld()
    {
        SetGameState(GameState.Act2_MemoryWorld);
        SceneLoader.Instance?.LoadScene("Act2_MemoryWorld");
    }

    /// <summary>
    /// Chuyển sang Act3 (kết thúc) — gọi khi thu thập đủ 3/3 mảnh ký ức.
    /// </summary>
    public void EnterEnding()
    {
        SetGameState(GameState.Act3_Ending);
        SceneLoader.Instance?.LoadScene("Act3_Ending");
    }

    /// <summary>
    /// Quay về Main Menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.MainMenu);
        SceneLoader.Instance?.LoadScene("MainMenu");
    }

    /// <summary>
    /// Thoát game.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Bật/tắt cursor (ẩn + khóa khi chơi FPS, hiện khi ở menu/UI).
    /// </summary>
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Tạm dừng/tiếp tục game (dùng cho pause menu nếu có).
    /// </summary>
    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        SetCursorState(paused);
    }
}
