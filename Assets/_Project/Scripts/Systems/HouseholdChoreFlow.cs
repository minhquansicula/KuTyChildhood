using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HouseholdChore
{
    FetchWater,
    MeasureRice,
    WashRice,
    LightFire,
    CookRice,
    SetTable,
    WashDishes,
    PutAwayDishes,
    FoldBlanket, ArrangeKeepsakes, TuneRadio, SortTools, GrindCorn, CarryWood, ReadLetter
}

/// <summary>Scene 2's ordered household chores and their one-time memory-piece rewards.</summary>
public sealed class HouseholdChoreFlow : MonoBehaviour
{
    public static HouseholdChoreFlow Instance { get; private set; }
    public const int PiecesPerChore = 10;
    public const int OptionalPiecesPerChore = 5;
    public const int ChoreCount = 12;
    public const int TotalPieces = PiecesPerChore * ChoreCount + OptionalPiecesPerChore * 2;
    public const string WakeupText = "Con dậy rồi đấy à. Vào dọn đồ, phụ mẹ một tay nhé.";
    public const string ResumeText = "Mình tiếp tục việc đang làm nhé.";

    public static readonly string[] Titles =
    {
        "Múc nước", "Đong gạo", "Vo gạo", "Nhóm lửa",
        "Nấu cơm", "Dọn mâm cơm", "Rửa chén", "Cất chén",
        "Gấp chăn", "Xếp đồ vào rương", "Chỉnh radio", "Xếp dụng cụ", "Xay ngô", "Gom củi", "Mở thư của mẹ"
    };

    // Keep serialized enum IDs stable; this array defines story order only.
    public static readonly HouseholdChore[] Order = {
        HouseholdChore.ArrangeKeepsakes, HouseholdChore.TuneRadio,
        HouseholdChore.CarryWood,
        HouseholdChore.FetchWater, HouseholdChore.MeasureRice, HouseholdChore.WashRice,
        HouseholdChore.LightFire, HouseholdChore.CookRice, HouseholdChore.SetTable,
        HouseholdChore.WashDishes, HouseholdChore.PutAwayDishes, HouseholdChore.ReadLetter
    };
    public static readonly string[] Locations = {
        "Bếp · lu nước", "Bếp · lu gạo", "Bếp · thau vo gạo", "Bếp · nồi trên bếp củi",
        "Bếp · lò nấu cơm", "Nhà chính · mâm cơm", "Bếp · chỗ rửa", "Bếp · tủ chén",
        "Phòng ngủ · mặt chăn", "Phòng ngủ · rương đồ", "Phòng ngủ · radio",
        "Phòng kho · bộ dụng cụ", "Phòng kho · cối xay", "Phòng kho · giỏ tre", "Nhà chính · thư của mẹ"
    };
    public static readonly string[] Memories = {
        "Gáo nước mát nhờ mẹ để dành từ sáng.", "Mẹ luôn đong thêm một chút cho con.",
        "Nước đục đi, hạt gạo lại trắng.", "Bếp bắt lửa, cả nhà bắt đầu ấm.",
        "Mùi cơm chín khiến con biết sắp được gọi về.", "Mâm cơm đủ người là bữa ngon nhất.",
        "Mẹ rửa, con tráng, hai mẹ con chuyện trò.", "Tiếng chén khẽ chạm nhau, việc nhà đã xong.",
        "Mẹ bảo: chăn gọn thì ngày mới cũng gọn.", "Chiếc rương giữ cả những mùa áo cũ.",
        "Tiếng radio mở đầu mỗi buổi sáng của nhà mình.", "Dùng xong nhớ cất, mai còn tìm được con nhé.",
        "Tiếng cối quay đều như nhịp tay mẹ.", "Bó củi nhỏ mang theo hơi ấm cho cả nhà.",
        "Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. Nhà mình luôn có một chỗ chờ con."
    };
    public string CurrentTitle => ActiveIndex < ChoreCount ? Titles[(int)Order[ActiveIndex]] : "Đã hoàn tất";
    public string CurrentLocation => ActiveIndex < ChoreCount ? Locations[(int)Order[ActiveIndex]] : "";

