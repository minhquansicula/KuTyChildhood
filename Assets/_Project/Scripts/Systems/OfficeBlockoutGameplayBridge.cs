using UnityEngine;

/// <summary>
/// Hands the existing boss-room intro over to the reusable office work flow without
/// disabling the intro component or moving any environment object.
/// </summary>
[DefaultExecutionOrder(-80)]
public sealed class OfficeBlockoutGameplayBridge : MonoBehaviour
{
    [SerializeField] private OfficeIntroController intro;
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PauseUI pauseUI;

    public bool HandoffComplete { get; private set; }

    private void Awake()
    {
        EnsureRuntimeServices();
    }

    private void OnEnable()
    {
        if (intro == null) return;
        intro.OpeningFinished += HandleOpeningFinished;
        intro.ReadyAtDesk += HandleReadyAtDesk;
    }

    private void Start()
    {
        HUDController.Instance?.SetCrosshairVisible(false);
        if (playerInteraction != null) playerInteraction.LockInteraction(true);
        if (pauseUI != null) pauseUI.enabled = false;

        if (intro != null && intro.State == OfficeIntroController.IntroState.ReturnToDesk)
            HandleOpeningFinished();
        else if (intro != null && intro.State == OfficeIntroController.IntroState.ReadyAtDesk)
            HandleReadyAtDesk();
    }

    private void Update()
    {
        if (HandoffComplete || playerInteraction == null || intro == null) return;
        bool introAllowsSharedInteraction = intro.State == OfficeIntroController.IntroState.ReturnToDesk && !intro.IsPaused;
        playerInteraction.LockInteraction(!introAllowsSharedInteraction);
    }

    private void HandleOpeningFinished()
    {
        HUDController.Instance?.SetCrosshairVisible(true);
        office?.BeginExternalReturnToDesk(false);
        if (playerInteraction != null) playerInteraction.LockInteraction(intro != null && intro.IsPaused);
    }

    private void HandleReadyAtDesk()
    {
        if (HandoffComplete) return;
        HandoffComplete = true;
        intro?.CompleteGameplayHandoff();
        office?.AdoptExternalSeatedState();
        if (playerInteraction != null) playerInteraction.LockInteraction(false);
        if (pauseUI != null) pauseUI.enabled = true;
        HUDController.Instance?.HideInteractPrompt();
        HUDController.Instance?.SetCrosshairHighlight(false);
        GameManager.Instance?.RefreshCursor();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && HandoffComplete) pauseUI?.Pause();
    }

    private void OnDisable()
    {
        if (intro == null) return;
        intro.OpeningFinished -= HandleOpeningFinished;
        intro.ReadyAtDesk -= HandleReadyAtDesk;
    }

    private static void EnsureRuntimeServices()
    {
        if (GameManager.Instance != null && SceneLoader.Instance != null) return;
        GameObject services = GameObject.Find("OfficeRuntimeServices");
        if (services == null) services = new GameObject("OfficeRuntimeServices");
        if (GameManager.Instance == null && services.GetComponent<GameManager>() == null)
            services.AddComponent<GameManager>();
        if (SceneLoader.Instance == null && services.GetComponent<SceneLoader>() == null)
            services.AddComponent<SceneLoader>();
        GameManager.Instance?.RefreshCursor();
    }
}
