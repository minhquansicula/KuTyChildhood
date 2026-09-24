using UnityEngine;

/// <summary>
/// CurrencyManager — Quản lý tiền trong game.
/// 
/// Tiền kiếm từ mini-game rửa chén, dùng để mua đồ ở tiệm tạp hóa.
/// Singleton nhẹ (không DontDestroyOnLoad — GameManager đã lo việc đó).
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static CurrencyManager Instance { get; private set; }

    // ========== STATE ==========
    [Header("Currency")]
    [SerializeField] private int startingMoney = 0;
    private int currentMoney;

    /// <summary>Số tiền hiện tại.</summary>
    public int CurrentMoney => currentMoney;

    // ========== EVENTS ==========
    /// <summary>Gọi khi số tiền thay đổi. Param: newAmount</summary>
    public System.Action<int> OnMoneyChanged;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentMoney = startingMoney;
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Thêm tiền (vd: từ mini-game rửa chén).
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[CurrencyManager] AddMoney với số <= 0, bỏ qua.");
            return;
        }

        currentMoney += amount;
        Debug.Log($"[CurrencyManager] +{amount} đồng. Tổng: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>
    /// Trừ tiền (vd: mua đồ ở tiệm tạp hóa).
    /// Trả về true nếu đủ tiền và đã trừ thành công.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[CurrencyManager] SpendMoney với số <= 0, bỏ qua.");
            return false;
        }

        if (currentMoney < amount)
        {
            Debug.Log($"[CurrencyManager] Không đủ tiền! Cần {amount}, chỉ có {currentMoney}.");
            return false;
        }

        currentMoney -= amount;
        Debug.Log($"[CurrencyManager] -{amount} đồng. Còn: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    /// <summary>
    /// Kiểm tra có đủ tiền không (không trừ).
    /// </summary>
    public bool HasEnoughMoney(int amount)
    {
        return currentMoney >= amount;
    }

    /// <summary>
    /// Reset tiền về số khởi đầu (dùng khi chơi lại).
    /// </summary>
    public void ResetCurrency()
    {
        currentMoney = startingMoney;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log("[CurrencyManager] Reset currency.");
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
