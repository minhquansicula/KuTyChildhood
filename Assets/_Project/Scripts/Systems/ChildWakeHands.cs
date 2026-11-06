using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Camera-relative skinned hands sampled on the same clock as the wake-up reveal.</summary>
public sealed class ChildWakeHands : MonoBehaviour
{
    [SerializeField] private GameObject modelRoot;
    [SerializeField] private AnimationClip motion;

    public static ChildWakeHands Create(Transform cameraTransform)
    {
        var prefab = Resources.Load<ChildWakeHands>("Cutscenes/KuTiChildWakeHands");
        if (prefab == null) return null;
        var hands = Instantiate(prefab, cameraTransform, false);
        hands.name = "Child Wake Hands";
        hands.Evaluate(0f);
        return hands;
    }

    public void Evaluate(float seconds)
    {
        if (motion != null && modelRoot != null)
            motion.SampleAnimation(modelRoot, Mathf.Clamp(seconds, 0f, motion.length));
    }

    private void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            renderer.updateWhenOffscreen = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
