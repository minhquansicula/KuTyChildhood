using UnityEngine;

/// <summary>Small, versioned checkpoint for the long household/outdoor memory chapter.</summary>
public static class Scene2Checkpoint
{
    private const string Prefix = "KuTy.Scene2Checkpoint.";
    private const int Version = 3;
    private const int Home = 1;
    private const int Outside = 2;
    private const int Finished = 3;

    private static int SavedVersion => PlayerPrefs.GetInt(Prefix + "Version", 0);
    private static bool Valid => SavedVersion >= 1 && SavedVersion <= Version;
    private static int Stage => Valid ? PlayerPrefs.GetInt(Prefix + "Stage", 0) : 0;
    public static int HomeChores
    {
        get
        {
            // Scene 3 is no longer on the shipped route. Legacy outdoor saves
            // resume at the indoor letter instead of loading a removed chapter.
            if (Stage == Outside) return HouseholdChoreFlow.ChoreCount - 1;
            if (Stage != Home) return 0;
            int saved = PlayerPrefs.GetInt(Prefix + "Chores", 0);
            saved = MigrateHomeCount(SavedVersion, saved);
            return Mathf.Clamp(saved, 0, HouseholdChoreFlow.ChoreCount - 1);
        }
    }
    public static int HomeOptional => 0;
    public static int MigrateHomeCount(int version, int count)
    {
        if (version == 1) count -= Mathf.Min(2, Mathf.Max(0, count - 2));
        // V2 order: six removed preparations, four retained physical tasks,
        // removed PutAwayDishes, then ReadLetter. Do not count removed work as a
        // completed physical task or turn old index 4 into a completed washing task.
        if (version == 1 || version == 2)
            return Mathf.Clamp(count - 6, 0, 4) + (count >= 12 ? 1 : 0);
        return version == Version ? Mathf.Clamp(count, 0, HouseholdChoreFlow.ChoreCount) : 0;
    }
    public static bool HasOutsideProgress => Stage == Outside &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) >= (int)QuestStep.BuyCandyAndMarbles &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) <= (int)QuestStep.UnlockFrontDoor;
    public static bool HasCheckpoint => HomeChores > 0 || HomeOptional != 0 || HasOutsideProgress ||
        Stage == Home && !string.IsNullOrEmpty(PlayerPrefs.GetString(Prefix + "Work", ""));
    public static string ResumeScene => SceneNames.Act2_Home;
    public static QuestStep OutsideStep => HasOutsideProgress
        ? (QuestStep)PlayerPrefs.GetInt(Prefix + "Step") : QuestStep.BuyCandyAndMarbles;
    public static int OutsideMoney => HasOutsideProgress ? Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Money")) : 2000;
    public static bool HasCandy => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Candy", 0) != 0;
    public static bool HasMarbles => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Marbles", 0) != 0;
    public static bool HasDreamKey => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Key", 0) != 0;
    public static bool HasMemory(MemoryType type) => HasOutsideProgress &&
        (PlayerPrefs.GetInt(Prefix + "Memories", 0) & (1 << (int)type)) != 0;

    public static void SaveHome(int completedChores, int optionalBits = 0)
    {
        if (completedChores < 0 || completedChores >= HouseholdChoreFlow.ChoreCount) return;
        var order = HouseholdChoreFlow.Order;
        bool compatibleWork = Scene2HouseholdActivityPolicy.IsActive((HouseholdChore)PlayerPrefs.GetInt(Prefix + "WorkChore", -1)) &&
            PlayerPrefs.GetInt(Prefix + "WorkChore", -1) == (int)order[completedChores];
        if (Stage != Home || HomeChores != completedChores || !compatibleWork) ClearWork();
        PlayerPrefs.SetInt(Prefix + "Stage", Home);
        PlayerPrefs.SetInt(Prefix + "Chores", completedChores);
        PlayerPrefs.SetInt(Prefix + "Optional", 0);
        Commit();
    }

    public static void SaveOutside(QuestManager quests)
    {
        if (quests == null || quests.CurrentStep < QuestStep.BuyCandyAndMarbles ||
            quests.CurrentStep > QuestStep.UnlockFrontDoor) return;
        ClearWork();
        int memoryBits = 0;
        var memories = MemoryCollectionManager.Instance;
        for (int i = 0; i < MemoryCollectionManager.TOTAL_MEMORIES; i++)
            if (memories != null && memories.HasCollected((MemoryType)i)) memoryBits |= 1 << i;
        var inventory = InventoryManager.Instance;
        PlayerPrefs.SetInt(Prefix + "Stage", Outside);
        PlayerPrefs.SetInt(Prefix + "Step", (int)quests.CurrentStep);
        PlayerPrefs.SetInt(Prefix + "Money", CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentMoney : 0);
        PlayerPrefs.SetInt(Prefix + "Candy", inventory != null && inventory.HasItem(quests.Candy) ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Marbles", inventory != null && inventory.HasItem(quests.MarbleJar) ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Key", quests.HasDreamKey ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Memories", memoryBits);
        Commit();
    }

    public static void MarkFinished()
    {
        ClearWork();
        PlayerPrefs.SetInt(Prefix + "Stage", Finished);
        Commit();
    }

    public static void Clear()
    {
        ClearWork();
        foreach (string name in new[] { "Version", "Stage", "Chores", "Optional", "Step", "Money", "Candy", "Marbles", "Key", "Memories" })
            PlayerPrefs.DeleteKey(Prefix + name);
        PlayerPrefs.Save();
    }

    public static string LoadWork(HouseholdChore chore) => Scene2HouseholdActivityPolicy.IsActive(chore) && Stage == Home &&
        PlayerPrefs.GetInt(Prefix + "WorkChore", -1) == (int)chore
            ? PlayerPrefs.GetString(Prefix + "Work", "") : "";

    public static void SaveWork(HouseholdChore chore, string json)
    {
        var flow = HouseholdChoreFlow.Instance;
        if (!flow || !Scene2HouseholdActivityPolicy.IsActive(chore) || flow.CurrentChore != chore || string.IsNullOrEmpty(json)) return;
        PlayerPrefs.SetInt(Prefix + "Stage", Home);
        PlayerPrefs.SetInt(Prefix + "Chores", flow.ActiveIndex);
        PlayerPrefs.SetInt(Prefix + "Optional", 0);
        PlayerPrefs.SetInt(Prefix + "WorkChore", (int)chore);
        PlayerPrefs.SetString(Prefix + "Work", json);
        Commit();
    }

    private static void ClearWork()
    {
        PlayerPrefs.DeleteKey(Prefix + "WorkChore");
        PlayerPrefs.DeleteKey(Prefix + "Work");
    }

    private static void Commit()
    {
        PlayerPrefs.SetInt(Prefix + "Version", Version);
        PlayerPrefs.Save();
    }
}
