using UnityEngine;

/// <summary>One visible prop, one ordered household mini-game. All reward state lives in HouseholdChoreFlow.</summary>
public sealed class HouseholdChoreInteractable : InteractableBase
{
    [SerializeField] private HouseholdChore chore;
    [SerializeField] private Transform effectAnchor;

    private HouseholdChoreFlow flow;
    private HouseholdWorkFeedback feedback;
    private bool running;
    private int successes;
    private float amount;
    private float previousAngle;
    private bool hasPreviousAngle;
    private bool secondPhase;

    public void Configure(HouseholdChore value, Transform anchor)
    {
        chore = value;
        effectAnchor = anchor;
        isOneTimeUse = false;
        promptText = "[E] " + HouseholdChoreFlow.Titles[(int)chore];
    }

    public HouseholdChore Chore => chore;

    private void Awake() => flow = HouseholdChoreFlow.Instance;

    public override string GetPromptText()
    {
        if (flow == null) flow = HouseholdChoreFlow.Instance;
        return isActiveAndEnabled && !running && flow != null && flow.IsCurrent(chore)
            ? "[E] " + HouseholdChoreFlow.Titles[(int)chore]
            : "";
    }

    protected override void OnInteract()
    {
        if (flow == null) flow = HouseholdChoreFlow.Instance;
        if (!isActiveAndEnabled || running || flow == null || !flow.IsCurrent(chore)) return;
        running = true;
        successes = 0;
        amount = 0;
        secondPhase = false;
        hasPreviousAngle = false;
        GameManager.Instance?.AcquireInput(this);
        feedback = new HouseholdWorkFeedback(chore, effectAnchor != null ? effectAnchor : transform);
        ShowInitialUI();
    }

    private void ShowInitialUI()
    {
        var ui = flow.UI;
        switch (chore)
        {
            case HouseholdChore.FoldBlanket:
                ShowOrder("Mép trái", "Mép phải", "Gấp đôi"); break;
            case HouseholdChore.ArrangeKeepsakes:
                ShowOrder("Áo", "Khăn", "Đóng rương"); break;
            case HouseholdChore.SortTools:
                ShowOrder("Cào", "Cuốc", "Xẻng"); break;
            case HouseholdChore.TuneRadio:
                ui.ShowGame("Chỉnh radio", "Space đúng vùng xanh 3 lần để bắt sóng", true); break;
            case HouseholdChore.GrindCorn:
                ui.ShowGame("Xay ngô", "Xoay chuột 3 vòng; chuột phải thu bột", false); break;
            case HouseholdChore.CarryWood:
                ui.ShowGame("Gom củi", "Giữ F 3 giây để gom bó củi cho mẹ", false); break;
            case HouseholdChore.ReadLetter:
                ui.ShowGame("Lá thư của mẹ", "Giữ F 3 giây để mở lá thư", false); break;
            case HouseholdChore.FetchWater:
                ui.ShowGame("Múc nước", "Nhấn Space khi kim đi qua vùng xanh — 3 lần", true);
                break;
            case HouseholdChore.MeasureRice:
                ui.ShowGame("Đong gạo", "Giữ chuột trái, thả khi vạch ở vùng giữa — 3 gáo", false);
                ui.ShowMeasureZone();
                break;
            case HouseholdChore.WashRice:
                ui.ShowGame("Vo gạo", "Xoay chuột 3 vòng; sau đó chuột phải để chắt nước", false);
                break;
            case HouseholdChore.LightFire:
                ui.ShowGame("Nhóm lửa", "Nhấn Space 15 lần để thổi bếp", false);
                break;
            case HouseholdChore.CookRice:
                ui.ShowGame("Nấu cơm", "Giữ F trong 4 giây để chăm nồi", false);
                break;
            case HouseholdChore.SetTable:
                ui.ShowGame("Dọn mâm cơm", "Bấm theo thứ tự: bát → đũa → nồi cơm", false);
                ui.ShowChoices(new[] { "Bát", "Đũa", "Nồi cơm" }, SelectChoice);
                ui.SetExpectedChoice(0);
                break;
            case HouseholdChore.WashDishes:
                ui.ShowGame("Rửa chén", "Giữ chuột trái 3 giây cho mỗi chén — 3 chén", false);
                break;
            case HouseholdChore.PutAwayDishes:
                ui.ShowGame("Cất chén", "Bấm theo thứ tự: chén → đĩa → đũa", false);
                ui.ShowChoices(new[] { "Chén", "Đĩa", "Đũa" }, SelectChoice);
                ui.SetExpectedChoice(0);
                break;
        }
        ui.SetProgress(0, Counter());
    }

    private void ShowOrder(params string[] labels)
    {
        flow.UI.ShowGame(HouseholdChoreFlow.Titles[(int)chore], "Bấm lần lượt: " + string.Join(" → ", labels), false);
        flow.UI.ShowChoices(labels, SelectChoice);
        flow.UI.SetExpectedChoice(0);
    }

