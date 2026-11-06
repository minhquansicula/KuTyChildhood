using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Temporary camera focus layer. Disposing it restores the scene's existing volumes.</summary>
internal sealed class FirstPersonFocusTransition
{
    private readonly GameObject root;
    private readonly VolumeProfile profile;
    private readonly DepthOfField focus;
    private readonly ColorAdjustments exposure;
    private readonly UniversalAdditionalCameraData cameraData;
    private readonly bool previousPostProcessing;

    internal FirstPersonFocusTransition(string name, Camera camera)
    {
        root = new GameObject(name);
        if (camera != null)
        {
            cameraData = camera.GetUniversalAdditionalCameraData();
            previousPostProcessing = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
            int mask = cameraData.volumeLayerMask.value;
            for (int layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) == 0) continue;
                root.layer = layer;
                break;
            }
        }
        var volume = root.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 200f;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        focus = profile.Add<DepthOfField>(true);
        focus.mode.Override(DepthOfFieldMode.Gaussian);
        focus.gaussianStart.Override(.15f);
        focus.gaussianEnd.Override(2.5f);
        focus.gaussianMaxRadius.Override(1.1f);
        focus.highQualitySampling.Override(true);
        exposure = profile.Add<ColorAdjustments>(true);
        Set(0f, 0f);
    }

    internal void Set(float blur, float stops)
    {
        // At zero the original depth-of-field settings are inherited again.
        focus.active = blur > .001f;
        focus.gaussianStart.value = Mathf.Lerp(18f, .15f, blur);
        focus.gaussianEnd.value = Mathf.Lerp(35f, 2.5f, blur);
        focus.gaussianMaxRadius.value = Mathf.Lerp(.1f, 1.1f, blur);
        exposure.active = Mathf.Abs(stops) > .001f;
        exposure.postExposure.value = stops;
    }

    internal void Dispose()
    {
        if (cameraData != null) cameraData.renderPostProcessing = previousPostProcessing;
        Object.Destroy(root);
        Object.Destroy(profile);
    }
}
