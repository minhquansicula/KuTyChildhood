using UnityEngine;
using UnityEngine.UI;

/// <summary>Tracks genuine lack of progress and reveals help in two gentle stages.</summary>
public sealed class OfficeGuidanceController : MonoBehaviour
{
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private OfficeIntroController intro;
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(1f)] private float textHintSeconds = 20f;
    [SerializeField, Min(1f)] private float markerSeconds = 40f;

    private readonly OfficeGuidanceState state = new OfficeGuidanceState();
    private Transform chairTarget;
    private Transform workstationTarget;
    private Transform printerTarget;
    private Transform bossTarget;
    private Transform waterTarget;
    private CanvasGroup markerGroup;
    private RectTransform markerRect;

    public bool HintShown => state.HintShown;
    public bool MarkerVisible => markerGroup != null && markerGroup.alpha > 0f;
    public Transform CurrentTarget => ResolveTarget();

    private void Awake()
    {
        if (office == null) office = GetComponent<OfficeSceneController>();
        if (office == null) office = OfficeSceneController.Instance;
        if (intro == null) intro = FindObjectOfType<OfficeIntroController>();
        if (playerCamera == null) playerCamera = Camera.main;
        CacheTargets();
        CreateMarker();
        state.Configure(textHintSeconds, Mathf.Max(textHintSeconds, markerSeconds));
    }

    private void OnEnable()
    {
        if (office != null)
        {
            office.PhaseChanged += HandleProgress;
            office.SeatedChanged += HandleSeatedChanged;
            office.WorkingChanged += HandleWorkingChanged;
        }
        if (intro != null)
        {
            intro.PlayerExitedBossRoom += ResetGuidance;
            intro.ReadyAtDesk += ResetGuidance;
        }
    }

    private void OnDisable()
    {
        if (office != null)
        {
            office.PhaseChanged -= HandleProgress;
            office.SeatedChanged -= HandleSeatedChanged;
            office.WorkingChanged -= HandleWorkingChanged;
        }
        if (intro != null)
        {
            intro.PlayerExitedBossRoom -= ResetGuidance;
            intro.ReadyAtDesk -= ResetGuidance;
        }
        SetMarkerVisible(false);
        office?.HideGuidanceHint();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && CanGuide()) ShowCurrentHint();

        OfficeGuidanceReveal reveal = state.Advance(Time.deltaTime, CanGuide());
        if (reveal == OfficeGuidanceReveal.Text) ShowCurrentHint();
        if (reveal == OfficeGuidanceReveal.Marker) SetMarkerVisible(ResolveTarget() != null);
        SetMarkerVisible(CanGuide() && state.MarkerShown && ResolveTarget() != null);
        UpdateMarkerPosition();
    }

    private bool CanGuide()
    {
        if (office == null || office.IsWorking) return false;
        if (office.Phase == OfficePhase.Opening || office.Phase == OfficePhase.Leaving) return false;
        return GameManager.Instance == null || !GameManager.Instance.InputBlocked;
    }

    private void ShowCurrentHint()
    {
        string hint = ResolveHint();
        if (!string.IsNullOrEmpty(hint)) office?.ShowGuidanceHint(hint);
    }

    private string ResolveHint()
    {
        if (intro != null && !intro.GameplayHandoffComplete && !intro.ExitedBossRoom)
            return "Cửa phòng sếp ở ngay phía trước. Lại gần và nhấn E để mở.";
        if (office == null) return string.Empty;
        switch (office.Phase)
        {
            case OfficePhase.ReturnToDesk:
                return office.IsSeated
                    ? "Nhìn vào màn hình máy tính và nhấn E để mở báo cáo."
                    : "Tìm ghế ở bàn làm việc của Tý, lại gần và nhấn E.";
            case OfficePhase.ExcelAuditing:
                return office.IsSeated
                    ? "Nhìn vào màn hình máy tính và nhấn E để mở lại báo cáo."
                    : "Quay lại ghế của Tý và nhấn E để ngồi, rồi mở lại báo cáo.";
            case OfficePhase.PrintingReport:
                return "Máy photo đang in ở khu tiện ích cạnh bình nước.";
            case OfficePhase.CollectPrint:
                return "Đến máy photo cạnh bình nước và nhấn E để lấy báo cáo.";
            case OfficePhase.DeliverReport:
                return "Mở cửa vào phòng sếp, đến sát bàn rồi nhấn E để đưa báo cáo.";
            case OfficePhase.WaterBreak:
                return "Bình nước nằm ngay cạnh máy photo. Lại gần và nhấn E.";
            case OfficePhase.Rest:
                return office.IsSeated
                    ? "Nhìn về bàn làm việc và nhấn E để nghỉ một chút."
                    : "Quay lại ghế của Tý, ngồi xuống rồi nhấn E để nghỉ.";
            default:
                return string.Empty;
        }
    }

    private Transform ResolveTarget()
    {
        if (intro != null && !intro.GameplayHandoffComplete && !intro.ExitedBossRoom)
            return intro.door != null ? intro.door.transform : bossTarget;
        if (office == null) return null;
        switch (office.Phase)
        {
            case OfficePhase.ReturnToDesk: return office.IsSeated ? workstationTarget : chairTarget;
            case OfficePhase.ExcelAuditing: return office.IsSeated ? workstationTarget : chairTarget;
            case OfficePhase.PrintingReport:
            case OfficePhase.CollectPrint: return printerTarget;
            case OfficePhase.DeliverReport: return bossTarget;
            case OfficePhase.WaterBreak: return waterTarget;
            case OfficePhase.Rest: return office.IsSeated ? workstationTarget : chairTarget;
            default: return null;
        }
    }

    private void CacheTargets()
    {
        OfficeBlockoutChairInteractable chair = FindObjectOfType<OfficeBlockoutChairInteractable>(true);
        OfficeWorkstationInteractable workstation = FindObjectOfType<OfficeWorkstationInteractable>(true);
        chairTarget = chair != null ? chair.transform : null;
        bossTarget = null;
        workstationTarget = workstation != null ? workstation.transform : null;
        foreach (OfficeMicroInteractable micro in FindObjectsOfType<OfficeMicroInteractable>(true))
        {
            if (micro.Kind == OfficeMicroInteractionKind.PrinterPaper) printerTarget = micro.transform;
            if (micro.Kind == OfficeMicroInteractionKind.WaterCooler) waterTarget = micro.transform;
        }
        foreach (OfficeInteractable interactable in FindObjectsOfType<OfficeInteractable>(true))
            if (bossTarget == null && interactable.Kind == OfficeInteractionKind.Boss) bossTarget = interactable.transform;
    }

    private void CreateMarker()
    {
        var root = new GameObject("OfficeGuidanceMarker", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        markerGroup = root.GetComponent<CanvasGroup>();
        markerGroup.interactable = markerGroup.blocksRaycasts = false;

        var marker = new GameObject("Target", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(root.transform, false);
        markerRect = (RectTransform)marker.transform;
        markerRect.anchorMin = markerRect.anchorMax = new Vector2(.5f, .5f);
        markerRect.sizeDelta = new Vector2(76f, 76f);
        Image image = marker.GetComponent<Image>();
        image.color = new Color(.05f, .08f, .11f, .9f);
        image.raycastTarget = false;

        // Draw the diamond geometrically so it never depends on font glyph coverage.
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(marker.transform, false);
        RectTransform iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .5f);
        iconRect.sizeDelta = new Vector2(22f, 22f);
        iconRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        icon.GetComponent<Image>().color = new Color(.96f, .77f, .32f, 1f);
        icon.GetComponent<Image>().raycastTarget = false;
        SetMarkerVisible(false);
    }

    private void UpdateMarkerPosition()
    {
        if (!MarkerVisible) return;
        Transform target = ResolveTarget();
        if (target == null || playerCamera == null)
        {
            SetMarkerVisible(false);
            return;
        }

        Vector3 screen = playerCamera.WorldToScreenPoint(target.position + Vector3.up * .65f);
        if (screen.z < 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }
        const float margin = 54f;
        screen.x = Mathf.Clamp(screen.x, margin, Screen.width - margin);
        screen.y = Mathf.Clamp(screen.y, margin, Screen.height - margin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markerRect.parent as RectTransform, screen, null, out Vector2 local);
        markerRect.anchoredPosition = local;
    }

    private void SetMarkerVisible(bool visible)
    {
        if (markerGroup != null) markerGroup.alpha = visible ? 1f : 0f;
    }

    private void HandleProgress(OfficePhase previous, OfficePhase current) => ResetGuidance();
    private void HandleSeatedChanged(bool seated) => ResetGuidance();
    private void HandleWorkingChanged(bool working)
    {
        if (!working) ResetGuidance();
        else SetMarkerVisible(false);
    }

    private void ResetGuidance()
    {
        state.Reset();
        SetMarkerVisible(false);
        office?.HideGuidanceHint();
        if (chairTarget == null || workstationTarget == null) CacheTargets();
    }

#if UNITY_EDITOR
    public void ConfigureThresholdsForTests(float textSeconds, float targetSeconds)
    {
        state.Configure(textSeconds, targetSeconds);
        ResetGuidance();
    }

    public void AdvanceForTests(float seconds)
    {
        OfficeGuidanceReveal reveal = state.Advance(seconds, true);
        if (reveal == OfficeGuidanceReveal.Text) ShowCurrentHint();
        if (reveal == OfficeGuidanceReveal.Marker) SetMarkerVisible(ResolveTarget() != null);
    }
#endif
}

public enum OfficeGuidanceReveal { None, Text, Marker }

/// <summary>Pure state so hint timing can be checked without relying on frame timing.</summary>
public sealed class OfficeGuidanceState
{
    private float textAt = 20f;
    private float markerAt = 40f;
    private float elapsed;

    public bool HintShown { get; private set; }
    public bool MarkerShown { get; private set; }

    public void Configure(float textSeconds, float markerSeconds)
    {
        textAt = Mathf.Max(.01f, textSeconds);
        markerAt = Mathf.Max(textAt, markerSeconds);
    }

    public OfficeGuidanceReveal Advance(float deltaTime, bool canAdvance)
    {
        if (!canAdvance || deltaTime <= 0f) return OfficeGuidanceReveal.None;
        elapsed += deltaTime;
        if (!HintShown && elapsed >= textAt)
        {
            HintShown = true;
            return OfficeGuidanceReveal.Text;
        }
        if (!MarkerShown && elapsed >= markerAt)
        {
            MarkerShown = true;
            return OfficeGuidanceReveal.Marker;
        }
        return OfficeGuidanceReveal.None;
    }

    public void Reset()
    {
        elapsed = 0f;
        HintShown = false;
        MarkerShown = false;
    }
}
