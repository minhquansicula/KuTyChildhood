using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 1 opening only. It layers office audio, fades into the meeting-room view,
/// presents the existing subtitle UI, and returns control to OfficeSceneController.
/// It deliberately does not own or change any desk-task gameplay.
/// </summary>
public sealed class OfficeOpeningSequence : MonoBehaviour
{
    [Header("Existing player systems")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private OfficeSceneController officeController;

    [Header("Fade and existing HUD")]
    [SerializeField] private CanvasGroup blackFade;
    [SerializeField] private CanvasGroup objectiveCanvasGroup;
    [SerializeField] private GameObject[] hideDuringOpening;

    [Header("Layered ambience")]
    [SerializeField] private AudioSource officeAmbient;
    [SerializeField] private AudioSource airConditioner;
    [SerializeField] private AudioSource trafficOutside;
    [SerializeField] private AudioSource keyboardDistant;
    [SerializeField] private AudioClip officeAmbientClip;
    [SerializeField] private AudioClip airConditionerClip;
    [SerializeField] private AudioClip trafficClip;
    [SerializeField] private AudioClip keyboardClip;

    [Header("Dialogue")]
    [SerializeField] private AudioSource bossVoice;
    [SerializeField] private AudioSource playerVoice;
    [SerializeField] private AudioSource deskSlamSource;
    [SerializeField] private AudioClip bossDialogueClip;
    [SerializeField] private AudioClip playerResponseClip;
    [SerializeField] private AudioClip deskSlamClip;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private string bossGestureTrigger = "Reprimand";

    [Header("Timing")]
    [SerializeField, Min(3f)] private float blackScreenDuration = 4f;
    [SerializeField, Min(.1f)] private float fadeDuration = 2.1f;
    [SerializeField, Min(0f)] private float bossStartDelay = .75f;
    [SerializeField, Min(0f)] private float deskSlamLeadSeconds = .35f;
    [SerializeField, Min(0f)] private float dialoguePause = .18f;
    [SerializeField, Min(0f)] private float playerResponseDelay = .7f;
    [SerializeField, Min(0f)] private float controlReturnDelay = 1.25f;
    [SerializeField, Min(0f)] private float objectiveDelay = 1f;
    [SerializeField, Min(.1f)] private float objectiveFadeDuration = .6f;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float officeAmbientVolume = .12f;
    [SerializeField, Range(0f, 1f)] private float airConditionerVolume = .2f;
    [SerializeField, Range(0f, 1f)] private float trafficVolume = .34f;
    [SerializeField, Range(0f, 1f)] private float keyboardVolume = .1f;
    [SerializeField, Range(0f, 1f)] private float bossVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float playerVolume = .55f;

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }

    private Coroutine sequenceRoutine;
    private Vector3 cameraStartPosition;
    private Quaternion cameraStartRotation;

    private void Awake()
    {
        SetBlack(1f);
        SetObjectiveAlpha(0f);
        SetOpeningHudVisible(false);
        if (playerCamera != null)
        {
            cameraStartPosition = playerCamera.localPosition;
            cameraStartRotation = playerCamera.localRotation;
        }
    }

    public bool Play(OfficeSceneController owner)
    {
        if (IsRunning || IsComplete) return false;
        if (owner != null) officeController = owner;
        if (officeController == null || playerController == null || playerCamera == null)
        {
            Debug.LogWarning("[OfficeOpeningSequence] Required references are missing; using controller fallback.", this);
            return false;
        }

        IsRunning = true;
        sequenceRoutine = StartCoroutine(RunOpening());
        return true;
    }

    public void SkipImmediately()
    {
        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        StopAllCoroutines();
        sequenceRoutine = null;
        SetBlack(0f);
        SetOpeningHudVisible(true);
        SetObjectiveAlpha(1f);
        RestoreControl();
        officeController?.CompleteOpeningControlReturn(true);
        IsRunning = false;
        IsComplete = true;
    }

