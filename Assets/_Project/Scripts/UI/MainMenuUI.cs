using UnityEngine;

/// <summary>
/// MainMenuUI — Giao diện Main Menu.
/// 
/// Nút Chơi → bắt đầu game mới
/// Nút Thoát → thoát game
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ========== PUBLIC METHODS (gán vào Button OnClick) ==========

    /// <summary>
    /// Bấm nút "Chơi" — bắt đầu game mới.
    /// Gán vào Button.OnClick() trong Inspector.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        Debug.Log("[MainMenu] Bắt đầu game mới!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            // Fallback nếu chưa có GameManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Act1);
        }
    }

    /// <summary>
    /// Bấm nút "Thoát" — thoát game.
    /// Gán vào Button.OnClick() trong Inspector.
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Debug.Log("[MainMenu] Thoát game!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
