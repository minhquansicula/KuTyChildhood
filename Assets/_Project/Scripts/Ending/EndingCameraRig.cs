using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Owns the cinematic camera: the director sets a base pose each frame, this adds a subtle handheld sway,
/// a slow breathing zoom and drives the depth-of-field focus so foreground rice and the house pull focus.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class EndingCameraRig : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Volume volume;
    [Header("Handheld")]
    [SerializeField] private float positionSway = .018f;
    [SerializeField] private float rotationSway = .28f;

    private Vector3 basePosition;
    private Quaternion baseRotation = Quaternion.identity;
    private float baseFov = 38f;
    private float focusDistance = 12f;
    private float swayWeight = 1f;
    private DepthOfField depthOfField;
    private bool hasBase;

    public Camera Camera => cam;
    public Volume Volume => volume;

    private void Awake()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (volume != null && volume.profile != null) volume.profile.TryGet(out depthOfField);
    }

    public void SetPose(Vector3 position, Quaternion rotation, float fov)
    {
        basePosition = position;
        baseRotation = rotation;
        baseFov = fov;
        hasBase = true;
    }

    public void SetFocus(float distance) { focusDistance = Mathf.Max(.3f, distance); }
    public void SetSway(float weight) { swayWeight = Mathf.Clamp01(weight); }

    public void SetExposure(float postExposure)
    {
        if (volume != null && volume.profile != null && volume.profile.TryGet(out ColorAdjustments color))
            color.postExposure.Override(postExposure);
    }

    public void SetVignette(float intensity)
    {
        if (volume != null && volume.profile != null && volume.profile.TryGet(out Vignette vignette))
            vignette.intensity.Override(intensity);
    }

    public void SetBloom(float intensity)
    {
        if (volume != null && volume.profile != null && volume.profile.TryGet(out Bloom bloom))
            bloom.intensity.Override(intensity);
    }

    private void LateUpdate()
    {
        if (cam == null || !hasBase) return;
        float t = Time.unscaledTime;
        Vector3 posNoise = new Vector3(
            Mathf.PerlinNoise(t * .45f, 3.1f) - .5f,
            Mathf.PerlinNoise(t * .38f, 9.7f) - .5f,
            Mathf.PerlinNoise(t * .41f, 5.3f) - .5f) * (positionSway * 2f * swayWeight);
        Vector3 rotNoise = new Vector3(
            Mathf.PerlinNoise(t * .33f, 1.9f) - .5f,
            Mathf.PerlinNoise(t * .29f, 7.7f) - .5f,
            Mathf.PerlinNoise(t * .21f, 4.2f) - .5f) * (rotationSway * 2f * swayWeight);
        cam.transform.SetPositionAndRotation(basePosition + baseRotation * posNoise,
            baseRotation * Quaternion.Euler(rotNoise));
        // A barely perceptible breathing zoom keeps long holds alive.
        cam.fieldOfView = baseFov + Mathf.Sin(t * .5f) * .35f * swayWeight;

        if (depthOfField != null)
            depthOfField.focusDistance.Override(Mathf.Lerp(depthOfField.focusDistance.value, focusDistance, Time.unscaledDeltaTime * 3f));
    }
}
