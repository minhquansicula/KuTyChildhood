using System.Collections;
using TMPro;
using UnityEngine;

public enum OfficePhase { Opening, ReturnToDesk, ProcessEmails, SortDocuments, FixReport, AfterWork, Rest, Leaving }
public enum OfficeInteractionKind { Laptop, Documents, Chair, Window, Boss }

/// <summary>Coordinates the complete Scene 1 narrative and work flow.</summary>
public sealed class OfficeSceneController : MonoBehaviour
{
    public static OfficeSceneController Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshPro screenWarning;
    [SerializeField] private TextMeshPro clockText;

    [Header("Scene systems")]
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField] private OfficeReportMiniGame reportMiniGame;
    [SerializeField] private OfficeOpeningSequence openingSequence;
    [SerializeField] private OfficeExhaustionSequence exhaustionSequence;
    [SerializeField] private Light afternoonLight;

    [Header("Player and chair")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform chairAnchor;
    [SerializeField] private float chairUseDistance = 1.5f;
    [SerializeField, Min(.1f)] private float chairStandDistance = .45f;
    [SerializeField] private float seatedCamY = .9f;

    [Header("Pacing")]
    [SerializeField] private float objectiveDelaySeconds = 2.5f;
    [SerializeField] private float bossReminderSeconds = 28f;
    [SerializeField] private float afterWorkFreedomSeconds = 15f;
    [SerializeField] private Color dreamFade = new Color(1f, .84f, .47f);

    public OfficePhase Phase { get; private set; } = OfficePhase.Opening;
    public bool IsWorking { get; private set; }
    public bool IsSeated { get; private set; }
    public float WorkProgress { get; private set; }
    public string ObjectiveText { get; private set; } = string.Empty;

    private Coroutine subtitleRoutine;
    private Coroutine seatRoutine;
    private Coroutine openingRoutine;
    private Coroutine afterWorkRoutine;
    private float standingCamY = 1.55f;
    private float reminderTimer;
    private bool nearChair;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    private void Start()
    {
        CacheStandingCameraY();
        SetPhase(OfficePhase.Opening);
        if (openingSequence != null && openingSequence.Play(this)) return;
        GameManager.Instance?.AcquireInput(this);
        openingRoutine = StartCoroutine(OpeningSequence());
    }

    private void Update()
    {
        if (IsWorking)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CancelWork();
            return;
        }
        if (Phase == OfficePhase.Opening || Phase == OfficePhase.Leaving || GameManager.Instance?.IsPaused == true) return;
        if (IsSeated && Input.GetKeyDown(KeyCode.Space)) { StandUp(); return; }
        if (Phase == OfficePhase.Rest && !IsSeated) UpdateChairInteraction();
        if (!IsWorkPhase(Phase)) return;

        reminderTimer += Time.deltaTime;
        if (reminderTimer < bossReminderSeconds) return;
        reminderTimer = 0f;
        atmosphere?.PlayBoss(true);
        ShowSubtitle("SẾP", "Tôi cần bản sửa trước khi mọi người về hết!", 3.5f);
    }

    private IEnumerator OpeningSequence()
    {
        yield return new WaitForSecondsRealtime(.7f);
        if (Phase != OfficePhase.Opening) yield break;
        float voiceSeconds = atmosphere != null ? atmosphere.PlayBoss(false) : 0f;
        yield return ShowOpeningBossSubtitles(voiceSeconds > .1f ? voiceSeconds : 7.5f);
        if (Phase != OfficePhase.Opening) yield break;

        float replySeconds = atmosphere != null ? atmosphere.PlayPlayerReply() : 0f;
        ShowSubtitle("QUÂN", "Dạ... dạ, để em sửa...", Mathf.Max(2.2f, replySeconds));
        yield return new WaitForSecondsRealtime(Mathf.Max(2.2f, replySeconds));
        atmosphere?.PlaySigh();
        yield return new WaitForSecondsRealtime(.8f);
        SetPhase(OfficePhase.ReturnToDesk, false);
        GameManager.Instance?.ReleaseInput(this);
        yield return new WaitForSecondsRealtime(objectiveDelaySeconds);
        if (Phase == OfficePhase.ReturnToDesk) RefreshObjective();
        openingRoutine = null;
    }

    private IEnumerator ShowOpeningBossSubtitles(float totalSeconds)
    {
        string[] lines =
        {
            "BÁO CÁO ĐÂU RỒI?!", "TÔI ĐÃ NÓI VỚI CẬU BAO NHIÊU LẦN RỒI?!",
            "CÁI NÀY MÀ CŨNG LÀM SAI ĐƯỢC ÀAAA?!", "CẢ PHÒNG ĐANG CHỜ MỖI MÌNH CẬU ĐẤY!",
            "HÔM NAY KHÔNG XONG THÌ ĐỪNG CÓ VỀ!", "LÀM LẠI NGAY CHO TÔI!"
        };
        float[] weights = { .12f, .17f, .17f, .18f, .20f, .16f };
        for (int i = 0; i < lines.Length; i++)
        {
            float seconds = totalSeconds * weights[i];
            ShowSubtitle("SẾP", lines[i], seconds + .05f);
            yield return new WaitForSecondsRealtime(seconds);
        }
    }

    public string PromptFor(OfficeInteractionKind kind)
    {
        if (IsWorking || Phase == OfficePhase.Opening || Phase == OfficePhase.Leaving) return string.Empty;
        if (kind == OfficeInteractionKind.Window) return "[E] Nhìn ra phố";
        if (kind == OfficeInteractionKind.Boss) return "[E] Nhìn về phía phòng họp";
        if (kind == OfficeInteractionKind.Chair)
        {
            if (!IsSeated) return "[E] Ngồi vào ghế";
            return Phase == OfficePhase.Rest ? "[E] Gục xuống nghỉ một chút" : string.Empty;
        }
        if (!IsSeated) return "[E] Ngồi vào bàn làm việc";

        switch (Phase)
        {
            case OfficePhase.ReturnToDesk: return "[E] Mở hộp thư công việc";
            case OfficePhase.ProcessEmails: return "[E] Tiếp tục xử lý email";
            case OfficePhase.SortDocuments: return "[E] Sắp xếp tài liệu trên bàn";
            case OfficePhase.FixReport: return "[E] Sửa báo cáo cho sếp";
            case OfficePhase.AfterWork: return "[E] Xem thông báo mới";
            case OfficePhase.Rest: return "[E] Nghỉ một chút";
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
            ShowSubtitle("QUÂN", "Bóng người sau lớp kính mờ vẫn khiến mình nghẹt thở.", 3.5f);
            return;
        }
        if (!IsSeated) { SitDown(false); return; }

        switch (Phase)
        {
            case OfficePhase.ReturnToDesk: SetPhase(OfficePhase.ProcessEmails); StartWork(OfficeWorkTask.Emails); break;
            case OfficePhase.ProcessEmails: StartWork(OfficeWorkTask.Emails); break;
            case OfficePhase.SortDocuments: StartWork(OfficeWorkTask.Documents); break;
            case OfficePhase.FixReport: StartWork(OfficeWorkTask.Report); break;
            case OfficePhase.AfterWork:
                atmosphere?.PlayEmailNotification();
                ShowSubtitle("MÀN HÌNH", "3 email mới. Nhưng hôm nay mình không còn sức nữa...", 4f);
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
            case OfficeMicroInteractionKind.WaterCooler: return "[E] Uống một ngụm nước";
            case OfficeMicroInteractionKind.PrinterPaper: return "[E] Nhặt tài liệu từ máy in";
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
                ShowSubtitle("QUÂN", Phase >= OfficePhase.AfterWork
                    ? "Hơn sáu giờ rồi... mọi người gần như đã về hết."
                    : "17:42. Deadline là 18:00. Mình còn quá nhiều việc.", 4f);
                break;
            case OfficeMicroInteractionKind.WaterCooler:
                atmosphere?.PlayWater();
                ShowSubtitle("QUÂN", "Một ngụm nước. Mình phải tỉnh táo thêm chút nữa.", 3f);
                break;
            case OfficeMicroInteractionKind.PrinterPaper:
                atmosphere?.PlayPrinter(); atmosphere?.PlayPaper();
                ShowSubtitle("TÀI LIỆU", "Bản in mới vẫn còn ấm. Lại thêm một việc cần xử lý.", 3.5f);
                break;
            case OfficeMicroInteractionKind.ColdCoffee:
                atmosphere?.PlayCup();
                ShowSubtitle("QUÂN", "Cà phê lạnh ngắt rồi... mình quên uống từ lúc nào nhỉ?", 3.5f);
                break;
            case OfficeMicroInteractionKind.ChildhoodMarble:
                atmosphere?.PlayMemoryBridge();
                ShowSubtitle("QUÂN", "Một viên bi cũ...? Lâu rồi mình không nhớ tới những buổi chiều ấy.", 4.5f);
                break;
        }
    }

    private void StartWork(OfficeWorkTask task)
    {
        if (IsWorking) return;
        IsWorking = true;
        GameManager.Instance?.AcquireInput(this);
        if (reportMiniGame != null && reportMiniGame.Open(this, task)) return;
        IsWorking = false;
        GameManager.Instance?.ReleaseInput(this);
        ShowSubtitle("HỆ THỐNG", "Giao diện công việc chưa được thiết lập.", 3f);
    }

    public void CompleteWorkTask(OfficeWorkTask completed)
    {
        IsWorking = false;
        reportMiniGame?.ClosePanel();
        GameManager.Instance?.ReleaseInput(this);
        switch (completed)
        {
            case OfficeWorkTask.Emails:
                SetPhase(OfficePhase.SortDocuments);
                atmosphere?.PlayEmailNotification();
                ShowSubtitle("HỆ THỐNG", "Đã xử lý email. Tài liệu trên bàn vẫn đang chờ.", 3.2f);
                break;
            case OfficeWorkTask.Documents:
                SetPhase(OfficePhase.FixReport);
                atmosphere?.PlayPrinter();
                ShowSubtitle("QUÂN", "Giấy tờ đã đúng khay. Giờ chỉ còn bản báo cáo.", 3.2f);
                break;
            case OfficeWorkTask.Report:
                WorkProgress = 1f;
                SetPhase(OfficePhase.AfterWork);
                atmosphere?.PlayEmailNotification();
                if (afternoonLight != null) afternoonLight.intensity *= .62f;
                if (afterWorkRoutine != null) StopCoroutine(afterWorkRoutine);
                afterWorkRoutine = StartCoroutine(AfterWorkBeat());
                break;
        }
    }

    public void UpdateReportMiniGameProgress(float progress) => WorkProgress = Mathf.Clamp01(progress);
    public void PlayReportTypingFeedback() => atmosphere?.PlayTyping();

    public void CancelWork()
    {
        IsWorking = false;
        reportMiniGame?.ClosePanel();
        GameManager.Instance?.ReleaseInput(this);
        RefreshObjective();
    }

    private IEnumerator AfterWorkBeat()
    {
        ShowSubtitle("HỆ THỐNG", "REPORT SENT SUCCESSFULLY", 2.5f);
        yield return new WaitForSecondsRealtime(afterWorkFreedomSeconds);
        if (Phase == OfficePhase.AfterWork)
        {
            atmosphere?.PlayEmailNotification();
            ShowSubtitle("QUÂN", "Lại có email mới... nhưng mình không thể tiếp tục nữa.", 4f);
            SetPhase(OfficePhase.Rest);
        }
        afterWorkRoutine = null;
    }

    private void BeginExhaustion()
    {
        if (Phase == OfficePhase.Leaving) return;
        SetPhase(OfficePhase.Leaving);
        GameManager.Instance?.AcquireInput(this);
        atmosphere?.PlaySigh();
        ShowSubtitle("QUÂN", "Mệt quá... chỉ nghỉ một chút thôi...", 3f);
        if (exhaustionSequence != null && exhaustionSequence.Play(LoadMemoryWorld)) return;
        StartCoroutine(FallbackLeaveOffice());
    }

    private IEnumerator FallbackLeaveOffice()
    {
        yield return new WaitForSecondsRealtime(4f);
        LoadMemoryWorld();
    }

    private void LoadMemoryWorld()
    {
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act2, dreamFade, 1.8f);
        GameManager.Instance?.ReleaseInput(this);
    }

    private void SetPhase(OfficePhase next, bool showObjective = true)
    {
        Phase = next;
        switch (next)
        {
            case OfficePhase.Opening: ObjectiveText = string.Empty; break;
            case OfficePhase.ReturnToDesk: ObjectiveText = "Quay lại bàn làm việc."; break;
            case OfficePhase.ProcessEmails: ObjectiveText = "Xử lý 3 email khẩn."; break;
            case OfficePhase.SortDocuments: ObjectiveText = "Sắp xếp tài liệu vào đúng khay."; break;
            case OfficePhase.FixReport: ObjectiveText = "Đối chiếu số liệu và gửi lại báo cáo."; break;
            case OfficePhase.AfterWork: ObjectiveText = "Công việc đã gửi. Có thể nhìn quanh một chút."; break;
            case OfficePhase.Rest:
                ObjectiveText = IsSeated ? "Nghỉ một chút." : "Quay lại ghế và nghỉ một chút.";
                nearChair = false;
                break;
            default: ObjectiveText = "..."; break;
        }
        if (screenWarning != null)
            screenWarning.text = next >= OfficePhase.AfterWork ? "REPORT SENT SUCCESSFULLY\n18:17" : "3 EMAIL KHẨN\nDEADLINE 18:00";
        if (clockText != null) clockText.text = next >= OfficePhase.AfterWork ? "18:17" : "17:42";
        if (showObjective) RefreshObjective();
    }

    private void RefreshObjective()
    {
        if (objectiveText == null) return;
        if (string.IsNullOrEmpty(ObjectiveText)) { objectiveText.text = string.Empty; return; }
        string hint = IsSeated && Phase != OfficePhase.Rest && Phase != OfficePhase.Leaving ? "  ([Space] Đứng dậy)" : string.Empty;
        objectiveText.text = "CÔNG VIỆC: " + ObjectiveText + hint;
    }

    public void SitDown(bool instant = false)
    {
        if (IsSeated || playerTransform == null || chairAnchor == null) return;
        IsSeated = true;
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
        if (!IsSeated || playerTransform == null) return;
        IsSeated = false;
        Vector3 targetPosition = ChairPositionAtPlayerHeight() - GetChairForward() * chairStandDistance;
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

    private static bool IsWorkPhase(OfficePhase phase) =>
        phase == OfficePhase.ProcessEmails || phase == OfficePhase.SortDocuments || phase == OfficePhase.FixReport;

    private void ShowSubtitle(string speaker, string line, float seconds)
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Subtitle(speaker, line, seconds));
    }

    public void ShowCinematicSubtitle(string speaker, string line, float seconds) =>
        ShowSubtitle(speaker, line, seconds);

    public void HideCinematicSubtitle()
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = null;
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    public void CompleteOpeningControlReturn(bool revealObjective)
    {
        if (Phase != OfficePhase.Opening) return;
        SetPhase(OfficePhase.ReturnToDesk, revealObjective);
        GameManager.Instance?.ReleaseInput(this);
    }

    public void RevealOpeningObjective()
    {
        if (Phase == OfficePhase.ReturnToDesk) RefreshObjective();
    }

    private IEnumerator Subtitle(string speaker, string line, float seconds)
    {
        if (subtitleText != null) subtitleText.text = "<b>" + speaker + ":</b> " + line;
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        subtitleRoutine = null;
    }

#if UNITY_EDITOR
    public void SkipOpeningForTests()
    {
        openingSequence?.SkipImmediately();
        if (openingRoutine != null) StopCoroutine(openingRoutine);
        openingRoutine = null;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        SetPhase(OfficePhase.ReturnToDesk);
        GameManager.Instance?.ReleaseInput(this);
    }

    public void MakeRestAvailableForTests()
    {
        if (afterWorkRoutine != null) StopCoroutine(afterWorkRoutine);
        afterWorkRoutine = null;
        SetPhase(OfficePhase.Rest);
    }
#endif

    private void OnDestroy()
    {
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
}
