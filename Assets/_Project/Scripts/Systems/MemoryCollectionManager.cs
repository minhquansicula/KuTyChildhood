using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MemoryCollectionManager — Theo dõi 3 mảnh ký ức.
/// 
/// Khi thu thập đủ 3/3 → trigger chuyển sang Act3 (Ending).
/// 
/// 3 mảnh ký ức:
/// 1. Mother (Mẹ) — từ mini-game rửa chén
/// 2. Friends (Bạn bè) — từ mini-game bắn bi
/// 3. SimpleJoy (Niềm vui giản dị) — từ mua vật phẩm đặc biệt ở tiệm tạp hóa
/// </summary>
public class MemoryCollectionManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static MemoryCollectionManager Instance { get; private set; }

    // ========== STATE ==========
    private Dictionary<MemoryType, bool> collectedMemories = new Dictionary<MemoryType, bool>();

    /// <summary>Số mảnh ký ức đã thu thập.</summary>
    public int CollectedCount { get; private set; } = 0;

    /// <summary>Tổng số mảnh ký ức cần thu thập.</summary>
    public const int TOTAL_MEMORIES = 3;

    // ========== EVENTS ==========
    /// <summary>Gọi khi thu thập 1 mảnh ký ức. Param: loại ký ức, tổng số đã thu thập</summary>
    public System.Action<MemoryType, int> OnMemoryCollected;

    /// <summary>Gọi khi thu thập đủ 3/3 mảnh ký ức.</summary>
    public System.Action OnAllMemoriesCollected;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeMemories();
    }

    private void InitializeMemories()
    {
        collectedMemories.Clear();
        collectedMemories[MemoryType.Mother] = false;
        collectedMemories[MemoryType.Friends] = false;
        collectedMemories[MemoryType.SimpleJoy] = false;
        CollectedCount = 0;
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Thu thập 1 mảnh ký ức.
    /// Gọi khi hoàn thành mini-game hoặc mua vật phẩm đặc biệt.
    /// </summary>
    public void CollectMemory(MemoryType type)
    {
        // Đã thu thập rồi → bỏ qua
        if (collectedMemories.ContainsKey(type) && collectedMemories[type])
        {
            Debug.Log($"[MemoryCollection] {type} đã được thu thập trước đó, bỏ qua.");
            return;
        }

        // Thu thập
        collectedMemories[type] = true;
        CollectedCount++;

        string memoryName = GetMemoryName(type);
        Debug.Log($"[MemoryCollection] ✨ Thu thập: {memoryName} ({CollectedCount}/{TOTAL_MEMORIES})");

        // Thông báo UI
        OnMemoryCollected?.Invoke(type, CollectedCount);

        // Kiểm tra đủ chưa
        if (CollectedCount >= TOTAL_MEMORIES)
        {
            Debug.Log("[MemoryCollection] 🔑 Đã thu thập đủ 3/3 mảnh ký ức! Ghép Chìa Khóa Ước Mơ...");
            OnAllMemoriesCollected?.Invoke();

            // Delay rồi chuyển sang Act3
            StartCoroutine(TransitionToEnding());
        }
    }

    /// <summary>
    /// Kiểm tra đã thu thập loại ký ức này chưa.
    /// </summary>
    public bool HasCollected(MemoryType type)
    {
        return collectedMemories.ContainsKey(type) && collectedMemories[type];
    }

    /// <summary>
    /// Reset tất cả ký ức (dùng khi chơi lại).
    /// </summary>
    public void ResetMemories()
    {
        InitializeMemories();
        Debug.Log("[MemoryCollection] Reset tất cả ký ức.");
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Lấy tên hiển thị tiếng Việt cho mỗi loại ký ức.
    /// </summary>
    public static string GetMemoryName(MemoryType type)
    {
        switch (type)
        {
            case MemoryType.Mother: return "Ký ức về Mẹ";
            case MemoryType.Friends: return "Ký ức về Bạn bè";
            case MemoryType.SimpleJoy: return "Ký ức về Niềm vui giản dị";
            default: return "Ký ức không xác định";
        }
    }

    /// <summary>
    /// Lấy mô tả cho mỗi loại ký ức (hiện trong dialogue).
    /// </summary>
    public static string GetMemoryDescription(MemoryType type)
    {
        switch (type)
        {
            case MemoryType.Mother:
                return "Những bữa cơm ấm áp, tiếng mẹ gọi về nhà khi hoàng hôn buông xuống...";
            case MemoryType.Friends:
                return "Những chiều hè rộn ràng tiếng cười, cùng nhau chơi đến khi trời tối mịt...";
            case MemoryType.SimpleJoy:
                return "Niềm vui giản dị khi được cầm trên tay món đồ chơi mơ ước bấy lâu...";
            default:
                return "";
        }
    }

    private System.Collections.IEnumerator TransitionToEnding()
    {
        // Chờ 3 giây cho player đọc text + xem animation ghép chìa khóa
        yield return new WaitForSeconds(3f);

        // Chuyển sang Act3
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterEnding();
        }
    }
}

/// <summary>
/// Enum đại diện cho 3 loại mảnh ký ức.
/// </summary>
public enum MemoryType
{
    Mother,         // Ký ức về Mẹ — từ rửa chén
    Friends,        // Ký ức về Bạn bè — từ bắn bi
    SimpleJoy       // Ký ức về Niềm vui giản dị — từ mua đồ ở tiệm
}
