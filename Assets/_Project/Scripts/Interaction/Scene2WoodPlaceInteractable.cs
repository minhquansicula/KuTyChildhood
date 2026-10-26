using UnityEngine;

/// <summary>The actual spot beside the stove uses the shared E input owner.</summary>
public sealed class Scene2WoodPlaceInteractable : InteractableBase, IProximityInteractable
{
    public Scene2RadioKitchenSequence sequence;
    public override string GetPromptText() => sequence && sequence.CanPlaceWood() ? "[E] Đặt bó củi cạnh bếp" : "";
    protected override void OnInteract() => sequence?.PlaceWood();
    public bool TryGetProximityDistance(Vector3 playerPosition, out float distance)
    {
        distance = Vector3.Distance(playerPosition, transform.position);
        return sequence && sequence.CanPlaceWood();
    }
}
