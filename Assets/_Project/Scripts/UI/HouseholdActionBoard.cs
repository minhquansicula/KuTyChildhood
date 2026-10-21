using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small close-up work surface for Scene 2. Input is read only while a chore owns the modal;
/// the quest flow remains the sole owner of completion and rewards.
/// </summary>
public sealed partial class HouseholdActionBoard
{
    private enum ActionMode { Placement, Dial, Rotary, Fetch, Measure, WashRice, Fire, Heat, Scrub, Letter }
    private const string MiniGameArtPath = "Scene2/MiniGames/";
    private static Sprite circleSprite;

    private readonly RectTransform root;
    private RectTransform rotaryPullCue;
    private readonly RectTransform[] items = new RectTransform[3];
    private readonly RectTransform[] targets = new RectTransform[3];
    private readonly bool[] placed = new bool[3];
    private readonly bool[] returning = new bool[3];
    private readonly Vector2[] itemHome = new Vector2[3];
    private readonly Image[] dirt = new Image[3];
    private TMP_FontAsset font;
    private Action completed;
    private Action<float, string> progress;
    private Action<string> hint;
    private Action pulse;
    private HouseholdChore chore;
    private ActionMode mode;
    private bool active;
    private bool ownsLetterMusic;
    private bool dragging;
    private int draggedIndex = -1;
    private int stage;
    private int successes;
    private float amount;
    private float heldTime;
    private float lastAngle;
    private float dialFrequency;
    private bool radioHasArt;
    private bool placementHasArt;
    private bool rotaryHasArt;
    private bool fireHasArt;
    private float idleSeconds;
    private float lastReported = -1f;
    private string lastMessage;
    private bool idleHintShown;
    private Vector2 dragOrigin;
    private Vector2 dragOffset;
    private Vector2 pointerStart;
    private Vector2 lastPointer;
    private RectTransform actionArea;
    private RectTransform rotaryTopPivot;
    private RectTransform millCrankArm;
    private RectTransform millCrankGrip;
    private readonly RectTransform[] millSurfaceMarks = new RectTransform[4];
    private readonly RectTransform[] washingGrains = new RectTransform[5];
    private RectTransform dialNeedle;
    private RectTransform token;
    private RectTransform fill;
    private RectTransform heatTrack;
    private RectTransform heatHandle;
    private RectTransform dish;
    private RectTransform scrubber;
    private Image ember;
    private TextMeshProUGUI status;
    private TextMeshProUGUI tokenText;
    private TextMeshProUGUI dishText;

    public HouseholdActionBoard(Transform parent)
    {
        root = Box("HandsOnWorkSurface", parent, Vector2.zero, new Vector2(570f, 310f),
            new Color(.12f, .095f, .066f, 1f));
        root.anchoredPosition = new Vector2(0f, -4f);
        root.localScale = Vector3.one * 1.4f;
        root.gameObject.SetActive(false);
    }

