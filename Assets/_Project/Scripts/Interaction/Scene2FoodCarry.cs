using System;
using System.Collections;
using UnityEngine;

public sealed class Scene2FoodCarry : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField, Min(0.1f)] private float reachDistance = 2.6f;
    [SerializeField] private Vector3 defaultHoldOffset = new Vector3(0.14f, -0.26f, 0.55f);
    [SerializeField] private LayerMask placementLayerMask = ~0;

    [Header("Quest Targets")]
    [SerializeField] private Collider diningTableCollider;
    [SerializeField] private Bounds diningTableBounds;

    private Scene2FoodInteractable held;
    private Transform view;
    private bool moving;
    private bool isQuestCompleted;

    // Current placement hit info cached during raycast
    private bool hasValidPlacement;
    private Vector3 currentPlacementPos;
    private Quaternion currentPlacementRot;
    private Transform currentPlacementParent;
    private string currentPlacementPrompt = "";

    // Track dishes for the quest
    private Scene2FoodInteractable canhDish;
    private Scene2FoodInteractable thitKhoDish;
    private Scene2FoodInteractable rauXaoDish;
    private Scene2FoodInteractable foodCover;

    public bool IsHolding => held != null;
    public bool CanPickUp => held == null && !moving;
    public Scene2FoodInteractable HeldItem => held;

    public event Action<Scene2FoodInteractable, Vector3> FoodPlaced;

    private void Awake()
    {
        var fpc = GetComponent<FirstPersonController>();
        if (fpc != null && fpc.PlayerCamera != null)
            view = fpc.PlayerCamera;
        else if (Camera.main != null)
            view = Camera.main.transform;
    }

    private void Start()
    {
        FindDiningTable();
        FindDishes();
    }

    private void FindDiningTable()
    {
        if (diningTableCollider != null) return;
        var tableObj = GameObject.Find("bandai_khach");
        if (tableObj != null)
        {
            diningTableCollider = tableObj.GetComponent<Collider>();
            if (diningTableCollider != null)
            {
                diningTableBounds = diningTableCollider.bounds;
            }
        }
    }

    private void FindDishes()
    {
        var allFoods = FindObjectsOfType<Scene2FoodInteractable>(true);
        foreach (var food in allFoods)
        {
            string n = food.gameObject.name.ToLower();
            if (n.Contains("canh")) canhDish = food;
            else if (n.Contains("thit")) thitKhoDish = food;
            else if (n.Contains("rau")) rauXaoDish = food;
            else if (n.Contains("cover") || n.Contains("longban")) foodCover = food;
        }
    }

    internal bool CanReach(Scene2FoodInteractable food)
    {
        if (food == null || moving || view == null) return false;
        Collider col = food.Target;
        if (col == null || !col.enabled) return false;
        Vector3 camPos = view.position;
        Vector3 closestPoint = col.ClosestPoint(camPos);
        return Vector3.Distance(camPos, closestPoint) <= reachDistance;
    }

    internal void PickUp(Scene2FoodInteractable food)
    {
        if (!CanPickUp || food == null || moving || view == null) return;
        held = food;
        if (food.Target != null) food.Target.enabled = false;

        Transform item = food.transform;
        Vector3 savedLossyScale = food.InitialWorldScale != Vector3.zero ? food.InitialWorldScale : item.lossyScale;
        item.SetParent(view, true);

        Vector3 targetLocalPos = food.CustomHoldOffset != Vector3.zero ? food.CustomHoldOffset : defaultHoldOffset;
        Quaternion uprightLocalRot = Quaternion.Inverse(Quaternion.Euler(0f, view.eulerAngles.y, 0f)) * food.InitialWorldRotation;

        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        StartCoroutine(MoveRoutine(food, targetLocalPos, uprightLocalRot, savedLossyScale, false, null));
    }

    private readonly RaycastHit[] placementHits = new RaycastHit[16];

    /// <summary>
    /// Called every frame from PlayerInteraction while holding food.
    /// Returns true if looking at a valid upward surface within distance.
    /// </summary>
    public bool UpdatePlacementRay(Ray ray, float maxDist)
    {
        hasValidPlacement = false;
        currentPlacementPrompt = "";
        if (!IsHolding || moving) return false;

        float dist = Mathf.Min(maxDist, reachDistance);
        int hitCount = Physics.RaycastNonAlloc(ray, placementHits, dist, placementLayerMask, QueryTriggerInteraction.Ignore);
        if (hitCount == 0) return false;

        float nearestDist = float.PositiveInfinity;
        RaycastHit bestHit = default;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = placementHits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(held.transform))
                continue;

            // Surface must be facing mostly upward (slope <= ~65 degrees)
            if (hit.normal.y < 0.35f) continue;

            if (hit.distance < nearestDist)
            {
                nearestDist = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        if (!found) return false;

        bool isDiningTable = IsHitOnDiningTable(bestHit);
        string itemName = !string.IsNullOrEmpty(held.FoodName) ? held.FoodName : "món ăn";
        currentPlacementPrompt = isDiningTable ? $"[E] Đặt {itemName} lên bàn ăn" : $"[E] Đặt {itemName}";

        currentPlacementPos = bestHit.point + Vector3.up * (held.BottomOffset + 0.003f);
        currentPlacementRot = held.InitialWorldRotation;
        currentPlacementParent = bestHit.collider.transform;

        hasValidPlacement = true;
        return true;
    }

    private bool IsHitOnDiningTable(RaycastHit hit)
    {
        if (diningTableCollider != null)
        {
            if (hit.collider == diningTableCollider || hit.collider.transform.IsChildOf(diningTableCollider.transform))
                return true;
            if (diningTableBounds.Contains(hit.point))
                return true;
        }
        else
        {
            FindDiningTable();
            if (diningTableCollider != null && (hit.collider == diningTableCollider || diningTableBounds.Contains(hit.point)))
                return true;
        }

        if (hit.collider != null && hit.collider.name.ToLower().Contains("bandai_khach"))
            return true;

        return false;
    }

    // ========== IInteractable Implementation ==========
    public string GetPromptText()
    {
        return hasValidPlacement ? currentPlacementPrompt : "";
    }

    public void Interact()
    {
        if (!IsHolding || !hasValidPlacement || moving) return;
        PlaceHeldItem(currentPlacementPos, currentPlacementRot, currentPlacementParent);
    }

    public void PlaceHeldItem(Vector3 worldPos, Quaternion worldRot, Transform hitSurface)
    {
        if (!IsHolding || moving) return;
        var food = held;
        held = null;
        hasValidPlacement = false;

        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        Vector3 savedLossyScale = food.InitialWorldScale;

        StartCoroutine(MoveRoutine(food, worldPos, worldRot, savedLossyScale, true, hitSurface));
    }

    private IEnumerator MoveRoutine(Scene2FoodInteractable food, Vector3 targetPos, Quaternion targetRot, Vector3 lossyScale, bool placing, Transform hitSurface)
    {
        moving = true;
        Transform item = food.transform;
        const float duration = 0.22f;

        if (placing)
        {
            Transform safeContainer = food.OriginalParent != null ? food.OriginalParent : null;
            item.SetParent(safeContainer, true);

            Vector3 startPos = item.position;
            Quaternion startRot = item.rotation;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                item.position = Vector3.Lerp(startPos, targetPos, t);
                item.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            item.position = targetPos;
            item.rotation = targetRot;

            if (food.Target != null)
            {
                food.Target.enabled = true;
                food.gameObject.layer = LayerMask.NameToLayer("Interactable");
            }

            moving = false;
            FoodPlaced?.Invoke(food, targetPos);

            CheckQuestCompletion(food);
        }
        else
        {
            Vector3 startPos = item.localPosition;
            Quaternion startRot = item.localRotation;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                item.localPosition = Vector3.Lerp(startPos, targetPos, t);
                item.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            item.localPosition = targetPos;
            item.localRotation = targetRot;
            moving = false;
        }
    }

    private bool IsOnDiningTable(Vector3 pos)
    {
        if (diningTableCollider == null) FindDiningTable();
        if (diningTableCollider != null)
        {
            Bounds b = diningTableCollider.bounds;
            bool inXZ = pos.x >= b.min.x - 0.2f && pos.x <= b.max.x + 0.2f &&
                        pos.z >= b.min.z - 0.2f && pos.z <= b.max.z + 0.2f;
            bool inY = pos.y >= b.max.y - 0.25f && pos.y <= b.max.y + 0.65f;
            return inXZ && inY;
        }
        return pos.x >= 7.7f && pos.x <= 9.4f && pos.z >= -4.9f && pos.z <= -2.1f && pos.y >= 0.95f && pos.y <= 1.8f;
    }

    private void CheckQuestCompletion(Scene2FoodInteractable justPlaced)
    {
        if (canhDish == null || thitKhoDish == null || rauXaoDish == null)
            FindDishes();

        bool canhOn = canhDish != null && IsOnDiningTable(canhDish.transform.position);
        bool thitOn = thitKhoDish != null && IsOnDiningTable(thitKhoDish.transform.position);
        bool rauOn = rauXaoDish != null && IsOnDiningTable(rauXaoDish.transform.position);

        int count = (canhOn ? 1 : 0) + (thitOn ? 1 : 0) + (rauOn ? 1 : 0);

        if (canhOn && thitOn && rauOn)
        {
            if (!isQuestCompleted)
            {
                isQuestCompleted = true;
                OnDiningTableQuestCompleted();
            }
        }
        else if (count > 0 && IsOnDiningTable(justPlaced.transform.position) && !isQuestCompleted)
        {
            HUDController.Instance?.ShowInteractPrompt($"<color=#FFFF88>Đã dọn {count}/3 món lên bàn ăn</color>");
        }
    }

    private void OnDiningTableQuestCompleted()
    {
        Debug.Log("[Scene2FoodCarry] All 3 dishes placed on bandai_khach! Marking quest completed.");

        KitchenFlowManager.Instance?.MarkTaskCompleted("DiningTable");

        if (HouseholdChoreFlow.Instance != null)
        {
            if (HouseholdChoreFlow.Instance.IsCurrent(HouseholdChore.SetTable))
            {
                HouseholdChoreFlow.Instance.Complete(HouseholdChore.SetTable);
            }
            else
            {
                HouseholdChoreFlow.Instance.Complete(HouseholdChore.SetTable, false);
            }
        }

        AudioManager.Instance?.PlaySFX(SfxId.UIConfirm);
        HUDController.Instance?.ShowInteractPrompt("<color=#44FF44><b>ĐÃ DỌN ĐỦ 3 MÓN LÊN BÀN ĂN! HOÀN THÀNH NHIỆM VỤ!</b></color>");
        QuestManager.Instance?.RefreshHouseholdObjective();
    }
}
