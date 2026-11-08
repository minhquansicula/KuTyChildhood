using UnityEngine;

/// <summary>
/// Snapshot of the player's Scene 2 journey, captured right before the ending loads so the
/// epilogue can show personal results. Opening the ending scene directly falls back to a full run.
/// </summary>
public static class EndingSummary
{
    public const int TotalPieces = HouseholdChoreFlow.TotalPieces;

    public static bool HasData { get; private set; }
    public static int Pieces { get; private set; } = TotalPieces;
    public static float PlaySeconds { get; private set; } = -1f;

    private static readonly bool[] completed = new bool[15];
    private static float sessionStart = -1f;

    /// <summary>Marks the start of a play session (new game or continue).</summary>
    public static void BeginSession()
    {
        sessionStart = Time.realtimeSinceStartup;
        HasData = false;
    }

    public static bool IsCompleted(HouseholdChore chore)
    {
        if (!HasData) return true;
        int index = (int)chore;
        return index >= 0 && index < completed.Length && completed[index];
    }

    public static void Capture(HouseholdChoreFlow flow)
    {
        if (flow == null) return;
        for (int i = 0; i < completed.Length; i++) completed[i] = flow.IsCompleted((HouseholdChore)i);
        Pieces = Mathf.Clamp(flow.Pieces, 0, TotalPieces);
        PlaySeconds = sessionStart >= 0f ? Time.realtimeSinceStartup - sessionStart : -1f;
        HasData = true;
    }

    /// <summary>Short honorific shown on the final card.</summary>
    public static string Honorific
    {
        get
        {
            if (Pieces >= TotalPieces) return "Người giữ lửa bếp nhà";
            if (Pieces >= HouseholdChoreFlow.PiecesPerChore * HouseholdChoreFlow.ChoreCount) return "Đứa con biết trở về";
            return "Đứa con còn nhớ nhà";
        }
    }

    public static string PlayTimeText
    {
        get
        {
            if (PlaySeconds < 60f) return null;
            int minutes = Mathf.RoundToInt(PlaySeconds / 60f);
            return minutes + " phút";
        }
    }
}
