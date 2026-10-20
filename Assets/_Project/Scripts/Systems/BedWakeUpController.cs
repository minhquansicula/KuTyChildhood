using System.Collections;
using UnityEngine;
using TMPro;

public class BedWakeUpController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptText;

    [Header("Bed Exit")]
    [Tooltip("Scene marker used for getting out of bed. Move this object to move the exit location.")]
    [SerializeField] private Transform bedExitPoint;
    [Tooltip("World-space offset from the marker, keeping the player beside its collider.")]
    [SerializeField] private Vector3 bedExitOffset = new Vector3(0.55f, 0f, 0f);
    [Tooltip("Fallback distance to the right of the bed when no exit marker is assigned.")]
    [SerializeField, Min(0f)] private float exitRightDistance = 0.55f;

    private FirstPersonController playerController;
    private bool canWakeUp = false;

    private void Start()
    {
        // Automatically get on bed when the scene starts
        GetOnBed(true);
    }

    public void GetOnBed(bool isInitial)
    {
        canWakeUp = false;
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
            
            // Read the marker's current position (or find xuonggiuong if not assigned)
            Transform exitTarget = bedExitPoint;
            if (exitTarget == null)
            {
                GameObject xg = GameObject.Find("xuonggiuong") ?? GameObject.Find("BedExitPlatform");
                if (xg != null) exitTarget = xg.transform;
            }

            if (exitTarget != null)
            {
                Vector3 exitPos = exitTarget.position + bedExitOffset;
                // Keep feet grounded at floor level if pivot is below ground
                if (exitPos.y < 0.05f) exitPos.y = 0.05f;

                Quaternion exitRot = exitTarget.rotation;
                // If marker has 3D tilt (e.g. FBX model root with X=270), keep upright yaw facing the bed
                GameObject bed = GameObject.Find("BedBlock");
                if (bed != null)
                {
                    Vector3 lookDir = bed.transform.position - exitPos;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        exitRot = Quaternion.LookRotation(lookDir);
                    }
                    else
                    {
                        exitRot = Quaternion.Euler(0f, 180f, 0f);
                    }
                }
                else if (Mathf.Abs(exitRot.eulerAngles.x) > 1f || Mathf.Abs(exitRot.eulerAngles.z) > 1f)
                {
                    exitRot = Quaternion.Euler(0f, exitRot.eulerAngles.y, 0f);
                }

                playerController.TeleportTo(exitPos, exitRot);
            }
            else
            {
                GameObject bed = GameObject.Find("BedBlock");
                if (bed != null)
                {
                    Vector3 bedPos = bed.transform.position;
                    playerController.TeleportTo(new Vector3(bedPos.x + exitRightDistance, 0.05f, bedPos.z), Quaternion.Euler(0f, 150f, 0f));
                }
            }
        }

        // Restore input
        GameManager.Instance?.ReleaseInput(this);

        // Hiển thị Checklist
        KitchenFlowManager.Instance?.ShowChecklist();

        // We don't disable the GameObject anymore, because they might want to use it again!
    }
}
