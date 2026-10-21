using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runs before mini-game Escape handling, preventing Escape from opening two panels.
[DefaultExecutionOrder(-100)]
public class PauseUI : MonoBehaviour
{
    public static int LastResumeFrame { get; private set; } = -1;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    private bool restoreCrosshair;
    private bool restoreControlsHint;
    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        StyleModal();
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
    }
    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (game.IsPaused) Resume();
        else if (!game.InputBlocked || CanPauseFromBed())
        {
            Pause();
        }
    }
    private static bool CanPauseFromBed()
    {
        var bed = FindObjectOfType<BedWakeUpController>();
        return bed != null && bed.CanPauseWhileWaiting;
    }
    public void Pause()
    {
        if (gameObject.scene.name == SceneNames.Act1)
        {
            restoreCrosshair = HUDController.Instance != null && HUDController.Instance.CrosshairVisible;
            HUDController.Instance?.SetCrosshairVisible(false);
            restoreControlsHint = OfficeSceneController.Instance != null && OfficeSceneController.Instance.ControlsHintVisible;
            OfficeSceneController.Instance?.SetControlsHintVisible(false);
        }
        if (panel != null) panel.SetActive(true);
        OfficeUiStyle.FitPauseCard(panel);
        GameManager.Instance?.SetPaused(true);
    }
    public void Resume()
    {
        if (panel != null) panel.SetActive(false);
        LastResumeFrame = Time.frameCount;
        GameManager.Instance?.SetPaused(false);
        if (gameObject.scene.name == SceneNames.Act1) HUDController.Instance?.SetCrosshairVisible(restoreCrosshair);
        if (gameObject.scene.name == SceneNames.Act1) OfficeSceneController.Instance?.SetControlsHintVisible(restoreControlsHint);
    }

    private void StyleModal()
    {
        if (panel == null) return;
        menuButton = OfficeUiStyle.BuildPause(panel, resumeButton, menuButton, GameUiTheme.Current.font);
        MenuButtonSfx.Attach(resumeButton);
        MenuButtonSfx.Attach(menuButton);
    }
}
