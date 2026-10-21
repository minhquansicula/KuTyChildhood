using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene 2's objective card, task board and gentle guidance; quest state stays in its existing owners.</summary>
public sealed class Scene2QuestHUD : MonoBehaviour
{
    private const float TextHintSeconds = 20f;
    private const float MarkerSeconds = 40f;
    private const float HintDuration = 7f;

    private readonly Dictionary<HouseholdChore, Transform> choreTargets = new Dictionary<HouseholdChore, Transform>();
    private readonly Dictionary<Transform, Collider> targetColliders = new Dictionary<Transform, Collider>();
    private HouseholdChoreFlow chores;
    private QuestManager quests;
    private HUDController existingHud;
    private Camera playerCamera;
    private CanvasGroup rootGroup;
    private GameObject board;
    private GameObject hintPanel;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI boardText;
    private TextMeshProUGUI hintText;
    private RectTransform marker;
    private Transform shopTarget;
    private Transform marbleTarget;
    private Transform journalTarget;
    private Transform keyTarget;
    private Transform doorTarget;
    private float idleSeconds;
    private float hintRemaining;
    private bool textHintShown;
    private bool markerShown;
    private bool boardOpen;

    private void Start()
    {
        chores = GetComponent<HouseholdChoreFlow>();
        quests = QuestManager.Instance;
        existingHud = HUDController.Instance;
        playerCamera = Camera.main;
        if (playerCamera == null) playerCamera = FindObjectOfType<Camera>();

        CreateView();
        CacheTargets();
        if (existingHud != null) existingHud.SetObjectiveVisible(false);
        if (chores != null) chores.OnChoreCompleted += HandleChoreCompleted;
        if (quests != null) quests.OnQuestChanged += HandleQuestChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (chores != null) chores.OnChoreCompleted -= HandleChoreCompleted;
        if (quests != null) quests.OnQuestChanged -= HandleQuestChanged;
        if (existingHud != null) existingHud.SetObjectiveVisible(true);
        if (rootGroup != null) Destroy(rootGroup.gameObject);
    }

    private void OnDisable()
    {
        if (rootGroup != null) rootGroup.alpha = 0f;
    }

    private void Update()
    {
        if (rootGroup == null) return;
        bool visible = CanShow();
        rootGroup.alpha = visible ? 1f : 0f;
        if (!visible) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            boardOpen = !boardOpen;
            board.SetActive(boardOpen);
            Refresh();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHint();
            markerShown = ResolveTarget() != null;
        }

