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
    [SerializeField] private Image blackout;
    [SerializeField] private Volume moodVolume;
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField, Min(1f)] private float collapseSeconds = 4f;
    [SerializeField, Min(0f)] private float silentBlackSeconds = 1.35f;
    [SerializeField, Min(0f)] private float memoryAudioSeconds = 1.8f;

    public bool IsRunning { get; private set; }

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;

    private void Awake()
    {
        CacheVolumeEffects();
        SetBlackout(0f);
    }

    public bool Play(Action onReadyForMemoryWorld)
    {
        if (IsRunning || cameraTransform == null) return false;
        StartCoroutine(Collapse(onReadyForMemoryWorld));
        return true;
    }

    private IEnumerator Collapse(Action onReadyForMemoryWorld)
    {
        IsRunning = true;
        Vector3 startPosition = cameraTransform.localPosition;
        Quaternion startRotation = cameraTransform.localRotation;
        Vector3 targetPosition = startPosition + new Vector3(0.08f, -0.62f, 0.34f);
        Quaternion targetRotation = startRotation * Quaternion.Euler(25f, 0f, 8f);

        for (float elapsed = 0f; elapsed < collapseSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / collapseSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            cameraTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
            cameraTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            ApplyMood(t);
            atmosphere?.SetMuffle(t);
            SetBlackout(Mathf.InverseLerp(0.68f, 1f, t));
            yield return null;
        }

        SetBlackout(1f);
        atmosphere?.StopOfficeAmbience();
        yield return new WaitForSecondsRealtime(silentBlackSeconds);
        atmosphere?.PlayMemoryBridge();
        yield return new WaitForSecondsRealtime(memoryAudioSeconds);
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
