using UnityEngine;

public sealed class Scene2FoodInteractable : InteractableBase
{
    [SerializeField] private string foodName;
    [SerializeField] private Scene2FoodCarry carrier;
    [SerializeField] private Vector3 customHoldOffset;

    public string FoodName => foodName;
    public Vector3 CustomHoldOffset => customHoldOffset;
    internal Collider Target { get; private set; }
    internal float BottomOffset { get; private set; }
    internal Quaternion InitialWorldRotation { get; private set; }
    internal Quaternion InitialFlatRotation { get; private set; }
    internal Vector3 InitialWorldScale { get; private set; }
    internal Transform OriginalParent { get; private set; }
    internal Transform PlacementSurface { get; set; }

    private void Awake()
    {
        Target = GetComponent<Collider>();
        OriginalParent = transform.parent;
        if (carrier == null)
        {
            var player = FindObjectOfType<FirstPersonController>();
            if (player != null) carrier = player.GetComponent<Scene2FoodCarry>();
        }
        CacheDimensions();
    }

    public void CacheDimensions()
    {
        InitialWorldRotation = transform.rotation;
        InitialFlatRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        InitialWorldScale = transform.lossyScale;

        var col = GetComponent<Collider>();
        if (col != null)
        {
            BottomOffset = transform.position.y - col.bounds.min.y;
        }
        else
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                BottomOffset = transform.position.y - rend.bounds.min.y;
        }
    }

    public void SetCarrier(Scene2FoodCarry newCarrier) => carrier = newCarrier;
    public void SetFoodName(string name) => foodName = name;
    public void SetCustomHoldOffset(Vector3 offset) => customHoldOffset = offset;

    public override string GetPromptText()
    {
        if (carrier == null || !carrier.CanPickUp) return "";
        if (!carrier.CanReach(this)) return "";
        string title = !string.IsNullOrEmpty(foodName) ? foodName : gameObject.name;
        return gameObject.name == "LongBan" ? "[E] Lấy lồng bàn" : $"[E] Cầm {title}";
    }

    protected override void OnInteract()
    {
        if (carrier == null || !carrier.CanPickUp) return;
        carrier.PickUp(this);
    }
}
