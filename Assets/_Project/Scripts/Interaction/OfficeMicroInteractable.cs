using UnityEngine;

public enum OfficeMicroInteractionKind
{
    Clock,
    WaterCooler,
    PrinterPaper,
    ColdCoffee,
    ChildhoodMarble
}

/// <summary>
/// A short office interaction. Most are optional; printer and water can advance Scene 1.
/// </summary>
public sealed class OfficeMicroInteractable : InteractableBase
{
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private OfficeMicroInteractionKind kind;
    [SerializeField, Min(0f)] private float cooldownSeconds = 1.25f;
    private float nextUseTime;

    public OfficeMicroInteractionKind Kind => kind;
    private OfficeSceneController Office => office != null ? office : OfficeSceneController.Instance;

    private void Awake()
    {
        // Older versions of the scene serialized the printer as one-shot. It must remain
        // reusable so checking it too early never consumes the main-task interaction.
        if (kind == OfficeMicroInteractionKind.PrinterPaper) isOneTimeUse = false;
    }

    public override string GetPromptText()
    {
        bool requiredWaterStep = kind == OfficeMicroInteractionKind.WaterCooler &&
            Office != null && Office.Phase == OfficePhase.WaterBreak;
        if ((isOneTimeUse && hasBeenUsed) || (!requiredWaterStep && Time.time < nextUseTime)) return string.Empty;
        return Office != null ? Office.PromptForMicro(kind) : string.Empty;
    }

    protected override void OnInteract()
    {
        Office?.HandleMicroInteraction(kind);
        nextUseTime = Time.time + cooldownSeconds;
    }
}
