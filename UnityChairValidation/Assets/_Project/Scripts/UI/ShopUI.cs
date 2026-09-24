using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI messageText;
    private readonly List<GameObject> slots = new List<GameObject>();
    public bool IsOpen { get; private set; }
    private ShopManager shop;
    private CurrencyManager currency;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (shopPanel != null) shopPanel.SetActive(false);
    }
    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        shop = ShopManager.Instance;
        currency = CurrencyManager.Instance;
        if (shop != null) { shop.OnPurchaseSuccess += Purchased; shop.OnPurchaseFailed += Failed; }
        if (currency != null) currency.OnMoneyChanged += MoneyChanged;
    }
    private void OnDestroy()
    {
        if (shop != null) { shop.OnPurchaseSuccess -= Purchased; shop.OnPurchaseFailed -= Failed; }
        if (currency != null) currency.OnMoneyChanged -= MoneyChanged;
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }
    public void OpenShop()
    {
        if (IsOpen) return;
        if (shopPanel == null || itemSlotPrefab == null || itemListParent == null || ShopManager.Instance == null)
        { Debug.LogError("Shop UI setup is incomplete.", this); return; }
        IsOpen = true;
        shopPanel.SetActive(true);
        GameManager.Instance?.AcquireInput(this);
        Populate();
        MoneyChanged(CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentMoney : 0);
        if (messageText != null) messageText.text = "Mua kẹo mút và hũ bi ve — tổng cộng 2.000 đồng.";
    }
    public void CloseShop()
    {
        IsOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        GameManager.Instance?.ReleaseInput(this);
    }
    private void Populate()
    {
        foreach (var slot in slots) { slot.SetActive(false); Destroy(slot); }
        slots.Clear();
        foreach (var item in ShopManager.Instance.ShopItems)
        {
            if (item == null) continue;
            var slot = Instantiate(itemSlotPrefab, itemListParent);
            slots.Add(slot);
            slot.GetComponent<ShopItemSlotUI>().Setup(item, Buy);
        }
    }
    private void Buy(ItemData item) => ShopManager.Instance.PurchaseItem(item);
    private void Purchased(ItemData item)
    {
        if (messageText != null) messageText.text = "Đã mua " + item.itemName;
        Populate();
    }
    private void Failed(ItemData item)
    {
        if (messageText == null) return;
        switch (ShopManager.Instance.LastFailure)
        {
            case PurchaseFailure.InsufficientMoney: messageText.text = "Chưa đủ tiền. Hãy giúp mẹ rửa chén."; break;
            case PurchaseFailure.AlreadyOwned: messageText.text = "Bạn đã mua món này rồi."; break;
            case PurchaseFailure.QuestLocked: messageText.text = "Hãy xem nhiệm vụ trong nhật ký trước."; break;
            default: messageText.text = "Không thể mua. Hãy kiểm tra cấu hình cửa hàng."; break;
        }
    }
    private void MoneyChanged(int amount) { if (playerMoneyText != null) playerMoneyText.text = "Tiền: " + amount + " đồng"; }
}
