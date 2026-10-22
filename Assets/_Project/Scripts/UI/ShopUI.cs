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
            default: messageText.text = "Chưa mua được món này. Bạn thử lại nhé."; break;
        }
    }
    private void MoneyChanged(int amount) { if (playerMoneyText != null) playerMoneyText.text = "Trong túi: " + GameUiPresentation.Money(amount); }

    private void StyleModal()
    {
        if (shopPanel == null) return;
        StyleShopPresentation();
    }

    private void StyleShopPresentation()
    {
        if (shopPanel == null) return;
        var parentCanvas = shopPanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas == null) return;
        modalRoot = GameUiPresentation.ModalRoot(shopPanel, "ShopModalCanvas", 90);
        var rect = shopPanel.transform as RectTransform;
        if (rect != null) OfficeUiStyle.Rect(rect, Vector2.one * .5f, new Vector2(1040f, 740f), Vector2.zero);
        var panelImage = shopPanel.GetComponent<Image>();
        GameUiPresentation.Card(panelImage, true);
        foreach (Transform child in shopPanel.transform)
        {
            var title = child.GetComponent<TextMeshProUGUI>();
            if (title == null || title == playerMoneyText || title == messageText) continue;
            title.text = "TIỆM TẠP HÓA";
            GameUiPresentation.Text(title, 34, GameUiTheme.Current.text);
            OfficeUiStyle.Rect(title.rectTransform, new Vector2(.5f, 1), new Vector2(850, 50), new Vector2(0, -57));
            title.fontStyle = FontStyles.Bold;
        }
        if (playerMoneyText != null)
        {
            GameUiPresentation.Text(playerMoneyText, 25, GameUiTheme.Current.muted);
            OfficeUiStyle.Rect(playerMoneyText.rectTransform, new Vector2(.5f, 1), new Vector2(850, 40), new Vector2(0, -110));
            playerMoneyText.alignment = TextAlignmentOptions.Center;
        }
        if (messageText != null)
        {
            GameUiPresentation.Text(messageText, 24, GameUiTheme.Current.text);
            OfficeUiStyle.Rect(messageText.rectTransform, new Vector2(.5f, 0), new Vector2(880, 80), new Vector2(0, 165));
            messageText.alignment = TextAlignmentOptions.Center;
        }
        if (closeButton != null)
        {
            OfficeUiStyle.Rect((RectTransform)closeButton.transform, new Vector2(.5f, 0), new Vector2(360, 78), new Vector2(0, 69));
            var label = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Đóng · Esc";
            }
            GameUiPresentation.Button(closeButton);
        }
        if (itemListParent is RectTransform list)
        {
            list.anchorMin = new Vector2(.06f, .34f); list.anchorMax = new Vector2(.94f, .78f);
            list.offsetMin = list.offsetMax = Vector2.zero;
            foreach (var layout in list.GetComponents<LayoutGroup>()) layout.enabled = false;
            var fitter = list.GetComponent<ContentSizeFitter>();
            if (fitter) fitter.enabled = false;
            var content = new GameObject("ShopItemGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            content.transform.SetParent(list, false);
            GameUiPresentation.Stretch((RectTransform)content.transform, 0, 0);
            itemListParent = content.transform;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(430, 310);
            grid.spacing = new Vector2(24, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
        }
        modalRoot.SetActive(false);
    }
}