    private readonly bool[] completed = new bool[15];
    [SerializeField] private Transform riceWashingPot;
    [SerializeField] private HouseholdChoreInteractable riceWashingTarget;
    [SerializeField] private Collider riceWashingPotBaseCollider;
    private bool riceWashingUsesMeshColliders;
    private HouseholdChoreUI ui;
    public HouseholdVoicePlayer Voice { get; private set; }
    private bool checklistVisible;
    private int pieces;

    public int Pieces => pieces;
    private int OptionalBits => (completed[(int)HouseholdChore.SortTools] ? 1 : 0) |
        (completed[(int)HouseholdChore.GrindCorn] ? 2 : 0);
    public static bool IsOptional(HouseholdChore chore) =>
        chore == HouseholdChore.SortTools || chore == HouseholdChore.GrindCorn;
    public bool ChecklistVisible => checklistVisible;
    public int ActiveIndex
    {
        get
        {
            for (int i = 0; i < Order.Length; i++)
                if (!completed[(int)Order[i]]) return i;
            return Order.Length;
        }
    }

    public HouseholdChoreUI UI => ui;
    public event Action<HouseholdChore, int> OnChoreCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Voice = gameObject.AddComponent<HouseholdVoicePlayer>();
        int restored = Scene2Checkpoint.HomeChores;
        for (int i = 0; i < restored; i++) completed[(int)Order[i]] = true;
        int restoredOptional = Scene2Checkpoint.HomeOptional;
        completed[(int)HouseholdChore.SortTools] = (restoredOptional & 1) != 0;
        completed[(int)HouseholdChore.GrindCorn] = (restoredOptional & 2) != 0;
        pieces = restored * PiecesPerChore +
            (completed[(int)HouseholdChore.SortTools] ? OptionalPiecesPerChore : 0) +
            (completed[(int)HouseholdChore.GrindCorn] ? OptionalPiecesPerChore : 0);
        BindRiceWashingTarget();
        RefreshRiceWashingPot();
        EnsureDiningTableTarget();
        EnsureSolidSceneProps();
        ui = new HouseholdChoreUI(transform);
        ui.SetChecklistVisible(false);
        if (GetComponent<Scene2QuestHUD>() == null) gameObject.AddComponent<Scene2QuestHUD>();
    }

    private static void EnsureDiningTableTarget()
    {
        // BambooTable remains solid scenery; the meal is served on tripo_part_3.
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (candidate.name != "BambooTable" ||
                !(candidate.GetComponent<Collider>() is BoxCollider tableCollider)) continue;
            FitSolidBoxToVisualBounds(candidate, tableCollider);
            break;
        }

        foreach (MeshCollider surface in FindObjectsOfType<MeshCollider>(true))
        {
            if (surface.sharedMesh == null ||
                surface.sharedMesh.name != "tripo_part_3 Instance") continue;
            var target = surface.GetComponent<HouseholdChoreInteractable>();
            if (target == null) target = surface.gameObject.AddComponent<HouseholdChoreInteractable>();
            int interactionLayer = LayerMask.NameToLayer("Interactable");
            if (interactionLayer >= 0) surface.gameObject.layer = interactionLayer;
            target.Configure(HouseholdChore.SetTable, surface.transform);
            return;
        }
        Debug.LogWarning("Scene 2: tripo_part_3 mesh collider not found; Dọn mâm cơm has no interaction point.");
    }

    private static void FitSolidBoxToVisualBounds(Transform table, BoxCollider collider)
    {
        // Imported table meshes use a large root scale. Refit the solid collider to
        // the visible geometry so the CharacterController cannot pass through it.
        var visuals = table.GetComponentsInChildren<Renderer>(true);
        if (visuals.Length == 0) return;

        Bounds visualBounds = visuals[0].bounds;
        for (int i = 1; i < visuals.Length; i++) visualBounds.Encapsulate(visuals[i].bounds);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = table.InverseTransformPoint(new Vector3(
                        x == 0 ? visualBounds.min.x : visualBounds.max.x,
                        y == 0 ? visualBounds.min.y : visualBounds.max.y,
                        z == 0 ? visualBounds.min.z : visualBounds.max.z));
                    min = Vector3.Min(min, corner);
                    max = Vector3.Max(max, corner);
                }
        collider.center = (min + max) * .5f;
        collider.size = max - min;
        collider.isTrigger = false;
        collider.enabled = true;
    }

    private static void EnsureSolidSceneProps()
    {
        // Keep the two kitchen curtains passable, even if their imported prefabs
        // acquire colliders later. Interaction triggers elsewhere remain intact.
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (candidate.name != "rembep" && candidate.name != "rembep (1)") continue;
            foreach (Collider curtainCollider in candidate.GetComponentsInChildren<Collider>(true))
                if (!curtainCollider.isTrigger) curtainCollider.enabled = false;
        }

        // All four GrainPile instances had no scene collider. A solid volume over
        // the visible pile guarantees the player cannot walk through the grain.
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (candidate.name != "GrainPile" && candidate.name != "GrainPile (1)" &&
                candidate.name != "GrainPile (2)" && candidate.name != "GrainPile (3)") continue;
            if (candidate.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
            BoxCollider pileCollider = candidate.GetComponent<BoxCollider>();
            if (pileCollider == null) pileCollider = candidate.gameObject.AddComponent<BoxCollider>();
            FitSolidBoxToVisualBounds(candidate, pileCollider);
        }

        int added = 0;
        foreach (MeshFilter mesh in FindObjectsOfType<MeshFilter>(true))
        {
            MeshRenderer visual = mesh.GetComponent<MeshRenderer>();
            if (!mesh.gameObject.activeInHierarchy || mesh.sharedMesh == null ||
                visual == null || !visual.enabled ||
                IsKitchenCurtain(mesh.transform) || HasSolidCollider(mesh.transform) ||
                mesh.GetComponentInParent<FirstPersonController>() != null) continue;

            var solid = mesh.gameObject.AddComponent<MeshCollider>();
            solid.sharedMesh = mesh.sharedMesh;
            solid.convex = false;
            solid.isTrigger = false;
            added++;
        }
        if (added > 0) Debug.Log($"Scene 2: added {added} solid mesh colliders to visible props.");
    }

    private static bool IsKitchenCurtain(Transform current)
    {
        for (; current != null; current = current.parent)
            if (current.name == "rembep" || current.name == "rembep (1)") return true;
        return false;
    }

    private static bool HasSolidCollider(Transform current)
    {
        for (; current != null; current = current.parent)
            foreach (Collider collider in current.GetComponents<Collider>())
                if (collider.enabled && !collider.isTrigger) return true;
        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        ui?.Tick();
        if (!checklistVisible || QuestManager.Instance == null ||
            QuestManager.Instance.CurrentStep != QuestStep.WashDishes ||
            (GameManager.Instance != null && GameManager.Instance.InputBlocked) ||
            PauseUI.LastResumeFrame == Time.frameCount || !Input.GetKeyDown(KeyCode.P)) return;

        ResetHomeChores();
    }

    private void ResetHomeChores()
    {
        for (int i = 0; i < completed.Length; i++) completed[i] = false;
        pieces = 0;
        RefreshRiceWashingPot();
        Scene2Checkpoint.Clear();
        RefreshUI();
        ui.Notify("Đã làm lại việc nhà từ đầu. Hãy đến rương đồ để bắt đầu.");
        QuestManager.Instance?.RefreshHouseholdObjective();
    }

    public void ShowChecklist()
    {
        if (checklistVisible) return;
        checklistVisible = true;
        // The original compact checklist remains a fallback when the shared Scene 2 board is unavailable.
        ui.SetChecklistVisible(GetComponent<Scene2QuestHUD>() == null);
        RefreshUI();
        bool resumed = ActiveIndex > 0 || OptionalBits != 0;
        float seconds = Voice.Play(resumed ? "act2_mom_resume_001" : "act2_mom_wakeup_001");
        ui.Notify("Mẹ: " + (resumed ? ResumeText : WakeupText) + " Tab: bảng nhiệm vụ · H: gợi ý.", Mathf.Max(7f, seconds));
        QuestManager.Instance?.RefreshHouseholdObjective();
    }

    public bool IsCurrent(HouseholdChore chore) => checklistVisible &&
        (QuestManager.Instance == null || QuestManager.Instance.CurrentStep == QuestStep.WashDishes) &&
        (IsOptional(chore) ? !completed[(int)chore] :
            ActiveIndex < ChoreCount && Order[ActiveIndex] == chore);

    public bool Complete(HouseholdChore chore)
    {
        int index = (int)chore;
        if (!IsCurrent(chore) || completed[index]) return false;
        if (chore == HouseholdChore.ReadLetter &&
            (QuestManager.Instance == null || !QuestManager.Instance.CanFinishHouseholdChores))
        {
            Debug.LogError("Cannot finish Scene 2: story systems or the outdoor scene are missing.", this);
            ui.Notify("Chưa thể hoàn tất: cảnh ngoài sân hoặc hệ thống nhiệm vụ chưa sẵn sàng.");
            return false;
        }
        completed[index] = true;
        bool optional = IsOptional(chore);
        pieces += optional ? OptionalPiecesPerChore : PiecesPerChore;
        RefreshRiceWashingPot();
        if (ActiveIndex < ChoreCount) Scene2Checkpoint.SaveHome(ActiveIndex, OptionalBits);
        RefreshUI();
        float voiceSeconds = chore != HouseholdChore.ReadLetter ? Voice.Play(HouseholdVoicePlayer.ChoreId(chore)) : 0f;
        bool finishedRequired = !optional && ActiveIndex == ChoreCount;
        ui.Notify(finishedRequired
            ? pieces + "/" + TotalPieces + " mảnh ký ức — Nhà mình vẫn có một chỗ chờ con."
            : "+" + (optional ? OptionalPiecesPerChore : PiecesPerChore) + " mảnh ký ức — " + Memories[index],
            Mathf.Max(7f, voiceSeconds));
        ui.PlayMemoryReward(finishedRequired);
        AudioManager.Instance?.PlaySFX(SfxId.MemoryCollected);
        OnChoreCompleted?.Invoke(chore, pieces);
        QuestManager.Instance?.RefreshHouseholdObjective();
        if (finishedRequired) QuestManager.Instance?.CompleteHouseholdChores();
        return true;
    }

    private void BindRiceWashingTarget()
    {
        if (riceWashingPot == null || riceWashingPotBaseCollider == null) return;

        // Imported FBX renderers have no colliders. Give their visible meshes precise
        // raycast surfaces instead of using the large noivogao box around empty space.
        int interactionLayer = LayerMask.NameToLayer("Interactable");
        foreach (MeshFilter mesh in riceWashingPot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null || mesh.GetComponent<Renderer>() == null) continue;
            var collider = mesh.GetComponent<MeshCollider>();
            if (collider == null) collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            collider.isTrigger = false;
            collider.enabled = true;
            if (interactionLayer >= 0) mesh.gameObject.layer = interactionLayer;
            riceWashingUsesMeshColliders = true;
        }

        GameObject potObject = riceWashingUsesMeshColliders
            ? riceWashingPot.gameObject : riceWashingPotBaseCollider.gameObject;
        var target = potObject.GetComponent<HouseholdChoreInteractable>();
        if (target == null) target = potObject.AddComponent<HouseholdChoreInteractable>();
        if (interactionLayer >= 0) potObject.layer = interactionLayer;
        target.Configure(HouseholdChore.WashRice, riceWashingPot);
        riceWashingPotBaseCollider.enabled = false;

        // Retire the former floating trigger; it could be focused without looking at the pot.
        if (riceWashingTarget != null && riceWashingTarget != target)
            riceWashingTarget.gameObject.SetActive(false);
    }

    private void RefreshRiceWashingPot()
    {
        bool show = completed[(int)HouseholdChore.MeasureRice] && !completed[(int)HouseholdChore.WashRice];
        if (riceWashingPot != null) riceWashingPot.gameObject.SetActive(show);
        if (riceWashingPotBaseCollider != null)
        {
            if (show && !riceWashingUsesMeshColliders && riceWashingPotBaseCollider is BoxCollider potBox)
                FitSolidBoxToVisualBounds(potBox.transform, potBox);
            riceWashingPotBaseCollider.enabled = show && !riceWashingUsesMeshColliders;
        }
    }

    private void RefreshUI()
    {
        var names = new string[ChoreCount];
        var done = new bool[ChoreCount];
        for (int i = 0; i < ChoreCount; i++) { names[i] = Titles[(int)Order[i]]; done[i] = completed[(int)Order[i]]; }
        ui.SetChecklist(names, done, ActiveIndex, pieces, TotalPieces, CurrentLocation);
    }
}

