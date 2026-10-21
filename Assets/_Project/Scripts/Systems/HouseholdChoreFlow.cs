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
    public const int ChoreCount = 14;
    public const int TotalPieces = PiecesPerChore * ChoreCount;
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
        HouseholdChore.SortTools, HouseholdChore.GrindCorn, HouseholdChore.CarryWood,
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
    private HouseholdChoreUI ui;
    public HouseholdVoicePlayer Voice { get; private set; }
    private bool checklistVisible;
    private int pieces;

    public int Pieces => pieces;
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
        pieces = restored * PiecesPerChore;
        ui = new HouseholdChoreUI(transform);
        ui.SetChecklistVisible(false);
        if (GetComponent<Scene2QuestHUD>() == null) gameObject.AddComponent<Scene2QuestHUD>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update() => ui?.Tick();

    public void ShowChecklist()
    {
        if (checklistVisible) return;
        checklistVisible = true;
        // The original compact checklist remains a fallback when the shared Scene 2 board is unavailable.
        ui.SetChecklistVisible(GetComponent<Scene2QuestHUD>() == null);
        RefreshUI();
        bool resumed = ActiveIndex > 0;
        float seconds = Voice.Play(resumed ? "act2_mom_resume_001" : "act2_mom_wakeup_001");
        ui.Notify("Mẹ: " + (resumed ? ResumeText : WakeupText) + " Tab: bảng nhiệm vụ · H: gợi ý.", Mathf.Max(7f, seconds));
        QuestManager.Instance?.RefreshHouseholdObjective();
    }

    public bool IsCurrent(HouseholdChore chore) => checklistVisible && ActiveIndex < ChoreCount && Order[ActiveIndex] == chore;

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
        pieces += PiecesPerChore;
        if (ActiveIndex < ChoreCount) Scene2Checkpoint.SaveHome(ActiveIndex);
        RefreshUI();
        float voiceSeconds = chore != HouseholdChore.ReadLetter ? Voice.Play(HouseholdVoicePlayer.ChoreId(chore)) : 0f;
        ui.Notify(ActiveIndex == ChoreCount
            ? TotalPieces + "/" + TotalPieces + " — Nhà mình vẫn có một chỗ chờ con."
            : "+10 mảnh ký ức — " + Memories[index], Mathf.Max(7f, voiceSeconds));
        ui.PlayMemoryReward(ActiveIndex == ChoreCount);
        AudioManager.Instance?.PlaySFX(SfxId.MemoryCollected);
        OnChoreCompleted?.Invoke(chore, pieces);
        QuestManager.Instance?.RefreshHouseholdObjective();
        if (ActiveIndex == ChoreCount) QuestManager.Instance?.CompleteHouseholdChores();
        return true;
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
    private readonly GameObject gamePanel;
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

        gamePanel = Box("ChorePanel", canvasObject.transform, Vector2.zero, new Vector2(1120, 780),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.035f, .065f, .075f, .96f));
        titleText = Label("Title", gamePanel.transform, new Vector2(0, 330), new Vector2(1020, 50), 31, TextAlignmentOptions.Center);
        titleText.color = new Color(.98f, .78f, .43f);
        hintText = Label("Instructions", gamePanel.transform, new Vector2(0, 260), new Vector2(1000, 60), 23, TextAlignmentOptions.Center);
        feedbackText = Label("Feedback", gamePanel.transform, new Vector2(0, -210), new Vector2(1000, 48), 20, TextAlignmentOptions.Center);
        feedbackText.color = new Color(.98f, .83f, .55f);
        progressText = Label("Progress", gamePanel.transform, new Vector2(0, -275), new Vector2(1000, 45), 20, TextAlignmentOptions.Center);
        cancelText = Label("CancelHint", gamePanel.transform, new Vector2(0, -345), new Vector2(1000, 35), 19, TextAlignmentOptions.Center);
        cancelText.text = "Esc · Tạm dừng việc này, không mất tiến độ đã xong";
        var bar = Box("ProgressBar", gamePanel.transform, new Vector2(0, -190), new Vector2(410, 18),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.35f, .3f, .23f));
        var fillObject = Box("Fill", bar.transform, new Vector2(0, 0), new Vector2(0, 18),
            new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Color(.94f, .69f, .28f));
        progressFill = fillObject.GetComponent<Image>();
        bar.SetActive(false); // Feedback comes from the prop; keep legacy meter API for older callers.
        measureZone = Box("MeasureSuccessZone", bar.transform, new Vector2(410 * .65f, 0),
            new Vector2(410 * .20f, 26), new Vector2(0, .5f), new Vector2(0, .5f),
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
        progressFill.rectTransform.sizeDelta = new Vector2(410 * Mathf.Clamp01(value), 18);
        progressText.text = message;
    }

    public void SetHint(string message)
    {
        feedbackText.text = message;
        feedbackText.color = message.StartsWith("Chưa") ? new Color(1f, .65f, .5f) :
            new Color(.69f, .94f, .71f);
        feedbackPulseRemaining = .22f;
    }
    public void HideGame()
    {
        actionBoard.Hide();
        gamePanel.SetActive(false);
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
