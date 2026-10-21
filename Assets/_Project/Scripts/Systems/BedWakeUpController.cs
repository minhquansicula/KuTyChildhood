using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BedWakeUpController : MonoBehaviour
{
    private const float ExitCameraPitch = -8f;

    [Header("UI References")]
    public TextMeshProUGUI promptText;

    [Header("Bed Exit")]
    [Tooltip("Scene marker used for getting out of bed. Move this object to move the exit location.")]
    [SerializeField] private Transform bedExitPoint;
    [Tooltip("World-space offset from the marker, keeping the player beside its collider.")]
    [SerializeField] private Vector3 bedExitOffset = new Vector3(0.55f, 0f, 0f);
    [Tooltip("Fallback distance to the right of the bed when no exit marker is assigned.")]
    [SerializeField, Min(0f)] private float exitRightDistance = 0.55f;

    [Header("Wake-up cinematic")]
    [SerializeField, Min(0.5f)] private float wakingDuration = 4.2f;
    [SerializeField, Min(0.3f)] private float risingDuration = 0.85f;
    [SerializeField] private Vector3 lyingCameraOffset = new Vector3(-0.12f, -0.78f, -0.08f);

    private FirstPersonController playerController;
    private GameObject promptCard;
    private Transform playerCamera;
    private Vector3 standingCameraPosition;
    private Camera viewCamera;
    private float normalFieldOfView;
    private GameObject visionCanvas;
    private Image visionHaze;
    private Image visionBlack;
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

            playerCamera = playerController.PlayerCamera;
            if (playerCamera != null)
            {
                standingCameraPosition = playerCamera.localPosition;
                viewCamera = playerCamera.GetComponent<Camera>();
                if (viewCamera != null) normalFieldOfView = viewCamera.fieldOfView;
            }
            
            // Teleport player to bed position dynamically
            GameObject bed = GameObject.Find("BedBlock");
            if (bed != null)
            {
                Vector3 bedPos = bed.transform.position;
                playerController.TeleportTo(new Vector3(bedPos.x, bedPos.y + 0.3f, bedPos.z), Quaternion.identity);
            }

            // Look up at the ceiling
            playerController.SetPitch(-85f);
            if (playerCamera != null) playerCamera.localPosition = standingCameraPosition + lyingCameraOffset;
        }

        // Start wait sequence
        StopAllCoroutines();
        if (isInitial && playerCamera != null)
            CreateVisionOverlay();
        else
            ClearVisionOverlay();
        StartCoroutine(WakeUpSequence(isInitial));
    }

    private IEnumerator WakeUpSequence(bool cinematic)
    {
        if (cinematic && playerCamera != null)
        {
            float elapsed = 0f;
            while (elapsed < wakingDuration)
            {
                if (GameManager.Instance == null || !GameManager.Instance.IsPaused)
                    elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / wakingDuration);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - .2f) / 1.25f));
                float blink = Mathf.Sin(Mathf.PI * Mathf.Clamp01((elapsed - 1.65f) / .38f));
                blink = elapsed >= 1.65f && elapsed <= 2.03f ? blink * .72f : 0f;
                SetVision(Mathf.Clamp01(fade + blink), .16f * (1f - t));

                float dizziness = (1f - t) * (1f - t);
                float pitch = Mathf.Lerp(-85f, -77f, Mathf.SmoothStep(0f, 1f, t)) +
                    Mathf.Sin(elapsed * 4.1f) * 2.4f * dizziness;
                float roll = Mathf.Sin(elapsed * 3.4f + .7f) * 5.5f * dizziness;
                playerController.SetPitch(pitch);
                playerCamera.localRotation *= Quaternion.Euler(0f, 0f, roll);
                playerCamera.localPosition = standingCameraPosition + lyingCameraOffset +
                    new Vector3(Mathf.Sin(elapsed * 2.3f) * .025f, Mathf.Sin(elapsed * 5f) * .012f, 0f) * dizziness;
                if (viewCamera != null)
                    viewCamera.fieldOfView = normalFieldOfView - 5f * dizziness +
                        Mathf.Sin(elapsed * 3f) * 1.6f * dizziness;
                yield return null;
            }
            playerController.SetPitch(-77f);
            playerCamera.localPosition = standingCameraPosition + lyingCameraOffset;
            if (viewCamera != null) viewCamera.fieldOfView = normalFieldOfView;
            SetVision(0f, 0f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1f);
        }

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
        
        if (playerController != null && playerCamera != null)
        {
            if (visionCanvas == null)
            {
                CreateVisionOverlay();
                SetVision(0f, 0f);
            }
            StartCoroutine(RiseFromBed());
            return;
        }
        FinishGettingOutOfBed();
        CompleteExit();
    }

    private IEnumerator RiseFromBed()
    {
        Vector3 fromPosition = playerCamera.localPosition;
        Quaternion fromRotation = playerCamera.localRotation;
        float elapsed = 0f;
        while (elapsed < risingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / risingDuration));
            playerCamera.localPosition = Vector3.Lerp(fromPosition, standingCameraPosition, t);
            playerCamera.localRotation = Quaternion.Slerp(fromRotation, Quaternion.Euler(ExitCameraPitch, 0f, 0f), t);
            SetVision(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .78f) / .22f)), 0f);
            yield return null;
        }
        FinishGettingOutOfBed();
        SetVision(1f, 0f);
        float fade = 0f;
        while (fade < .18f)
        {
            fade += Time.deltaTime;
            SetVision(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fade / .18f)), 0f);
            yield return null;
        }
        ClearVisionOverlay();
        CompleteExit();
    }

    private void FinishGettingOutOfBed()
    {
        if (playerController != null)
        {
            if (playerCamera != null) playerCamera.localPosition = standingCameraPosition;
            if (viewCamera != null) viewCamera.fieldOfView = normalFieldOfView;

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

                // Keep the direction reached by the camera animation when moving
                // the player beside the bed; the marker only provides a position.
                Quaternion facing = Quaternion.Euler(0f, playerController.transform.eulerAngles.y, 0f);
                playerController.TeleportTo(exitPos, facing);
            }
            else
            {
                GameObject bed = GameObject.Find("BedBlock");
                if (bed != null)
                {
                    Vector3 bedPos = bed.transform.position;
                    Quaternion facing = Quaternion.Euler(0f, playerController.transform.eulerAngles.y, 0f);
                    playerController.TeleportTo(new Vector3(bedPos.x + exitRightDistance, 0.05f, bedPos.z), facing);
                }
            }
            // TeleportTo resets the controller's stored pitch; restore the angle
            // reached by the rise animation before mouse-look resumes.
            playerController.SetPitch(ExitCameraPitch);
        }

    }

    private void CompleteExit()
    {
        GameManager.Instance?.ReleaseInput(this);
        KitchenFlowManager.Instance?.ShowChecklist();
    }

    private void CreateVisionOverlay()
    {
        ClearVisionOverlay();
        visionCanvas = new GameObject("WakeVision", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        visionCanvas.transform.SetParent(transform, false);
        var canvas = visionCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        visionHaze = CreateVisionImage("SleepHaze", new Color(1f, .82f, .62f, 0f));
        visionBlack = CreateVisionImage("ClosedEyes", Color.black);
        SetVision(1f, 0f);
    }

    private Image CreateVisionImage(string name, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(visionCanvas.transform, false);
        var rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void SetVision(float black, float haze)
    {
        if (visionBlack != null) visionBlack.color = new Color(0f, 0f, 0f, Mathf.Clamp01(black));
        if (visionHaze != null) visionHaze.color = new Color(1f, .82f, .62f, Mathf.Clamp01(haze));
    }

    private void ClearVisionOverlay()
    {
        if (visionCanvas != null) Destroy(visionCanvas);
        visionCanvas = null;
        visionBlack = null;
        visionHaze = null;
    }
}