/// <summary>Runtime UI so the chore flow works without adding a new prefab or editing the HUD.</summary>
public sealed class HouseholdChoreUI
{
    private readonly GameObject checklist;
    private readonly GameObject modalBackdrop;
    private readonly GameObject gamePanel;
    private readonly GameObject progressBar;
    private readonly TextMeshProUGUI checklistText;
    private readonly TextMeshProUGUI notificationText;
    private readonly TextMeshProUGUI titleText;
    private readonly TextMeshProUGUI hintText;
    private readonly TextMeshProUGUI feedbackText;
    private readonly TextMeshProUGUI progressText;
    private readonly TextMeshProUGUI cancelText;
    private readonly TextMeshProUGUI timingZoneText;
    private readonly TextMeshProUGUI measureZoneText;
    private readonly TextMeshProUGUI rewardText;
    private readonly Image letterGlow;
    private readonly Image progressFill;
    private readonly RectTransform marker;
    private readonly GameObject meter;
    private readonly GameObject measureZone;
    private readonly GameObject choices;
    private readonly Button[] choiceButtons = new Button[3];
    private readonly TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[3];
    private readonly HouseholdActionBoard actionBoard;
    private const float MeterHeight = 170f;
    private float notificationUntil;
    private float rewardRemaining;
    private float glowRemaining;
    private float feedbackPulseRemaining;

