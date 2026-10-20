using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    private GameObject promptCard;
    private bool canWakeUp = false;
    public bool CanPauseWhileWaiting => canWakeUp &&
        (ShopUI.Instance == null || !ShopUI.Instance.IsOpen) &&
        (JournalUI.Instance == null || !JournalUI.Instance.IsOpen) &&
        (DialogueUI.Instance == null || !DialogueUI.Instance.IsShowing);

    private void Start()
    {
        StylePrompt();
        // Automatically get on bed when the scene starts
        GetOnBed(true);
        // A saved chapter resumes beside the bed with the next chore already selected.
        if (Scene2Checkpoint.HomeChores > 0) GetOutOfBed();
    }

    public void GetOnBed(bool isInitial)
    {
        canWakeUp = false;
        SetPromptVisible(false);

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
            promptText.text = "<color=#F4C66A><b>E</b></color>   Xuống giường";
            SetPromptVisible(true);
        }

        canWakeUp = true;
    }

    private void Update()
    {
        bool modalOpen = GameManager.Instance != null && GameManager.Instance.IsPaused;
        modalOpen |= ShopUI.Instance != null && ShopUI.Instance.IsOpen;
        modalOpen |= DialogueUI.Instance != null && DialogueUI.Instance.IsShowing;
        if (canWakeUp) SetPromptVisible(!modalOpen);
        if (canWakeUp && !modalOpen && Input.GetKeyDown(KeyCode.E))
        {
            GetOutOfBed();
        }
    }

    private void StylePrompt()
    {
        if (promptText == null) return;
        var canvas = promptText.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        canvas.sortingOrder = 12; // Gameplay hint sits below modal and task canvases.
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
        }

        promptCard = new GameObject("WakePromptCard", typeof(RectTransform), typeof(Image));
        promptCard.transform.SetParent(canvas.transform, false);
        var cardRect = (RectTransform)promptCard.transform;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, 0f);
        cardRect.pivot = new Vector2(.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 42f);
        cardRect.sizeDelta = new Vector2(320f, 62f);
        var image = promptCard.GetComponent<Image>();
        image.color = new Color(.055f, .105f, .13f, .93f);
        image.raycastTarget = false;

        promptText.transform.SetParent(promptCard.transform, false);
        var textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 5f);
        textRect.offsetMax = new Vector2(-14f, -5f);
        promptText.fontSize = 26f;
        promptText.fontStyle = FontStyles.Normal;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(.95f, .97f, .96f);
        promptText.raycastTarget = false;
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText != null && promptText.gameObject.activeSelf != visible)
            promptText.gameObject.SetActive(visible);
        if (promptCard != null && promptCard.activeSelf != visible)
            promptCard.SetActive(visible);
    }

    private void GetOutOfBed()
    {
        StopAllCoroutines();
        canWakeUp = false;
        
        // Hide prompt
        SetPromptVisible(false);
        
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
