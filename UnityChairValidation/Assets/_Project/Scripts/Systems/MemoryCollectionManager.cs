using System;
using System.Collections.Generic;
using UnityEngine;

public enum MemoryType { FilialLove, SimpleJoy, Freedom }

// Keeps the three memories independent from scene transitions and quest presentation.
public class MemoryCollectionManager : MonoBehaviour
{
    public static MemoryCollectionManager Instance { get; private set; }
    public const int TOTAL_MEMORIES = 3;
    private readonly HashSet<MemoryType> collected = new HashSet<MemoryType>();
    public int CollectedCount => collected.Count;
    public event Action<MemoryType, int> OnMemoryCollected;
    public event Action OnAllMemoriesCollected;
    public event Action OnMemoriesReset;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public bool HasCollected(MemoryType type) => collected.Contains(type);
    public bool CollectMemory(MemoryType type)
    {
        if (!Enum.IsDefined(typeof(MemoryType), type) || !collected.Add(type)) return false;
        OnMemoryCollected?.Invoke(type, collected.Count);
        AudioManager.Instance?.PlaySFX("memory_collected");
        DialogueUI.Instance?.ShowDialogue(GetMemoryName(type), GetMemoryDescription(type), 3f);
        if (collected.Count == TOTAL_MEMORIES) OnAllMemoriesCollected?.Invoke();
        return true;
    }
    public void ResetMemories() { collected.Clear(); OnMemoriesReset?.Invoke(); }
    public static string GetMemoryName(MemoryType type)
    {
        switch (type)
        {
            case MemoryType.FilialLove: return "Mảnh ký ức #1 — Mẹ";
            case MemoryType.SimpleJoy: return "Mảnh ký ức #2 — Niềm vui giản đơn";
            case MemoryType.Freedom: return "Mảnh ký ức #3 — Sự tự do";
            default: return "Ký ức";
        }
    }
    public static string GetMemoryDescription(MemoryType type)
    {
        switch (type)
        {
            case MemoryType.FilialLove: return "Mẹ xoa đầu khen mình ngoan. Cảm giác thật bình yên.";
            case MemoryType.SimpleJoy: return "Vị ngọt của viên kẹo làm mình quên đi vết xước ở đầu gối hồi chiều.";
            case MemoryType.Freedom: return "Một buổi chiều không deadline, không áp lực, chỉ có tiếng cười giòn tan.";
            default: return "";
        }
    }
}
