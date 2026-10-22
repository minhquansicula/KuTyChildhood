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
        if (flow == null) flow = HouseholdChoreFlow.Instance;
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
        if (distance > 2.7f || Mathf.Abs(offset.y) > 2.2f) return false;
        Camera view = Camera.main;
        if (view == null) return false;
        Vector3 towardPot = pot.position + Vector3.up * .38f - view.transform.position;
        if (towardPot.sqrMagnitude <= .01f ||
            Vector3.Dot(view.transform.forward, towardPot.normalized) < .55f) return false;
        // Assist aiming, but never use the pot through a wall or another prop.
        int count = Physics.RaycastNonAlloc(view.transform.position, towardPot.normalized,
            focusHits, towardPot.magnitude + .5f, ~0, QueryTriggerInteraction.Ignore);
        Collider nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = focusHits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<FirstPersonController>() != null ||
                hit.distance >= nearestDistance) continue;
            nearest = hit.collider;
            nearestDistance = hit.distance;
        }
        return nearest != null && nearest.GetComponentInParent<HouseholdChoreInteractable>() == this;
    }

    protected override void OnInteract()
    {
        if (flow == null) flow = HouseholdChoreFlow.Instance;
        if (!isActiveAndEnabled || running || flow == null || flow.UI == null || !flow.IsCurrent(chore)) return;

        running = true;
        GameManager.Instance?.AcquireInput(this);
        try
        {
            feedback = new HouseholdWorkFeedback(chore, effectAnchor != null ? effectAnchor : transform);
            flow.UI.ShowGame(HouseholdChoreFlow.Titles[(int)chore], InstructionFor(chore), false);
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
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (flow?.UI?.Actions.Restart() == true)
            {
                feedback?.Dispose();
                feedback = new HouseholdWorkFeedback(chore, effectAnchor != null ? effectAnchor : transform);
                flow.UI.SetHint("Đã làm lại nhiệm vụ từ đầu.");
            }
            return;
        }
        feedback?.Tick();
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
