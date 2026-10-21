using UnityEngine;

public class OfficeInteractable : InteractableBase, IProximityInteractable
{
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private OfficeInteractionKind kind;
    public OfficeInteractionKind Kind => kind;
    private void Reset() { isOneTimeUse = false; }
    public override string GetPromptText() => office != null ? office.PromptFor(kind) : "";
    protected override void OnInteract() { office?.HandleInteraction(kind); }

    // The desk can occlude the seated boss from the camera ray. Allow handing
    // over paper from the visitor side nearby, well inside the boss's room.
    public bool TryGetProximityDistance(Vector3 playerPosition, out float distance)
    {
        Vector3 offset = playerPosition - transform.position;
        distance = new Vector2(offset.x, offset.z).magnitude;
        return kind == OfficeInteractionKind.Boss && office != null &&
            office.Phase == OfficePhase.DeliverReport &&
            Mathf.Abs(offset.y) <= 1.5f && distance <= 2.2f;
    }
}
