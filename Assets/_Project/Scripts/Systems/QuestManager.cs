using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private QuestStep currentStep = QuestStep.WashDishes;
    public QuestStep CurrentStep => currentStep;
    public ItemData Candy => candy;
    public ItemData MarbleJar => marbleJar;
    public bool HasDreamKey { get; private set; }
    public bool IsLeavingHome { get; private set; }
    private bool outsideTransitionRequested;
    private Coroutine outsideFallback;
    public bool HasReadJournal => currentStep != QuestStep.ReadJournal;
    public event Action<QuestStep> OnQuestChanged;
    public event Action OnDreamKeyCrafted;
    public event Action OnDreamKeyTaken;
    public event Action OnFrontDoorUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        // The outdoor scene owns the shop, marble table, journal and final door.
        // It also starts in a playable state when opened directly for testing.
        if (SceneManager.GetActiveScene().name == SceneNames.Act3_OutSide)
        {
            currentStep = Scene2Checkpoint.OutsideStep;
            HasDreamKey = Scene2Checkpoint.HasDreamKey;
            if (GetComponent<Scene2QuestHUD>() == null) gameObject.AddComponent<Scene2QuestHUD>();
        }
        else if (currentStep == QuestStep.ReadJournal)
            currentStep = QuestStep.WashDishes;
    }
    private void Start()
    {
        if (SceneManager.GetActiveScene().name == SceneNames.Act3_OutSide)
        {
            if (Scene2Checkpoint.HasOutsideProgress)
            {
                CurrencyManager.Instance?.RestoreMoney(Scene2Checkpoint.OutsideMoney);
                if (Scene2Checkpoint.HasCandy && candy != null) inventory?.AddItem(candy);
                if (Scene2Checkpoint.HasMarbles && marbleJar != null) inventory?.AddItem(marbleJar);
                for (int i = 0; i < MemoryCollectionManager.TOTAL_MEMORIES; i++)
                    if (Scene2Checkpoint.HasMemory((MemoryType)i)) memories?.RestoreMemory((MemoryType)i);
                // Repair interrupted writes at the boundaries between a purchase and its quest event.
                memories?.RestoreMemory(MemoryType.FilialLove);
                if (currentStep >= QuestStep.PlayMarbles)
                {
                    if (candy != null && inventory != null && !inventory.HasItem(candy)) inventory.AddItem(candy);
                    if (marbleJar != null && inventory != null && !inventory.HasItem(marbleJar)) inventory.AddItem(marbleJar);
                    memories?.RestoreMemory(MemoryType.SimpleJoy);
                }
                if (currentStep >= QuestStep.ReturnToJournal) memories?.RestoreMemory(MemoryType.Freedom);
                if (currentStep == QuestStep.BuyCandyAndMarbles && HasRequiredPurchases)
                {
                    currentStep = QuestStep.PlayMarbles;
                    memories?.RestoreMemory(MemoryType.SimpleJoy);
                }
                if (currentStep >= QuestStep.UnlockFrontDoor) HasDreamKey = true;
                Scene2Checkpoint.SaveOutside(this);
            }
            else
            {
                memories?.RestoreMemory(MemoryType.FilialLove);
                CurrencyManager.Instance?.AddMoney(2000);
            }
        }
        if (marbleGame != null) marbleGame.OnGameEnded += MarbleEnded;
        if (shop != null) shop.OnPurchaseSuccess += Purchased;
        OnQuestChanged?.Invoke(currentStep);
    }
    private void OnDestroy()
    {
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
    public bool CanWashDishes => currentStep == QuestStep.WashDishes && HouseholdChoreFlow.Instance == null;
    public bool CanFinishHouseholdChores => currentStep == QuestStep.WashDishes && memories != null &&
        CurrencyManager.Instance != null &&
        Application.CanStreamedLevelBeLoaded(SceneNames.Act3_OutSide);
    public bool CompleteHouseholdChores()
    {
        if (!CanFinishHouseholdChores || HouseholdChoreFlow.Instance == null ||
            HouseholdChoreFlow.Instance.ActiveIndex != HouseholdChoreFlow.ChoreCount) return false;
        memories.CollectMemory(MemoryType.FilialLove);
        // The household sequence is the only source of the shopping allowance.
        // Advance guards this grant against duplicate completion calls.
        CurrencyManager.Instance.AddMoney(2000);
        Advance(QuestStep.WashDishes, QuestStep.BuyCandyAndMarbles);
        Scene2Checkpoint.SaveOutside(this);
        if (SceneManager.GetActiveScene().name == SceneNames.Act2_Home)
        {
            IsLeavingHome = true;
            outsideFallback = StartCoroutine(LeaveHomeAfterLetter());
        }
        return true;
    }

    private IEnumerator LeaveHomeAfterLetter()
    {
        // Safety net if the letter's dialogue is unavailable or interrupted by another UI.
        yield return new WaitForSecondsRealtime(20f);
        // Reading the letter has no deadline; only recover once its modal has closed.
        while (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing) yield return null;
        outsideFallback = null;
        ContinueOutside();
    }

    public void ContinueOutside()
    {
        if (!IsLeavingHome || outsideTransitionRequested) return;
        outsideTransitionRequested = true;
        if (outsideFallback != null) StopCoroutine(outsideFallback);
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act3_OutSide, Color.white, 1.2f);
        else
        {
            Debug.LogWarning("SceneLoader is missing; continuing to the outdoor scene without a fade.", this);
            SceneManager.LoadScene(SceneNames.Act3_OutSide);
        }
    }
    public void RefreshHouseholdObjective()
    {
        if (currentStep == QuestStep.WashDishes) OnQuestChanged?.Invoke(currentStep);
    }
    public bool CanShop => currentStep == QuestStep.BuyCandyAndMarbles;
    public bool CanPlayMarbles => currentStep == QuestStep.PlayMarbles &&
        inventory != null && marbleJar != null && inventory.HasItem(marbleJar);
    public bool HasRequiredPurchases => inventory != null && candy != null && marbleJar != null &&
        inventory.HasItem(candy) && inventory.HasItem(marbleJar);
    private void Purchased(ItemData item)
    {
        if (currentStep != QuestStep.BuyCandyAndMarbles) return;
        Scene2Checkpoint.SaveOutside(this);
        if (!HasRequiredPurchases || memories == null) return;
        ShopUI.Instance?.CloseShop();
        memories.CollectMemory(MemoryType.SimpleJoy);
        Advance(QuestStep.BuyCandyAndMarbles, QuestStep.PlayMarbles);
        Scene2Checkpoint.SaveOutside(this);
    }
    private void MarbleEnded(bool won)
    {
        if (!won || currentStep != QuestStep.PlayMarbles || memories == null) return;
        memories.CollectMemory(MemoryType.Freedom);
        Advance(QuestStep.PlayMarbles, QuestStep.ReturnToJournal);
        Scene2Checkpoint.SaveOutside(this);
    }
    public bool CraftDreamKey()
    {
        if (currentStep != QuestStep.ReturnToJournal || memories == null ||
            memories.CollectedCount != MemoryCollectionManager.TOTAL_MEMORIES) return false;
        Advance(QuestStep.ReturnToJournal, QuestStep.TakeDreamKey);
        OnDreamKeyCrafted?.Invoke();
        AudioManager.Instance?.PlaySFX(SfxId.AllMemoriesComplete);
        Scene2Checkpoint.SaveOutside(this);
        return true;
    }
    public bool TakeDreamKey()
    {
        if (currentStep != QuestStep.TakeDreamKey || HasDreamKey) return false;
        HasDreamKey = true;
        Advance(QuestStep.TakeDreamKey, QuestStep.UnlockFrontDoor);
        OnDreamKeyTaken?.Invoke();
        Scene2Checkpoint.SaveOutside(this);
        return true;
    }
    public bool UnlockFrontDoor()
    {
        if (currentStep != QuestStep.UnlockFrontDoor || !HasDreamKey) return false;
        Advance(QuestStep.UnlockFrontDoor, QuestStep.Ending);
        OnFrontDoorUnlocked?.Invoke();
        Scene2Checkpoint.MarkFinished();
        return true;
    }
    public string ObjectiveText
    {
        get
        {
            switch (currentStep)
            {
                case QuestStep.ReadJournal: return "Đọc cuốn nhật ký trên bàn.";
                case QuestStep.WashDishes:
                    var chores = HouseholdChoreFlow.Instance;
                    return chores != null && chores.ActiveIndex < HouseholdChoreFlow.ChoreCount
                        ? "Phụ mẹ: " + chores.CurrentTitle + " — " + chores.CurrentLocation + "."
                        : "Phụ mẹ làm việc nhà.";
                case QuestStep.BuyCandyAndMarbles:
                    return SceneManager.GetActiveScene().name == SceneNames.Act2_Home
                        ? "Mang tiền mẹ cho ra sân chơi." : "Mua một cây kẹo mút và một hũ bi ve.";
                case QuestStep.PlayMarbles: return "Mang hũ bi ra sân chơi.";
                case QuestStep.ReturnToJournal: return "Trở lại cuốn nhật ký.";
                case QuestStep.TakeDreamKey: return "Cầm Chìa Khóa Ước Mơ.";
                case QuestStep.UnlockFrontDoor: return "Mở cánh cửa đang bị xiềng xích.";
                default: return "Trở về hiện tại.";
            }
        }
    }
}
