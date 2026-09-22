using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ShopUI — Giao diện mua hàng tiệm tạp hóa.
/// 
/// Hiển thị danh sách vật phẩm từ ShopManager,
/// cho phép player mua bằng cách bấm nút Mua.
/// 
/// Setup trong Unity:
/// 1. Tạo Panel lớn (shop window) — ẩn mặc định
/// 2. Tạo ScrollView chứa danh sách items
/// 3. Tạo Prefab "ShopItemSlot" (icon + tên + giá + nút Mua)
/// 4. Thêm TextMeshPro cho tiền hiện có
/// 5. Thêm nút Đóng
/// </summary>
public class ShopUI : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static ShopUI Instance { get; private set; }

    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemListParent;       // Parent cho danh sách items (Content của ScrollView)
    [SerializeField] private GameObject itemSlotPrefab;      // Prefab ShopItemSlot
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private Button closeButton;

    [Header("Messages")]
    [SerializeField] private TextMeshProUGUI messageText;    // "Đã mua!", "Không đủ tiền!"
    [SerializeField] private float messageDuration = 2f;

    // ========== STATE ==========
    private bool isOpen = false;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Start()
    {
        // Setup nút Đóng
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        // Subscribe events
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
            ShopManager.Instance.OnPurchaseFailed += OnPurchaseFailed;
        }
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
            ShopManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
        }
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Mở shop UI — hiện panel, tạo danh sách vật phẩm.
    /// </summary>
    public void OpenShop()
    {
        if (isOpen) return;

        isOpen = true;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        // Hiện cursor
        GameManager.Instance?.SetCursorState(true);

        // Lock player
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null) player.LockMovement(true);

        var interaction = FindObjectOfType<PlayerInteraction>();
        if (interaction != null) interaction.LockInteraction(true);

        // Tạo danh sách items
        PopulateItems();

        // Update tiền
        UpdateMoneyDisplay();

        // Ẩn message
        if (messageText != null)
            messageText.text = "";
    }

    /// <summary>
    /// Đóng shop UI.
    /// </summary>
    public void CloseShop()
    {
        if (!isOpen) return;

        isOpen = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Ẩn cursor (quay về FPS)
        GameManager.Instance?.SetCursorState(false);

        // Unlock player
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null) player.LockMovement(false);

        var interaction = FindObjectOfType<PlayerInteraction>();
        if (interaction != null) interaction.LockInteraction(false);
    }

    // ========== PRIVATE METHODS ==========

    private void PopulateItems()
    {
        // Xóa items cũ
        foreach (var slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot);
        }
        spawnedSlots.Clear();

        if (ShopManager.Instance == null || itemSlotPrefab == null || itemListParent == null)
            return;

        // Tạo slot cho mỗi item
        foreach (var item in ShopManager.Instance.ShopItems)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemListParent);
            spawnedSlots.Add(slot);

            // Setup slot UI
            SetupItemSlot(slot, item);
        }
    }

    private void SetupItemSlot(GameObject slot, ItemData item)
    {
        // Tìm các component trong prefab
        // Cấu trúc prefab expected:
        // ShopItemSlot
        //   ├── ItemIcon (Image)
        //   ├── ItemName (TextMeshProUGUI)
        //   ├── ItemPrice (TextMeshProUGUI)
        //   ├── ItemDescription (TextMeshProUGUI) [optional]
        //   └── BuyButton (Button)

        var nameTexts = slot.GetComponentsInChildren<TextMeshProUGUI>();
        var images = slot.GetComponentsInChildren<Image>();
        var buttons = slot.GetComponentsInChildren<Button>();

        // Set tên (component thứ 1)
        if (nameTexts.Length > 0)
            nameTexts[0].text = item.itemName;

        // Set giá (component thứ 2)
        if (nameTexts.Length > 1)
            nameTexts[1].text = $"{item.price} đồng";

        // Set mô tả (component thứ 3, nếu có)
        if (nameTexts.Length > 2)
            nameTexts[2].text = item.description;

        // Set icon
        if (item.icon != null)
        {
            // Tìm Image không phải background
            foreach (var img in images)
            {
                if (img.gameObject != slot && img.GetComponent<Button>() == null)
                {
                    img.sprite = item.icon;
                    break;
                }
            }
        }

        // Setup nút Mua
        if (buttons.Length > 0)
        {
            bool alreadyOwned = InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item);

            if (alreadyOwned)
            {
                buttons[0].interactable = false;
                var btnText = buttons[0].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Đã mua";
            }
            else
            {
                // Capture item trong closure
                ItemData capturedItem = item;
                buttons[0].onClick.AddListener(() => OnBuyButtonClicked(capturedItem));

                var btnText = buttons[0].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Mua";
            }
        }
    }

    private void OnBuyButtonClicked(ItemData item)
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PurchaseItem(item);
        }
    }

    private void OnPurchaseSuccess(ItemData item)
    {
        ShowMessage($"Đã mua {item.itemName}! 🎉");
        UpdateMoneyDisplay();
        PopulateItems(); // Refresh danh sách (update trạng thái nút Mua)
    }

    private void OnPurchaseFailed(ItemData item)
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item))
        {
            ShowMessage($"Đã mua {item.itemName} rồi!");
        }
        else
        {
            ShowMessage($"Không đủ tiền mua {item.itemName}!");
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (playerMoneyText != null && CurrencyManager.Instance != null)
        {
            playerMoneyText.text = $"Tiền: {CurrencyManager.Instance.CurrentMoney} đồng";
        }
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), messageDuration);
        }
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }
}