    public HouseholdChoreUI(Transform parent)
    {
        var canvasObject = new GameObject("HouseholdChoreCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var glowObject = new GameObject("LetterWarmGlow", typeof(RectTransform), typeof(Image));
        glowObject.transform.SetParent(canvasObject.transform, false);
        var glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.sizeDelta = Vector2.zero;
        letterGlow = glowObject.GetComponent<Image>();
        letterGlow.color = new Color(1f, .72f, .36f, 0f);
        letterGlow.raycastTarget = false;

        checklist = Box("Checklist", canvasObject.transform, new Vector2(15, -15), new Vector2(365, 335),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Color(.07f, .055f, .04f, .78f));
        checklistText = Label("Tasks", checklist.transform, new Vector2(14, -12), new Vector2(340, 310), 23, TextAlignmentOptions.TopLeft);
        notificationText = Label("MemoryNotice", canvasObject.transform, new Vector2(0, -38), new Vector2(900, 60), 28, TextAlignmentOptions.Center);
        notificationText.rectTransform.anchorMin = new Vector2(.5f, 1);
        notificationText.rectTransform.anchorMax = new Vector2(.5f, 1);
        notificationText.rectTransform.pivot = new Vector2(.5f, 1);
        notificationText.color = new Color(1f, .9f, .55f);
        notificationText.text = "";
        rewardText = Label("MemoryReward", canvasObject.transform, Vector2.zero,
            new Vector2(390, 52), 26, TextAlignmentOptions.Center);
        rewardText.color = new Color(1f, .85f, .5f, 0f);

        modalBackdrop = Box("ChoreBackdrop", canvasObject.transform, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Color(.012f, .012f, .011f, .72f));
        modalBackdrop.GetComponent<Image>().raycastTarget = false;
        modalBackdrop.SetActive(false);
        gamePanel = Box("ChorePanel", canvasObject.transform, Vector2.zero, new Vector2(1120, 780),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.11f, .075f, .045f, .99f));
        Box("PanelInset", gamePanel.transform, Vector2.zero, new Vector2(1104, 764),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.065f, .075f, .065f, 1f));
        Box("HeaderLeather", gamePanel.transform, new Vector2(0, 325), new Vector2(1098, 114),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.18f, .12f, .075f, 1f));
        Box("FooterLeather", gamePanel.transform, new Vector2(0, -348), new Vector2(1098, 60),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.15f, .11f, .073f, 1f));
        Box("PanelTopTrim", gamePanel.transform, new Vector2(0, 382), new Vector2(1098, 2),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.76f, .54f, .25f));
        Box("PanelBottomTrim", gamePanel.transform, new Vector2(0, -382), new Vector2(1098, 2),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.76f, .54f, .25f));
        Box("PanelLeftTrim", gamePanel.transform, new Vector2(-549, 0), new Vector2(2, 764),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.58f, .40f, .21f));
        Box("PanelRightTrim", gamePanel.transform, new Vector2(549, 0), new Vector2(2, 764),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.58f, .40f, .21f));
        Box("PanelTitleRule", gamePanel.transform, new Vector2(0, 273), new Vector2(1010, 2),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.60f, .44f, .25f, .7f));
        var chapter = Label("ChapterKicker", gamePanel.transform, new Vector2(-391, 344),
            new Vector2(230, 28), 15, TextAlignmentOptions.Left);
        chapter.text = "CHƯƠNG 02  /  NHÀ MÌNH";
        chapter.color = new Color(.82f, .68f, .43f);
        chapter.characterSpacing = 1.5f;
        titleText = Label("Title", gamePanel.transform, new Vector2(0, 315), new Vector2(1020, 48), 33, TextAlignmentOptions.Center);
        titleText.color = new Color(.98f, .81f, .50f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 1.1f;
        hintText = Label("Instructions", gamePanel.transform, new Vector2(0, 245), new Vector2(980, 48), 20, TextAlignmentOptions.Center);
        hintText.color = new Color(.87f, .89f, .82f);
        feedbackText = Label("Feedback", gamePanel.transform, new Vector2(0, -244), new Vector2(1000, 37), 19, TextAlignmentOptions.Center);
        feedbackText.color = new Color(.98f, .83f, .55f);
        progressText = Label("Progress", gamePanel.transform, new Vector2(0, -278), new Vector2(1000, 28), 17, TextAlignmentOptions.Center);
        progressText.color = new Color(.87f, .89f, .82f);
        cancelText = Label("CancelHint", gamePanel.transform, new Vector2(0, -349), new Vector2(1000, 31), 17, TextAlignmentOptions.Center);
        cancelText.color = new Color(.82f, .76f, .62f);
        cancelText.text = "P  LÀM LẠI     ·     ESC  TẠM DỪNG";
        progressBar = Box("ProgressBar", gamePanel.transform, new Vector2(0, -306), new Vector2(520, 7),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.27f, .24f, .18f));
        var fillObject = Box("Fill", progressBar.transform, new Vector2(0, 0), new Vector2(0, 7),
            new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Color(.94f, .69f, .28f));
        progressFill = fillObject.GetComponent<Image>();
        measureZone = Box("MeasureSuccessZone", progressBar.transform, new Vector2(520 * .65f, 0),
            new Vector2(520 * .20f, 26), new Vector2(0, .5f), new Vector2(0, .5f),
            new Vector2(.5f, .5f), new Color(.30f, .82f, .43f, .72f));
        measureZoneText = Label("MeasureZoneLabel", measureZone.transform, new Vector2(0, 27),
            new Vector2(140, 25), 16, TextAlignmentOptions.Center);
        measureZoneText.text = "VỪA ĐỦ";

        meter = Box("TimingMeter", gamePanel.transform, new Vector2(0, 25), new Vector2(84, MeterHeight),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.3f, .27f, .22f));
        var successZone = Box("SuccessZone", meter.transform, Vector2.zero, new Vector2(84, MeterHeight * .2f),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.27f, .67f, .39f));
        timingZoneText = Label("TimingZoneLabel", successZone.transform, Vector2.zero,
            new Vector2(82, 30), 16, TextAlignmentOptions.Center);
        timingZoneText.text = "ĐÚNG";
        marker = Box("Marker", meter.transform, new Vector2(0, -MeterHeight / 2), new Vector2(100, 7),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(1f, .83f, .28f))
            .GetComponent<RectTransform>();

        choices = new GameObject("Choices", typeof(RectTransform));
        choices.transform.SetParent(gamePanel.transform, false);
        var choicesRect = choices.GetComponent<RectTransform>();
        choicesRect.anchorMin = choicesRect.anchorMax = new Vector2(.5f, .5f);
        choicesRect.sizeDelta = new Vector2(570, 75);
        choicesRect.anchoredPosition = new Vector2(0, 25);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var buttonObject = Box("Choice" + i, choices.transform, new Vector2((i - 1) * 190, 0), new Vector2(178, 70),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.38f, .25f, .15f));
            choiceButtons[i] = buttonObject.AddComponent<Button>();
            choiceTexts[i] = Label("Text", buttonObject.transform, Vector2.zero, new Vector2(168, 66), 22, TextAlignmentOptions.Center);
        }
        actionBoard = new HouseholdActionBoard(gamePanel.transform);
        gamePanel.SetActive(false);
    }

    public HouseholdActionBoard Actions => actionBoard;

    public void SetChecklistVisible(bool visible) => checklist.SetActive(visible);

    public void SetChecklist(string[] names, bool[] done, int current, int pieces, int total, string location)
    {
        var text = "<color=#F9D88C><b>VIỆC NHÀ CÙNG MẸ</b></color>\n" +
                   "Mảnh ký ức: " + pieces + "/" + total + "\n" + location + "\n\n";
        for (int i = Mathf.Max(0, Mathf.Min(current - 1, names.Length - 6)); i < Mathf.Min(names.Length, Mathf.Max(6, current + 5)); i++)
        {
            if (done[i]) text += "<color=#9ADD91>✓ " + names[i] + "</color>\n";
            else if (i == current) text += "<color=#FFE19B>▶ " + names[i] + "</color>\n";
            else text += "<color=#BDB3A7>○ " + names[i] + "</color>\n";
        }
        checklistText.text = text;
    }

    public void Notify(string message, float seconds = 7f)
    {
        ApplyHudFont();
        notificationText.text = message;
        notificationUntil = Time.time + seconds;
    }

    public void Tick()
    {
        actionBoard.Tick();
        if (notificationUntil > 0f && Time.time >= notificationUntil)
        {
            notificationUntil = 0f;
            notificationText.text = string.Empty;
        }
        if (rewardRemaining > 0f)
        {
            rewardRemaining = Mathf.Max(0f, rewardRemaining - Time.deltaTime);
            float t = 1f - rewardRemaining / 1.4f;
            rewardText.rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(510, -60),
                new Vector2(690, 305), Mathf.SmoothStep(0f, 1f, t));
            rewardText.color = new Color(1f, .85f, .5f, Mathf.Clamp01((1f - t) * 3f));
        }
        if (glowRemaining > 0f)
        {
            glowRemaining = Mathf.Max(0f, glowRemaining - Time.deltaTime);
            letterGlow.color = new Color(1f, .72f, .36f, .16f * glowRemaining / 2f);
        }
        if (feedbackPulseRemaining > 0f)
        {
            feedbackPulseRemaining = Mathf.Max(0f, feedbackPulseRemaining - Time.deltaTime);
            feedbackText.rectTransform.localScale = Vector3.one * (1f + .08f * feedbackPulseRemaining / .22f);
        }
    }

    public void PlayMemoryReward(bool finalLetter)
    {
        ApplyHudFont();
        rewardText.text = finalLetter ? "MẸ VẪN CHỜ CON" : "+10 MẢNH KÝ ỨC";
        rewardRemaining = 1.4f;
        if (finalLetter) glowRemaining = 2f;
    }

    public void ShowGame(string title, string instructions, bool showMeter)
    {
        actionBoard.Hide();
        modalBackdrop.SetActive(true);
        gamePanel.SetActive(true);
        notificationText.text = string.Empty;
        notificationUntil = 0f;
        titleText.text = title;
        hintText.text = instructions;
        feedbackText.text = string.Empty;
        feedbackText.color = new Color(.98f, .83f, .55f);
        feedbackText.rectTransform.localScale = Vector3.one;
        feedbackPulseRemaining = 0f;
        ApplyHudFont();
        meter.SetActive(showMeter);
        measureZone.SetActive(false);
        choices.SetActive(false);
        SetProgress(0, "");
    }

    public void ShowChoices(string[] labels, Action<int> onSelected)
    {
        choices.SetActive(true);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int selected = i;
            choiceTexts[i].text = i < labels.Length ? (i + 1) + " · " + labels[i] : "";
            choiceButtons[i].gameObject.SetActive(i < labels.Length);
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => onSelected(selected));
        }
    }

    public void SetExpectedChoice(int expected)
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].interactable = i >= expected;
            choiceButtons[i].GetComponent<Image>().color = i == expected
                ? new Color(.75f, .48f, .18f)
                : new Color(.38f, .25f, .15f);
        }
    }

    public void SetMeter(float value) => marker.anchoredPosition = new Vector2(0, Mathf.Clamp01(value) * MeterHeight - MeterHeight / 2);
    public void ShowMeasureZone() => measureZone.SetActive(true);

    public void SetProgress(float value, string message)
    {
        progressFill.rectTransform.sizeDelta = new Vector2(520 * Mathf.Clamp01(value), 7);
        progressText.text = message;
    }

    public void SetHint(string message)
    {
        bool correction = message.StartsWith("Chưa");
        if (!correction) hintText.text = message;
        feedbackText.text = correction ? message : string.Empty;
        feedbackText.color = correction ? new Color(1f, .65f, .5f) :
            new Color(.69f, .94f, .71f);
        feedbackPulseRemaining = .22f;
    }
    public void HideGame()
    {
        actionBoard.Hide();
        gamePanel.SetActive(false);
        modalBackdrop.SetActive(false);
    }

    private void ApplyHudFont()
    {
        TMP_FontAsset font = HUDController.Instance != null ? HUDController.Instance.ObjectiveFont : null;
        if (font == null) return;
        titleText.font = hintText.font = feedbackText.font = progressText.font = cancelText.font = font;
        timingZoneText.font = measureZoneText.font = font;
        rewardText.font = font;
        for (int i = 0; i < choiceTexts.Length; i++) choiceTexts[i].font = font;
        actionBoard.SetFont(font);
        notificationText.font = checklistText.font = font;
    }

    private static GameObject Box(string name, Transform parent, Vector2 position, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.richText = true;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }
}
