using System;
using UnityEngine;

/// <summary>Small, model-independent feedback layer. Flow remains the sole reward owner.</summary>
public sealed class Scene2ChoreGuidance : MonoBehaviour
{
    private HouseholdChoreFlow flow;
    private string pending;
    private float remaining, expiry;
    private int progress;
    public event Action<string> OnMotherReaction;

    private void Start()
    {
        flow = GetComponent<HouseholdChoreFlow>();
        progress = flow.ActiveIndex;
        flow.OnChoreCompleted += Completed;
    }

    public static string CompletionLine(HouseholdChore chore)
    {
        switch (chore)
        {
            case HouseholdChore.ArrangeKeepsakes: return "Con xếp gọn quá. Mai tìm đồ cũng dễ hơn rồi.";
            case HouseholdChore.CarryWood: return "Có con phụ, mẹ đỡ chạy ra chạy vào.";
            case HouseholdChore.SetTable: return "Con nhớ cả chỗ ngồi của mẹ nữa.";
            case HouseholdChore.PutAwayDishes: return "Xong rồi, nghỉ tay đi con. Lát đi chơi nhớ về ăn cơm nhé.";
            default: return null; // Kitchen already owns its own voiced reactions.
        }
    }

    private void Completed(HouseholdChore chore, int pieces)
    {
        pending = CompletionLine(chore);
        remaining = 8f; // Let Ty's memory notification finish first.
        expiry = 18f;
        progress = flow.ActiveIndex;
    }

    private void Update()
    {
        if (!flow || pending == null || GameManager.Instance?.IsPaused == true) return;
        if (flow.ActiveIndex != progress) { pending = null; return; }
        remaining -= Time.deltaTime;
        expiry -= Time.deltaTime;
        if (expiry <= 0f) { pending = null; return; }
        var kitchen = GetComponent<Scene2KitchenHandsOn>();
        var radio = GetComponent<Scene2RadioKitchenSequence>();
        if (remaining > 0f || GameManager.Instance?.InputBlocked == true ||
            DialogueUI.Instance?.IsShowing == true || flow.Voice?.Voice?.IsPlaying == true ||
            (kitchen && kitchen.MotherSpeaking) || (radio && radio.MotherSpeaking)) return;
        string line = pending;
        pending = null;
        flow.UI?.Notify("Mẹ: " + line, 5f);
        OnMotherReaction?.Invoke(line);
    }

    private void OnDisable() => pending = null;
    private void OnDestroy()
    {
        if (flow) flow.OnChoreCompleted -= Completed;
    }
}

/// <summary>Signed accumulation: shaking back and forth must not count as full circles.</summary>
public static class Scene2CircularGesture
{
    public static float Accumulate(float turns, float previousAngle, float angle)
    {
        float delta = Mathf.DeltaAngle(previousAngle, angle);
        return Mathf.Abs(delta) <= 55f ? turns + delta / 360f : turns;
    }
}