    private IEnumerator RunOpening()
    {
        LockControl();
        PrepareLoop(officeAmbient, officeAmbientClip);
        PrepareLoop(airConditioner, airConditionerClip);
        PrepareLoop(trafficOutside, trafficClip);
        PrepareLoop(keyboardDistant, keyboardClip);
        PrepareDialogueSources();

        StartLoop(officeAmbient);
        StartLoop(airConditioner);
        StartCoroutine(FadeAudio(officeAmbient, officeAmbientVolume, 1.5f));
        StartCoroutine(FadeAudio(airConditioner, airConditionerVolume, 1.6f));

        yield return new WaitForSecondsRealtime(1f);
        StartLoop(trafficOutside);
        StartCoroutine(FadeAudio(trafficOutside, trafficVolume, 1.4f));

        yield return new WaitForSecondsRealtime(1f);
        StartLoop(keyboardDistant);
        StartCoroutine(FadeAudio(keyboardDistant, keyboardVolume, 1.2f));

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, blackScreenDuration - 2f));
        yield return FadeOfficeIntoView();
        yield return new WaitForSecondsRealtime(bossStartDelay);

        if (deskSlamSource != null && deskSlamClip != null) deskSlamSource.PlayOneShot(deskSlamClip);
        TriggerBossGesture();
        yield return new WaitForSecondsRealtime(deskSlamLeadSeconds);
        yield return PlayBossDialogue();

        yield return new WaitForSecondsRealtime(playerResponseDelay);
        float responseSeconds = playerResponseClip != null ? playerResponseClip.length : 2.8f;
        if (playerVoice != null && playerResponseClip != null)
        {
            playerVoice.volume = playerVolume;
            playerVoice.PlayOneShot(playerResponseClip);
        }
        officeController.ShowCinematicSubtitle("QUÂN", "Dạ... dạ, để em sửa và gửi ngay cho sếp ạ.", responseSeconds);
        yield return new WaitForSecondsRealtime(responseSeconds);

        officeController.HideCinematicSubtitle();
        yield return LowerCameraDuringSilence(controlReturnDelay);
        officeController.CompleteOpeningControlReturn(false);
        RestoreControl();
        SetOpeningHudVisible(true);

        yield return new WaitForSecondsRealtime(objectiveDelay);
        officeController.RevealOpeningObjective();
        yield return FadeCanvasGroup(objectiveCanvasGroup, 0f, 1f, objectiveFadeDuration);

        IsRunning = false;
        IsComplete = true;
        sequenceRoutine = null;
    }

    private IEnumerator FadeOfficeIntoView()
    {
        float duration = Mathf.Max(.1f, fadeDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            SetBlack(1f - Mathf.SmoothStep(0f, 1f, t));
            ApplyCameraBreathing(t);
            yield return null;
        }
        SetBlack(0f);
    }

    private IEnumerator PlayBossDialogue()
    {
        string[] lines =
        {
            "BÁO CÁO ĐÂU RỒI?!",
            "TÔI ĐÃ NÓI VỚI CẬU BAO NHIÊU LẦN RỒI?!",
            "CÁI NÀY MÀ CŨNG LÀM SAI ĐƯỢC À?!",
            "CẢ PHÒNG ĐANG CHỜ MỖI MÌNH CẬU ĐẤY!",
            "HÔM NAY KHÔNG XONG THÌ ĐỪNG CÓ VỀ!",
            "LÀM LẠI NGAY CHO TÔI!"
        };
        float[] weights = { .12f, .17f, .17f, .18f, .20f, .16f };
        float total = bossDialogueClip != null ? bossDialogueClip.length : 7.5f;

        if (bossVoice != null && bossDialogueClip != null)
        {
            bossVoice.volume = bossVolume;
            bossVoice.Stop();
            bossVoice.PlayOneShot(bossDialogueClip);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            float seconds = Mathf.Max(.55f, total * weights[i] - dialoguePause);
            officeController.ShowCinematicSubtitle("SẾP", lines[i], seconds + dialoguePause);
            yield return new WaitForSecondsRealtime(seconds + dialoguePause);
        }
    }

    private IEnumerator LowerCameraDuringSilence(float seconds)
    {
        float duration = Mathf.Max(.1f, seconds);
        Vector3 fromPosition = playerCamera.localPosition;
        Quaternion fromRotation = playerCamera.localRotation;
        Vector3 targetPosition = cameraStartPosition + Vector3.down * .045f;
        Quaternion targetRotation = cameraStartRotation * Quaternion.Euler(4f, 0f, 0f);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerCamera.localPosition = Vector3.Lerp(fromPosition, targetPosition, t);
            playerCamera.localRotation = Quaternion.Slerp(fromRotation, targetRotation, t);
            yield return null;
        }
        playerCamera.localPosition = targetPosition;
        playerController.ResetCameraVerticalRotation(4f);
    }

    private void ApplyCameraBreathing(float progress)
    {
        if (playerCamera == null) return;
        float wave = Mathf.Sin(progress * Mathf.PI * 2f);
        playerCamera.localPosition = cameraStartPosition + new Vector3(wave * .003f, wave * .002f, 0f);
        playerCamera.localRotation = cameraStartRotation * Quaternion.Euler(wave * .18f, 0f, wave * .08f);
    }

    private void LockControl()
    {
        GameManager.Instance?.AcquireInput(this);
        playerController.LockMovement(true);
        playerInteraction?.LockInteraction(true);
        HUDController.Instance?.HideInteractPrompt();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RestoreControl()
    {
        playerController.LockMovement(false);
        playerInteraction?.LockInteraction(false);
        GameManager.Instance?.ReleaseInput(this);
    }

    private void PrepareDialogueSources()
    {
        ConfigureOneShot(bossVoice, true);
        ConfigureOneShot(deskSlamSource, true);
        ConfigureOneShot(playerVoice, false);
    }

    private static void ConfigureOneShot(AudioSource source, bool spatial)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatial ? 1f : 0f;
    }

    private static void PrepareLoop(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        source.Stop();
        if (clip != null) source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
    }

    private static void StartLoop(AudioSource source)
    {
        if (source != null && source.clip != null && !source.isPlaying) source.Play();
    }

    private static IEnumerator FadeAudio(AudioSource source, float target, float seconds)
    {
        if (source == null) yield break;
        float start = source.volume;
        float duration = Mathf.Max(.05f, seconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        source.volume = target;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null) yield break;
        float duration = Mathf.Max(.05f, seconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private void TriggerBossGesture()
    {
        if (bossAnimator != null && !string.IsNullOrEmpty(bossGestureTrigger))
            bossAnimator.SetTrigger(bossGestureTrigger);
    }

    private void SetOpeningHudVisible(bool visible)
    {
        if (hideDuringOpening == null) return;
        for (int i = 0; i < hideDuringOpening.Length; i++)
            if (hideDuringOpening[i] != null) hideDuringOpening[i].SetActive(visible);
    }

    private void SetBlack(float alpha)
    {
        if (blackFade == null) return;
        blackFade.alpha = Mathf.Clamp01(alpha);
        blackFade.blocksRaycasts = alpha > .01f;
        blackFade.interactable = false;
    }

    private void SetObjectiveAlpha(float alpha)
    {
        if (objectiveCanvasGroup == null) return;
        objectiveCanvasGroup.alpha = alpha;
        objectiveCanvasGroup.blocksRaycasts = false;
        objectiveCanvasGroup.interactable = false;
    }

    private void OnDestroy()
    {
        if (IsRunning) RestoreControl();
    }
}
