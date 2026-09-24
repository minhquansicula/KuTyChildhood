using UnityEngine;

/// <summary>
/// PlayerInteraction — Hệ thống tương tác bằng Raycast + phím E.
/// 
/// Cách hoạt động:
/// 1. Mỗi frame bắn Raycast từ camera về phía trước
/// 2. Nếu trúng vật có IInteractable → hiện prompt trên HUD
/// 3. Nếu bấm E → gọi Interact() trên vật đó
/// 
/// Setup trong Unity:
/// 1. Gán lên cùng GameObject với FirstPersonController
/// 2. Đặt các vật tương tác lên Layer "Interactable"
/// 3. Các vật tương tác cần có Collider + script implement IInteractable
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // ========== SETTINGS ==========
    [Header("Raycast Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;   // Layer "Interactable"
    [SerializeField] private Transform raycastOrigin;        // Camera transform

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    // ========== STATE ==========
    private IInteractable currentTarget = null;
    private string currentPromptText = "";
    private Camera mainCamera;

    /// <summary>
    /// Khi true, player không thể tương tác (đang trong mini-game, cutscene, v.v.).
    /// </summary>
    public bool IsInteractionLocked { get; set; } = false;

    // ========== LIFECYCLE ==========
    private void Start()
    {
        mainCamera = Camera.main;

        // Tự dùng camera nếu chưa gán raycastOrigin
        if (raycastOrigin == null && mainCamera != null)
        {
            raycastOrigin = mainCamera.transform;
        }
        if (raycastOrigin == null) { Debug.LogError("PlayerInteraction requires a camera.", this); enabled = false; }
    }

    private void Update()
    {
        if (IsInteractionLocked || (GameManager.Instance != null && GameManager.Instance.InputBlocked))
        {
            // Ẩn prompt khi bị lock
            if (currentTarget != null)
            {
                currentTarget = null;
                currentPromptText = "";
                HUDController.Instance?.HideInteractPrompt();
                HUDController.Instance?.SetCrosshairHighlight(false);
            }
            return;
        }

        HandleRaycast();
        HandleInteractInput();
    }

    // ========== RAYCAST ==========
    private void HandleRaycast()
    {
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);

        // Cast against walls too, so an interactable cannot be used through a wall.
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore)
            && (interactableLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
        {
            // Tìm IInteractable trên vật bị trúng
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                // Thử tìm trên parent (trường hợp collider ở child)
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null && !string.IsNullOrEmpty(interactable.GetPromptText()))
            {
                string promptText = interactable.GetPromptText();
                if (currentTarget != interactable || currentPromptText != promptText)
                {
                    currentTarget = interactable;
                    currentPromptText = promptText;

                    HUDController.Instance?.ShowInteractPrompt(promptText);
                    HUDController.Instance?.SetCrosshairHighlight(true);
                }
                return;
            }
        }

        // Không trúng gì hoặc không có IInteractable → ẩn prompt
        if (currentTarget != null)
        {
            currentTarget = null;
            currentPromptText = "";
            HUDController.Instance?.HideInteractPrompt();
            HUDController.Instance?.SetCrosshairHighlight(false);
        }
    }

    // ========== INPUT ==========
    private void HandleInteractInput()
    {
        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[PlayerInteraction] Interacting with: {currentTarget.GetPromptText()}");
            currentTarget.Interact();
            currentTarget = null;
            currentPromptText = "";
            HUDController.Instance?.HideInteractPrompt();
            HUDController.Instance?.SetCrosshairHighlight(false);
        }
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Khóa/mở tương tác. Gọi khi đang chơi mini-game hoặc mở UI.
    /// </summary>
    public void LockInteraction(bool locked)
    {
        IsInteractionLocked = locked;
    }
}