    private void Update()
    {
        if (!running) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
        feedback?.Tick();
        switch (chore)
        {
            case HouseholdChore.TuneRadio:
                Timing(3, 2.6f, "Sóng rõ", true); break;
            case HouseholdChore.GrindCorn:
                WashRice(); break;
            case HouseholdChore.CarryWood:
            case HouseholdChore.ReadLetter:
                if (Input.GetKey(KeyCode.F)) amount += Time.deltaTime;
                feedback?.SetIntensity(amount / 3f);
                flow.UI.SetProgress(amount / 3f, amount.ToString("0.0") + "/3 giây");
                if (amount >= 3f) Finish();
                break;
            case HouseholdChore.FetchWater:
                Timing(3, 2.9f, "Lần múc", false);
                break;
            case HouseholdChore.MeasureRice:
                Measure();
                break;
            case HouseholdChore.WashRice:
                WashRice();
                break;
            case HouseholdChore.LightFire:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    successes++;
                    feedback?.Pulse();
                    flow.UI.SetProgress(successes / 15f, successes + "/15 nhịp thổi");
                    if (successes >= 15) Finish();
                }
                break;
            case HouseholdChore.CookRice:
                if (!secondPhase)
                {
                    if (Input.GetKey(KeyCode.F)) amount = Mathf.Min(4f, amount + Time.deltaTime);
                    feedback?.SetIntensity(amount / 4f);
                    flow.UI.SetProgress(amount / 4f, amount.ToString("0.0") + "/4 giây");
                    if (amount >= 4f)
                    {
                        secondPhase = true;
                        amount = 0;
                        flow.UI.ShowGame("Canh nồi cơm", "Nhấn Space khi kim nhiệt ở vùng xanh", true);
                    }
                }
                else Timing(1, 2.4f, "Lần canh lửa", true);
                break;
            case HouseholdChore.WashDishes:
                WashDishes();
                break;
        }
    }

    private void Timing(int target, float speed, string label, bool cooking)
    {
        float needle = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * .5f;
        flow.UI.SetMeter(needle);
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (needle >= .40f && needle <= .60f)
        {
            successes++;
            feedback?.Pulse();
            if (!cooking) AudioManager.Instance?.PlaySFX("water_splash");
            flow.UI.SetProgress(successes / (float)target, label + " " + successes + "/" + target);
            if (successes >= target) Finish();
        }
        else flow.UI.SetHint("Chưa đúng vùng xanh — đợi kim trở lại");
    }

    private void Measure()
    {
        if (Input.GetMouseButton(0)) amount = Mathf.Min(1f, amount + Time.deltaTime * .62f);
        feedback?.SetIntensity(amount);
        if (Input.GetMouseButtonUp(0))
        {
            if (amount >= .55f && amount <= .75f)
            {
                successes++;
                feedback?.Pulse();
                if (successes >= 3) { Finish(); return; }
                flow.UI.SetHint("Gáo vừa đủ — đong gáo tiếp theo");
            }
            else flow.UI.SetHint("Chưa đúng lượng — đong lại gáo này");
            amount = 0;
        }
        flow.UI.SetProgress(amount, successes + "/3 gáo — thả trong vùng xanh");
    }

    private void WashRice()
    {
        if (secondPhase)
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (chore == HouseholdChore.WashRice) AudioManager.Instance?.PlaySFX("water_splash");
                Finish();
            }
            return;
        }
        var center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        var delta = (Vector2)Input.mousePosition - center;
        if (delta.sqrMagnitude < 1600f) { hasPreviousAngle = false; return; }
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        if (hasPreviousAngle)
        {
            float step = Mathf.Abs(Mathf.DeltaAngle(previousAngle, angle));
            if (step < 45f) amount += step;
        }
        previousAngle = angle;
        hasPreviousAngle = true;
        feedback?.SetIntensity(Mathf.Clamp01(amount / 1080f));
        flow.UI.SetProgress(amount / 1080f, Mathf.FloorToInt(Mathf.Min(3f, amount / 360f)) + "/3 vòng");
        if (amount >= 1080f)
        {
            secondPhase = true;
            feedback?.Pulse();
            flow.UI.SetHint(chore == HouseholdChore.GrindCorn ? "Bột đã mịn — chuột phải để thu bột" : "Gạo đã sạch — nhấn chuột phải để chắt nước");
        }
    }

    private void WashDishes()
    {
        if (Input.GetMouseButton(0)) amount += Time.deltaTime;
        feedback?.SetIntensity(amount / 3f);
        if (amount >= 3f)
        {
            amount = 0;
            successes++;
            feedback?.Pulse();
            AudioManager.Instance?.PlaySFX("dish_clean");
            if (successes >= 3) { Finish(); return; }
        }
        flow.UI.SetProgress((successes + amount / 3f) / 3f,
            "Chén " + (successes + 1) + "/3 — " + amount.ToString("0.0") + "/3 giây");
    }

    private void SelectChoice(int selection)
    {
        if (!running || (chore != HouseholdChore.SetTable && chore != HouseholdChore.PutAwayDishes && chore != HouseholdChore.FoldBlanket && chore != HouseholdChore.ArrangeKeepsakes && chore != HouseholdChore.SortTools)) return;
        if (selection != successes)
        {
            flow.UI.SetHint("Chưa đúng thứ tự — xem ô đang sáng");
            return;
        }
        successes++;
        feedback?.Pulse();
        AudioManager.Instance?.PlaySFX("button_click");
        flow.UI.SetProgress(successes / 3f, successes + "/3 món");
        flow.UI.SetExpectedChoice(successes);
        if (successes >= 3) Finish();
    }

    private string Counter() => chore == HouseholdChore.LightFire ? "0/15 nhịp thổi" : "0/3";

    private void Finish()
    {
        EndInteraction();
        if (flow != null && flow.Complete(chore) && chore == HouseholdChore.ReadLetter)
            DialogueUI.Instance?.ShowDialogue("Thư của mẹ", HouseholdChoreFlow.Memories[(int)chore], 10f);
    }

    private void Cancel() => EndInteraction();

    private void EndInteraction()
    {
        if (!running) return;
        running = false;
        feedback?.Dispose();
        feedback = null;
        flow?.UI.HideGame();
        GameManager.Instance?.ReleaseInput(this);
    }

    private void OnDisable() => EndInteraction();
}
