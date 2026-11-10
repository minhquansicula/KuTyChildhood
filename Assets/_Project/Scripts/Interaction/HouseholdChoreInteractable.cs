using System;
using UnityEngine;

/// <summary>One visible prop starts a hands-on chore. Reward and checkpoint state stay in HouseholdChoreFlow.</summary>
public sealed class HouseholdChoreInteractable : InteractableBase, IProximityInteractable
{
    [SerializeField] private HouseholdChore chore;
    [SerializeField] private Transform effectAnchor;

    private HouseholdChoreFlow flow;
    private HouseholdWorkFeedback feedback;
    private bool running;
    private Scene2HouseholdSimulator simulator;
    private bool simpleAction;
    private readonly RaycastHit[] focusHits = new RaycastHit[32];

    public HouseholdChore Chore => chore;

    public void Configure(HouseholdChore value, Transform anchor)
    {
        chore = value;
        effectAnchor = anchor;
        isOneTimeUse = false;
        promptText = "[E] " + HouseholdChoreFlow.Titles[(int)chore];
    }

    private void Awake()
    {
        isOneTimeUse = false; // A cancelled mini-game must always be reopenable.
        flow = HouseholdChoreFlow.Instance;
    }

    public override string GetPromptText()
    {
        if (!isActiveAndEnabled || running || !Scene2HouseholdActivityPolicy.IsActive(chore)) return "";
        if (flow == null) flow = HouseholdChoreFlow.Instance;
        var kitchen = flow != null ? flow.GetComponent<Scene2KitchenHandsOn>() : null;
        if (kitchen != null && kitchen.Handles(chore)) return kitchen.PromptFor(chore);
        var sequence = flow != null ? flow.GetComponent<Scene2RadioKitchenSequence>() : null;
        if (sequence != null && sequence.Handles(chore)) return sequence.PromptFor(chore);
        return isActiveAndEnabled && !running && flow != null && flow.IsCurrent(chore)
            ? "[E] " + HouseholdChoreFlow.Titles[(int)chore] +
              (HouseholdChoreFlow.IsOptional(chore) ? " · Tùy chọn (+5 mảnh)" : "") : "";
    }

