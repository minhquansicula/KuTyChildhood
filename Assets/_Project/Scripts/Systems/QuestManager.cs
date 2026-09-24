using System;
using UnityEngine;

public enum QuestStep
{
    ReadJournal,
    WashDishes,
    BuyCandyAndMarbles,
    PlayMarbles,
    ReturnToJournal,
    TakeDreamKey,
    UnlockFrontDoor,
    Ending
}

// One state owner for the complete Act 2 story. Attach only once to Act2 Systems.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    [SerializeField] private ItemData candy;
    [SerializeField] private ItemData marbleJar;
    [SerializeField] private DishWashingGame dishGame;
    [SerializeField] private MarbleShootingGame marbleGame;
    [SerializeField] private ShopManager shop;
    [SerializeField] private MemoryCollectionManager memories;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private QuestStep currentStep = QuestStep.ReadJournal;
    public QuestStep CurrentStep => currentStep;
    public ItemData Candy => candy;
    public ItemData MarbleJar => marbleJar;
    public bool HasDreamKey { get; private set; }
    public bool HasReadJournal => currentStep != QuestStep.ReadJournal;
    public event Action<QuestStep> OnQuestChanged;
    public event Action OnDreamKeyCrafted;
    public event Action OnDreamKeyTaken;
    public event Action OnFrontDoorUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void Start()
    {
        if (dishGame != null) dishGame.OnGameCompleted += DishCompleted;
        if (marbleGame != null) marbleGame.OnGameEnded += MarbleEnded;
        if (shop != null) shop.OnPurchaseSuccess += Purchased;
        OnQuestChanged?.Invoke(currentStep);
    }
    private void OnDestroy()
    {
        if (dishGame != null) dishGame.OnGameCompleted -= DishCompleted;
        if (marbleGame != null) marbleGame.OnGameEnded -= MarbleEnded;
        if (shop != null) shop.OnPurchaseSuccess -= Purchased;
        if (Instance == this) Instance = null;
    }
    private void Advance(QuestStep expected, QuestStep next)
    {
        if (currentStep != expected) return;
        currentStep = next;
        OnQuestChanged?.Invoke(next);
    }
    public void ReadJournal()
    {
        if (currentStep != QuestStep.ReadJournal) return;
        Advance(QuestStep.ReadJournal, QuestStep.WashDishes);
    }
    public bool CanWashDishes => currentStep == QuestStep.WashDishes;
    public bool CanShop => currentStep == QuestStep.BuyCandyAndMarbles;
    public bool CanPlayMarbles => currentStep == QuestStep.PlayMarbles &&
        inventory != null && marbleJar != null && inventory.HasItem(marbleJar);
    public bool HasRequiredPurchases => inventory != null && candy != null && marbleJar != null &&
        inventory.HasItem(candy) && inventory.HasItem(marbleJar);
    private void DishCompleted(int reward)
    {
        if (currentStep != QuestStep.WashDishes || memories == null) return;
        memories.CollectMemory(MemoryType.FilialLove);
        Advance(QuestStep.WashDishes, QuestStep.BuyCandyAndMarbles);
    }
    private void Purchased(ItemData item)
    {
        if (currentStep != QuestStep.BuyCandyAndMarbles || !HasRequiredPurchases || memories == null) return;
        ShopUI.Instance?.CloseShop();
        memories.CollectMemory(MemoryType.SimpleJoy);
        Advance(QuestStep.BuyCandyAndMarbles, QuestStep.PlayMarbles);
    }
    private void MarbleEnded(bool won)
    {
        if (!won || currentStep != QuestStep.PlayMarbles || memories == null) return;
        memories.CollectMemory(MemoryType.Freedom);
        Advance(QuestStep.PlayMarbles, QuestStep.ReturnToJournal);
    }
    public bool CraftDreamKey()
    {
        if (currentStep != QuestStep.ReturnToJournal || memories == null ||
            memories.CollectedCount != MemoryCollectionManager.TOTAL_MEMORIES) return false;
        Advance(QuestStep.ReturnToJournal, QuestStep.TakeDreamKey);
        OnDreamKeyCrafted?.Invoke();
        AudioManager.Instance?.PlaySFX("all_memories");
        return true;
    }
    public bool TakeDreamKey()
    {
        if (currentStep != QuestStep.TakeDreamKey || HasDreamKey) return false;
        HasDreamKey = true;
        Advance(QuestStep.TakeDreamKey, QuestStep.UnlockFrontDoor);
        OnDreamKeyTaken?.Invoke();
        return true;
    }
    public bool UnlockFrontDoor()
    {
        if (currentStep != QuestStep.UnlockFrontDoor || !HasDreamKey) return false;
        Advance(QuestStep.UnlockFrontDoor, QuestStep.Ending);
        OnFrontDoorUnlocked?.Invoke();
        return true;
    }
    public string ObjectiveText
    {
        get
        {
            switch (currentStep)
            {
                case QuestStep.ReadJournal: return "Đọc cuốn nhật ký trên bàn.";
                case QuestStep.WashDishes: return "Phụ mẹ rửa chén.";
                case QuestStep.BuyCandyAndMarbles: return "Mua một cây kẹo mút và một hũ bi ve.";
                case QuestStep.PlayMarbles: return "Mang hũ bi ra sân chơi.";
                case QuestStep.ReturnToJournal: return "Trở lại cuốn nhật ký.";
                case QuestStep.TakeDreamKey: return "Cầm Chìa Khóa Ước Mơ.";
                case QuestStep.UnlockFrontDoor: return "Mở cánh cửa đang bị xiềng xích.";
                default: return "Trở về hiện tại.";
            }
        }
    }
}
