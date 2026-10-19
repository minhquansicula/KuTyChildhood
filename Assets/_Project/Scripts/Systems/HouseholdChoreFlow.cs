using System;
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
        "Gáo nước mát mẹ để dành từ sáng.", "Mẹ luôn đong thêm một chút cho con.",
        "Nước đục đi, hạt gạo lại trắng.", "Bếp bắt lửa, cả nhà bắt đầu ấm.",
        "Mùi cơm chín khiến con biết sắp được gọi về.", "Mâm cơm đủ người là bữa ngon nhất.",
        "Mẹ rửa, con tráng, hai mẹ con chuyện trò.", "Tiếng chén khẽ chạm nhau, việc nhà đã xong.",
        "Mẹ bảo: chăn gọn thì ngày mới cũng gọn.", "Chiếc rương giữ cả những mùa áo cũ.",
        "Tiếng radio mở đầu mỗi buổi sáng của nhà mình.", "Dùng xong nhớ cất, mai còn tìm được con nhé.",
        "Tiếng cối quay đều như nhịp tay mẹ.", "Bó củi nhỏ mang theo hơi ấm cho cả nhà.",
        "Sau này đi đâu, con cũng nhớ ăn cơm đúng bữa. Nhà mình vẫn có một chỗ chờ con."
    };
    public string CurrentTitle => ActiveIndex < ChoreCount ? Titles[(int)Order[ActiveIndex]] : "Đã hoàn tất";
    public string CurrentLocation => ActiveIndex < ChoreCount ? Locations[(int)Order[ActiveIndex]] : "";

    private readonly bool[] completed = new bool[15];
    private HouseholdChoreUI ui;
    private bool checklistVisible;
    private int pieces;

    public int Pieces => pieces;
    public int ActiveIndex
    {
        get
        {
            for (int i = 0; i < completed.Length; i++)
                if (!completed[(int)Order[i]]) return i;
            return completed.Length;
        }
    }

    public HouseholdChoreUI UI => ui;
    public event Action<HouseholdChore, int> OnChoreCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        ui = new HouseholdChoreUI(transform);
        ui.SetChecklistVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowChecklist()
    {
        if (checklistVisible) return;
        checklistVisible = true;
        ui.SetChecklistVisible(true);
        RefreshUI();
        ui.Notify("Mẹ: Con dậy rồi thì dọn đồ, phụ mẹ một tay nhé.");
        QuestManager.Instance?.RefreshHouseholdObjective();
    }

    public bool IsCurrent(HouseholdChore chore) => checklistVisible && ActiveIndex < ChoreCount && Order[ActiveIndex] == chore;

    public bool Complete(HouseholdChore chore)
    {
        int index = (int)chore;
        if (!IsCurrent(chore) || completed[index]) return false;
        completed[index] = true;
        pieces += PiecesPerChore;
        RefreshUI();
        ui.Notify(ActiveIndex == ChoreCount
            ? TotalPieces + "/" + TotalPieces + " — Nhà mình vẫn có một chỗ chờ con."
            : "+10 mảnh ký ức — " + Memories[index]);
        AudioManager.Instance?.PlaySFX("memory_collected");
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
    private readonly Text checklistText;
    private readonly Text notificationText;
    private readonly Text titleText;
    private readonly Text hintText;
    private readonly Text progressText;
    private readonly Image progressFill;
    private readonly RectTransform marker;
    private readonly GameObject meter;
    private readonly GameObject measureZone;
    private readonly GameObject choices;
    private readonly Button[] choiceButtons = new Button[3];
    private readonly Text[] choiceTexts = new Text[3];
    private const float MeterHeight = 170f;

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

        checklist = Box("Checklist", canvasObject.transform, new Vector2(15, -15), new Vector2(365, 335),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Color(.07f, .055f, .04f, .78f));
        checklistText = Label("Tasks", checklist.transform, new Vector2(14, -12), new Vector2(340, 310), 23, TextAnchor.UpperLeft);
        notificationText = Label("MemoryNotice", canvasObject.transform, new Vector2(0, -38), new Vector2(900, 60), 28, TextAnchor.MiddleCenter);
        notificationText.rectTransform.anchorMin = new Vector2(.5f, 1);
        notificationText.rectTransform.anchorMax = new Vector2(.5f, 1);
        notificationText.rectTransform.pivot = new Vector2(.5f, 1);
        notificationText.color = new Color(1f, .9f, .55f);
        notificationText.text = "";

        gamePanel = Box("ChorePanel", canvasObject.transform, new Vector2(-20, 0), new Vector2(650, 455),
            new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(1f, .5f), new Color(.065f, .05f, .04f, .94f));
        titleText = Label("Title", gamePanel.transform, new Vector2(0, 168), new Vector2(590, 50), 31, TextAnchor.MiddleCenter);
        hintText = Label("Hint", gamePanel.transform, new Vector2(0, 111), new Vector2(590, 70), 23, TextAnchor.MiddleCenter);
        progressText = Label("Progress", gamePanel.transform, new Vector2(0, -98), new Vector2(580, 50), 26, TextAnchor.MiddleCenter);
        Label("CancelHint", gamePanel.transform, new Vector2(0, -183), new Vector2(580, 34), 19, TextAnchor.MiddleCenter).text = "Esc: hủy và quay lại cảnh";
        var bar = Box("ProgressBar", gamePanel.transform, new Vector2(0, -145), new Vector2(410, 18),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.35f, .3f, .23f));
        var fillObject = Box("Fill", bar.transform, new Vector2(0, 0), new Vector2(0, 18),
            new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Color(.94f, .69f, .28f));
        progressFill = fillObject.GetComponent<Image>();
        measureZone = Box("MeasureSuccessZone", bar.transform, new Vector2(410 * .65f, 0),
            new Vector2(410 * .20f, 26), new Vector2(0, .5f), new Vector2(0, .5f),
            new Vector2(.5f, .5f), new Color(.30f, .82f, .43f, .72f));

        meter = Box("TimingMeter", gamePanel.transform, new Vector2(0, 3), new Vector2(52, MeterHeight),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.3f, .27f, .22f));
        Box("SuccessZone", meter.transform, Vector2.zero, new Vector2(52, MeterHeight * .2f),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.27f, .67f, .39f));
        marker = Box("Marker", meter.transform, new Vector2(0, -MeterHeight / 2), new Vector2(72, 7),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(1f, .83f, .28f))
            .GetComponent<RectTransform>();

        choices = new GameObject("Choices", typeof(RectTransform));
        choices.transform.SetParent(gamePanel.transform, false);
        var choicesRect = choices.GetComponent<RectTransform>();
        choicesRect.anchorMin = choicesRect.anchorMax = new Vector2(.5f, .5f);
        choicesRect.sizeDelta = new Vector2(570, 75);
        choicesRect.anchoredPosition = new Vector2(0, 8);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var buttonObject = Box("Choice" + i, choices.transform, new Vector2((i - 1) * 190, 0), new Vector2(178, 70),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.38f, .25f, .15f));
            choiceButtons[i] = buttonObject.AddComponent<Button>();
            choiceTexts[i] = Label("Text", buttonObject.transform, Vector2.zero, new Vector2(168, 66), 22, TextAnchor.MiddleCenter);
        }
        gamePanel.SetActive(false);
    }

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

    public void Notify(string message) => notificationText.text = message;

    public void ShowGame(string title, string instructions, bool showMeter)
    {
        gamePanel.SetActive(true);
        titleText.text = title;
        hintText.text = instructions;
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
            choiceTexts[i].text = i < labels.Length ? labels[i] : "";
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

    public void SetHint(string message) => hintText.text = message;
    public void HideGame() => gamePanel.SetActive(false);

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

    private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.supportRichText = true;
        label.raycastTarget = false;
        return label;
    }
}
