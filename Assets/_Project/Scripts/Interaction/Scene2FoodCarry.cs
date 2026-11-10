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

    private Scene2FoodInteractable held;
    private Transform view;
    private bool moving;
    private Transform diningTable;
    private Camera placementCamera;
    private Material placementPreview;
    private MeshFilter[] previewMeshes;
    private Vector3 footprintCenter, footprintExtents;
    private int placementFrame;

    // Current placement hit info cached during raycast
    private bool hasValidPlacement;
    private Vector3 currentPlacementPos;
    private Quaternion currentPlacementRot;
    private Transform currentPlacementParent;
    private string currentPlacementPrompt = "";

    private static readonly string[] RequiredFoodNames = { "Canh", "ThitKho", "RauXao", "noicom" };
    private readonly Scene2FoodInteractable[] requiredFoods = new Scene2FoodInteractable[RequiredFoodNames.Length];

    public bool IsHolding => held != null;
    internal bool IsHandlingItem => IsHolding || moving;
    public bool CanPickUp => held == null && !moving;
    public Scene2FoodInteractable HeldItem => held;
    public Transform MealDeliveryTarget
    {
        get
        {
            if (IsHolding) return Array.IndexOf(requiredFoods, held) >= 0 ? diningTable : null;
            foreach (var food in requiredFoods)
                if (food != null && !IsDiningTable(food.PlacementSurface)) return food.transform;
            return null;
        }
    }

    public event Action<Scene2FoodInteractable, Vector3> FoodPlaced;

    private void Awake()
    {
        var fpc = GetComponent<FirstPersonController>();
        if (fpc != null && fpc.PlayerCamera != null)
            view = fpc.PlayerCamera;
        else if (Camera.main != null)
            view = Camera.main.transform;
        if (view != null) placementCamera = view.GetComponent<Camera>();
    }

    private void Start()
    {
        diningTable = diningTableCollider != null ? diningTableCollider.transform :
            GameObject.Find("bandai_khach")?.transform;
        foreach (var food in FindObjectsOfType<Scene2FoodInteractable>(true))
        {
            int index = Array.IndexOf(RequiredFoodNames, food.gameObject.name);
            if (index >= 0) requiredFoods[index] = food;
        }
    }

    private void LateUpdate()
    {
        DrawPlacementPreview();
        var flow = HouseholdChoreFlow.Instance;
        if (moving || flow == null || !flow.IsCurrent(HouseholdChore.SetTable) ||
            CountDelivered() != requiredFoods.Length) return;

        if (flow.Complete(HouseholdChore.SetTable))
            KitchenFlowManager.Instance?.MarkTaskCompleted("DiningTable");
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
        if (!CanPickUp || !CanReach(food)) return;
        held = food;
        food.PlacementSurface = null;
        Bounds footprint = food.Target.bounds;
        footprintCenter = footprint.center - food.transform.position;
        footprintExtents = footprint.extents;
        food.Target.enabled = false;
        previewMeshes = food.GetComponentsInChildren<MeshFilter>();

        Transform item = food.transform;
        item.SetParent(view, true);

        Vector3 targetLocalPos = food.CustomHoldOffset != Vector3.zero ? food.CustomHoldOffset : defaultHoldOffset;
        Quaternion uprightLocalRot = Quaternion.Inverse(Quaternion.Euler(0f, view.eulerAngles.y, 0f)) * food.InitialWorldRotation;

        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        StartCoroutine(MoveRoutine(food, targetLocalPos, uprightLocalRot, false, null));
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
        float nearestDist = float.PositiveInfinity;
        RaycastHit bestHit = default;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = placementHits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(held.transform))
                continue;

            if (hit.distance < nearestDist)
            {
                nearestDist = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        bool isDiningTable = TryGetTabletopPoint(ray, dist, bestHit, out Vector3 point);
        if (!isDiningTable)
        {
            if (!found || IsDiningTable(bestHit.collider.transform) || bestHit.normal.y < .35f ||
                bestHit.collider.GetComponentInParent<Scene2FoodInteractable>() != null) return false;
            point = bestHit.point;
        }
        string itemName = !string.IsNullOrEmpty(held.FoodName) ? held.FoodName : "món ăn";
        currentPlacementPrompt = isDiningTable ? $"[E] Đặt {itemName} lên bàn dài phòng khách" : $"[E] Đặt {itemName}";

        currentPlacementPos = point + Vector3.up * (held.BottomOffset + 0.003f);
        currentPlacementRot = held.InitialWorldRotation;
        currentPlacementParent = isDiningTable ? diningTable : bestHit.collider.transform;

        hasValidPlacement = true;
        placementFrame = Time.frameCount;
        return true;
    }

    private bool TryGetTabletopPoint(Ray ray, float distance, RaycastHit nearest, out Vector3 point)
    {
        point = default;
        if (diningTableCollider == null || ray.direction.y >= 0f) return false;
        Bounds bounds = diningTableCollider.bounds;
        var tabletop = new Plane(Vector3.up, new Vector3(0f, bounds.max.y, 0f));
        if (!tabletop.Raycast(ray, out float depth) || depth > distance ||
            (nearest.collider != null && !IsDiningTable(nearest.collider.transform) &&
             !IsServedDish(nearest.collider) &&
             nearest.distance < depth)) return false;
        point = ray.GetPoint(depth);
        if (point.x < bounds.min.x || point.x > bounds.max.x ||
            point.z < bounds.min.z || point.z > bounds.max.z) return false;

        int slot = Array.IndexOf(requiredFoods, held);
        if (slot >= 0)
        {
            // Two pairs frame the letter in the middle of the table.
            bool lengthAlongX = bounds.size.x >= bounds.size.z;
            float row = slot < 2 ? -.24f : .24f;
            float column = slot % 2 == 0 ? -.23f : .23f;
            point.x = bounds.center.x + bounds.size.x * (lengthAlongX ? row : column) - footprintCenter.x;
            point.z = bounds.center.z + bounds.size.z * (lengthAlongX ? column : row) - footprintCenter.z;
        }

        // Keep the whole dish on the table even when aiming at an edge.
        Vector3 extents = footprintExtents;
        extents.x = Mathf.Min(extents.x, bounds.extents.x);
        extents.z = Mathf.Min(extents.z, bounds.extents.z);
        point.x = Mathf.Clamp(point.x + footprintCenter.x, bounds.min.x + extents.x, bounds.max.x - extents.x) - footprintCenter.x;
        point.z = Mathf.Clamp(point.z + footprintCenter.z, bounds.min.z + extents.z, bounds.max.z - extents.z) - footprintCenter.z;
        return true;
    }

    private bool IsServedDish(Collider collider)
    {
        var dish = collider.GetComponentInParent<Scene2FoodInteractable>();
        return dish != null && IsDiningTable(dish.PlacementSurface);
    }

    private void DrawPlacementPreview()
    {
        if (!hasValidPlacement || placementFrame != Time.frameCount || held == null ||
            placementCamera == null || previewMeshes == null) return;
        if (placementPreview == null)
        {
            var shader = Resources.Load<Shader>("FoodPlacementPreview");
            if (shader == null) return;
            placementPreview = new Material(shader);
        }
        Matrix4x4 destination = Matrix4x4.TRS(currentPlacementPos, currentPlacementRot, held.transform.lossyScale)
            * held.transform.worldToLocalMatrix;
        foreach (var filter in previewMeshes)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Matrix4x4 matrix = destination * filter.transform.localToWorldMatrix;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                Graphics.DrawMesh(mesh, matrix, placementPreview, gameObject.layer, placementCamera, submesh,
                    null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
    }

    private void OnDestroy()
    {
        if (placementPreview != null) Destroy(placementPreview);
    }

    private bool IsDiningTable(Transform surface) =>
        diningTable != null && surface != null && surface.IsChildOf(diningTable);

    private int CountDelivered()
    {
        int count = 0;
        foreach (var food in requiredFoods)
            if (food != null && IsDiningTable(food.PlacementSurface)) count++;
        return count;
    }

    // ========== IInteractable Implementation ==========
    public string GetPromptText()
    {
        return hasValidPlacement ? currentPlacementPrompt : "";
    }

    public void Interact()
    {
        if (!hasValidPlacement || view == null || !UpdatePlacementRay(new Ray(view.position, view.forward), reachDistance))
            return;
        PlaceHeldItem(currentPlacementPos, currentPlacementRot, currentPlacementParent);
    }

    public void PlaceHeldItem(Vector3 worldPos, Quaternion worldRot, Transform hitSurface)
    {
        if (!IsHolding || moving) return;
        var food = held;
        held = null;
        hasValidPlacement = false;

        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);

        StartCoroutine(MoveRoutine(food, worldPos, worldRot, true, hitSurface));
    }

    private IEnumerator MoveRoutine(Scene2FoodInteractable food, Vector3 targetPos, Quaternion targetRot, bool placing, Transform hitSurface)
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

            food.PlacementSurface = hitSurface;
            moving = false;
            FoodPlaced?.Invoke(food, targetPos);
            var flow = HouseholdChoreFlow.Instance;
            if (flow != null && flow.IsCurrent(HouseholdChore.SetTable) && IsDiningTable(hitSurface) &&
                Array.IndexOf(requiredFoods, food) >= 0)
            {
                int delivered = CountDelivered();
                if (delivered < requiredFoods.Length)
                    flow.UI?.Notify($"Đã dọn {delivered}/{requiredFoods.Length} món lên bàn dài phòng khách.");
            }
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
}
