using System.Collections;
using UnityEngine;
using TMPro;

public class BedWakeUpController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptText;

    private FirstPersonController playerController;
    private bool canWakeUp = false;

    private void Start()
    {
        // Automatically get on bed when the scene starts
        GetOnBed(true);
    }

    public void GetOnBed(bool isInitial)
    {
        // Ensure prompt is hidden
        if (promptText != null) promptText.gameObject.SetActive(false);

        // Find player if not cached
        if (playerController == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null) playerController = playerObj.GetComponent<FirstPersonController>();
        }

        if (playerController != null)
        {
            // Lock movement & input
            GameManager.Instance?.AcquireInput(this);
            
            // Teleport player to bed position dynamically
            GameObject bed = GameObject.Find("BedBlock");
            if (bed != null)
            {
                Vector3 bedPos = bed.transform.position;
                playerController.TeleportTo(new Vector3(bedPos.x, bedPos.y + 0.3f, bedPos.z), Quaternion.identity);
            }

            // Look up at the ceiling
            playerController.SetPitch(-90f);
        }

        // Start wait sequence
        StopAllCoroutines();
        StartCoroutine(WakeUpSequence());
    }

    private IEnumerator WakeUpSequence()
    {
        // Wait 1 second (realtime in case game pauses on unfocus)
        yield return new WaitForSecondsRealtime(1f);

        // Show prompt
        if (promptText != null)
        {
            promptText.text = "[E] Xuống giường";
            promptText.gameObject.SetActive(true);
        }

        canWakeUp = true;
    }

    private void Update()
    {
        if (canWakeUp && Input.GetKeyDown(KeyCode.E))
        {
            GetOutOfBed();
        }
    }

    private void GetOutOfBed()
    {
        canWakeUp = false;
        
        // Hide prompt
        if (promptText != null) promptText.gameObject.SetActive(false);
        
        // Restore camera rotation slightly
        if (playerController != null)
        {
            playerController.SetPitch(0f);
            
            // Teleport slightly to the side of the bed so they don't get stuck interacting immediately
            GameObject bed = GameObject.Find("BedBlock");
            if (bed != null)
            {
                Vector3 bedPos = bed.transform.position;
                playerController.TeleportTo(new Vector3(bedPos.x + 1.5f, 0.05f, bedPos.z), Quaternion.identity);
            }
        }

        // Restore input
        GameManager.Instance?.ReleaseInput(this);

        // Hiển thị Checklist
        KitchenFlowManager.Instance?.ShowChecklist();

        // We don't disable the GameObject anymore, because they might want to use it again!
    }
}
