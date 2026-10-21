using UnityEngine;

/// <summary>Routes the existing Blockout door through the shared PlayerInteraction raycast.</summary>
public sealed class OfficeBlockoutDoorInteractable : InteractableBase, IProximityInteractable
{
    [SerializeField] private OfficeIntroController intro;
    [SerializeField] private OfficeSceneController office;

    public override string GetPromptText()
    {
        return intro != null ? intro.GetDoorPrompt() : string.Empty;
    }

    public bool TryGetProximityDistance(Vector3 playerPosition, out float distance)
    {
        Vector3 offset = playerPosition - transform.position;
        distance = new Vector2(offset.x, offset.z).magnitude;
        return Mathf.Abs(offset.y) <= 1.5f && distance <= 2f;
    }

    protected override void OnInteract()
    {
        intro?.TryOpenDoor();
    }
}
