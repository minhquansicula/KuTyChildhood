using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum OfficePhase
{
    Opening,
    ReturnToDesk,
    ExcelAuditing,
    AfterWork,
    Rest,
    Leaving,
    PrintingReport,
    CollectPrint,
    DeliverReport,
    WaterBreak
}
public enum OfficeInteractionKind { Laptop, Documents, Chair, Window, Boss }

/// <summary>Coordinates the complete Scene 1 narrative and work flow.</summary>
public sealed class OfficeSceneController : MonoBehaviour
{
    public static OfficeSceneController Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI controlsHintText;
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshPro screenWarning;
    [SerializeField] private TextMeshPro clockText;

    [Header("Scene systems")]
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField] private ExcelDataAuditorGame excelMiniGame;
    [SerializeField] private OfficeOpeningSequence openingSequence;
    [SerializeField] private OfficeExhaustionSequence exhaustionSequence;
    [SerializeField] private Light afternoonLight;
    [SerializeField] private bool openingControlledExternally;

    [Header("Player and chair")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform chairAnchor;
    [SerializeField] private float chairUseDistance = 1.5f;
    [SerializeField, Min(.1f)] private float chairStandDistance = .45f;
    [SerializeField] private float seatedCamY = .9f;

    [Header("Pacing")]
    [SerializeField] private float objectiveDelaySeconds = 2.5f;
    [SerializeField] private float bossReminderSeconds = 28f;
    [SerializeField, Min(.5f)] private float printerDurationSeconds = 3f;
    [SerializeField] private Color dreamFade = new Color(1f, .84f, .47f);

    public OfficePhase Phase { get; private set; } = OfficePhase.Opening;
    public bool IsWorking { get; private set; }
    public bool IsSeated { get; private set; }
    public float WorkProgress { get; private set; }
    public string ObjectiveText { get; private set; } = string.Empty;
    public bool HasPrintedReport { get; private set; }
    public bool ReportDelivered { get; private set; }
    public bool WaterBreakCompleted { get; private set; }
    public event Action<OfficePhase, OfficePhase> PhaseChanged;
    public event Action<bool> WorkingChanged;
    public event Action<bool> SeatedChanged;
    public event Action ObjectiveChanged;
    public event Action<string, float> SpeakerChanged;

    private Coroutine subtitleRoutine;
    private Coroutine seatRoutine;
    private Coroutine openingRoutine;
    private Coroutine printRoutine;
    private float standingCamY = 1.55f;
    private float reminderTimer;
    private bool nearChair;
    private bool memoryTransitionRequested;
    private bool builtInObjectiveVisible = true;
    private bool guidanceSubtitleActive;
    private float afternoonLightInitialIntensity;
    private OfficeSceneEffects sceneEffects;
    private DialogueSequence gameplayVoices;
    private DialogueVoicePlayer gameplayVoice;
    private GameManager voicePauseOwner;
    public DialogueVoicePlayer GameplayVoicePlayer => gameplayVoice;
    public float CurrentSubtitleSeconds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameplayVoices = Resources.Load<DialogueSequence>("OfficeGameplayVoices");
        var voiceObject = new GameObject("OfficeGameplayVoice", typeof(AudioSource), typeof(DialogueVoicePlayer));
        voiceObject.transform.SetParent(transform, false);
        gameplayVoice = voiceObject.GetComponent<DialogueVoicePlayer>();
        sceneEffects = GetComponent<OfficeSceneEffects>();
        if (sceneEffects == null) sceneEffects = gameObject.AddComponent<OfficeSceneEffects>();
        ConfigureHudPresentation();
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        SetControlsHintVisible(false);
        if (afternoonLight != null) afternoonLightInitialIntensity = afternoonLight.intensity;
    }

    private void Start()
    {
        voicePauseOwner = GameManager.Instance;
        if (voicePauseOwner != null) voicePauseOwner.OnPauseChanged += PauseGameplayVoice;
        CacheStandingCameraY();
        SetPhase(OfficePhase.Opening);
        if (openingControlledExternally) return;
        if (openingSequence != null && openingSequence.Play(this)) return;
        GameManager.Instance?.AcquireInput(this);
        openingRoutine = StartCoroutine(OpeningSequence());
    }

    private void Update()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        if (IsWorking)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CancelWork();
            return;
        }
        if (Phase == OfficePhase.Opening || Phase == OfficePhase.Leaving) return;
        if (IsSeated && Input.GetKeyDown(KeyCode.Space)) { StandUp(); return; }
        if (!IsWorkPhase(Phase)) return;

        reminderTimer += Time.deltaTime;
        if (reminderTimer < bossReminderSeconds) return;
        reminderTimer = 0f;
        atmosphere?.PlayBoss(true);
        ShowSubtitle("SẾP", "Tôi cần bản sửa trước khi mọi người về hết!", 3.5f);
    }

    private IEnumerator OpeningSequence()
    {
        yield return new WaitForSeconds(.7f);
        if (Phase != OfficePhase.Opening) yield break;
        float voiceSeconds = atmosphere != null ? atmosphere.PlayBoss(false) : 0f;
        yield return ShowOpeningBossSubtitles(voiceSeconds > .1f ? voiceSeconds : 7.5f);
        if (Phase != OfficePhase.Opening) yield break;

        float replySeconds = atmosphere != null ? atmosphere.PlayPlayerReply() : 0f;
        ShowSubtitle("TÝ", "Dạ... em kiểm tra lại ngay ạ.", Mathf.Max(2.2f, replySeconds));
        yield return new WaitForSeconds(Mathf.Max(2.2f, replySeconds));
        atmosphere?.PlaySigh();
        yield return new WaitForSeconds(.8f);
        SetPhase(OfficePhase.ReturnToDesk, false);
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
        yield return new WaitForSeconds(objectiveDelaySeconds);
        if (Phase == OfficePhase.ReturnToDesk) RefreshObjective();
        openingRoutine = null;
    }

    private IEnumerator ShowOpeningBossSubtitles(float totalSeconds)
    {
        string[] lines =
        {
            "SỐ LIỆU TRONG FILE NÀY LÀ SAO?!", "NHIỀU Ô CÒN SAI MÀ CẬU ĐÃ GỬI RỒI À?!",
            "KHÁCH HÀNG ĐANG CHỜ BẢN CHỐT!", "TÔI ĐÃ NHẮC CẬU TỪ SÁNG RỒI!",
            "QUAY VỀ BÀN, KIỂM TRA LẠI FILE EXCEL!", "XONG THÌ IN MỘT BẢN, MANG LẠI ĐÂY CHO TÔI!"
        };
        float[] weights = { .12f, .17f, .17f, .18f, .20f, .16f };
        for (int i = 0; i < lines.Length; i++)
        {
            float seconds = totalSeconds * weights[i];
            ShowSubtitle("SẾP", lines[i], seconds + .05f);
            yield return new WaitForSeconds(seconds);
        }
    }

    public string PromptFor(OfficeInteractionKind kind)
    {
        if (IsWorking || seatRoutine != null || Phase == OfficePhase.Opening || Phase == OfficePhase.Leaving) return string.Empty;
        if (kind == OfficeInteractionKind.Window) return "[E] Nhìn ra phố";
        if (kind == OfficeInteractionKind.Boss)
            return Phase == OfficePhase.DeliverReport ? "[E] Đặt báo cáo lên bàn sếp" : string.Empty;
        if (kind == OfficeInteractionKind.Chair)
        {
            if (!IsSeated) return "[E] Ngồi vào ghế";
            return Phase == OfficePhase.Rest ? "[E] Gục xuống nghỉ một chút" : string.Empty;
        }
        if (!IsSeated) return string.Empty;

        switch (Phase)
        {
            case OfficePhase.ReturnToDesk: return "[E] Mở báo cáo Excel";
            case OfficePhase.ExcelAuditing: return "[E] Tiếp tục báo cáo Excel";
            case OfficePhase.AfterWork: return "[E] Kiểm tra trạng thái báo cáo";
            case OfficePhase.Rest: return "[E] Gục xuống nghỉ một chút";
            default: return string.Empty;
        }
    }

    public void HandleInteraction(OfficeInteractionKind kind)
    {
        if (string.IsNullOrEmpty(PromptFor(kind))) return;
        reminderTimer = 0f;
        if (kind == OfficeInteractionKind.Window)
        {
            ShowSubtitle("CỬA SỔ", "Dòng người vẫn vội vã. Tiếng còi xe chẳng lúc nào ngớt.", 4f);
            return;
        }
        if (kind == OfficeInteractionKind.Boss)
        {
            if (Phase == OfficePhase.DeliverReport)
            {
                ReportDelivered = true;
                atmosphere?.PlayPaper();
                sceneEffects?.PlayReportDelivered();
                SetPhase(OfficePhase.WaterBreak);
                ShowSubtitle("SẾP", "Để trên bàn đi. Được rồi, cậu về đi.", 4f);
                return;
            }
            ShowSubtitle("TÝ", "Bóng người sau lớp kính mờ vẫn khiến mình nghẹt thở.", 3.5f);
            return;
        }
        if (!IsSeated) { SitDown(false); return; }

        switch (Phase)
        {
            case OfficePhase.ReturnToDesk: SetPhase(OfficePhase.ExcelAuditing); StartExcelWork(); break;
            case OfficePhase.ExcelAuditing: StartExcelWork(); break;
            case OfficePhase.AfterWork:
                ShowSubtitle("TÝ", "Bản Excel đã gửi rồi. Hôm nay mình dừng ở đây thôi...", 4f);
                break;
            case OfficePhase.Rest: BeginExhaustion(); break;
        }
    }

    public string PromptForMicro(OfficeMicroInteractionKind kind)
    {
        if (Phase == OfficePhase.Opening || Phase == OfficePhase.Leaving || IsWorking) return string.Empty;
        switch (kind)
        {
            case OfficeMicroInteractionKind.Clock: return "[E] Nhìn đồng hồ và deadline";
            case OfficeMicroInteractionKind.WaterCooler:
                return Phase == OfficePhase.WaterBreak ? "[E] Lấy nước uống" : "[E] Uống một ngụm nước";
            case OfficeMicroInteractionKind.PrinterPaper:
                if (Phase == OfficePhase.PrintingReport) return "[E] Kiểm tra máy photo";
                if (Phase == OfficePhase.CollectPrint) return "[E] Lấy bản báo cáo";
                return "[E] Kiểm tra máy photo";
            case OfficeMicroInteractionKind.ColdCoffee: return "[E] Cầm cốc cà phê";
            case OfficeMicroInteractionKind.ChildhoodMarble: return "[E] Nhìn viên bi cũ";
            default: return string.Empty;
        }
    }

    public void HandleMicroInteraction(OfficeMicroInteractionKind kind)
    {
        if (string.IsNullOrEmpty(PromptForMicro(kind))) return;
        switch (kind)
        {
            case OfficeMicroInteractionKind.Clock:
                ShowSubtitle("TÝ", Phase == OfficePhase.AfterWork || Phase == OfficePhase.Rest || Phase == OfficePhase.Leaving
                    ? "Hơn sáu giờ rồi... mọi người gần như đã về hết."
                    : "Sắp hết giờ làm rồi. Mình phải chốt xong file Excel.", 4f);
                break;
            case OfficeMicroInteractionKind.WaterCooler:
                atmosphere?.PlayWater();
                sceneEffects?.PlayWater();
                if (Phase == OfficePhase.WaterBreak)
                {
                    WaterBreakCompleted = true;
                    SetPhase(OfficePhase.Rest);
                    ShowSubtitle("TÝ", "Sao vẫn choáng thế này... về ghế ngồi một lát đã.", 4f);
                }
                else ShowSubtitle("TÝ", "Một ngụm nước. Mình phải tỉnh táo thêm chút nữa.", 3f);
                break;
            case OfficeMicroInteractionKind.PrinterPaper:
                HandlePrinterInteraction();
                break;
            case OfficeMicroInteractionKind.ColdCoffee:
                atmosphere?.PlayCup();
                ShowSubtitle("TÝ", "Cà phê lạnh ngắt rồi... mình quên uống từ lúc nào nhỉ?", 3.5f);
                break;
            case OfficeMicroInteractionKind.ChildhoodMarble:
                atmosphere?.PlayMemoryBridge();
                ShowSubtitle("TÝ", "Một viên bi cũ...? Lâu rồi mình không nhớ tới những buổi chiều ấy.", 4.5f);
                break;
        }
    }

    private void StartExcelWork()
    {
        if (IsWorking) return;
        SetWorking(true);
        RefreshObjective();
        SetControlsHintVisible(false);
        GameManager.Instance?.AcquireInput(this);
        if (excelMiniGame != null && excelMiniGame.Open(this))
        {
            atmosphere?.StartWorkTyping();
            return;
        }
        SetWorking(false);
        SetControlsHintVisible(true);
        RefreshObjective();
        atmosphere?.StopWorkTyping();
        GameManager.Instance?.ReleaseInput(this);
        ShowSubtitle("HỆ THỐNG", "Không thể mở bảng tính. Hãy kiểm tra lại cấu hình Excel.", 3f);
    }

    // Compatibility entry point for older scene assets. The current Scene 1 flow only uses Excel.
    public void CompleteWorkTask(OfficeWorkTask completed)
    {
        SetWorking(false);
        atmosphere?.StopWorkTyping();
        GameManager.Instance?.ReleaseInput(this);
        SetPhase(OfficePhase.ExcelAuditing);
        ShowSubtitle("HỆ THỐNG", "Nhiệm vụ hiện tại đã được chuyển sang báo cáo Excel.", 2.8f);
    }

    public void CompleteExcelTask()
    {
        // Ignore stale callbacks when the Excel task is no longer active.
        if (Phase != OfficePhase.ExcelAuditing || !IsWorking) return;

        SetWorking(false);
        atmosphere?.StopWorkTyping();
        excelMiniGame?.ClosePanel();
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
        
        WorkProgress = 1f;
        SetPhase(OfficePhase.PrintingReport);
        atmosphere?.PlayPrinter();
        if (afternoonLight != null)
        {
            if (afternoonLightInitialIntensity <= 0f) afternoonLightInitialIntensity = afternoonLight.intensity;
            afternoonLight.intensity = afternoonLightInitialIntensity * .62f;
        }
        if (printRoutine != null) StopCoroutine(printRoutine);
        printRoutine = StartCoroutine(PrintReportBeat());
    }

    public void UpdateReportMiniGameProgress(float progress) => WorkProgress = Mathf.Clamp01(progress);
    public void PlayReportTypingFeedback() => atmosphere?.PlayTyping();

    public void CancelWork()
    {
        SetWorking(false);
        atmosphere?.StopWorkTyping();
        if (excelMiniGame != null) excelMiniGame.ClosePanel();
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
        RefreshObjective();
    }

    private IEnumerator PrintReportBeat()
    {
        ShowSubtitle("HỆ THỐNG", "Dữ liệu chính xác. Đã gửi lệnh in tới máy photo.", 2.8f);
        yield return new WaitForSeconds(printerDurationSeconds);
        if (Phase == OfficePhase.PrintingReport)
        {
            SetPhase(OfficePhase.CollectPrint);
            ShowSubtitle("TÝ", "Máy photo ở cạnh bình nước. Lấy bản in rồi mang cho sếp là xong.", 4f);
        }
        printRoutine = null;
    }

    private void HandlePrinterInteraction()
    {
        switch (Phase)
        {
            case OfficePhase.PrintingReport:
                ShowSubtitle("MÁY PHOTO", "Máy vẫn đang in... chờ thêm một chút.", 2.5f);
                break;
            case OfficePhase.CollectPrint:
                HasPrintedReport = true;
                atmosphere?.PlayPaper();
                sceneEffects?.PlayPaperPickup();
                SetPhase(OfficePhase.DeliverReport);
                ShowSubtitle("TÝ", "Bản in còn ấm. Mang đến phòng sếp thôi.", 3f);
                break;
            case OfficePhase.ReturnToDesk:
            case OfficePhase.ExcelAuditing:
                ShowSubtitle("TÝ", "Mình phải sửa xong file Excel trước khi in.", 3f);
                break;
            case OfficePhase.DeliverReport:
            case OfficePhase.WaterBreak:
            case OfficePhase.Rest:
            case OfficePhase.AfterWork:
                ShowSubtitle("TÝ", "Không cần in thêm bản nào nữa.", 2.5f);
                break;
            default:
                ShowSubtitle("MÁY PHOTO", "Không có lệnh in nào trong hàng đợi.", 2.5f);
                break;
        }
    }

    private void BeginExhaustion()
    {
        if (Phase == OfficePhase.Leaving || !IsSeated) return;
        SetPhase(OfficePhase.Leaving);
        GameManager.Instance?.AcquireInput(this);
        SceneLoader.Instance?.PrepareSleepTransition(SceneNames.Act2_Home);
        atmosphere?.PlaySigh();
        SetBuiltInObjectiveVisible(false);
        SetControlsHintVisible(false);
        HUDController.Instance?.SetCrosshairVisible(false);
        float speechSeconds = ShowSubtitle("TÝ", "Mệt quá... chỉ nghỉ một chút thôi...", 3f);
        int token = gameplayVoice != null ? gameplayVoice.PlaybackToken : -1;
        sleepDialogueProtected = true;
        bool VoiceActive() => gameplayVoice != null && gameplayVoice.PlaybackToken == token &&
            (gameplayVoice.IsPlaying || gameplayVoice.IsPaused);
        if (exhaustionSequence != null && exhaustionSequence.Play(LoadMemoryWorld, speechSeconds, VoiceActive)) return;
        StartCoroutine(FallbackLeaveOffice(speechSeconds, VoiceActive));
    }

    private bool sleepDialogueProtected;

    private IEnumerator FallbackLeaveOffice(float speechSeconds, Func<bool> voiceActive)
    {
        yield return new WaitForSeconds(speechSeconds);
        while (voiceActive()) yield return null;
        yield return new WaitForSeconds(.45f);
        LoadMemoryWorld();
    }

    private void LoadMemoryWorld()
    {
        if (memoryTransitionRequested) return;
        memoryTransitionRequested = true;
        if (SceneLoader.Instance != null)
        {
            if (SceneLoader.Instance.IsSleepTransitionCovered) SceneLoader.Instance.CompleteSleepTransition();
            else SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act2_Home, Color.black, .2f);
        }
        GameManager.Instance?.ReleaseInput(this);
    }

    private void SetPhase(OfficePhase next, bool showObjective = true)
    {
        OfficePhase previous = Phase;
        Phase = next;
        switch (next)
        {
            case OfficePhase.Opening: ObjectiveText = string.Empty; break;
            case OfficePhase.ReturnToDesk: ObjectiveText = "Quay lại bàn và mở báo cáo Excel."; break;
            case OfficePhase.ExcelAuditing: ObjectiveText = "Đối chiếu số liệu và hoàn thiện báo cáo Excel."; break;
            case OfficePhase.PrintingReport: ObjectiveText = "Chờ máy photo in báo cáo."; break;
            case OfficePhase.CollectPrint: ObjectiveText = "Lấy bản báo cáo ở máy photo."; break;
            case OfficePhase.DeliverReport: ObjectiveText = "Vào phòng sếp, đến bàn và đưa bản báo cáo đã photo."; break;
            case OfficePhase.WaterBreak: ObjectiveText = "Đến bình nước uống một chút."; break;
            case OfficePhase.AfterWork: ObjectiveText = "Báo cáo đã hoàn tất."; break;
            case OfficePhase.Rest:
                ObjectiveText = IsSeated ? "Nghỉ một chút." : "Quay lại ghế và nghỉ một chút.";
                nearChair = false;
                break;
            default: ObjectiveText = "..."; break;
        }
        bool corrected = next == OfficePhase.PrintingReport || next == OfficePhase.CollectPrint ||
            next == OfficePhase.DeliverReport;
        bool delivered = next == OfficePhase.WaterBreak || next == OfficePhase.AfterWork ||
            next == OfficePhase.Rest || next == OfficePhase.Leaving;
        if (screenWarning != null) screenWarning.text = delivered ? "BÁO CÁO ĐÃ NỘP" :
            corrected ? "BÁO CÁO ĐÃ SỬA" : "REPORT_Q3_FINAL.xlsx";
        if (clockText != null) clockText.text = corrected || delivered ? "18:17" : "17:42";
        if (showObjective) RefreshObjective();
        sceneEffects?.SetPhase(next);
        if (previous != next) PhaseChanged?.Invoke(previous, next);
    }

    private void RefreshObjective()
    {
        if (Phase == OfficePhase.Rest) ObjectiveText = IsSeated ? "Nhìn máy tính, nhấn E để nghỉ một chút." : "Quay lại ghế và nghỉ một chút.";
        ObjectiveChanged?.Invoke();
        if (objectiveText == null) return;
        CanvasGroup group = objectiveText.GetComponentInParent<CanvasGroup>();
        if (group != null) group.alpha = builtInObjectiveVisible && !IsWorking && Phase != OfficePhase.Leaving ? 1f : 0f;
        if (string.IsNullOrEmpty(ObjectiveText)) { objectiveText.text = string.Empty; return; }
        string hint = IsSeated && Phase != OfficePhase.Rest && Phase != OfficePhase.Leaving
            ? "  <color=#8FA4B8>•  <b>SPACE</b> Đứng dậy</color>"
            : string.Empty;
        objectiveText.text = "<color=#F4C66A><b>NHIỆM VỤ</b></color>  <color=#EDF3F8>" + ObjectiveText + "</color>" + hint;
    }

    public void SitDown(bool instant = false)
    {
        if (IsSeated || playerTransform == null || chairAnchor == null) return;
        SetSeated(true);
        atmosphere?.PlayChair();
        FirstPersonController fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.LockWalkingOnly = true;
            if (fpc.PlayerCamera != null && standingCamY <= .01f) standingCamY = fpc.PlayerCamera.localPosition.y;
        }
        Vector3 targetPosition = ChairPositionAtPlayerHeight();
        Quaternion targetRotation = Quaternion.LookRotation(GetChairForward());
        if (seatRoutine != null) StopCoroutine(seatRoutine);
        if (instant)
        {
            MoveCharacter(targetPosition, targetRotation, true);
            SetCameraHeight(fpc, seatedCamY, true);
            RefreshObjective();
        }
        else seatRoutine = StartCoroutine(TransitionSeat(targetPosition, targetRotation, seatedCamY, true));
    }

    public void StandUp(bool instant = false)
    {
        if (seatRoutine != null || IsWorking || Phase == OfficePhase.Leaving) return;
        if (!IsSeated || playerTransform == null) return;
        Vector3 targetPosition = ChairPositionAtPlayerHeight() - GetChairForward() * chairStandDistance;
        if (!IsStandPositionClear(targetPosition))
        {
            ShowSubtitle("HỆ THỐNG", "Lối đứng dậy đang bị chắn.", 2f);
            return;
        }
        SetSeated(false);
        atmosphere?.PlayChair();
        if (seatRoutine != null) StopCoroutine(seatRoutine);
        FirstPersonController fpc = playerTransform.GetComponent<FirstPersonController>();
        if (instant)
        {
            MoveCharacter(targetPosition, playerTransform.rotation, false);
            SetCameraHeight(fpc, standingCamY, false);
            RefreshObjective();
        }
        else seatRoutine = StartCoroutine(TransitionSeat(targetPosition, playerTransform.rotation, standingCamY, false));
    }

    private IEnumerator TransitionSeat(Vector3 targetPosition, Quaternion targetRotation, float targetCamY, bool sittingDown)
    {
        FirstPersonController fpc = playerTransform.GetComponent<FirstPersonController>();
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        Transform playerCamera = fpc != null ? fpc.PlayerCamera : null;
        Vector3 startPosition = playerTransform.position;
        Quaternion startRotation = playerTransform.rotation;
        float startCamY = playerCamera != null ? playerCamera.localPosition.y : standingCamY;
        const float duration = .45f;
        if (cc != null) cc.enabled = false;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            if (sittingDown) playerTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            if (playerCamera != null)
            {
                Vector3 local = playerCamera.localPosition;
                local.y = Mathf.Lerp(startCamY, targetCamY, t);
                playerCamera.localPosition = local;
            }
            yield return null;
        }
        playerTransform.position = targetPosition;
        if (sittingDown) playerTransform.rotation = targetRotation;
        if (cc != null) cc.enabled = true;
        SetCameraHeight(fpc, targetCamY, sittingDown);
        RefreshObjective();
        seatRoutine = null;
    }

    private void UpdateChairInteraction()
    {
        if (GameManager.Instance?.InputBlocked == true || playerTransform == null || chairAnchor == null) return;
        Vector3 offset = playerTransform.position - chairAnchor.position;
        offset.y = 0f;
        bool inRange = offset.sqrMagnitude <= chairUseDistance * chairUseDistance;
        if (inRange != nearChair) { nearChair = inRange; RefreshObjective(); }
        if (inRange && Input.GetKeyDown(KeyCode.E)) HandleInteraction(OfficeInteractionKind.Chair);
    }

    private bool IsStandPositionClear(Vector3 targetPosition)
    {
        CharacterController cc = playerTransform != null ? playerTransform.GetComponent<CharacterController>() : null;
        float radius = cc != null ? Mathf.Max(.15f, cc.radius - cc.skinWidth) : .28f;
        float height = cc != null ? Mathf.Max(radius * 2f, cc.height) : 1.7f;
        Vector3 center = targetPosition + Vector3.up * (height * .5f);
        Vector3 half = Vector3.up * Mathf.Max(0f, height * .5f - radius);
        Collider[] overlaps = Physics.OverlapCapsule(center - half, center + half, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider candidate = overlaps[i];
            if (candidate == null || candidate.transform.IsChildOf(playerTransform)) continue;
            if (chairAnchor != null && (candidate.transform.IsChildOf(chairAnchor) || chairAnchor.IsChildOf(candidate.transform))) continue;
            if (candidate.GetComponentInParent<OfficeBlockoutChairInteractable>() != null) continue;
            return false;
        }
        return true;
    }

    private void CacheStandingCameraY()
    {
        if (playerTransform == null) return;
        FirstPersonController fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null && fpc.PlayerCamera != null) standingCamY = fpc.PlayerCamera.localPosition.y;
    }

    private Vector3 ChairPositionAtPlayerHeight() => new Vector3(chairAnchor.position.x, playerTransform.position.y, chairAnchor.position.z);

    private Vector3 GetChairForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(chairAnchor.forward, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(-chairAnchor.up, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(chairAnchor.right, Vector3.up);
        return forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
    }

    private void MoveCharacter(Vector3 position, Quaternion rotation, bool rotate)
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerTransform.position = position;
        if (rotate) playerTransform.rotation = rotation;
        if (cc != null) cc.enabled = true;
    }

    private static void SetCameraHeight(FirstPersonController fpc, float height, bool seated)
    {
        if (fpc == null) return;
        fpc.LockWalkingOnly = seated;
        if (fpc.PlayerCamera == null) return;
        Vector3 local = fpc.PlayerCamera.localPosition;
        local.y = height;
        fpc.PlayerCamera.localPosition = local;
        if (seated) fpc.ResetCameraVerticalRotation(5f);
    }

    private static bool IsWorkPhase(OfficePhase phase) => phase == OfficePhase.ExcelAuditing;

    private void SetWorking(bool value)
    {
        if (IsWorking == value) return;
        IsWorking = value;
        WorkingChanged?.Invoke(value);
    }

    private void SetSeated(bool value)
    {
        if (IsSeated == value) return;
        IsSeated = value;
        SeatedChanged?.Invoke(value);
    }

    private float ShowSubtitle(string speaker, string line, float seconds)
    {
        if (sleepDialogueProtected) return CurrentSubtitleSeconds;
        gameplayVoice?.Stop();
        if (gameplayVoices != null)
        {
            foreach (var recording in gameplayVoices.Lines)
            {
                if (recording.speakerId != speaker || recording.text != line || recording.voiceClip == null) continue;
                seconds = recording.ResolveDuration();
                gameplayVoice.Play(recording.voiceClip);
                if (GameManager.Instance != null && GameManager.Instance.IsPaused) gameplayVoice.Pause();
                break;
            }
        }
        CurrentSubtitleSeconds = seconds;
        guidanceSubtitleActive = false;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Subtitle(speaker, line, seconds));
        SpeakerChanged?.Invoke(speaker, seconds);
        return seconds;
    }

    public void ShowCinematicSubtitle(string speaker, string line, float seconds) =>
        ShowSubtitle(speaker, line, seconds);

    public void ShowGuidanceHint(string line)
    {
        if (sleepDialogueProtected) return;
        if (string.IsNullOrEmpty(line)) return;
        gameplayVoice?.Stop();
        SpeakerChanged?.Invoke(string.Empty, 0f);
        guidanceSubtitleActive = true;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Subtitle("GỢI Ý", line, 4.5f));
    }

    public void HideGuidanceHint()
    {
        if (!guidanceSubtitleActive) return;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = null;
        guidanceSubtitleActive = false;
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    public void SetBuiltInObjectiveVisible(bool visible)
    {
        builtInObjectiveVisible = visible;
        RefreshObjective();
    }

    public void HideCinematicSubtitle()
    {
        if (sleepDialogueProtected) return;
        gameplayVoice?.Stop();
        SpeakerChanged?.Invoke(string.Empty, 0f);
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = null;
        guidanceSubtitleActive = false;
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    public void CompleteOpeningControlReturn(bool revealObjective)
    {
        if (Phase != OfficePhase.Opening) return;
        SetPhase(OfficePhase.ReturnToDesk, revealObjective);
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
    }

    public void BeginExternalReturnToDesk(bool revealObjective)
    {
        if (!openingControlledExternally || Phase != OfficePhase.Opening) return;
        SetPhase(OfficePhase.ReturnToDesk, revealObjective);
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
    }

    public void AdoptExternalSeatedState()
    {
        if (!openingControlledExternally || playerTransform == null) return;
        SetSeated(true);
        FirstPersonController fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.IsMovementLocked = false;
            fpc.LockWalkingOnly = true;
            if (fpc.PlayerCamera != null) seatedCamY = fpc.PlayerCamera.localPosition.y;
        }
        SetPhase(OfficePhase.ReturnToDesk, false);
        ObjectiveText = "Mở báo cáo Excel trên máy tính.";
        SetControlsHintVisible(true);
        RefreshObjective();
    }

    public void RevealOpeningObjective()
    {
        if (Phase == OfficePhase.ReturnToDesk) RefreshObjective();
    }

    private IEnumerator Subtitle(string speaker, string line, float seconds)
    {
        if (subtitleText != null) subtitleText.text = BuildStyledSubtitle(speaker, line);
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        CanvasGroup group = subtitlePanel != null ? subtitlePanel.GetComponent<CanvasGroup>() : null;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            if (group != null) group.alpha = Mathf.Clamp01(elapsed / .15f);
            yield return null;
        }
        while (gameplayVoice != null && (gameplayVoice.IsPlaying || gameplayVoice.IsPaused)) yield return null;
        for (float fade = 0f; fade < .2f; fade += Time.deltaTime)
        {
            if (group != null) group.alpha = 1f - Mathf.Clamp01(fade / .2f);
            yield return null;
        }
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        subtitleRoutine = null;
        guidanceSubtitleActive = false;
    }

    private static string BuildStyledSubtitle(string speaker, string line)
    {
        string accent = speaker == "SẾP" ? "#FF786B" :
            speaker == "TÝ" ? "#F4C66A" :
            speaker == "HỆ THỐNG" ? "#69D7B2" : "#71CFE8";
        return "<align=center><size=78%><color=" + accent + "><b>" + speaker +
            "</b></color></size>\n<color=#F2F5F8>" + line + "</color></align>";
    }

    private void ConfigureHudPresentation()
    {
        if (subtitleText != null)
        {
            subtitleText.richText = true;
            subtitleText.enableAutoSizing = true;
            subtitleText.fontSizeMin = 20f;
            subtitleText.fontSizeMax = 28f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.lineSpacing = 6f;
        }

        if (subtitlePanel != null)
        {
            RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(.12f, 0f);
                panelRect.anchorMax = new Vector2(.88f, 0f);
                panelRect.anchoredPosition = new Vector2(0f, 112f);
                panelRect.sizeDelta = new Vector2(0f, 110f);
            }
            Image panelImage = subtitlePanel.GetComponent<Image>();
            if (panelImage != null) panelImage.enabled = false;
            if (subtitlePanel.GetComponentInChildren<CinematicSubtitleBackdrop>(true) == null)
            {
                var backdrop = new GameObject("SubtitleScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(CinematicSubtitleBackdrop));
                backdrop.transform.SetParent(subtitlePanel.transform, false);
                backdrop.transform.SetAsFirstSibling();
                var rect = (RectTransform)backdrop.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-30f, -14f);
                rect.offsetMax = new Vector2(30f, 14f);
                backdrop.GetComponent<CinematicSubtitleBackdrop>().raycastTarget = false;
            }
            if (subtitlePanel.GetComponent<CanvasGroup>() == null) subtitlePanel.AddComponent<CanvasGroup>();
            Canvas subtitleCanvas = subtitlePanel.GetComponent<Canvas>();
            if (subtitleCanvas == null) subtitleCanvas = subtitlePanel.AddComponent<Canvas>();
            subtitleCanvas.overrideSorting = true;
            subtitleCanvas.sortingOrder = 950;
        }

        if (objectiveText != null)
        {
            var objectivePanel = objectiveText.transform.parent;
            if (objectivePanel != null && objectivePanel.GetComponent<CanvasGroup>() == null)
                objectivePanel.gameObject.AddComponent<CanvasGroup>();
            objectiveText.richText = true;
            objectiveText.enableAutoSizing = true;
            objectiveText.fontSizeMin = 17f;
            objectiveText.fontSizeMax = 23f;
            CanvasGroup objectiveGroup = objectiveText.GetComponentInParent<CanvasGroup>();
            if (objectiveGroup != null)
            {
                objectiveGroup.alpha = 1f;
                objectiveGroup.interactable = false;
                objectiveGroup.blocksRaycasts = false;
            }
        }

        if (controlsHintText == null) return;
        controlsHintText.richText = true;
        controlsHintText.enableWordWrapping = false;
        controlsHintText.enableAutoSizing = true;
        controlsHintText.fontSizeMin = 14f;
        controlsHintText.fontSizeMax = 18f;
        controlsHintText.alignment = TextAlignmentOptions.Center;
        controlsHintText.text =
            "<color=#F4C66A><b>WASD</b></color> <color=#DCE5EC>Di chuyển</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>CHUỘT</b></color> <color=#DCE5EC>Quan sát</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>E</b></color> <color=#DCE5EC>Tương tác</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>ESC</b></color> <color=#DCE5EC>Tạm dừng</color>";
        RectTransform controlsRect = controlsHintText.rectTransform;
        controlsRect.anchorMin = new Vector2(.5f, 0f);
        controlsRect.anchorMax = new Vector2(.5f, 0f);
        controlsRect.anchoredPosition = new Vector2(0f, 30f);
        controlsRect.sizeDelta = new Vector2(1180f, 40f);
        Shadow shadow = controlsHintText.GetComponent<Shadow>();
        if (shadow == null) shadow = controlsHintText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    public bool ControlsHintVisible => controlsHintText != null && controlsHintText.gameObject.activeSelf;
    public void SetControlsHintVisible(bool visible)
    {
        if (controlsHintText != null) controlsHintText.gameObject.SetActive(visible);
    }

