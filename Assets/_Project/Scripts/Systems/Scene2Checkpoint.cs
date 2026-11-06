using UnityEngine;

/// <summary>Small, versioned checkpoint for the long household/outdoor memory chapter.</summary>
public static class Scene2Checkpoint
{
    private const string Prefix = "KuTy.Scene2Checkpoint.";
    private const int Version = 2;
    private const int Home = 1;
    private const int Outside = 2;
    private const int Finished = 3;

    private static int SavedVersion => PlayerPrefs.GetInt(Prefix + "Version", 0);
    private static bool Valid => SavedVersion == 1 || SavedVersion == Version;
    private static int Stage => Valid ? PlayerPrefs.GetInt(Prefix + "Stage", 0) : 0;
    public static int HomeChores
    {
        get
        {
            if (Stage != Home) return 0;
            int saved = PlayerPrefs.GetInt(Prefix + "Chores", 0);
            // Version 1 counted Xếp dụng cụ and Xay ngô in the required order.
            if (SavedVersion == 1) saved -= Mathf.Min(2, Mathf.Max(0, saved - 2));
            return Mathf.Clamp(saved, 0, HouseholdChoreFlow.ChoreCount - 1);
        }
    }
    public static int HomeOptional
    {
        get
        {
            if (Stage != Home) return 0;
            if (SavedVersion != 1) return PlayerPrefs.GetInt(Prefix + "Optional", 0) & 3;
            int saved = PlayerPrefs.GetInt(Prefix + "Chores", 0);
            return (saved >= 3 ? 1 : 0) | (saved >= 4 ? 2 : 0);
        }
    }
    public static bool HasOutsideProgress => Stage == Outside &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) >= (int)QuestStep.BuyCandyAndMarbles &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) <= (int)QuestStep.UnlockFrontDoor;
    public static bool HasCheckpoint => HomeChores > 0 || HomeOptional != 0 || HasOutsideProgress;
    public static string ResumeScene => HasOutsideProgress ? SceneNames.Act3_OutSide : SceneNames.Act2_Home;
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
        PlayerPrefs.SetInt(Prefix + "Stage", Home);
        PlayerPrefs.SetInt(Prefix + "Chores", completedChores);
        PlayerPrefs.SetInt(Prefix + "Optional", optionalBits & 3);
        Commit();
    }

    public static void SaveOutside(QuestManager quests)
    {
        if (quests == null || quests.CurrentStep < QuestStep.BuyCandyAndMarbles ||
            quests.CurrentStep > QuestStep.UnlockFrontDoor) return;
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
        PlayerPrefs.SetInt(Prefix + "Stage", Finished);
        Commit();
    }

    public static void Clear()
    {
        foreach (string name in new[] { "Version", "Stage", "Chores", "Optional", "Step", "Money", "Candy", "Marbles", "Key", "Memories" })
            PlayerPrefs.DeleteKey(Prefix + name);
        PlayerPrefs.Save();
    }

    private static void Commit()
    {
        PlayerPrefs.SetInt(Prefix + "Version", Version);
        PlayerPrefs.Save();
    }
}
