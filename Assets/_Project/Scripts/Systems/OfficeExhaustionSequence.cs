using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// First-person collapse at the desk. It deliberately avoids a full-body animation so
/// the sequence stays compatible with the current asset set.
/// </summary>
public sealed class OfficeExhaustionSequence : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform deskCollapseTarget;
    [SerializeField] private Image blackout;
    [SerializeField] private Volume moodVolume;
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField, Min(.5f)] private float sleepCollapseDuration = 1.5f;
    [SerializeField, Min(.2f)] private float finalSettleSeconds = .45f;
    [SerializeField] private AnimationCurve headWeight = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private SleepEyelidOverlay eyelids;

    public bool IsRunning { get; private set; }

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;

    private void Awake()
    {
        if (moodVolume != null && moodVolume.sharedProfile != null)
            moodVolume.profile = Instantiate(moodVolume.sharedProfile);
        CacheVolumeEffects();
        SetBlackout(0f);
    }

    public bool Play(Action onReadyForMemoryWorld, float speechSeconds = 0f, Func<bool> speechStillPlaying = null)
    {
        if (IsRunning || cameraTransform == null) return false;
        StartCoroutine(Collapse(onReadyForMemoryWorld, speechSeconds, speechStillPlaying));
        return true;
    }

    private IEnumerator Collapse(Action onReadyForMemoryWorld, float speechSeconds, Func<bool> speechStillPlaying)
    {
        IsRunning = true;
        if (blackout != null)
            eyelids = SleepEyelidOverlay.Create(blackout.transform);
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        Vector3 targetPosition = deskCollapseTarget != null
            ? deskCollapseTarget.position
            : startPosition + cameraTransform.TransformDirection(new Vector3(0.08f, -0.62f, 0.34f));
        Quaternion targetRotation = deskCollapseTarget != null
            ? deskCollapseTarget.rotation
            : startRotation * Quaternion.Euler(25f, 0f, 8f);

        float speechElapsed = 0f;
        float settleElapsed = 0f;
        float leadDuration = Mathf.Max(sleepCollapseDuration, speechSeconds);
        while (settleElapsed < finalSettleSeconds)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) { yield return null; continue; }
            speechElapsed += Time.unscaledDeltaTime;
            bool speaking = speechElapsed < leadDuration || (speechStillPlaying != null && speechStillPlaying());
            if (!speaking) settleElapsed += Time.deltaTime;
            float lead = Mathf.Clamp01(speechElapsed / leadDuration);
            float settle = Mathf.Clamp01(settleElapsed / finalSettleSeconds);
            float t = speaking ? .32f * headWeight.Evaluate(lead) : Mathf.Lerp(.32f, 1f, headWeight.Evaluate(settle));
            float eased = t;
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            ApplyMood(t * .8f);
            atmosphere?.SetMuffle(Mathf.Lerp(0f, 1f, t));
            float closed = speaking ? .12f * lead : Mathf.Lerp(.12f, 1f, Mathf.SmoothStep(0f, 1f, settle));
            if (eyelids != null) eyelids.SetClosure(closed);
            else SetBlackout(speaking ? 0f : settle);
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
        SetBlackout(1f);
        atmosphere?.StopOfficeAmbience();
        onReadyForMemoryWorld?.Invoke();
    }

    private void CacheVolumeEffects()
    {
        if (moodVolume == null || moodVolume.profile == null) return;
        moodVolume.profile.TryGet(out vignette);
        moodVolume.profile.TryGet(out colorAdjustments);
        moodVolume.profile.TryGet(out depthOfField);
    }

    private void ApplyMood(float t)
    {
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.Override(Mathf.Lerp(0.2f, 0.58f, t));
            vignette.smoothness.Override(Mathf.Lerp(0.32f, 0.82f, t));
        }
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.Override(Mathf.Lerp(-12f, -65f, t));
            colorAdjustments.postExposure.Override(Mathf.Lerp(-0.12f, -1.35f, t));
        }
        if (depthOfField != null)
        {
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(Mathf.Lerp(7f, 0.2f, t));
            depthOfField.gaussianEnd.Override(Mathf.Lerp(18f, 1.8f, t));
            depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0.2f, 1.25f, t));
        }
    }

    private void SetBlackout(float alpha)
    {
        if (blackout == null) return;
        Color color = blackout.color;
        color.a = Mathf.Clamp01(alpha);
        blackout.color = color;
        blackout.raycastTarget = alpha > 0.01f;
    }
}
