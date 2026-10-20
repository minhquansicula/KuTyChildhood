using UnityEngine;

/// <summary>Small, versioned checkpoint for the long household/outdoor memory chapter.</summary>
public static class Scene2Checkpoint
{
    private const string Prefix = "KuTy.Scene2Checkpoint.";
    private const int Version = 1;
    private const int Home = 1;
    private const int Outside = 2;
    private const int Finished = 3;

    private static bool Valid => PlayerPrefs.GetInt(Prefix + "Version", 0) == Version;
    private static int Stage => Valid ? PlayerPrefs.GetInt(Prefix + "Stage", 0) : 0;
    public static int HomeChores => Stage == Home
        ? Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Chores", 0), 0, HouseholdChoreFlow.ChoreCount - 1) : 0;
    public static bool HasOutsideProgress => Stage == Outside &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) >= (int)QuestStep.BuyCandyAndMarbles &&
        PlayerPrefs.GetInt(Prefix + "Step", -1) <= (int)QuestStep.UnlockFrontDoor;
    public static bool HasCheckpoint => HomeChores > 0 || HasOutsideProgress;
    public static string ResumeScene => HasOutsideProgress ? SceneNames.Act3_OutSide : SceneNames.Act2_Home;
    public static QuestStep OutsideStep => HasOutsideProgress
        ? (QuestStep)PlayerPrefs.GetInt(Prefix + "Step") : QuestStep.BuyCandyAndMarbles;
    public static int OutsideMoney => HasOutsideProgress ? Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Money")) : 2000;
    public static bool HasCandy => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Candy", 0) != 0;
    public static bool HasMarbles => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Marbles", 0) != 0;
    public static bool HasDreamKey => HasOutsideProgress && PlayerPrefs.GetInt(Prefix + "Key", 0) != 0;
    public static bool HasMemory(MemoryType type) => HasOutsideProgress &&
        (PlayerPrefs.GetInt(Prefix + "Memories", 0) & (1 << (int)type)) != 0;

    public static void SaveHome(int completedChores)
    {
        if (completedChores <= 0 || completedChores >= HouseholdChoreFlow.ChoreCount) return;
        PlayerPrefs.SetInt(Prefix + "Stage", Home);
        PlayerPrefs.SetInt(Prefix + "Chores", completedChores);
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
        foreach (string name in new[] { "Version", "Stage", "Chores", "Step", "Money", "Candy", "Marbles", "Key", "Memories" })
            PlayerPrefs.DeleteKey(Prefix + name);
        PlayerPrefs.Save();
    }

    private static void Commit()
    {
        PlayerPrefs.SetInt(Prefix + "Version", Version);
        PlayerPrefs.Save();
    }
}
