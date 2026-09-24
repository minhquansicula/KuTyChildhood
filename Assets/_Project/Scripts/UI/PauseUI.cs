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
        panel.SetActive(false);
        resumeButton.onClick.AddListener(Resume);
        menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());
    }
    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (game.IsPaused) Resume();
        else if (!game.InputBlocked)
        {
            panel.SetActive(true);
            game.SetPaused(true);
        }
    }
    public void Resume() { panel.SetActive(false); GameManager.Instance?.SetPaused(false); }
}