        idleSeconds += Time.deltaTime;
        if (!textHintShown && idleSeconds >= TextHintSeconds) ShowHint();
        if (!markerShown && idleSeconds >= MarkerSeconds) markerShown = ResolveTarget() != null;
        if (hintRemaining > 0f)
        {
            hintRemaining -= Time.deltaTime;
            if (hintRemaining <= 0f) hintPanel.SetActive(false);
        }
        UpdateMarker();
    }

    private bool CanShow()
    {
        if (quests == null || quests.CurrentStep == QuestStep.Ending || quests.IsLeavingHome) return false;
        if (quests.CurrentStep == QuestStep.WashDishes && (chores == null || !chores.ChecklistVisible)) return false;
        return GameManager.Instance == null || !GameManager.Instance.InputBlocked;
    }

    private void HandleChoreCompleted(HouseholdChore chore, int pieces)
    {
        ResetGuidance();
        Refresh();
    }

    private void HandleQuestChanged(QuestStep step)
    {
        ResetGuidance();
        Refresh();
    }

    private void ResetGuidance()
    {
        idleSeconds = 0f;
        hintRemaining = 0f;
        textHintShown = false;
        markerShown = false;
        if (hintText != null) hintText.text = string.Empty;
        if (hintPanel != null) hintPanel.SetActive(false);
        if (marker != null) marker.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (quests == null || objectiveText == null || boardText == null) return;
        bool household = quests.CurrentStep == QuestStep.WashDishes;
        int completed = chores != null ? chores.ActiveIndex : 0;
        string status = household
            ? "VIỆC NHÀ  " + Mathf.Min(completed + 1, HouseholdChoreFlow.ChoreCount) + "/" + HouseholdChoreFlow.ChoreCount
            : StageName(quests.CurrentStep);
        string location = household && chores != null ? chores.CurrentLocation : string.Empty;
        string action = household && chores != null ? "Phụ mẹ: " + chores.CurrentTitle : quests.ObjectiveText;
        string chapter = SceneManager.GetActiveScene().name == SceneNames.Act3_OutSide
            ? "CHƯƠNG 02 · SÂN NHÀ" : "CHƯƠNG 02 · NHÀ MÌNH";
        objectiveText.text = "<size=15><color=#F4C66A>" + chapter + "</color></size>\n" +
            "<size=24><b><color=#EDF3F8>" + status + "</color></b></size>\n\n" +
            "<size=16><color=#B5C3CF>NHIỆM VỤ HIỆN TẠI</color></size>\n" +
            "<size=21><b><color=#FFFFFF>›  " + action + "</color></b></size>\n" +
            (string.IsNullOrEmpty(location) ? string.Empty : "<size=15><color=#F4C66A>" + location + "</color></size>\n") +
            "<size=15><color=#A9BBC9>E Tương tác  ·  H Gợi ý  ·  Tab Bảng nhiệm vụ</color></size>" +
            (household ? "\n<size=17><color=#F4C66A>P · Làm lại việc nhà từ đầu</color></size>" : string.Empty);
        boardText.text = BuildBoardText();
    }

    private string BuildBoardText()
    {
        var text = new StringBuilder(900);
        bool household = quests.CurrentStep == QuestStep.WashDishes;
        if (household)
        {
            int active = chores != null ? chores.ActiveIndex : 0;
            text.Append("<size=24><b>VIỆC NHÀ CÙNG MẸ</b></size>\n");
            text.Append("<color=#F4C66A>Mảnh ký ức: ").Append(chores != null ? chores.Pieces : 0)
                .Append('/').Append(HouseholdChoreFlow.TotalPieces).Append("</color>\n\n");
            for (int i = 0; i < HouseholdChoreFlow.ChoreCount; i++)
            {
                string title = HouseholdChoreFlow.Titles[(int)HouseholdChoreFlow.Order[i]];
                if (i < active) text.Append("<color=#9ADD91>✓  ").Append(title).Append("</color>\n");
                else if (i == active) text.Append("<color=#FFE19B><b>›  ").Append(title).Append("</b></color>\n");
                else text.Append("<color=#B9C0C2>○  ").Append(title).Append("</color>\n");
            }
        }
        else
        {
            text.Append("<size=24><b>HÀNH TRÌNH KÝ ỨC</b></size>\n\n");
            AppendStage(text, QuestStep.WashDishes, "Phụ mẹ làm việc nhà");
            AppendStage(text, QuestStep.BuyCandyAndMarbles, "Mua kẹo và bi ve");
            AppendStage(text, QuestStep.PlayMarbles, "Chơi bi ngoài sân");
            AppendStage(text, QuestStep.ReturnToJournal, "Trở lại cuốn nhật ký");
            AppendStage(text, QuestStep.TakeDreamKey, "Nhặt Chìa Khóa Ước Mơ");
            AppendStage(text, QuestStep.UnlockFrontDoor, "Mở cửa bị xích");
        }
        text.Append(household
            ? "\n<size=15><color=#F4C66A>P · Làm lại từ đầu</color></size>"
            : "\n<size=15><color=#9CAEB8>Tự động lưu sau mỗi bước.</color></size>");
        return text.ToString();
    }

    private void AppendStage(StringBuilder text, QuestStep step, string title)
    {
        if (quests.CurrentStep > step) text.Append("<color=#9ADD91>✓  ").Append(title).Append("</color>\n\n");
        else if (quests.CurrentStep == step) text.Append("<color=#FFE19B><b>›  ").Append(title).Append("</b></color>\n\n");
        else text.Append("<color=#B9C0C2>○  ").Append(title).Append("</color>\n\n");
    }

    private static string StageName(QuestStep step)
    {
        switch (step)
        {
            case QuestStep.BuyCandyAndMarbles: return "TIỆM TẠP HÓA";
            case QuestStep.PlayMarbles: return "SÂN CHƠI";
            case QuestStep.ReturnToJournal: return "CUỐN NHẬT KÝ";
            case QuestStep.TakeDreamKey: return "CHÌA KHÓA ƯỚC MƠ";
            case QuestStep.UnlockFrontDoor: return "CÁNH CỬA BỊ XÍCH";
            default: return "KÝ ỨC TUỔI THƠ";
        }
    }

    private void ShowHint()
    {
        if (quests == null || hintText == null) return;
        hintText.text = quests.CurrentStep == QuestStep.WashDishes && chores != null
            ? "Đến " + chores.CurrentLocation + ", tìm " + chores.CurrentTitle + " rồi nhấn E."
            : ResolveTarget() != null
                ? quests.ObjectiveText + " Hãy tìm vật có lời nhắc [E] khi lại gần."
                : "Nhiệm vụ này chưa có điểm tương tác trong cảnh hiện tại.";
        hintRemaining = HintDuration;
        textHintShown = true;
        hintPanel.SetActive(true);
    }

    private void CacheTargets()
    {
        foreach (HouseholdChoreInteractable task in FindObjectsOfType<HouseholdChoreInteractable>(true))
            if (task.enabled && !choreTargets.ContainsKey(task.Chore))
            {
                choreTargets.Add(task.Chore, task.transform);
                CacheCollider(task.transform);
            }
        shopTarget = FindObjectOfType<ShopInteractable>(true)?.transform;
        marbleTarget = FindObjectOfType<MarbleInteractable>(true)?.transform;
        journalTarget = FindObjectOfType<JournalInteractable>(true)?.transform;
        keyTarget = FindObjectOfType<DreamKeyInteractable>(true)?.transform;
        doorTarget = FindObjectOfType<ChainedDoorInteractable>(true)?.transform;
        CacheCollider(shopTarget);
        CacheCollider(marbleTarget);
        CacheCollider(journalTarget);
        CacheCollider(keyTarget);
        CacheCollider(doorTarget);
    }

    private void CacheCollider(Transform target)
    {
        if (target != null && !targetColliders.ContainsKey(target))
            targetColliders.Add(target, target.GetComponent<Collider>());
    }

    private Transform ResolveTarget()
    {
        if (quests == null) return null;
        Transform target;
        if (quests.CurrentStep == QuestStep.WashDishes && chores != null && chores.ActiveIndex < HouseholdChoreFlow.ChoreCount)
            choreTargets.TryGetValue(HouseholdChoreFlow.Order[chores.ActiveIndex], out target);
        else
        {
            switch (quests.CurrentStep)
            {
                case QuestStep.BuyCandyAndMarbles: target = shopTarget; break;
                case QuestStep.PlayMarbles: target = marbleTarget; break;
                case QuestStep.ReturnToJournal: target = journalTarget; break;
                case QuestStep.TakeDreamKey: target = keyTarget; break;
                case QuestStep.UnlockFrontDoor: target = doorTarget; break;
                default: target = null; break;
            }
        }
        return target != null && target.gameObject.activeInHierarchy ? target : null;
    }

    private void UpdateMarker()
    {
        if (marker == null) return;
        Transform target = markerShown ? ResolveTarget() : null;
        bool show = target != null && playerCamera != null;
        marker.gameObject.SetActive(show);
        if (!show) return;

        // Imported FBX pivots can be far from the actual prop; point to its interaction volume instead.
        targetColliders.TryGetValue(target, out Collider interactionCollider);
        Vector3 worldPoint = interactionCollider != null
            ? interactionCollider.bounds.center : target.position + Vector3.up * .65f;
        Vector3 screen = playerCamera.WorldToScreenPoint(worldPoint);
        if (screen.z < 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }
        const float margin = 54f;
        screen.x = Mathf.Clamp(screen.x, margin, Screen.width - margin);
        screen.y = Mathf.Clamp(screen.y, margin, Screen.height - margin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            marker.parent as RectTransform, screen, null, out Vector2 local);
        marker.anchoredPosition = local;
    }

    private void CreateView()
    {
        var root = new GameObject("Scene2QuestCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        rootGroup = root.GetComponent<CanvasGroup>();
        rootGroup.interactable = rootGroup.blocksRaycasts = false;

        var card = Panel("CurrentObjective", root.transform, new Vector2(1, 1),
            new Vector2(-28, -112), new Vector2(455, 255));
        objectiveText = Label("ObjectiveText", card.transform, new Vector2(22, 17), new Vector2(-22, -17), 20);

        board = Panel("MissionBoard", root.transform, new Vector2(0, 1),
            new Vector2(28, -112), new Vector2(400, 535));
        boardText = Label("MissionList", board.transform, new Vector2(22, 15), new Vector2(-22, -15), 20);
        board.SetActive(false);

        hintPanel = Panel("GuidanceHint", root.transform, new Vector2(.5f, 0),
            new Vector2(0, 100), new Vector2(800, 76));
        hintText = Label("HintText", hintPanel.transform, new Vector2(18, 8), new Vector2(-18, -8), 21);
        hintText.alignment = TextAlignmentOptions.Center;
        hintPanel.SetActive(false);

        TMP_FontAsset sharedFont = existingHud != null ? existingHud.ObjectiveFont : null;
        if (sharedFont != null)
        {
            objectiveText.font = sharedFont;
            boardText.font = sharedFont;
            hintText.font = sharedFont;
        }

        var markerObject = new GameObject("ObjectiveMarker", typeof(RectTransform), typeof(Image));
        markerObject.transform.SetParent(root.transform, false);
        marker = (RectTransform)markerObject.transform;
        marker.anchorMin = marker.anchorMax = new Vector2(.5f, .5f);
        marker.sizeDelta = new Vector2(60, 60);
        var markerImage = markerObject.GetComponent<Image>();
        markerImage.color = new Color(.04f, .06f, .08f, .9f);
        markerImage.raycastTarget = false;
        var diamond = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
        diamond.transform.SetParent(marker, false);
        var diamondRect = (RectTransform)diamond.transform;
        diamondRect.anchorMin = diamondRect.anchorMax = new Vector2(.5f, .5f);
        diamondRect.sizeDelta = new Vector2(20, 20);
        diamondRect.localRotation = Quaternion.Euler(0, 0, 45);
        diamond.GetComponent<Image>().color = new Color(.96f, .77f, .32f);
        diamond.GetComponent<Image>().raycastTarget = false;
        markerObject.SetActive(false);
    }

    private static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = new Color(.025f, .045f, .06f, .94f);
        image.raycastTarget = false;
        return go;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 min, Vector2 max, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = min;
        text.rectTransform.offsetMax = max;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }
}
