using System.Collections;
using TMPro;
using UnityEngine;

public enum OfficePhase { ReadScreen, InspectDocuments, RewriteReport, Rest, Leaving }
public enum OfficeInteractionKind { Laptop, Documents, Chair, Window, Boss }

// Act 1 only. Keeps exploration free while the boss speaks; input is locked only while typing/collapsing.
public class OfficeSceneController : MonoBehaviour
{
    public static OfficeSceneController Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshPro screenWarning;
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform chairAnchor;
    [SerializeField] private float chairUseDistance = 1.5f;
    [SerializeField] private float workSeconds = 4f;
    [SerializeField] private float bossReminderSeconds = 19f;
    [SerializeField] private Color dreamFade = new Color(1f, .84f, .47f);
    [SerializeField] private float seatedCamY = 0.9f;
    public OfficePhase Phase { get; private set; } = OfficePhase.ReadScreen;
    public bool IsWorking { get; private set; }
    public bool IsSeated { get; private set; }
    public float WorkProgress { get; private set; }
    public string ObjectiveText { get; private set; }
    private Coroutine subtitleRoutine;
    private Coroutine seatRoutine;
    private float standingCamY = 1.55f;
    private float reminderTimer;
    private float typingTimer;
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
        SetPhase(OfficePhase.ReadScreen);
        StartCoroutine(Opening());
    }
    private void CacheStandingCameraY()
    {
        if (playerTransform == null) return;
        var fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null && fpc.PlayerCamera != null)
            standingCamY = fpc.PlayerCamera.localPosition.y;
    }
    private IEnumerator Opening()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        if (Phase == OfficePhase.ReadScreen)
        {
            atmosphere?.PlayBoss(false);
            ShowSubtitle("SẾP", "Báo cáo làm thế này à? Cậu có biết dùng não không? Làm lại ngay!", 5f);
        }
    }
    private void Update()
    {
        if (IsWorking)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelWork(); return; }
            if (Input.GetMouseButton(0)) WorkFor(Time.deltaTime);
            return;
        }

        if (IsSeated && !IsWorking && Phase != OfficePhase.Leaving && GameManager.Instance?.IsPaused != true)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StandUp();
                return;
            }
        }

        if (Phase == OfficePhase.Rest)
        {
            if (IsSeated)
            {
                if (Input.GetKeyDown(KeyCode.E) && (GameManager.Instance == null || !GameManager.Instance.InputBlocked))
                {
                    HandleInteraction(OfficeInteractionKind.Chair);
                }
            }
            else
            {
                UpdateChairInteraction();
            }
            return;
        }

        if (Phase == OfficePhase.Leaving || GameManager.Instance?.IsPaused == true) return;
        reminderTimer += Time.deltaTime;
        if (reminderTimer >= bossReminderSeconds)
        {
            reminderTimer = 0;
            atmosphere?.PlayBoss(true);
            ShowSubtitle("SẾP", "Tôi cần bản sửa trước khi mọi người về hết!", 3.5f);
        }
    }
    private void UpdateChairInteraction()
    {
        if (GameManager.Instance?.InputBlocked == true || playerTransform == null || chairAnchor == null) return;
        Vector3 offset = playerTransform.position - chairAnchor.position;
        offset.y = 0;
        bool inRange = offset.sqrMagnitude <= chairUseDistance * chairUseDistance;
        if (inRange != nearChair)
        {
            nearChair = inRange;
            if (objectiveText != null)
                objectiveText.text = inRange
                    ? (IsSeated ? "CÔNG VIỆC: [E] Tựa lưng nghỉ ngơi" : "CÔNG VIỆC: [E] Ngồi xuống nghỉ một lát")
                    : "CÔNG VIỆC: " + ObjectiveText;
        }
        if (inRange && Input.GetKeyDown(KeyCode.E)) HandleInteraction(OfficeInteractionKind.Chair);
    }
    public string PromptFor(OfficeInteractionKind kind)
    {
        if (IsWorking || Phase == OfficePhase.Leaving) return "";
        switch (kind)
        {
            case OfficeInteractionKind.Laptop:
                if (!IsSeated) return "[E] Ngồi vào bàn làm việc";
                return Phase == OfficePhase.ReadScreen ? "[E] Đọc thông báo trên màn hình" :
                    Phase == OfficePhase.RewriteReport ? "[E] Làm lại báo cáo" : "";
            case OfficeInteractionKind.Documents:
                if (!IsSeated) return "[E] Ngồi vào bàn làm việc";
                return Phase == OfficePhase.InspectDocuments ? "[E] Kiểm tra chồng hồ sơ" : "";
            case OfficeInteractionKind.Chair:
                if (!IsSeated) return "[E] Ngồi vào bàn làm việc";
                return Phase == OfficePhase.Rest ? "[E] Tựa lưng nghỉ ngơi" : "";
            case OfficeInteractionKind.Window: return "[E] Nhìn ra phố";
            case OfficeInteractionKind.Boss: return "[E] Nhìn về phía sếp";
            default: return "";
        }
    }
    public void HandleInteraction(OfficeInteractionKind kind)
    {
        if (string.IsNullOrEmpty(PromptFor(kind))) return;
        reminderTimer = 0;
        switch (kind)
        {
            case OfficeInteractionKind.Laptop:
                if (!IsSeated)
                {
                    SitDown(false);
                    return;
                }
                if (Phase == OfficePhase.ReadScreen)
                {
                    ShowSubtitle("MÀN HÌNH", "BÁO CÁO BỊ TRẢ VỀ — LÀM LẠI NGAY.", 3.8f);
                    SetPhase(OfficePhase.InspectDocuments);
                }
                else if (Phase == OfficePhase.RewriteReport) StartWork();
                break;
            case OfficeInteractionKind.Documents:
                if (!IsSeated)
                {
                    SitDown(false);
                    return;
                }
                ShowSubtitle("HỒ SƠ", "Những trang giấy chồng lên nhau. Mình phải sửa lại từng con số.", 4f);
                SetPhase(OfficePhase.RewriteReport);
                break;
            case OfficeInteractionKind.Chair:
                if (Phase == OfficePhase.Rest)
                {
                    if (!IsSeated) SitDown(true);
                    SetPhase(OfficePhase.Leaving);
                    GameManager.Instance?.AcquireInput(this);
                    ShowSubtitle("QUÂN", "Tiếng còi xe xa dần. Mình chỉ muốn được nghỉ một lát...", 3f);
                    StartCoroutine(LeaveOffice());
                }
                else if (!IsSeated)
                {
                    SitDown(false);
                }
                else
                {
                    StandUp(false);
                }
                break;
            case OfficeInteractionKind.Window:
                ShowSubtitle("CỬA SỔ", "Ngoài kia, dòng người vẫn vội vã. Tiếng còi xe chẳng lúc nào ngớt.", 4f);
                break;
            case OfficeInteractionKind.Boss:
                ShowSubtitle("SẾP", "Ánh mắt ông ấy vẫn đang chờ bản báo cáo mới.", 3f);
                break;
        }
    }
    public void SitDown(bool instant = false)
    {
        if (IsSeated || playerTransform == null || chairAnchor == null) return;
        IsSeated = true;

        var fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.LockWalkingOnly = true;
            if (fpc.PlayerCamera != null && standingCamY <= 0.01f)
                standingCamY = fpc.PlayerCamera.localPosition.y;
        }

        Vector3 targetPos = new Vector3(chairAnchor.position.x, playerTransform.position.y, chairAnchor.position.z);
        Vector3 forward = chairAnchor.forward;
        forward.y = 0;
        Quaternion targetRot = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity;

        if (seatRoutine != null) StopCoroutine(seatRoutine);
        if (instant)
        {
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;
            if (cc != null) cc.enabled = true;

            if (fpc != null && fpc.PlayerCamera != null)
            {
                var camPos = fpc.PlayerCamera.localPosition;
                camPos.y = seatedCamY;
                fpc.PlayerCamera.localPosition = camPos;
                fpc.ResetCameraVerticalRotation(5f);
            }
            SetPhase(Phase);
        }
        else
        {
            seatRoutine = StartCoroutine(TransitionSeat(targetPos, targetRot, seatedCamY, true));
        }
    }
    public void StandUp(bool instant = false)
    {
        if (!IsSeated || playerTransform == null) return;
        IsSeated = false;

        Vector3 targetPos = new Vector3(chairAnchor.position.x, playerTransform.position.y, chairAnchor.position.z + 0.35f);

        if (seatRoutine != null) StopCoroutine(seatRoutine);
        if (instant)
        {
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = targetPos;
            if (cc != null) cc.enabled = true;

            var fpc = playerTransform.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                fpc.LockWalkingOnly = false;
                if (fpc.PlayerCamera != null)
                {
                    var camPos = fpc.PlayerCamera.localPosition;
                    camPos.y = standingCamY;
                    fpc.PlayerCamera.localPosition = camPos;
                }
            }
            SetPhase(Phase);
        }
        else
        {
            seatRoutine = StartCoroutine(TransitionSeat(targetPos, playerTransform.rotation, standingCamY, false));
        }
    }
    private IEnumerator TransitionSeat(Vector3 targetPos, Quaternion targetRot, float targetCamY, bool isSittingDown)
    {
        var fpc = playerTransform.GetComponent<FirstPersonController>();
        var cc = playerTransform.GetComponent<CharacterController>();
        Transform cam = fpc != null ? fpc.PlayerCamera : null;

        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;
        float startCamY = cam != null ? cam.localPosition.y : standingCamY;

        float duration = 0.45f;
        float elapsed = 0f;

        if (cc != null) cc.enabled = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            if (isSittingDown)
            {
                playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                if (fpc != null) fpc.ResetCameraVerticalRotation(Mathf.LerpAngle(cam != null ? cam.localEulerAngles.x : 0f, 5f, t));
            }

            if (cam != null)
            {
                var p = cam.localPosition;
                p.y = Mathf.Lerp(startCamY, targetCamY, t);
                cam.localPosition = p;
            }

            yield return null;
        }

        playerTransform.position = targetPos;
        if (isSittingDown) playerTransform.rotation = targetRot;
        if (cc != null) cc.enabled = true;

        if (cam != null)
        {
            var p = cam.localPosition;
            p.y = targetCamY;
            cam.localPosition = p;
        }

        if (fpc != null && !isSittingDown)
        {
            fpc.LockWalkingOnly = false;
        }

        SetPhase(Phase);
        seatRoutine = null;
    }
    private void SetPhase(OfficePhase next)
    {
        Phase = next;
        switch (next)
        {
            case OfficePhase.ReadScreen:
                ObjectiveText = IsSeated ? "Đọc thông báo trên màn hình laptop." : "Đến bàn làm việc, nhấn E để ngồi vào chỗ.";
                break;
            case OfficePhase.InspectDocuments:
                ObjectiveText = "Kiểm tra hồ sơ ngay trên bàn.";
                break;
            case OfficePhase.RewriteReport:
                ObjectiveText = "Tương tác với bàn để sửa báo cáo.";
                break;
            case OfficePhase.Rest:
                ObjectiveText = IsSeated ? "[E] Tựa lưng vào ghế để nghỉ ngơi." : "Quay lại ghế, nhấn E để nghỉ.";
                nearChair = false;
                break;
            default: ObjectiveText = "..."; break;
        }
        if (objectiveText != null)
        {
            string standHint = (IsSeated && next != OfficePhase.Rest && next != OfficePhase.Leaving) ? "  ([Space] Đứng dậy)" : "";
            objectiveText.text = "CÔNG VIỆC: " + ObjectiveText + standHint;
        }
        if (screenWarning != null)
            screenWarning.text = next >= OfficePhase.Rest ? "ĐÃ GỬI\n23:47" : "BÁO CÁO BỊ TRẢ VỀ\nLÀM LẠI NGAY";
    }
    private void StartWork()
    {
        if (IsWorking) return;
        IsWorking = true;
        GameManager.Instance?.AcquireInput(this);
        ProgressBarUI.Instance?.Show("Giữ chuột trái để sửa báo cáo · Esc: dừng");
        ProgressBarUI.Instance?.SetProgress(WorkProgress);
    }
    public void WorkFor(float seconds)
    {
        if (!IsWorking || Phase != OfficePhase.RewriteReport || seconds <= 0 || GameManager.Instance?.IsPaused == true) return;
        WorkProgress = Mathf.Clamp01(WorkProgress + seconds / Mathf.Max(.1f, workSeconds));
        ProgressBarUI.Instance?.SetProgress(WorkProgress);
        typingTimer += seconds;
        if (typingTimer >= .13f) { atmosphere?.PlayTyping(); typingTimer = 0; }
        if (WorkProgress < 1f) return;
        CancelWork();
        SetPhase(OfficePhase.Rest);
        atmosphere?.PlayBoss(true);
        ShowSubtitle("SẾP", "Cuối cùng cũng xong. Lần sau tập trung hơn đi!", 4f);
    }
    public void CancelWork()
    {
        IsWorking = false;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }
    private IEnumerator LeaveOffice()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act2_Home, dreamFade, 1.8f);
        GameManager.Instance?.ReleaseInput(this);
    }
    private void ShowSubtitle(string speaker, string line, float seconds)
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Subtitle(speaker, line, seconds));
    }
    private IEnumerator Subtitle(string speaker, string line, float seconds)
    {
        if (subtitleText != null) subtitleText.text = "<b>" + speaker + ":</b> " + line;
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        subtitleRoutine = null;
    }
    private void OnDestroy()
    {
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
}
