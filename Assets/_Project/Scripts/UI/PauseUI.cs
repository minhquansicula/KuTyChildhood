using UnityEngine;
using UnityEngine.UI;

// Runs before mini-game Escape handling, preventing Escape from opening two panels.
[DefaultExecutionOrder(-100)]
public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
    }
    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (game.IsPaused) Resume();
        else if (!game.InputBlocked)
        {
            Pause();
        }
    }
    public void Pause()
    {
        if (panel != null) panel.SetActive(true);
        GameManager.Instance?.SetPaused(true);
    }
    public void Resume()
    {
        if (panel != null) panel.SetActive(false);
        GameManager.Instance?.SetPaused(false);
    }
}
