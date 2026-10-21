using UnityEngine;

/// <summary>One narration source for home chores, memory and the mother's letter.</summary>
[RequireComponent(typeof(DialogueVoicePlayer))]
public sealed class HouseholdVoicePlayer : MonoBehaviour
{
    private DialogueSequence recordings;
    private DialogueVoicePlayer voice;
    private GameManager pauseOwner;
    public string CurrentId { get; private set; }
    public DialogueVoicePlayer Voice => voice;
    private void Awake()
    {
        recordings = Resources.Load<DialogueSequence>("HomeGameplayVoices");
        voice = GetComponent<DialogueVoicePlayer>();
    }
    private void Start()
    {
        pauseOwner = GameManager.Instance;
        if (pauseOwner) pauseOwner.OnPauseChanged += SetPaused;
    }
    private void OnDestroy()
    {
        if (pauseOwner) pauseOwner.OnPauseChanged -= SetPaused;
    }
    public float Play(string id)
    {
        Stop();
        if (!recordings || string.IsNullOrEmpty(id)) return 0f;
        foreach (var line in recordings.Lines)
        {
            if (line.id != id || !line.voiceClip) continue;
            CurrentId = id;
            voice.Play(line.voiceClip);
            if (pauseOwner && pauseOwner.IsPaused) voice.Pause();
            return line.ResolveDuration();
        }
        return 0f;
    }
    public void Stop()
    {
        CurrentId = null;
        voice?.Stop();
    }
    private void SetPaused(bool paused)
    {
        if (paused) voice.Pause(); else voice.Resume();
    }
    public static string ChoreId(HouseholdChore chore)
    {
        switch (chore)
        {
            case HouseholdChore.ArrangeKeepsakes: return "act2_ty_keepsakes_001";
            case HouseholdChore.TuneRadio: return "act2_ty_radio_001";
            case HouseholdChore.SortTools: return "act2_mom_tools_001";
            case HouseholdChore.GrindCorn: return "act2_ty_grind_corn_001";
            case HouseholdChore.CarryWood: return "act2_ty_firewood_001";
            case HouseholdChore.FetchWater: return "act2_ty_fetch_water_001";
            case HouseholdChore.MeasureRice: return "act2_ty_measure_rice_001";
            case HouseholdChore.WashRice: return "act2_ty_wash_rice_001";
            case HouseholdChore.LightFire: return "act2_ty_light_fire_001";
            case HouseholdChore.CookRice: return "act2_ty_cook_rice_001";
            case HouseholdChore.SetTable: return "act2_ty_set_table_001";
            case HouseholdChore.WashDishes: return "act2_ty_wash_dishes_001";
            case HouseholdChore.PutAwayDishes: return "act2_ty_put_away_dishes_001";
            default: return null;
        }
    }
}