#if UNITY_EDITOR
    public void MakePrintingAvailableForTests()
    {
        if (printRoutine != null) StopCoroutine(printRoutine);
        SetPhase(OfficePhase.PrintingReport);
        printRoutine = StartCoroutine(PrintReportBeat());
    }

    public void MakeReportDeliveryAvailableForTests()
    {
        HasPrintedReport = true;
        SetPhase(OfficePhase.DeliverReport);
    }

    public void SkipOpeningForTests()
    {
        openingSequence?.SkipImmediately();
        if (openingRoutine != null) StopCoroutine(openingRoutine);
        openingRoutine = null;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        SetPhase(OfficePhase.ReturnToDesk);
        SetControlsHintVisible(true);
        GameManager.Instance?.ReleaseInput(this);
    }

    public void MakeRestAvailableForTests()
    {
        if (printRoutine != null) StopCoroutine(printRoutine);
        printRoutine = null;
        HasPrintedReport = true;
        ReportDelivered = true;
        WaterBreakCompleted = true;
        SetPhase(OfficePhase.Rest);
    }
#endif

    private void OnDestroy()
    {
        if (voicePauseOwner != null) voicePauseOwner.OnPauseChanged -= PauseGameplayVoice;
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }

    private void PauseGameplayVoice(bool paused)
    {
        if (paused) gameplayVoice?.Pause();
        else gameplayVoice?.Resume();
    }
}
