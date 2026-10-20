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
    private GameObject modalRoot;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (shopPanel != null) shopPanel.SetActive(false);
    }
    private void Start()
    {
        StyleModal();
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
        if (IsOpen && PauseUI.LastResumeFrame != Time.frameCount && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }
    public void OpenShop()
    {
        if (IsOpen) return;
        if (shopPanel == null || itemSlotPrefab == null || itemListParent == null || ShopManager.Instance == null)
        { Debug.LogError("Shop UI setup is incomplete.", this); return; }
        IsOpen = true;
        if (modalRoot != null) modalRoot.SetActive(true);
        shopPanel.SetActive(true);
        GameManager.Instance?.AcquireInput(this);
        Populate();
        MoneyChanged(CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentMoney : 0);
        if (messageText != null) messageText.text = "Mẹ đưa 2.000 đồng. Mua kẹo mút và bi ve trước khi ra sân.";
    }
    public void CloseShop()
    {
        IsOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
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
        if (messageText != null)
            messageText.text = QuestManager.Instance != null && QuestManager.Instance.HasRequiredPurchases
                ? "Đã đủ kẹo và bi ve. Ra sân chơi nào!"
                : "Đã mua " + item.itemName + ". Hãy chọn món còn lại.";
        Populate();
    }
    private void Failed(ItemData item)
    {
        if (messageText == null) return;
        switch (ShopManager.Instance.LastFailure)
        {
            case PurchaseFailure.InsufficientMoney: messageText.text = "Chưa đủ tiền cho món này. Hãy kiểm tra số tiền và giá món hàng."; break;
            case PurchaseFailure.AlreadyOwned: messageText.text = "Bạn đã mua món này rồi."; break;
            case PurchaseFailure.QuestLocked: messageText.text = "Hãy xem nhiệm vụ trong nhật ký trước."; break;
            default: messageText.text = "Không thể mua. Hãy kiểm tra cấu hình cửa hàng."; break;
        }
    }
    private void MoneyChanged(int amount) { if (playerMoneyText != null) playerMoneyText.text = "Tiền: " + amount + " đồng"; }

    private void StyleModal()
    {
        if (shopPanel == null) return;
        var parentCanvas = shopPanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas == null) return;
        modalRoot = new GameObject("ShopModalCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        modalRoot.transform.SetParent(parentCanvas.transform, false);
        var rootRect = (RectTransform)modalRoot.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        var canvas = modalRoot.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 90;

        var backdrop = new GameObject("ShopDimmer", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(modalRoot.transform, false);
        var backdropRect = (RectTransform)backdrop.transform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(.015f, .025f, .035f, .70f);

        shopPanel.transform.SetParent(modalRoot.transform, false);
        var rect = shopPanel.transform as RectTransform;
        if (rect != null) rect.sizeDelta = new Vector2(840f, 550f);
        var panelImage = shopPanel.GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color(.055f, .10f, .13f, .98f);
        foreach (Transform child in shopPanel.transform)
        {
            var title = child.GetComponent<TextMeshProUGUI>();
            if (title == null || title == playerMoneyText || title == messageText) continue;
            title.text = "TIỆM TẠP HÓA";
            title.fontSize = 31f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(.97f, .79f, .42f);
        }
        if (playerMoneyText != null)
        {
            playerMoneyText.fontSize = 23f;
            playerMoneyText.color = new Color(.84f, .95f, .88f);
        }
        if (messageText != null)
        {
            messageText.fontSize = 18f;
            messageText.color = new Color(.85f, .90f, .91f);
            messageText.enableWordWrapping = true;
        }
        if (closeButton != null)
        {
            var buttonImage = closeButton.GetComponent<Image>();
            if (buttonImage != null) buttonImage.color = new Color(.16f, .25f, .32f);
            var label = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Đóng · Esc";
                label.fontSize = 20f;
                label.color = new Color(.96f, .97f, .92f);
            }
        }
        modalRoot.SetActive(false);
    }
}
