using UnityEngine;

/// <summary>
/// Uses the intro seating transition once, then hands later sit/rest interactions to
/// OfficeSceneController without adding a second input reader.
/// </summary>
public sealed class OfficeBlockoutChairInteractable : InteractableBase, IProximityInteractable
{
    [SerializeField] private OfficeIntroController intro;
    [SerializeField] private OfficeSceneController office;

    public override string GetPromptText()
    {
        if (intro != null && !intro.GameplayHandoffComplete)
            return intro.GetChairPrompt();
        return office != null ? office.PromptFor(OfficeInteractionKind.Chair) : string.Empty;
    }

    public bool TryGetProximityDistance(Vector3 playerPosition, out float distance)
    {
        Vector3 offset = playerPosition - transform.position;
        distance = new Vector2(offset.x, offset.z).magnitude;
        return Mathf.Abs(offset.y) <= 1.5f && distance <= 1.6f;
    }

    protected override void OnInteract()
    {
        if (intro != null && !intro.GameplayHandoffComplete) intro.TrySitAtDesk();
        else office?.HandleInteraction(OfficeInteractionKind.Chair);
    }
}
