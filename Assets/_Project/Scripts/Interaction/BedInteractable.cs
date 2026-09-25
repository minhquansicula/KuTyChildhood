using UnityEngine;

public class BedInteractable : InteractableBase
{
    private BedWakeUpController wakeUpController;

    private void Start()
    {
        promptText = "[E] Lên giường";
        isOneTimeUse = false;
        
        GameObject manager = GameObject.Find("BedWakeUpManager");
        if (manager != null)
        {
            wakeUpController = manager.GetComponent<BedWakeUpController>();
        }
    }

    protected override void OnInteract()
    {
        if (wakeUpController != null)
        {
            wakeUpController.GetOnBed(false);
        }
    }
}
