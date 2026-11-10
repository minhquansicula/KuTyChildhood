using UnityEngine;

public sealed class Scene2FoodInteractable : InteractableBase
{
    [SerializeField] private string foodName;
    [SerializeField] private Scene2FoodCarry carrier;
    private Scene2FoodInteractable foodForSlot;

    internal Collider Target { get; private set; }
    internal Scene2FoodInteractable Slot { get; private set; }
    internal int OriginalSiblingIndex { get; private set; }

    private void Awake() => Target = GetComponent<Collider>();

    private void Start()
    {
        if (foodForSlot != null || carrier == null || !(Target is MeshCollider mesh)) return;
        OriginalSiblingIndex = transform.GetSiblingIndex();
        var slotObject = new GameObject(name + " Placement");
        slotObject.layer = gameObject.layer;
        slotObject.transform.SetParent(transform.parent, false);
        slotObject.transform.SetLocalPositionAndRotation(transform.localPosition, transform.localRotation);
        slotObject.transform.localScale = transform.localScale;
        var trigger = slotObject.AddComponent<MeshCollider>();
        trigger.sharedMesh = mesh.sharedMesh;
        trigger.convex = true;
        trigger.isTrigger = true;
        trigger.enabled = false;
        Slot = slotObject.AddComponent<Scene2FoodInteractable>();
        Slot.carrier = carrier;
        Slot.foodForSlot = this;
    }

    public override string GetPromptText()
    {
        if (carrier == null || !carrier.CanUse(Target)) return "";
        if (foodForSlot != null)
            return carrier.IsHoldingItem(foodForSlot) ? $"[E] Đặt {foodForSlot.foodName}" : "";
        return Slot != null && carrier.CanPickUp ? $"[E] Cầm {foodName}" : "";
    }

    protected override void OnInteract()
    {
        if (string.IsNullOrEmpty(GetPromptText())) return;
        if (foodForSlot != null) carrier.Place(foodForSlot);
        else carrier.PickUp(this);
    }
}