    // The rice pot sits among solid kitchen props. A narrow near-and-facing focus
    // keeps E available when those props intercept the camera's exact ray.
    public bool TryGetProximityDistance(Vector3 playerPosition, out float distance)
    {
        distance = float.PositiveInfinity;
        if (chore != HouseholdChore.WashRice || string.IsNullOrEmpty(GetPromptText())) return false;
        Transform pot = effectAnchor != null ? effectAnchor : transform;
        Vector3 offset = pot.position - playerPosition;
        distance = new Vector2(offset.x, offset.z).magnitude;
        // Keep the local, near-and-facing focus and fail closed on ambiguous visibility.
        if (distance > 1.85f || Mathf.Abs(offset.y) > 1.8f) return false;
        Camera view = Camera.main;
        if (view == null) return false;
        Vector3 towardPot = pot.position + Vector3.up * .12f - view.transform.position;
        if (towardPot.sqrMagnitude <= .01f ||
            Vector3.Dot(view.transform.forward, towardPot.normalized) < .68f) return false;

        // Kiểm tra tầm nhìn: không cho phép tương tác xuyên tường
        int count = Physics.RaycastNonAlloc(view.transform.position, towardPot.normalized,
            focusHits, towardPot.magnitude + .35f, ~0, QueryTriggerInteraction.Ignore);
        if (count == 0 || count == focusHits.Length) return false;
        float nearestPot = float.PositiveInfinity;
        float nearestObstacle = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = focusHits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<FirstPersonController>() != null) continue;
            if (hit.collider.transform.IsChildOf(pot) ||
                hit.collider.GetComponentInParent<HouseholdChoreInteractable>() == this)
                nearestPot = Mathf.Min(nearestPot, hit.distance);
            else nearestObstacle = Mathf.Min(nearestObstacle, hit.distance);
        }
        return nearestPot < nearestObstacle;
    }

    protected override void OnInteract()
    {
        if (flow == null) flow = HouseholdChoreFlow.Instance;
        if (!isActiveAndEnabled || running || flow == null || flow.UI == null || !flow.IsCurrent(chore) || GameManager.Instance?.InputBlocked == true) return;

        if (Scene2HouseholdActivityPolicy.IsSimple(chore))
        {
            running = simpleAction = true;
            flow.GetComponent<Scene2KitchenHandsOn>()?.SilenceMother();
            flow.Voice.Stop();
            GameManager.Instance?.AcquireInput(this);
            try { flow.UI.ShowSimpleAction(chore, Finish, EndInteraction); }
            catch (Exception error) { Debug.LogException(error, this); EndInteraction(); }
            return;
        }

        simulator = flow.GetComponent<Scene2HouseholdSimulator>();
        if (simulator != null && simulator.Handles(chore))
        {
            running = true;
            if (!simulator.Begin(chore, Finish, EndInteraction)) running = false;
            return;
        }

        var kitchen = flow.GetComponent<Scene2KitchenHandsOn>();
        if (kitchen != null && kitchen.Handles(chore))
        {
            kitchen.Begin(chore);
            return;
        }

        var sequence = flow.GetComponent<Scene2RadioKitchenSequence>();
        if (sequence != null && sequence.Handles(chore))
        {
            sequence.InteractWith(chore);
            return;
        }

        running = true;
        kitchen?.SilenceMother();
        flow.Voice.Stop();
        GameManager.Instance?.AcquireInput(this);
        try
        {
            feedback = new HouseholdWorkFeedback(chore, effectAnchor != null ? effectAnchor : transform);
            flow.UI.ShowGame(HouseholdChoreFlow.Titles[(int)chore], InstructionFor(chore), false, EndInteraction, RestartInteraction);
            flow.UI.Actions.Begin(chore, Finish, UpdateProgress, ShowHint, Pulse);
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
            EndInteraction();
        }
    }

    private void Update()
    {
        if (!running) return;
        if ((GameManager.Instance != null && GameManager.Instance.IsPaused) ||
            PauseUI.LastResumeFrame == Time.frameCount) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { EndInteraction(); return; }
        if (simpleAction)
        {
            if (!flow || !flow.IsCurrent(chore)) { EndInteraction(); return; }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Alpha1)) flow.UI?.ConfirmSimpleAction();
            return;
        }
        if (simulator != null && simulator.IsWorking) return;
        if (Input.GetKeyDown(KeyCode.P))
        {
            RestartInteraction();
            return;
        }
        feedback?.Tick();
    }

    private void RestartInteraction()
    {
        if (!running || GameManager.Instance?.IsPaused == true || flow?.UI?.Actions.Restart() != true) return;
        feedback?.Dispose();
        feedback = new HouseholdWorkFeedback(chore, effectAnchor != null ? effectAnchor : transform);
        flow.UI.SetHint("Đã làm lại nhiệm vụ từ đầu.");
    }

    private void UpdateProgress(float value, string label)
    {
        if (!running || flow == null) return;
        feedback?.SetIntensity(value);
        flow.UI.SetProgress(value, label);
    }

    private void ShowHint(string message)
    {
        if (running && flow != null) flow.UI.SetHint(message);
    }

    private void Pulse()
    {
        if (!running) return;
        feedback?.Pulse();
        SfxId sound = chore == HouseholdChore.WashDishes ? SfxId.DishClean :
            chore == HouseholdChore.FetchWater || chore == HouseholdChore.WashRice
                ? SfxId.WaterSplash : SfxId.TaskProgress;
        AudioManager.Instance?.PlaySFX(sound);
    }

    private void Finish()
    {
        if (!running) return;
        EndInteraction();
        if (flow == null || !flow.Complete(chore) || chore != HouseholdChore.ReadLetter) return;
        var quests = QuestManager.Instance;
        if (DialogueUI.Instance != null)
        {
            Action showLetter = () =>
            {
                AudioManager.Instance?.BeginLetterMusic();
                flow.Voice.Play("act2_mom_letter_001");
                DialogueUI.Instance.ShowDialogue("Thư của mẹ", HouseholdChoreFlow.Memories[(int)chore], 0f,
                    () =>
                    {
                        flow.Voice.Stop();
                        AudioManager.Instance?.EndLetterMusic();
                        if (quests != null) quests.ContinueOutside();
                    });
            };
            // The final memory has its own Ty narration. Finish it before opening the letter.
            if (DialogueUI.Instance.IsShowing) DialogueUI.Instance.AddOnClosed(showLetter);
            else showLetter();
        }
        else quests?.ContinueOutside();
    }

    private void EndInteraction()
    {
        if (!running) return;
        running = false;
        simpleAction = false;
        if (simulator != null && simulator.IsWorking) simulator.End();
        flow?.UI?.HideGame();
        feedback?.Dispose();
        feedback = null;
        GameManager.Instance?.ReleaseInput(this);
    }

    private void OnDisable() => EndInteraction();

    private static string InstructionFor(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.ArrangeKeepsakes: return "Kéo áo, khăn và kỷ vật vào từng ngăn; sau đó đóng nắp rương.";
            case HouseholdChore.FoldBlanket: return "Kéo từng mép chăn vào đúng vị trí để gấp gọn.";
            case HouseholdChore.SortTools: return "Kéo dụng cụ lên móc; giữ R hoặc lăn chuột khi kéo để xoay cuốc.";
            case HouseholdChore.CarryWood: return "Xếp ba thanh củi thành bó rồi kéo dây buộc.";
            case HouseholdChore.SetTable: return "Đặt bát, đũa và nồi cơm lên đúng chỗ trên mâm.";
            case HouseholdChore.PutAwayDishes: return "Đưa chén, đĩa và đũa vào đúng ngăn tủ.";
            case HouseholdChore.TuneRadio: return "Giữ chuột và xoay núm đến khi tiếng rè dịu đi.";
            case HouseholdChore.GrindCorn: return "Rê chuột theo elip trên mặt cối; khi bột mịn, kéo cối xuống để thu bột.";
            case HouseholdChore.FetchWater: return "Đưa gáo vào lu, múc đầy rồi chuyển sang thau.";
            case HouseholdChore.MeasureRice: return "Kéo ba gáo gạo từ lu sang rá đong.";
            case HouseholdChore.WashRice: return "Khuấy theo elip mặt nước trong nồi hai vòng rồi bấm chắt nước.";
            case HouseholdChore.LightFire: return "Đặt củi vào bếp, sau đó thổi khi than sáng vàng.";
            case HouseholdChore.CookRice: return "Đẩy củi vào hoặc rút ra để giữ nồi cơm sôi đều.";
            case HouseholdChore.WashDishes: return "Dùng miếng rửa chén chà sạch, tráng trong thau nước rồi đặt lên giá.";
            case HouseholdChore.ReadLetter: return "Tháo dây, mở nếp giấy và lật trang để đọc thư.";
            default: return "Tương tác với vật dụng để hoàn thành việc này.";
        }
    }
}
