using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MainMenuUI — Giao diện Main Menu.
/// 
/// Nút Chơi → bắt đầu game mới
/// Nút Thoát → thoát game
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private void Start()
    {
        if (!Scene2Checkpoint.HasCheckpoint) return;
        Button play = null;
        Button quit = null;
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method == nameof(OnPlayButtonClicked)) play = button;
                if (method == nameof(OnQuitButtonClicked)) quit = button;
            }
        }
        if (play == null) return;
        var continueButton = Instantiate(play, play.transform.parent);
        continueButton.name = "Tiếp tục Scene 2";
        continueButton.onClick = new Button.ButtonClickedEvent();
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        var label = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "▶  TIẾP TỤC";
        var continueRect = continueButton.transform as RectTransform;
        var playRect = play.transform as RectTransform;
        if (continueRect != null && playRect != null)
        {
            continueRect.anchoredPosition = playRect.anchoredPosition;
            playRect.anchoredPosition += Vector2.down * 73f;
        }
        var quitRect = quit != null ? quit.transform as RectTransform : null;
        if (quitRect != null) quitRect.anchoredPosition += Vector2.down * 73f;
    }

    public void OnContinueButtonClicked()
    {
        if (!Scene2Checkpoint.HasCheckpoint) { OnPlayButtonClicked(); return; }
        if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(Scene2Checkpoint.ResumeScene);
    }

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
            Scene2Checkpoint.Clear();
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