    private void BuildBoardFrame()
    {
        float halfHeight = root.sizeDelta.y * .5f;
        Color brass = new Color(.74f, .53f, .28f, .92f);
        Box("InnerWalnut", root, Vector2.zero, new Vector2(558f, root.sizeDelta.y - 12f),
            new Color(.075f, .082f, .072f, 1f));
        Box("WorkCloth", root, Vector2.zero, new Vector2(540f, root.sizeDelta.y - 28f),
            new Color(.115f, .13f, .12f, 1f));
        Box("BoardHeader", root, new Vector2(0f, halfHeight - 29f),
            new Vector2(540f, 46f), new Color(.24f, .18f, .12f, .97f));
        Box("BoardFooter", root, new Vector2(0f, -halfHeight + 28f),
            new Vector2(540f, 45f), new Color(.16f, .19f, .16f, .98f));
        Shape(root, "BoardTopRule", 0f, halfHeight - 7f, 546f, 2f, brass);
        Shape(root, "BoardBottomRule", 0f, -halfHeight + 7f, 546f, 2f, brass);
        Shape(root, "HeaderAccent", 0f, halfHeight - 53f, 540f, 2f, brass);
        Shape(root, "FooterAccent", 0f, -halfHeight + 51f, 540f, 2f, brass);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape(root, "BoardSideRail", side * 274f, 0f, 2f, root.sizeDelta.y - 14f, brass);
            Circle("CornerRivet", root, new Vector2(side * 264f, halfHeight - 16f), 5f, brass);
            Circle("CornerRivet", root, new Vector2(side * 264f, -halfHeight + 16f), 5f, brass);
        }
    }

    private void PolishBoardLayout()
    {
        float halfHeight = root.sizeDelta.y * .5f;
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.transform.parent != root) continue;
            if (!label.name.EndsWith("Heading") && label.name != "PlacementGuide") continue;
            label.rectTransform.anchoredPosition = new Vector2(0f, halfHeight - 28f);
            label.rectTransform.sizeDelta = new Vector2(520f, 38f);
            label.fontSize = 20f;
            label.color = new Color(.98f, .80f, .47f);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 1.2f;
        }
        if (status != null && status.transform.parent == root)
        {
            status.rectTransform.anchoredPosition = new Vector2(0f, -halfHeight + 29f);
            status.rectTransform.sizeDelta = new Vector2(520f, 36f);
            status.fontSize = 16f;
            status.color = new Color(.94f, .91f, .79f);
        }
    }

    public void SetFont(TMP_FontAsset value)
    {
        if (value == null) return;
        font = value;
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true)) label.font = font;
    }

    public void Begin(HouseholdChore task, Action onComplete, Action<float, string> onProgress,
        Action<string> onHint, Action onPulse)
    {
        Hide();
        chore = task;
        completed = onComplete;
        progress = onProgress;
        hint = onHint;
        pulse = onPulse;
        stage = successes = mistakes = 0;
        elapsed = 0f;
        breathUsed = false;
        gestureHome = Vector2.zero;
        tutorialHand = rinseZone = null;
        for (int i = 0; i < 3; i++) stainRemaining[i] = 1f;
        amount = heldTime = idleSeconds = 0f;
        dialFrequency = 0f;
        radioHasArt = placementHasArt = false;
        rotaryHasArt = fireHasArt = false;
        idleHintShown = false;
        draggedIndex = -1;
        lastReported = -1f;
        lastMessage = null;
        actionArea = rotaryTopPivot = millCrankArm = millCrankGrip = dialNeedle = token = fill = heatTrack = heatHandle = dish = scrubber = rotaryPullCue = null;
        for (int i = 0; i < washingGrains.Length; i++) washingGrains[i] = null;
        for (int i = 0; i < millSurfaceMarks.Length; i++) millSurfaceMarks[i] = null;
        ember = null;
        status = tokenText = dishText = null;
        for (int i = 0; i < 3; i++)
        {
            items[i] = targets[i] = null;
            placed[i] = returning[i] = false;
            dirt[i] = null;
        }
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
        root.sizeDelta = new Vector2(570f, 310f);
        root.localScale = Vector3.one * 1.4f;
        BuildBoardFrame();

        switch (task)
        {
            case HouseholdChore.ArrangeKeepsakes:
                BuildPlacement(new[] { "ÁO", "KHĂN", "KỶ VẬT" },
                    new[] { "NGĂN ÁO", "NGĂN KHĂN", "HỘP KỶ NIỆM" }); break;
            case HouseholdChore.SortTools:
                BuildPlacement(new[] { "CÀO", "CUỐC", "XẺNG" },
                    new[] { "MÓC CÀO", "MÓC CUỐC", "MÓC XẺNG" }); break;
            case HouseholdChore.CarryWood:
                BuildPlacement(new[] { "CỦI 1", "CỦI 2", "CỦI 3" },
                    new[] { "ĐÁY BÓ", "GIỮA BÓ", "TRÊN CÙNG" }); break;
            case HouseholdChore.SetTable:
                BuildPlacement(new[] { "BÁT", "ĐŨA", "NỒI CƠM" },
                    new[] { "CHỖ BÁT", "CHỖ ĐŨA", "GIỮA MÂM" }); break;
            case HouseholdChore.PutAwayDishes:
                BuildDishStorage(); break;
            case HouseholdChore.FoldBlanket:
                BuildPlacement(new[] { "MÉP TRÁI", "MÉP PHẢI", "MÉP TRÊN" },
                    new[] { "NẾP TRÁI", "NẾP PHẢI", "GẤP ĐÔI" }); break;
            case HouseholdChore.TuneRadio: BuildDial(); break;
            case HouseholdChore.GrindCorn: BuildRotary(); break;
            case HouseholdChore.WashRice: BuildWashRice(); break;
            case HouseholdChore.FetchWater: BuildFetch(); break;
            case HouseholdChore.MeasureRice: BuildMeasure(); break;
            case HouseholdChore.LightFire: BuildFire(); break;
            case HouseholdChore.CookRice: BuildHeat(); break;
            case HouseholdChore.WashDishes: BuildScrub(); break;
            case HouseholdChore.ReadLetter: BuildLetter(); break;
            default: throw new ArgumentOutOfRangeException(nameof(task), task, "Unknown household chore");
        }
        PolishBoardLayout();
        root.gameObject.SetActive(true);
        active = true;
        if (task == HouseholdChore.ReadLetter && AudioManager.Instance != null)
        {
            ownsLetterMusic = true;
            AudioManager.Instance.BeginLetterMusic();
        }
        BuildHandGuide();
        SetFont(font);
    }

    public void Hide()
    {
        if (ownsLetterMusic && AudioManager.Instance != null)
            AudioManager.Instance.EndLetterMusic();
        ownsLetterMusic = false;
        active = dragging = false;
        draggedIndex = -1;
        root.gameObject.SetActive(false);
        StopRadioAudio();
        completed = null;
        progress = null;
        hint = null;
        pulse = null;
    }

    public bool Restart()
    {
        if (!active) return false;

        var task = chore;
        var onComplete = completed;
        var onProgress = progress;
        var onHint = hint;
        var onPulse = pulse;
        Begin(task, onComplete, onProgress, onHint, onPulse);
        Report(0f, "");
        return true;
    }

    public void Tick()
    {
        if (!active || !root.gameObject.activeInHierarchy ||
            (GameManager.Instance != null && GameManager.Instance.IsPaused) ||
            PauseUI.LastResumeFrame == Time.frameCount || Input.GetKeyDown(KeyCode.Escape)) return;

        elapsed += Time.deltaTime;
        TickHandGuide();
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F) || ((Vector2)Input.mousePosition - lastPointer).sqrMagnitude > 1f)
            idleSeconds = 0f;
        else idleSeconds += Time.deltaTime;
        lastPointer = Input.mousePosition;
        if (!idleHintShown && (idleSeconds >= 7f || mistakes >= 2))
        {
            idleHintShown = true;
            hint?.Invoke(IdleHint());
        }

        switch (mode)
        {
            case ActionMode.Placement: TickPlacement(); break;
            case ActionMode.Dial: TickDial(); break;
            case ActionMode.Rotary: TickRotary(); break;
            case ActionMode.Fetch: TickFetch(); break;
            case ActionMode.Measure: TickMeasure(); break;
            case ActionMode.WashRice: TickWashRice(); break;
            case ActionMode.Fire: TickFire(); break;
            case ActionMode.Heat: TickHeat(); break;
            case ActionMode.Scrub: TickScrub(); break;
            case ActionMode.Letter: TickLetter(); break;
        }
    }

    private void BuildPlacement(string[] itemNames, string[] targetNames)
    {
        mode = ActionMode.Placement;
        string artName = PlacementArtName(chore);
        bool illustrated = artName != null && LoadArt(artName) != null;
        placementHasArt = illustrated;
        if (illustrated) AddArtBackground(artName, chore == HouseholdChore.ArrangeKeepsakes ||
            chore == HouseholdChore.SortTools || chore == HouseholdChore.SetTable ? 1f : .82f);
        if ((chore == HouseholdChore.PutAwayDishes && !illustrated) ||
            (chore == HouseholdChore.ArrangeKeepsakes && LoadArt("Chest_Open") == null))
        {
            Shape(root, "WoodFrame", 0f, 37f, 558f, 80f, new Color(.40f, .26f, .15f));
            Shape(root, "LowerTrim", 0f, -4f, 562f, 7f, new Color(.70f, .49f, .25f));
        }
        if (chore == HouseholdChore.SetTable && !illustrated)
        {
            var tray = Circle("MealTray", root, new Vector2(0f, 35f), 535f, new Color(.55f, .59f, .55f));
            tray.sizeDelta = new Vector2(550f, 85f);
        }
        Label("PlacementGuide", root, new Vector2(0f, illustrated ? 137f : 87f), new Vector2(520f, 28f),
            "KÉO VẬT DỤNG VÀO ĐÚNG CHỖ", 19f, new Color(.97f, .80f, .47f));
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * (chore == HouseholdChore.SetTable ? 95f : 180f);
            targets[i] = Box("Compartment" + i, root, new Vector2(x, illustrated ? 25f : 39f),
                new Vector2(chore == HouseholdChore.SetTable ? 120f : 164f,
                    illustrated ? 94f : 65f), illustrated
                    ? new Color(.08f, .12f, .12f, .36f) : new Color(.20f, .27f, .28f));
            if (!illustrated)
                Box("ShelfEdge", targets[i], new Vector2(0f, -29f), new Vector2(156f, 4f),
                    new Color(.66f, .51f, .30f));
            Label("CompartmentName", targets[i], Vector2.zero,
                new Vector2(chore == HouseholdChore.SetTable ? 112f : 155f, 60f),
                targetNames[i], 13f, new Color(.75f, .85f, .83f));
            DrawProp(targets[i], i);
            targets[i].Find("PropIcon").localPosition = new Vector3(0f, 13f, 0f);
            if (chore == HouseholdChore.SetTable)
                targets[i].Find("PropIcon").localScale = Vector3.one * 1.35f;
            targets[i].Find("PropIcon").gameObject.AddComponent<CanvasGroup>().alpha = .3f;
            targets[i].GetComponentInChildren<TextMeshProUGUI>().rectTransform.anchoredPosition = new Vector2(0f, -30f);
            // Keep the visual order different from the destination order: this is a sorting task.
            itemHome[i] = new Vector2((((i + 1) % 3) - 1) * 180f, illustrated ? -87f : -50f);
            items[i] = Box("MovableItem" + i, root, itemHome[i],
                new Vector2(164f, illustrated ? 94f : 58f), new Color(.52f, .36f, .21f));
            DrawProp(items[i], i);
            items[i].Find("PropIcon").localPosition = new Vector3(0f, 12f, 0f);
            items[i].Find("PropIcon").localScale = Vector3.one * (chore == HouseholdChore.SetTable ? 1.7f : 1.15f);
            if (chore == HouseholdChore.SortTools && i == 1)
                items[i].Find("PropIcon").localRotation = Quaternion.Euler(0f, 0f, 90f);
            Label("ItemName", items[i], new Vector2(0f, illustrated ? -36f : -27f), new Vector2(158f, 22f),
                itemNames[i], 17f, new Color(1f, .96f, .86f));
        }
        Report(0f, "0/3 món đã xếp");
    }

    private void BuildDishStorage()
    {
        mode = ActionMode.Placement;
        placementHasArt = true;
        Label("PlacementGuide", root, new Vector2(0f, 137f), new Vector2(520f, 28f),
            "CẤT CHÉN BÁT VÀO TỦ", 19f, new Color(.97f, .80f, .47f));
        Box("CabinetMat", root, new Vector2(-104f, 0f),
            new Vector2(316f, 230f), new Color(.12f, .16f, .15f, .76f)).SetSiblingIndex(2);
        if (LoadArt("Dish_Cabinet") is Sprite cabinetArt)
            AddArt("DishCabinetArt", root, new Vector2(-104f, 0f),
                new Vector2(300f, 228f), cabinetArt);
        Box("StorageTray", root, new Vector2(175f, 0f),
            new Vector2(155f, 224f), new Color(.22f, .19f, .15f, .93f));
        string[] itemNames = { "CHÉN", "ĐĨA", "ĐŨA" };
        string[] targetNames = { "NGĂN CHÉN", "NGĂN ĐĨA", "ỐNG ĐŨA" };
        for (int i = 0; i < 3; i++)
        {
            Vector2 destination = new Vector2(-104f, 68f - i * 68f);
            targets[i] = Box("DishShelf" + i, root, destination,
                new Vector2(152f, 61f), new Color(.11f, .17f, .15f, .57f));
            Box("ShelfLip", targets[i], new Vector2(0f, -29f),
                new Vector2(152f, 3f), new Color(.73f, .52f, .28f, .87f));
            DrawProp(targets[i], i);
            targets[i].Find("PropIcon").GetComponent<Image>().color = new Color(1f, 1f, 1f, .26f);
            Label("ShelfName", targets[i], new Vector2(0f, -20f),
                new Vector2(148f, 16f), targetNames[i], 10f, new Color(.91f, .82f, .59f));

            itemHome[i] = new Vector2(175f, 68f - i * 68f);
            items[i] = Box("DishItem" + i, root, itemHome[i],
                new Vector2(128f, 60f), new Color(.42f, .32f, .22f, .96f));
            DrawProp(items[i], i);
            Label("DishItemName", items[i], new Vector2(0f, -23f),
                new Vector2(122f, 17f), itemNames[i], 12f, new Color(1f, .94f, .79f));
        }
        Report(0f, "0/3 món đã cất");
    }

    private void TickPlacement()
    {
        if (stage == 1) { TickActionSwipe(chore == HouseholdChore.ArrangeKeepsakes ? Vector2.down : Vector2.right); return; }
        bool illustrated = placementHasArt;
        if (dragging && chore == HouseholdChore.SortTools && draggedIndex == 1)
            items[1].Find("PropIcon").Rotate(0f, 0f, Input.mouseScrollDelta.y * 15f + (Input.GetKey(KeyCode.R) ? Time.deltaTime * 100f : 0f));
        for (int i = 0; i < 3; i++)
        {
            if (returning[i] && items[i] != null && !placed[i])
            {
                items[i].anchoredPosition = Vector2.MoveTowards(items[i].anchoredPosition,
                    itemHome[i], Time.deltaTime * 780f);
                if ((items[i].anchoredPosition - itemHome[i]).sqrMagnitude < 1f) returning[i] = false;
            }
            if (targets[i] != null && !placed[i])
                targets[i].GetComponent<Image>().color = dragging && Inside(targets[i])
                    ? i == draggedIndex ? new Color(.25f, .49f, .35f, illustrated ? .72f : 1f)
                        : new Color(.48f, .26f, .23f, illustrated ? .72f : 1f)
                    : new Color(.20f, .27f, .28f, illustrated ? .52f : 1f);
        }
        if (successes == 0 && items[0] != null && !dragging)
            items[0].GetComponent<Image>().color = Color.Lerp(new Color(.52f, .36f, .21f),
                new Color(.72f, .52f, .27f), .5f + .5f * Mathf.Sin(Time.unscaledTime * 3f));
        if (Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < 3; i++)
            {
                if (placed[i] || !Inside(items[i])) continue;
                draggedIndex = i;
                returning[i] = false;
                dragOrigin = items[i].anchoredPosition;
                if (Pointer(out Vector2 start)) dragOffset = dragOrigin - start;
                dragging = true;
                items[i].localScale = Vector3.one * 1.06f;
                items[i].SetAsLastSibling();
                break;
            }
        }
        if (dragging && draggedIndex >= 0 && Input.GetMouseButton(0) && Pointer(out Vector2 point))
            items[draggedIndex].anchoredPosition = ClampToSurface(point + dragOffset);
        if (dragging && Input.GetMouseButtonUp(0))
        {
            int source = draggedIndex;
            int destination = -1;
            for (int i = 0; i < 3; i++) if (Inside(targets[i])) { destination = i; break; }
            dragging = false;
            draggedIndex = -1;
            items[source].localScale = Vector3.one;
            bool aligned = chore != HouseholdChore.SortTools || source != 1 ||
                Mathf.Abs(Mathf.DeltaAngle(items[source].Find("PropIcon").localEulerAngles.z, 0f)) <= 18f;
            if (destination == source && aligned) Place(source);
            else
            {
                returning[source] = true;
                mistakes++;
                hint?.Invoke(!aligned ? "Giữ R hoặc lăn chuột khi kéo để xoay cuốc khớp móc." : destination < 0 ? "Hãy thả vật dụng vào đúng ngăn."
                    : "Chưa đúng ngăn — thử nhìn tên ngăn nhé.");
                AudioManager.Instance?.PlaySFX(SfxId.UIInvalid);
            }
        }
        // Keyboard fallback without replacing the primary drag interaction.
        if (!dragging)
            for (int i = 0; i < 3; i++)
                if (!placed[i] && Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { Place(i); break; }
    }

    private void Place(int index)
    {
        if (index < 0 || index >= 3 || placed[index]) return;
        placed[index] = true;
        returning[index] = false;
        items[index].SetParent(targets[index], false);
        items[index].anchoredPosition = Vector2.zero;
        items[index].sizeDelta = chore == HouseholdChore.PutAwayDishes
            ? new Vector2(112f, 54f) : new Vector2(147f, placementHasArt ? 69f : 53f);
        items[index].GetComponent<Image>().color = chore == HouseholdChore.PutAwayDishes
            ? new Color(.44f, .35f, .23f, .96f) : new Color(.28f, .52f, .36f);
        targets[index].GetComponent<Image>().color = new Color(.23f, .43f, .32f,
            placementHasArt ? .58f : 1f);
        if (chore == HouseholdChore.PutAwayDishes)
        {
            targets[index].Find("PropIcon").gameObject.SetActive(false);
            targets[index].Find("ShelfName").gameObject.SetActive(false);
        }
        successes++;
        mistakes = 0;
        pulse?.Invoke();
        if (successes < 3)
        {
            Report(successes / 3f, successes + (chore == HouseholdChore.PutAwayDishes
                ? "/3 món đã cất" : "/3 món đã xếp"));
            hint?.Invoke("Đúng chỗ rồi. Tiếp tục với món còn lại.");
            return;
        }
        if (chore == HouseholdChore.ArrangeKeepsakes || chore == HouseholdChore.CarryWood)
        {
            stage = 1;
            string label = chore == HouseholdChore.ArrangeKeepsakes ? "KÉO NẮP RƯƠNG XUỐNG" : "KÉO DÂY BUỘC SANG PHẢI";
            if (chore == HouseholdChore.CarryWood && LoadArt("Wood_Bundle") is Sprite bundleArt)
                AddArt("TiedWoodBundle", root, new Vector2(0f, 19f), new Vector2(430f, 124f), bundleArt);
            actionArea = Box("FinalGesture", root, new Vector2(0f, 55f),
                new Vector2(548f, chore == HouseholdChore.ArrangeKeepsakes ? 75f : 18f), new Color(.40f, .28f, .17f));
            if (chore == HouseholdChore.ArrangeKeepsakes && LoadArt("Chest_Lid") is Sprite lid)
            {
                actionArea.anchoredPosition = new Vector2(0f, 25f);
                actionArea.sizeDelta = new Vector2(548f, 108f);
                var lidImage = actionArea.GetComponent<Image>();
                lidImage.sprite = lid;
                lidImage.color = Color.white;
            }
            gestureHome = actionArea.anchoredPosition;
            Shape(actionArea, "BrassClasp", 0f, -8f, 22f, 27f, new Color(.86f, .66f, .30f));
            Label("FinalGestureText", actionArea, Vector2.zero, new Vector2(370f, 52f),
                label, 20f, new Color(1f, .91f, .72f));
            Report(.85f, "Sắp xong — " + (chore == HouseholdChore.ArrangeKeepsakes ? "đóng nắp" : "buộc bó củi"));
        }
        else Complete();
    }

    private void BuildDial()
    {
        mode = ActionMode.Dial;
        Sprite radioArt = LoadArt("Radio_Body");
        radioHasArt = radioArt != null;
        Label("RadioBody", root, new Vector2(0f, radioArt != null ? 130f : 72f), new Vector2(500f, 35f),
            "RADIO CỦA NHÀ MÌNH", 22f, new Color(.97f, .80f, .47f));
        if (radioArt != null)
        {
            var body = Box("RadioPhoto", root, new Vector2(0f, 0f), new Vector2(470f, 260f), Color.white);
            body.GetComponent<Image>().sprite = radioArt;
        }
        else
        {
            Box("RadioCase", root, new Vector2(0f, -5f), new Vector2(425f, 125f),
                new Color(.48f, .31f, .19f));
            Box("Speaker", root, new Vector2(-112f, -5f), new Vector2(132f, 86f),
                new Color(.18f, .23f, .23f));
            for (int i = 0; i < 6; i++)
                Shape(root, "SpeakerGrille", -164f + i * 20f, -5f, 5f, 71f,
                    new Color(.59f, .48f, .32f));
        }
        actionArea = Circle("RadioDial", root, new Vector2(radioArt != null ? 155f : 111f,
            radioArt != null ? -46f : -5f),
            radioArt != null ? 67f : 102f, radioArt != null
                ? new Color(.22f, .34f, .30f, .14f) : new Color(.19f, .27f, .27f));
        if (radioArt == null)
            Circle("DialCenter", actionArea, Vector2.zero, 70f, new Color(.76f, .65f, .44f));
        dialNeedle = Box("NeedlePivot", actionArea, Vector2.zero, Vector2.one, Color.clear);
        var needle = Shape(dialNeedle, "FrequencyNeedle", 0f, radioArt != null ? 16f : 25f,
            radioArt != null ? 14f : 6f, radioArt != null ? 32f : 43f, new Color(.21f, .27f, .26f));
        if (radioArt != null && LoadArt("Radio_Needle") is Sprite needleArt)
        {
            var image = needle.GetComponent<Image>();
            image.sprite = needleArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        Circle("NeedleHub", actionArea, Vector2.zero, radioArt != null ? 9f : 17f,
            new Color(.19f, .27f, .27f));
        status = Label("RadioStatus", root, new Vector2(0f, radioArt != null ? -130f : -83f), new Vector2(510f, 28f),
            "Kéo quanh núm để dò sóng", 19f, new Color(.82f, .89f, .86f));
        Report(0f, "Đang tìm tần số");
    }

    private void TickDial()
    {
        if (Input.GetMouseButtonDown(0) && Inside(actionArea)) dragging = true;
        if (Input.GetMouseButtonUp(0)) dragging = false;
        bool turning = dragging && Input.GetMouseButton(0);
        if (turning && Pointer(out Vector2 pointer))
        {
            Vector2 offset = pointer - actionArea.anchoredPosition;
            if (offset.sqrMagnitude >= 225f)
                dialFrequency = Mathf.Repeat(90f - Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg, 360f) / 360f;
        }
        float keys = (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
            (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        if (keys != 0f) { dialFrequency = Mathf.Clamp01(dialFrequency + keys * Time.deltaTime * .3f); turning = true; }
        // The knob stays tuned after the player releases it.
        if (dialNeedle != null) dialNeedle.localRotation = Quaternion.Euler(0f, 0f, -dialFrequency * 360f);
        float closeness = 1f - Mathf.Clamp01(Mathf.Abs(dialFrequency - .68f) / .32f);
        bool clear = Mathf.Abs(dialFrequency - .68f) <= .065f;
        heldTime = clear ? heldTime + Time.deltaTime : 0f;
        UpdateRadioAudio(closeness);
        actionArea.GetComponent<Image>().color = radioHasArt
            ? Color.Lerp(new Color(.32f, .40f, .39f, .12f), new Color(.30f, .72f, .43f, .33f), closeness)
            : Color.Lerp(new Color(.32f, .40f, .39f), new Color(.30f, .62f, .43f), closeness);
        status.text = clear ? "SÓNG RÕ — GIỮ NÚM MỘT CHÚT" : closeness > .65f ? "Sắp nghe rõ rồi…" : "Tiếng rè vẫn còn";
        Report(Mathf.Clamp01(closeness * .8f + heldTime / .8f * .2f), clear ? "Đang giữ đúng sóng" : "Đang dò sóng");
        if (heldTime >= .75f) Complete();
    }

    private void BuildRotary()
    {
        mode = ActionMode.Rotary;
        string title = "BƯỚC 1  ·  XOAY MẶT CỐI";
        Label("RotaryHeading", root, new Vector2(0f, 73f), new Vector2(510f, 35f),
            title, 21f, new Color(.97f, .80f, .47f));
        actionArea = Box("RotarySurface", root, new Vector2(0f, -3f),
            new Vector2(425f, 248f), Color.clear);
        actionArea.SetSiblingIndex(2);
        Sprite millBase = LoadArt("Corn_Mill_Base");
        Sprite millTop = LoadArt("Corn_Mill_UpperDisk");
        bool splitMill = millBase != null && millTop != null;
        Sprite rotaryArt = splitMill ? millBase : null;
        rotaryHasArt = rotaryArt != null;
        if (rotaryHasArt)
        {
            var image = actionArea.GetComponent<Image>();
            image.sprite = rotaryArt;
            image.color = Color.white;
            image.preserveAspect = true;
            if (splitMill)
                rotaryTopPivot = AddArt("UpperMillstoneArt", actionArea,
                    new Vector2(0f, 17f), new Vector2(195f, 125f), millTop);
        }
        else
        {
            actionArea.GetComponent<Image>().sprite = CircleSprite;
            rotaryTopPivot = Circle("UpperMillstoneArt", actionArea, new Vector2(0f, 17f), 150f,
                new Color(.65f, .59f, .48f));
            rotaryTopPivot.sizeDelta = new Vector2(150f, 83f);
            Transform details = rotaryTopPivot;
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2f / 9f;
                float radius = i % 2 == 0 ? 28f : 16f;
                Circle("Grain", details, new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .45f),
                    9f, new Color(.94f, .77f, .31f));
            }
        }
        for (int i = 0; i < millSurfaceMarks.Length; i++)
        {
            millSurfaceMarks[i] = Circle("TurningStoneMark" + i, rotaryTopPivot,
                Vector2.zero, 8f, new Color(.41f, .35f, .28f, .64f));
            millSurfaceMarks[i].sizeDelta = new Vector2(11f, 4f);
        }
        millCrankArm = Shape(actionArea, "HorizontalCrankArm", 0f, 0f, 70f, 10f,
            new Color(.48f, .29f, .14f));
        Shape(millCrankArm, "CrankWoodHighlight", 0f, 2f, 62f, 2f,
            new Color(.74f, .48f, .25f, .8f));
        millCrankGrip = Box("UprightCrankGrip", actionArea, Vector2.zero,
            new Vector2(16f, 32f), new Color(.49f, .29f, .15f));
        Circle("GripCap", millCrankGrip, new Vector2(0f, 16f), 16f,
            new Color(.70f, .43f, .22f));
        UpdateHorizontalMillVisual(0f);
        rotaryPullCue = Box("CollectFlourCue", root, new Vector2(191f, -15f),
            new Vector2(78f, 94f), new Color(.30f, .22f, .13f, .92f));
        Label("CollectArrow", rotaryPullCue, new Vector2(0f, 5f), new Vector2(72f, 66f),
            "↓", 47f, new Color(.99f, .78f, .39f));
        rotaryPullCue.gameObject.SetActive(false);
        status = Label("RotaryStatus", root, new Vector2(0f, -83f), new Vector2(520f, 28f),
            "Giữ chuột · di theo elip trên mặt cối", 18f, new Color(.83f, .89f, .86f));
        Report(0f, "0/3 vòng");
    }

    private void TickRotary()
    {
        if (stage == 1) { TickActionSwipe(Vector2.down); return; }
        float accepted = Input.GetKey(KeyCode.F) ? Time.deltaTime * 200f : 0f;
        if (Input.GetMouseButtonDown(0) && Inside(actionArea) && Pointer(out Vector2 start))
        { dragging = true; lastAngle = AngleAroundEllipse(start, actionArea.anchoredPosition + new Vector2(0f, 17f), 82f, 28f); }
        if (Input.GetMouseButtonUp(0)) dragging = false;
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
        {
            Vector2 center = actionArea.anchoredPosition + new Vector2(0f, 17f);
            float radius = EllipseRadius(point, center, 82f, 28f);
            float angle = AngleAroundEllipse(point, center, 82f, 28f);
            float step = Mathf.DeltaAngle(lastAngle, angle);
            lastAngle = angle;
            if (radius >= .30f && radius <= 1.70f && Mathf.Abs(step) <= 1080f * Time.deltaTime) accepted = step;
            else if (Mathf.Abs(step) > 1080f * Time.deltaTime) status.text = "Quay đều tay theo mặt cối nhé";
        }
        // Signed accumulation prevents small back-and-forth gestures counting as full turns.
        amount = Mathf.Clamp(amount + accepted, -1080f, 1080f);
        UpdateHorizontalMillVisual(amount);
        float fraction = Mathf.Abs(amount) / 1080f;
        if (!rotaryHasArt)
            rotaryTopPivot.GetComponent<Image>().color = Color.Lerp(new Color(.56f, .51f, .39f),
                new Color(.93f, .84f, .57f), fraction);
        Report(fraction * .85f, Mathf.FloorToInt(Mathf.Abs(amount) / 360f) + "/3 vòng đều tay");
        if (fraction < 1f) return;
        stage = 1; dragging = false;
        pulse?.Invoke();
        gestureHome = actionArea.anchoredPosition;
        millCrankArm.gameObject.SetActive(false);
        millCrankGrip.gameObject.SetActive(false);
        rotaryPullCue.gameObject.SetActive(true);
        root.Find("RotaryHeading").GetComponent<TextMeshProUGUI>().text = "BƯỚC 2  ·  THU BỘT NGÔ";
        status.text = "Kéo cối xuống để thu khay bột";
        hint?.Invoke(status.text);
    }

    private void UpdateHorizontalMillVisual(float degrees)
    {
        if (rotaryTopPivot == null || millCrankArm == null || millCrankGrip == null) return;
        float angle = degrees * Mathf.Deg2Rad;
        Vector2 center = new Vector2(0f, 17f);
        Vector2 endpoint = center + new Vector2(Mathf.Cos(angle) * 82f, Mathf.Sin(angle) * 28f);
        Vector2 offset = endpoint - center;
        millCrankArm.anchoredPosition = (center + endpoint) * .5f;
        millCrankArm.sizeDelta = new Vector2(offset.magnitude, 10f);
        millCrankArm.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
        var highlight = millCrankArm.Find("CrankWoodHighlight") as RectTransform;
        if (highlight != null) highlight.sizeDelta = new Vector2(Mathf.Max(12f, offset.magnitude - 8f), 2f);
        millCrankGrip.anchoredPosition = endpoint + new Vector2(0f, 13f);
        millCrankGrip.localScale = Vector3.one * (.86f - Mathf.Sin(angle) * .13f);
        for (int i = 0; i < millSurfaceMarks.Length; i++)
        {
            float phase = angle + i * Mathf.PI * 2f / millSurfaceMarks.Length;
            millSurfaceMarks[i].anchoredPosition = new Vector2(Mathf.Cos(phase) * 48f,
                Mathf.Sin(phase) * 18f);
        }
        // On the far half of the ellipse the arm passes behind the upper stone.
        if (Mathf.Sin(angle) > 0f)
        {
            millCrankArm.SetSiblingIndex(0);
            millCrankGrip.SetSiblingIndex(1);
            rotaryTopPivot.SetAsLastSibling();
        }
        else
        {
            rotaryTopPivot.SetSiblingIndex(0);
            millCrankArm.SetSiblingIndex(1);
            millCrankGrip.SetAsLastSibling();
        }
    }

    private void BuildWashRice()
    {
        mode = ActionMode.WashRice;
        Label("WashRiceHeading", root, new Vector2(0f, 123f), new Vector2(520f, 30f),
            "BƯỚC 1  ·  KHUẤY GẠO TRONG NỒI", 21f, new Color(.97f, .80f, .47f));
        actionArea = Box("RiceWashingPot", root, Vector2.zero,
            new Vector2(390f, 205f), Color.clear);
        if (LoadArt("Rice_Washing_Pot") is Sprite potArt)
        {
            var image = actionArea.GetComponent<Image>();
            image.sprite = potArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            actionArea.GetComponent<Image>().sprite = CircleSprite;
            actionArea.GetComponent<Image>().color = new Color(.60f, .62f, .57f);
            Circle("RiceAndWater", actionArea, new Vector2(0f, 7f), 102f,
                new Color(.82f, .86f, .82f));
        }
        // These light ripples move over the rice. The pot itself never rotates.
        rotaryTopPivot = new GameObject("WaterRipples", typeof(RectTransform)).GetComponent<RectTransform>();
        rotaryTopPivot.SetParent(actionArea, false);
        rotaryTopPivot.anchorMin = rotaryTopPivot.anchorMax = rotaryTopPivot.pivot = new Vector2(.5f, .5f);
        rotaryTopPivot.sizeDelta = Vector2.zero;
        var outerRipple = Circle("SoftWaterRipple", rotaryTopPivot, new Vector2(0f, 33f), 70f,
            new Color(.74f, .86f, .83f, .16f));
        outerRipple.sizeDelta = new Vector2(104f, 42f);
        fill = outerRipple;
        var innerRipple = Circle("InnerWaterRipple", rotaryTopPivot, new Vector2(3f, 35f), 35f,
            new Color(1f, 1f, 1f, .10f));
        innerRipple.sizeDelta = new Vector2(58f, 24f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f;
            var grain = Circle("FloatingGrain", rotaryTopPivot,
                new Vector2(Mathf.Cos(a) * 38f, 33f + Mathf.Sin(a) * 20f), 7f,
                new Color(1f, .97f, .85f, .78f));
            grain.sizeDelta = new Vector2(11f, 4f);
            grain.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            washingGrains[i] = grain;
        }
        rinseZone = Box("DrainWaterButton", root, new Vector2(202f, -9f),
            new Vector2(135f, 56f), new Color(.32f, .57f, .57f));
        Label("DrainWaterText", rinseZone, Vector2.zero, new Vector2(130f, 54f),
            "CHẮT NƯỚC  →", 16f, Color.white);
        rinseZone.gameObject.SetActive(false);
        status = Label("WashRiceStatus", root, new Vector2(0f, -127f), new Vector2(540f, 28f),
            "Giữ chuột · khuấy theo elip mặt nước", 18f, new Color(.84f, .90f, .88f));
        Report(0f, "Khuấy gạo trong nước · 0/2 vòng");
    }

    private void TickWashRice()
    {
        if (stage == 1)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                (Input.GetMouseButtonDown(0) && Inside(rinseZone))) Complete();
            return;
        }
        float accepted = Input.GetKey(KeyCode.F) ? Time.deltaTime * 210f : 0f;
        if (Input.GetMouseButtonDown(0) && Inside(actionArea) && Pointer(out Vector2 start))
        {
            dragging = true;
            lastAngle = AngleAroundEllipse(start, actionArea.anchoredPosition + new Vector2(0f, 34f), 78f, 30f);
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
        {
            Vector2 center = actionArea.anchoredPosition + new Vector2(0f, 34f);
            float radius = EllipseRadius(point, center, 78f, 30f);
            float angle = AngleAroundEllipse(point, center, 78f, 30f);
            float step = Mathf.DeltaAngle(lastAngle, angle);
            lastAngle = angle;
            if (radius >= .25f && radius <= 1.7f && Mathf.Abs(step) <= 1080f * Time.deltaTime)
                accepted = Mathf.Abs(step);
        }
        amount = Mathf.Min(720f, amount + accepted);
        for (int i = 0; i < washingGrains.Length; i++)
        {
            float angle = amount * Mathf.Deg2Rad + i * Mathf.PI * 2f / washingGrains.Length;
            washingGrains[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * 43f,
                33f + Mathf.Sin(angle) * 16f);
        }
        fill.sizeDelta = new Vector2(104f + Mathf.Sin(amount * Mathf.Deg2Rad) * 5f,
            42f + Mathf.Cos(amount * Mathf.Deg2Rad) * 2f);
        status.text = "ĐANG VO GẠO  ·  " + Mathf.Min(2, Mathf.FloorToInt(amount / 360f)) + "/2 VÒNG";
        Report(amount / 720f * .85f, "Khuấy gạo trong nước · " + Mathf.FloorToInt(amount / 360f) + "/2 vòng");
        if (amount < 720f) return;
        stage = 1;
        dragging = false;
        rinseZone.gameObject.SetActive(true);
        root.Find("WashRiceHeading").GetComponent<TextMeshProUGUI>().text = "BƯỚC 2  ·  CHẮT NƯỚC";
        status.text = "Gạo đã sạch — bấm CHẮT NƯỚC";
        hint?.Invoke(status.text);
        pulse?.Invoke();
    }

    private void BuildFetch()
    {
        mode = ActionMode.Fetch;
        Label("FetchHeading", root, new Vector2(0f, 76f), new Vector2(520f, 32f),
            "MÚC NƯỚC CHO MẸ", 21f, new Color(.97f, .80f, .47f));
        token = Box("WaterScoop", root, new Vector2(-178f, -18f),
            new Vector2(120f, 86f), new Color(.46f, .35f, .22f));
        Sprite scoopArt = LoadArt("Water_Scoop");
        bool scoopHasArt = scoopArt != null;
        if (scoopHasArt)
        {
            token.sizeDelta = new Vector2(178f, 116f);
            var image = token.GetComponent<Image>();
            image.sprite = scoopArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            Shape(token, "ScoopHandle", 24f, 14f, 31f, 7f, new Color(.82f, .69f, .46f), 28f);
            Circle("ScoopCup", token, new Vector2(-9f, 12f), 37f, new Color(.84f, .72f, .51f));
        }
        Circle("WaterInScoop", token, scoopHasArt ? new Vector2(-39f, -17f) : new Vector2(-9f, 12f),
            scoopHasArt ? 32f : 29f, new Color(.33f, .65f, .79f, 0f));
        tokenText = Label("ScoopName", token, new Vector2(0f, scoopHasArt ? -43f : -26f), new Vector2(115f, 26f),
            "GÁO RỖNG", 16f, Color.white);
        targets[0] = Box("WaterJarZone", root, new Vector2(175f, -18f),
            new Vector2(185f, 140f), new Color(.17f, .40f, .49f));
        if (LoadArt("Water_Jar") is Sprite jarArt)
        {
            var image = targets[0].GetComponent<Image>();
            image.sprite = jarArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            Shape(targets[0], "JarBody", 0f, 13f, 64f, 54f, new Color(.61f, .47f, .32f));
            Shape(targets[0], "JarLip", 0f, 41f, 78f, 8f, new Color(.87f, .72f, .48f));
            Shape(targets[0], "JarWater", 0f, 23f, 54f, 13f, new Color(.42f, .73f, .79f));
        }
        Label("WaterJarName", targets[0], new Vector2(0f, -38f), new Vector2(140f, 27f),
            "LU NƯỚC", 17f, Color.white);
        targets[1] = Box("WaterBasinZone", root, new Vector2(-178f, -18f),
            new Vector2(185f, 140f), new Color(.27f, .44f, .35f));
        if (LoadArt("Water_Basin") is Sprite basinArt)
        {
            var image = targets[1].GetComponent<Image>();
            image.sprite = basinArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            Circle("Basin", targets[1], new Vector2(0f, 13f), 66f, new Color(.77f, .80f, .68f));
            Circle("BasinInside", targets[1], new Vector2(0f, 13f), 46f, new Color(.26f, .51f, .58f));
        }
        Label("WaterBasinName", targets[1], new Vector2(0f, -38f), new Vector2(140f, 27f),
            "THAU NHẬN", 17f, Color.white);
        targets[1].gameObject.SetActive(false);
        token.SetAsLastSibling();
        status = Label("FetchStatus", root, new Vector2(0f, -82f), new Vector2(510f, 26f),
            "Kéo gáo vào lu nước", 18f, new Color(.84f, .90f, .89f));
        Report(0f, "Gáo chưa có nước");
    }

    private void TickFetch()
    {
        if (Input.GetMouseButtonDown(0) && Inside(token))
        {
            dragging = true; dragOrigin = token.anchoredPosition;
            if (Pointer(out Vector2 start)) dragOffset = dragOrigin - start;
        }
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
        {
            Vector2 previous = token.anchoredPosition;
            token.anchoredPosition = ClampToSurface(point + dragOffset);
            bool inJar = Vector2.Distance(token.anchoredPosition, targets[0].anchoredPosition) < 65f;
            if (inJar)
            {
                amount = Mathf.Min(1f, amount + Time.deltaTime / 2f);
                if (amount >= 1f && stage != 2)
                {
                    stage = 2; targets[1].gameObject.SetActive(true);
                    hint?.Invoke("Đầy gáo rồi — đưa chậm sang thau bên trái."); pulse?.Invoke();
                }
            }
            else if (stage == 2)
            {
                float speed = Vector2.Distance(previous, token.anchoredPosition) / Mathf.Max(Time.deltaTime, .001f);
                if (speed > 700f) amount = Mathf.Max(.65f, amount - Time.deltaTime * .14f);
                token.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * Mathf.Min(12f, speed * .012f));
            }
        }
        if (Input.GetKey(KeyCode.F))
        {
            amount = Mathf.Min(1f, amount + Time.deltaTime / 2f);
            if (amount >= 1f) { stage = 2; targets[1].gameObject.SetActive(true); }
        }
        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false; token.localRotation = Quaternion.identity;
            if (stage == 2 && Inside(targets[1])) { Complete(); return; }
        }
        if (stage == 2 && Input.GetKeyDown(KeyCode.Return)) { Complete(); return; }
        token.Find("WaterInScoop").GetComponent<Image>().color = new Color(.33f, .65f, .79f, amount);
        tokenText.text = amount > 0f ? "NƯỚC " + Mathf.RoundToInt(amount * 100f) + "%" : "GÁO RỖNG";
        status.text = stage == 2 ? "Đưa gáo vào thau rồi thả; đi chậm để nước ít sóng sánh" : "Kéo gáo vào lu và giữ để múc đầy";
        Report(amount * .8f, status.text);
    }

    private void BuildMeasure()
    {
        mode = ActionMode.Measure;
        Label("MeasureHeading", root, new Vector2(0f, 77f), new Vector2(520f, 30f),
            "ĐONG GẠO CHO BỮA CƠM", 21f, new Color(.97f, .80f, .47f));
        targets[0] = Box("RiceJar", root, new Vector2(-172f, -10f),
            new Vector2(170f, 145f), new Color(.48f, .31f, .18f));
        if (LoadArt("Rice_Jar") is Sprite jarArt)
        {
            var image = targets[0].GetComponent<Image>();
            image.sprite = jarArt; image.color = Color.white; image.preserveAspect = true;
        }
        Label("JarLabel", root, new Vector2(-178f, -68f), new Vector2(125f, 22f),
            "LU GẠO", 15f, Color.white);
        targets[1] = Box("RiceBasket", root, new Vector2(177f, -10f),
            new Vector2(190f, 142f), new Color(.62f, .43f, .23f));
        if (LoadArt("Rice_Basket_Empty") is Sprite basketArt)
        {
            var image = targets[1].GetComponent<Image>();
            image.sprite = basketArt; image.color = Color.white; image.preserveAspect = true;
        }
        for (int i = 0; i < 3; i++)
        {
            var pile = Circle("MeasuredRice" + i, targets[1],
                new Vector2((i - 1) * 31f, 4f + (i % 2) * 5f), 32f,
                new Color(.96f, .90f, .73f));
            pile.sizeDelta = new Vector2(40f, 15f);
            for (int grain = 0; grain < 5; grain++)
                Shape(pile, "RiceGrain", (grain - 2) * 7f, (grain % 2) * 5f,
                    8f, 3f, new Color(1f, .98f, .87f), grain % 2 == 0 ? 20f : -18f);
            items[i] = pile;
            pile.gameObject.SetActive(false);
        }
        Label("BasketLabel", root, new Vector2(178f, -68f), new Vector2(125f, 22f),
            "RÁ ĐONG  0/3", 15f, Color.white);
        token = Box("RiceScoop", root, new Vector2(-12f, -9f),
            new Vector2(135f, 108f), new Color(.72f, .51f, .28f));
        if (LoadArt("Rice_Scoop") is Sprite scoopArt)
        {
            var image = token.GetComponent<Image>();
            image.sprite = scoopArt; image.color = Color.white; image.preserveAspect = true;
        }
        token.SetAsLastSibling();
        status = Label("MeasureStatus", root, new Vector2(0f, -83f), new Vector2(540f, 28f),
            "Kéo gáo gạo từ lu sang rá · 3 lần", 18f, Color.white);
        Report(0f, "Đong gạo · 0/3 gáo");
    }

    private void TickMeasure()
    {
        if (!TickTokenDrag(targets[1]) && !Input.GetKeyDown(KeyCode.Return)) return;
        successes++;
        token.anchoredPosition = new Vector2(-12f, -9f);
        items[successes - 1].gameObject.SetActive(true);
        root.Find("BasketLabel").GetComponent<TextMeshProUGUI>().text = "RÁ ĐONG  " + successes + "/3";
        pulse?.Invoke();
        Report(successes / 3f, "Đong gạo · " + successes + "/3 gáo");
        if (successes >= 3) Complete();
    }

    private void BuildFire()
    {
        mode = ActionMode.Fire;
        Label("FireHeading", root, new Vector2(0f, 77f), new Vector2(520f, 30f),
            "NHÓM LỬA BẾP CỦI", 21f, new Color(.97f, .80f, .47f));
        token = Box("Kindling", root, new Vector2(-175f, -15f),
            new Vector2(155f, 96f), new Color(.45f, .30f, .17f));
        if (LoadArt("Wood_Bundle") is Sprite firewood)
        {
            var image = token.GetComponent<Image>();
            image.sprite = firewood;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            Shape(token, "KindlingLogA", -5f, 11f, 68f, 12f, new Color(.70f, .47f, .27f), 16f);
            Shape(token, "KindlingLogB", 5f, 5f, 68f, 12f, new Color(.82f, .57f, .32f), -15f);
        }
        tokenText = Label("KindlingName", token, new Vector2(0f, -24f), new Vector2(120f, 23f),
            "CỦI KHÔ", 16f, Color.white);
        targets[0] = Box("Hearth", root, new Vector2(168f, -15f),
            new Vector2(180f, 130f), new Color(.38f, .18f, .12f));
        Sprite hearthArt = LoadArt("Fire_Hearth");
        fireHasArt = hearthArt != null;
        if (fireHasArt)
        {
            var image = targets[0].GetComponent<Image>();
            image.sprite = hearthArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else Circle("HearthOpening", targets[0], new Vector2(0f, 9f), 66f, new Color(.16f, .13f, .12f));
        ember = Shape(targets[0], "Ember", 0f, 10f, 28f, 27f,
            new Color(.54f, .27f, .14f)).GetComponent<Image>();
        Label("HearthName", targets[0], new Vector2(0f, -36f), new Vector2(136f, 24f),
            "BẾP CỦI", 17f, Color.white);
        status = Label("FireStatus", root, new Vector2(0f, -82f), new Vector2(520f, 28f),
            "Đặt củi vào bếp trước", 18f, new Color(.85f, .89f, .84f));
        Report(0f, "Chưa có củi trong bếp");
    }

    private void TickFire()
    {
        if (stage == 0)
        {
            if (!TickTokenDrag(targets[0]) && !Input.GetKeyDown(KeyCode.Return)) return;
            stage = 1;
            token.anchoredPosition = new Vector2(-175f, -15f);
            tokenText.text = "MỒI LỬA";
            token.GetComponent<Image>().color = token.GetComponent<Image>().sprite != null
                ? Color.white : new Color(.82f, .57f, .24f);
            status.text = "Kéo mồi lửa vào giữa củi khô";
            hint?.Invoke("Đã đặt củi — thêm mồi lửa nhé.");
            pulse?.Invoke();
            return;
        }
        if (stage == 1)
        {
            if (!TickTokenDrag(targets[0]) && !Input.GetKeyDown(KeyCode.Return)) return;
            stage = 2;
            token.gameObject.SetActive(false);
            targets[0].anchoredPosition = Vector2.zero;
            status.text = "Chờ than sáng rồi nhấn Space hoặc bấm bếp";
            hint?.Invoke("Than sáng lên theo nhịp. Thổi khi bếp chuyển màu vàng.");
            Report(.25f, "Đã đặt củi · 0/3 nhịp thổi");
            pulse?.Invoke();
            return;
        }
        float beat = (Mathf.Sin(elapsed * 3f) + 1f) * .5f;
        if (beat < .3f) breathUsed = false;
        bool ready = beat >= .48f && beat <= .76f;
        if (!fireHasArt)
            targets[0].GetComponent<Image>().color = ready ? new Color(.90f, .53f, .18f)
                : new Color(.38f, .18f, .12f);
        if (ember != null) ember.color = ready ? new Color(1f, .88f, .38f)
            : new Color(.54f, .27f, .14f);
        if (!(Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && Inside(targets[0])))) return;
        if (!ready || breathUsed)
        {
            mistakes++;
            hint?.Invoke("Chờ than vàng sáng hơn một chút rồi thổi nhé.");
            return;
        }
        breathUsed = true;
        successes++;
        pulse?.Invoke();
        if (successes >= 3) { Complete(); return; }
        Report(.25f + successes * .25f, "Đã thổi đúng " + successes + "/3 nhịp");
        hint?.Invoke("Lửa đang bắt lên rồi!");
    }

    private void BuildHeat()
    {
        mode = ActionMode.Heat;

        heatTrack = Box("HeatTrack", root, new Vector2(0f, 0f),
            new Vector2(380f, 50f), new Color(.30f, .18f, .12f));
        if (LoadArt("Cook_Pot") is Sprite potArt)
            AddArt("CookPotArt", root, new Vector2(0f, 51f), new Vector2(215f, 145f), potArt);
        else
        {
            Shape(root, "PotBody", 0f, 56f, 96f, 37f, new Color(.68f, .71f, .65f));
            Shape(root, "PotLid", 0f, 77f, 108f, 5f, new Color(.89f, .81f, .65f));
            Shape(root, "PotHandleL", -56f, 56f, 17f, 8f, new Color(.89f, .81f, .65f));
            Shape(root, "PotHandleR", 56f, 56f, 17f, 8f, new Color(.89f, .81f, .65f));
        }
        Box("WarmEmbers", heatTrack, Vector2.zero,
            new Vector2(76f, 12f), new Color(.96f, .50f, .18f));
        heatHandle = Box("HeatHandle", heatTrack, new Vector2(-114f, 0f),
            new Vector2(90f, 22f), new Color(.64f, .39f, .19f));
        Shape(heatHandle, "LogGrain", 0f, 0f, 75f, 3f, new Color(.85f, .62f, .34f));
        for (int i = 0; i < 3; i++)
            steam[i] = Circle("Steam", root, new Vector2((i - 1) * 25f, 82f), 14f, new Color(1f, 1f, 1f, .2f));
        amount = .2f;
        status = Label("HeatStatus", root, new Vector2(0f, -71f), new Vector2(510f, 32f),
            "Đẩy củi vào hoặc rút ra để cơm sôi đều", 19f, new Color(.85f, .90f, .86f));
        Report(0f, "Cơm đang được nấu");
    }

    private void TickHeat()
    {
        if (Input.GetMouseButtonDown(0) && Inside(heatHandle)) dragging = true;
        if (Input.GetMouseButtonUp(0)) dragging = false;
        if (dragging && Input.GetMouseButton(0) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(heatTrack, Input.mousePosition, null, out Vector2 point))
            amount = Mathf.Clamp01((point.x + 190f) / 380f);
        if (Input.GetKey(KeyCode.LeftArrow)) amount = Mathf.Clamp01(amount - Time.deltaTime * .25f);
        if (Input.GetKey(KeyCode.RightArrow)) amount = Mathf.Clamp01(amount + Time.deltaTime * .25f);
        heatHandle.anchoredPosition = new Vector2(-190f + amount * 380f, 0f);
        bool warm = amount >= .40f && amount <= .60f;
        amount = Mathf.Clamp01(amount + Mathf.Sin(elapsed * .7f) * Time.deltaTime * .025f);
        heldTime = Mathf.Clamp(heldTime + Time.deltaTime * (warm ? 1f : 0f), 0f, 16f);
        for (int i = 0; i < 3; i++)
        {
            float rise = Mathf.Repeat(elapsed * (warm ? 18f : 8f) + i * 17f, 48f);
            steam[i].anchoredPosition = new Vector2((i - 1) * 25f + Mathf.Sin(elapsed + i) * 6f, 63f + rise);
            steam[i].GetComponent<Image>().color = new Color(1f, .96f, .87f, (1f - rise / 48f) * .5f);
        }
        status.text = warm ? "Lửa vừa · cơm đang sôi đều" : amount < .40f ? "Lửa hơi nhỏ" : "Lửa hơi lớn";
        Report(heldTime / 16f, warm ? "Cơm đang sôi đều" : "Kéo củi gần hoặc xa tâm bếp một chút");
        if (heldTime >= 16f) Complete();
    }

    private void BuildScrub()
    {
        mode = ActionMode.Scrub;
        Label("ScrubHeading", root, new Vector2(0f, 126f), new Vector2(520f, 30f),
            "CHÉN 1/3  ·  CHÀ VẾT DƠ", 21f, new Color(.97f, .80f, .47f));
        dish = Box("Dish", root, new Vector2(40f, -3f),
            new Vector2(360f, 202f), new Color(.74f, .78f, .72f));
        dish.SetSiblingIndex(2);
        if (LoadArt("Clean_Dish") is Sprite dishArt)
        {
            var image = dish.GetComponent<Image>();
            image.sprite = dishArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            dish.GetComponent<Image>().sprite = CircleSprite;
            Circle("DishWell", dish, new Vector2(0f, -8f), 87f, new Color(.89f, .90f, .79f));
        }
        scrubber = Box("Scrubber", root, new Vector2(-212f, -6f),
            new Vector2(86f, 86f), new Color(.71f, .47f, .23f));
        if (LoadArt("Dish_Scrubber") is Sprite scrubberArt)
        {
            var image = scrubber.GetComponent<Image>();
            image.sprite = scrubberArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        dishText = Label("ScrubberLabel", root, new Vector2(-212f, -77f), new Vector2(130f, 24f),
            "BÙI NHÙI", 15f, new Color(.91f, .80f, .57f));
        for (int i = 0; i < 3; i++)
        {
            Vector2 spot = i == 0 ? new Vector2(-64f, 17f) :
                i == 1 ? new Vector2(68f, 19f) : new Vector2(0f, -31f);
            dirt[i] = Circle("Dirt" + i, dish, spot, 30f,
                new Color(.45f, .25f, .10f, .88f)).GetComponent<Image>();
            dirt[i].rectTransform.sizeDelta = new Vector2(i == 2 ? 42f : 35f, i == 1 ? 25f : 20f);
            dirt[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 1 ? -18f : 24f);
        }
        rinseZone = Box("RinseWater", root, new Vector2(178f, -5f),
            new Vector2(175f, 150f), new Color(.25f, .62f, .74f));
        if (LoadArt("Water_Basin") is Sprite rinseArt)
        {
            var image = rinseZone.GetComponent<Image>();
            image.sprite = rinseArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        rinseZone.gameObject.SetActive(false);
        status = Label("ScrubStatus", root, new Vector2(0f, -126f), new Vector2(510f, 28f),
            "Kéo bùi nhùi lên từng vết dơ và chà qua lại", 18f, new Color(.84f, .89f, .86f));
        Report(0f, "Chén 1/3 · 3 vết dơ");
    }

    private void TickScrub()
    {
        if (stage == 0)
        {
            if (Input.GetMouseButtonDown(0) && Inside(scrubber) && Pointer(out Vector2 start))
            {
                dragging = true;
                pointerStart = start;
                dragOffset = scrubber.anchoredPosition - start;
                scrubber.SetAsLastSibling();
            }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
            {
                float distance = Vector2.Distance(point, pointerStart);
                pointerStart = point;
                scrubber.anchoredPosition = ClampToSurface(point + dragOffset);
                if (distance > .5f && distance < 70f)
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 stainCenter = root.InverseTransformPoint(dirt[i].rectTransform.position);
                        if (Vector2.Distance(scrubber.anchoredPosition, stainCenter) < 42f)
                            stainRemaining[i] = Mathf.Max(0f, stainRemaining[i] - distance / 95f);
                    }
            }
            if (!dragging) scrubber.anchoredPosition = Vector2.MoveTowards(
                scrubber.anchoredPosition, new Vector2(-212f, -6f), Time.deltaTime * 600f);
            float remaining = 0f;
            for (int i = 0; i < 3; i++)
            {
                dirt[i].color = new Color(.45f, .25f, .10f, stainRemaining[i] * .88f);
                remaining += stainRemaining[i];
            }
            Report((successes + (1f - remaining / 3f) * .8f) / 3f,
                "Chén " + (successes + 1) + "/3 · " + Mathf.CeilToInt(remaining) + " vết dơ");
            if (remaining > .01f) return;
            stage = 1;
            dragging = false;
            scrubber.gameObject.SetActive(false);
            dish.anchoredPosition = new Vector2(-115f, -4f);
            dish.sizeDelta = new Vector2(185f, 137f);
            rinseZone.gameObject.SetActive(true);
            status.text = "Kéo chén sạch vào thau nước để tráng";
            hint?.Invoke(status.text);
            pulse?.Invoke();
            return;
        }
        if (!TickTokenDrag(rinseZone, dish)) return;
        AudioManager.Instance?.PlaySFX(SfxId.WaterSplash);
        successes++; pulse?.Invoke();
        if (successes >= 3) { Complete(); return; }
        stage = 0;
        dish.anchoredPosition = new Vector2(40f, -3f);
        dish.sizeDelta = new Vector2(360f, 202f);
        scrubber.anchoredPosition = new Vector2(-212f, -6f);
        scrubber.gameObject.SetActive(true);
        rinseZone.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            stainRemaining[i] = 1f;
            dirt[i].color = new Color(.45f, .25f, .10f, .88f);
        }
        root.Find("ScrubHeading").GetComponent<TextMeshProUGUI>().text =
            "CHÉN " + (successes + 1) + "/3  ·  CHÀ VẾT DƠ";
        status.text = "Kéo bùi nhùi lên từng vết dơ và chà qua lại";
        hint?.Invoke(status.text);
    }

    private void BuildLetter()
    {
        mode = ActionMode.Letter;
        Label("LetterHeading", root, new Vector2(0f, 78f), new Vector2(520f, 30f),
            "LÁ THƯ CỦA MẸ", 23f, new Color(.97f, .80f, .47f));
        actionArea = Box("LetterEnvelope", root, Vector2.zero,
            new Vector2(460f, 190f), new Color(.74f, .62f, .43f));
        actionArea.SetSiblingIndex(2);
        if (LoadArt("Letter_Envelope") is Sprite letterArt)
        {
            var image = actionArea.GetComponent<Image>();
            image.sprite = letterArt;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            Shape(actionArea, "EnvelopeFoldLeft", -91f, 15f, 151f, 3f, new Color(.58f, .43f, .28f), 29f);
            Shape(actionArea, "EnvelopeFoldRight", 91f, 15f, 151f, 3f, new Color(.58f, .43f, .28f), -29f);
        }
        Shape(actionArea, "Ribbon", 0f, -20f, 16f, 73f, new Color(.50f, .26f, .19f));
        status = Label("EnvelopeText", root, new Vector2(0f, -126f), new Vector2(520f, 36f),
            "DÂY BUỘC  ·  KÉO SANG PHẢI", 16f, new Color(.94f, .91f, .79f));
        Report(0f, "Tháo dây buộc lá thư");
    }

    private void TickLetter()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceLetter();
            return;
        }
        if (Input.GetMouseButtonDown(0) && Inside(actionArea) && Pointer(out Vector2 start))
        {
            dragging = true;
            pointerStart = start;
        }
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 current))
        {
            Vector2 movement = current - pointerStart;
            if (stage == 0) actionArea.Find("Ribbon").localPosition = new Vector3(Mathf.Clamp(movement.x, 0f, 95f), -20f, 0f);
            else actionArea.localRotation = Quaternion.Euler(stage == 1 ? Mathf.Clamp(-movement.y, -60f, 0f) : 0f,
                stage == 2 ? Mathf.Clamp(-movement.x, 0f, 60f) : 0f, 0f);
        }
        if (!dragging || !Input.GetMouseButtonUp(0) || !Pointer(out Vector2 end)) return;
        dragging = false;
        actionArea.localRotation = Quaternion.identity;
        Vector2 delta = end - pointerStart;
        bool correct = stage == 0 ? delta.x > 45f : stage == 1 ? delta.y > 45f : delta.x < -45f;
        if (!correct)
        {
            hint?.Invoke(stage == 0 ? "Chưa đúng hướng — kéo dây sang phải để tháo." :
                stage == 1 ? "Chưa đúng hướng — kéo mép giấy lên." : "Chưa đúng hướng — lật trang sang trái.");
            mistakes++;
            if (stage == 0) actionArea.Find("Ribbon").localPosition = new Vector3(0f, -20f, 0f);
            return;
        }
        AdvanceLetter();
    }

    private void AdvanceLetter()
    {
        stage++;
        actionArea.Find("Ribbon").gameObject.SetActive(false);
        pulse?.Invoke();
        if (stage >= 3) { Complete(); return; }
        status.text = stage == 1 ? "MỞ NẾP GIẤY  ·  KÉO LÊN" : "LẬT TRANG  ·  KÉO SANG TRÁI";
        if (actionArea.GetComponent<Image>().sprite == null)
            actionArea.GetComponent<Image>().color = stage == 1 ? new Color(.85f, .76f, .57f) : new Color(.93f, .87f, .71f);
        Report(stage / 3f, stage == 1 ? "Đã tháo dây · mở lá thư" : "Đã mở lá thư · lật trang");
    }

    private string IdleHint()
    {
        switch (mode)
        {
            case ActionMode.Placement: return chore == HouseholdChore.PutAwayDishes
                ? "Kéo chén, đĩa và đũa ở khay bên phải sang đúng ngăn tủ."
                : chore == HouseholdChore.SortTools && stage == 0 ? "Kéo dụng cụ vào móc cùng hình. Riêng cuốc: giữ R hoặc lăn chuột khi kéo để xoay khớp bóng." : stage == 1 ? "Kéo thẻ ở dưới theo hướng chỉ dẫn để hoàn tất."
                : "Giữ chuột lên một vật ở hàng dưới, kéo tới ngăn cùng tên rồi thả.";
            case ActionMode.Dial: return "Kéo quanh núm radio; có thể dùng Q/E hoặc phím mũi tên.";
            case ActionMode.Rotary: return "Giữ chuột và di theo vòng quanh tâm; có thể giữ F.";
            case ActionMode.WashRice: return stage == 0
                ? "Giữ chuột khuấy nhẹ gạo trong nồi khoảng hai vòng; có thể giữ F."
                : "Bấm CHẮT NƯỚC hoặc Enter để hoàn tất.";
            case ActionMode.Fetch: return "Kéo gáo vào lu, giữ chuột để múc, rồi đưa sang thau.";
            case ActionMode.Measure: return "Kéo gáo gạo từ lu sang rá, thả vào rá ba lần.";
            case ActionMode.Fire: return "Đặt củi trước; khi bếp sáng vàng hãy nhấn Space.";
            case ActionMode.Heat: return "Kéo thanh củi vào hoặc rút ra, nghe nồi sôi và nhìn hơi nước để canh lửa.";
            case ActionMode.Scrub: return stage == 0
                ? "Cầm bùi nhùi bên trái, kéo lên từng vết dơ và chà qua lại."
                : "Kéo chén sạch sang thau nước để tráng.";
            default: return "Giữ chuột trên lá thư và kéo theo hướng ghi trên giấy.";
        }
    }

    private bool TickTokenDrag(RectTransform destination, RectTransform movable = null)
    {
        RectTransform moving = movable != null ? movable : token;
        if (moving == null || destination == null) return false;
        if (Input.GetMouseButtonDown(0) && Inside(moving))
        {
            dragging = true;
            dragOrigin = moving.anchoredPosition;
            if (Pointer(out Vector2 start)) dragOffset = dragOrigin - start;
            moving.SetAsLastSibling();
        }
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
            moving.anchoredPosition = ClampToSurface(point + dragOffset);
        if (!dragging || !Input.GetMouseButtonUp(0)) return false;
        dragging = false;
        bool hit = Inside(destination);
        moving.anchoredPosition = hit ? destination.anchoredPosition : dragOrigin;
        if (!hit)
        {
            mistakes++;
            hint?.Invoke("Chưa vào đúng vùng — kéo vật tới vùng được đánh dấu rồi thả.");
            AudioManager.Instance?.PlaySFX(SfxId.UIInvalid);
        }
        return hit;
    }

    private void TickActionSwipe(Vector2 direction)
    {
        if (actionArea == null) return;
        if (Input.GetKeyDown(KeyCode.Return)) { Complete(); return; }
        if (Input.GetMouseButtonDown(0) && Inside(actionArea) && Pointer(out Vector2 start)) { dragging = true; pointerStart = start; }
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
        {
            float travel = Mathf.Clamp(Vector2.Dot(point - pointerStart, direction), 0f, 90f);
            actionArea.anchoredPosition = gestureHome + direction * travel;
            if (travel >= 85f) { dragging = false; Complete(); return; }
        }
        if (dragging && Input.GetMouseButtonUp(0)) { dragging = false; mistakes++; hint?.Invoke("Kéo thêm một chút theo hướng gợi ý nhé."); }
        if (!dragging)
        {
            actionArea.anchoredPosition = Vector2.MoveTowards(actionArea.anchoredPosition, gestureHome, Time.deltaTime * 200f);
            actionArea.localRotation = Quaternion.RotateTowards(actionArea.localRotation, Quaternion.identity, Time.deltaTime * 200f);
        }
    }

    private void Complete()
    {
        if (!active) return;
        active = false;
        pulse?.Invoke();
        Report(1f, "Hoàn thành");
        completed?.Invoke();
    }

    private void Report(float value, string message)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Abs(value - lastReported) < .015f && message == lastMessage) return;
        lastReported = value;
        lastMessage = message;
        progress?.Invoke(value, message);
    }

    private bool Pointer(out Vector2 point) =>
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Input.mousePosition, null, out point);

    private static bool Inside(RectTransform rect) => rect != null && rect.gameObject.activeInHierarchy &&
        RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);

    private static float AngleAround(Vector2 point, Vector2 center) =>
        Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;

    private static float AngleAroundEllipse(Vector2 point, Vector2 center, float radiusX, float radiusY) =>
        Mathf.Atan2((point.y - center.y) / radiusY, (point.x - center.x) / radiusX) * Mathf.Rad2Deg;

    private static float EllipseRadius(Vector2 point, Vector2 center, float radiusX, float radiusY)
    {
        Vector2 normalized = new Vector2((point.x - center.x) / radiusX,
            (point.y - center.y) / radiusY);
        return normalized.magnitude;
    }

    private static Vector2 ClampToSurface(Vector2 point) =>
        new Vector2(Mathf.Clamp(point.x, -260f, 260f), Mathf.Clamp(point.y, -90f, 90f));

    private static Sprite LoadArt(string name) => Resources.Load<Sprite>(MiniGameArtPath + name);

    private static string PlacementArtName(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.ArrangeKeepsakes: return "Chest_Open";
            case HouseholdChore.SortTools: return "Tool_Rack";
            case HouseholdChore.SetTable: return "Meal_Tray_Empty";
            case HouseholdChore.PutAwayDishes: return "Dish_Cabinet";
            default: return null;
        }
    }

    private static RectTransform AddArt(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite)
    {
        var artwork = Box(name, parent, position, size, Color.white);
        var image = artwork.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return artwork;
    }

    private void AddArtBackground(string name, float opacity = 1f)
    {
        Sprite sprite = LoadArt(name);
        if (sprite == null) return;
        var background = AddArt(name + "Background", root, Vector2.zero,
            new Vector2(558f, Mathf.Min(278f, root.sizeDelta.y - 8f)), sprite);
        var image = background.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, opacity);
        if (name == "Dish_Cabinet") background.sizeDelta = new Vector2(465f, 290f);
        // Keep the illustration above the inset mat and below the frame/header.
        background.SetSiblingIndex(2);
    }

    private void DrawProp(RectTransform card, int index)
    {
        var icon = Box("PropIcon", card, new Vector2(-47f, 0f), new Vector2(46f, 46f),
            Color.clear);
        string spriteName = null;
        if (chore == HouseholdChore.ArrangeKeepsakes)
            spriteName = index == 0 ? "Old_Shirt" : index == 1 ? "Old_Towel" : "Keepsake_Box";
        else if (chore == HouseholdChore.SortTools)
            spriteName = index == 0 ? "Tool_Rake" : index == 1 ? "Tool_Hoe" : "Tool_Shovel";
        else if (chore == HouseholdChore.CarryWood)
            spriteName = "Firewood_Log";
        else if (chore == HouseholdChore.PutAwayDishes)
            spriteName = index == 0 ? "Dish_Bowl" : index == 1 ? "Dish_Plate" : "Dish_Chopsticks";
        Sprite art = spriteName == null ? null : LoadArt(spriteName);
        if (art != null)
        {
            if (chore == HouseholdChore.PutAwayDishes)
                icon.anchoredPosition = new Vector2(0f, 8f);
            icon.sizeDelta = chore == HouseholdChore.PutAwayDishes
                ? new Vector2(101f, 49f) : chore == HouseholdChore.SortTools
                    ? new Vector2(136f, 83f) : new Vector2(130f, 78f);
            var image = icon.GetComponent<Image>();
            image.sprite = art;
            image.color = Color.white;
            image.preserveAspect = true;
            if (chore == HouseholdChore.CarryWood)
                icon.localRotation = Quaternion.Euler(0f, 0f, (index - 1) * 8f);
            return;
        }
        // Keep the native UGUI silhouette as a fallback when an art asset is missing.
        Color cream = new Color(.95f, .85f, .64f);
        Color clay = new Color(.68f, .38f, .23f);
        Color wood = new Color(.58f, .34f, .18f);
        Color light = new Color(.95f, .91f, .76f);
        switch (chore)
        {
            case HouseholdChore.ArrangeKeepsakes:
                if (index == 0)
                {
                    Shape(icon, "ShirtBody", 0f, -4f, 24f, 22f, cream);
                    Shape(icon, "LeftSleeve", -15f, 7f, 11f, 14f, cream, 24f);
                    Shape(icon, "RightSleeve", 15f, 7f, 11f, 14f, cream, -24f);
                    Shape(icon, "Collar", 0f, 11f, 10f, 4f, wood);
                }
                else if (index == 1)
                {
                    Shape(icon, "Towel", 0f, 0f, 30f, 34f, light);
                    Shape(icon, "Hem", 0f, -12f, 30f, 4f, clay);
                    Shape(icon, "Fold", 7f, 5f, 3f, 24f, cream);
                }
                else
                {
                    Shape(icon, "KeepsakeBox", 0f, -5f, 29f, 25f, wood);
                    Shape(icon, "KeepsakeLid", 0f, 9f, 34f, 6f, cream);
                    Shape(icon, "KeepsakeClasp", 0f, -2f, 7f, 9f, light);
                }
                break;
            case HouseholdChore.SortTools:
                Shape(icon, "ToolHandle", 0f, -2f, 5f, 36f, wood, -12f);
                if (index == 0)
                {
                    Shape(icon, "RakeHead", 4f, 17f, 29f, 5f, light);
                    for (int n = -1; n <= 1; n++) Shape(icon, "RakeTooth", 4f + n * 10f, 12f, 3f, 10f, light);
                }
                else if (index == 1) Shape(icon, "HoeHead", 7f, 16f, 27f, 8f, light, -16f);
                else Shape(icon, "ShovelHead", 4f, 17f, 17f, 15f, light);
                break;
            case HouseholdChore.CarryWood:
                for (int n = -1; n <= 1; n++)
                {
                    Shape(icon, "Log", n * 2f, n * 10f, 34f, 8f, wood, n * 8f);
                    Shape(icon, "LogEnd", 16f + n * 2f, n * 10f, 4f, 8f, cream, n * 8f);
                }
                break;
            case HouseholdChore.SetTable:
            case HouseholdChore.PutAwayDishes:
                bool bowl = index == 0;
                bool chopsticks = chore == HouseholdChore.SetTable ? index == 1 : index == 2;
                if (bowl)
                {
                    Shape(icon, "BowlRim", 0f, 10f, 35f, 5f, light);
                    Shape(icon, "Bowl", 0f, -1f, 29f, 18f, cream);
                    Shape(icon, "BowlFoot", 0f, -12f, 17f, 4f, light);
                }
                else if (chopsticks)
                {
                    Shape(icon, "ChopstickA", -6f, 0f, 4f, 36f, wood, 13f);
                    Shape(icon, "ChopstickB", 6f, 0f, 4f, 36f, light, -13f);
                }
                else if (chore == HouseholdChore.SetTable)
                {
                    Shape(icon, "PotBody", 0f, -3f, 31f, 23f, light);
                    Shape(icon, "PotLid", 0f, 11f, 36f, 5f, cream);
                    Shape(icon, "PotKnob", 0f, 16f, 8f, 5f, wood);
                    Shape(icon, "PotHandleL", -19f, -3f, 7f, 6f, light);
                    Shape(icon, "PotHandleR", 19f, -3f, 7f, 6f, light);
                }
                else
                {
                    Shape(icon, "PlateRim", 0f, 0f, 37f, 32f, light);
                    Shape(icon, "PlateWell", 0f, 0f, 27f, 22f, clay);
                    Shape(icon, "PlateCenter", 0f, 0f, 20f, 15f, cream);
                }
                break;
            case HouseholdChore.FoldBlanket:
                Shape(icon, "Blanket", 0f, 0f, 36f, 31f, clay);
                Shape(icon, "BlanketHem", 0f, -12f, 36f, 5f, cream);
                Shape(icon, "FoldGuide", index == 0 ? -10f : index == 1 ? 10f : 0f,
                    index == 2 ? 8f : 0f, index == 2 ? 30f : 4f,
                    index == 2 ? 4f : 27f, light);
                break;
        }
    }

    private static RectTransform Shape(Transform parent, string name, float x, float y,
        float width, float height, Color color, float angle = 0f)
    {
        var shape = Box(name, parent, new Vector2(x, y), new Vector2(width, height), color);
        shape.localRotation = Quaternion.Euler(0f, 0f, angle);
        return shape;
    }

    private static RectTransform Circle(string name, Transform parent, Vector2 position, float diameter, Color color)
    {
        var shape = Box(name, parent, position, new Vector2(diameter, diameter), color);
        shape.GetComponent<Image>().sprite = CircleSprite;
        return shape;
    }

    private static Sprite CircleSprite
    {
        get
        {
            if (circleSprite != null) return circleSprite;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "HouseholdUIRoundMask";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(15.5f - distance));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f), size);
            circleSprite.name = "HouseholdUIRoundSprite";
            return circleSprite;
        }
    }

    private static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size,
        string text, float sizeInPixels, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = sizeInPixels;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.color = color;
        label.raycastTarget = false;
        if (font != null) label.font = font;
        return label;
    }
}
