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
    private readonly RaycastHit[] raycastHits = new RaycastHit[32];

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

        // Cast against all layers to detect obstacles, triggers, and interactables.
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, interactDistance, ~0, QueryTriggerInteraction.Collide);
        if (hitCount > 0)
        {
            // Sort hits by distance to evaluate from nearest to farthest
            System.Array.Sort(raycastHits, 0, hitCount, RaycastHitComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = raycastHits[i];
                if (candidate.collider == null || candidate.transform.IsChildOf(transform))
                    continue;

                // Check if this object or its parent has an interactable
                bool isInteractableLayer = (interactableLayer.value & (1 << candidate.collider.gameObject.layer)) != 0;
                IInteractable interactable = null;
                if (isInteractableLayer)
                {
                    interactable = candidate.collider.GetComponent<IInteractable>() ?? candidate.collider.GetComponentInParent<IInteractable>();
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

                // If this is a solid physical collider (not a trigger) without an interaction,
                // it is an obstacle (e.g. wall, door) that blocks line of sight. Stop checking further.
                if (!candidate.collider.isTrigger)
                {
                    break;
                }
                // Immaterial triggers without active interactions do not block rays to objects behind them.
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

    private class RaycastHitComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly RaycastHitComparer Instance = new RaycastHitComparer();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
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
