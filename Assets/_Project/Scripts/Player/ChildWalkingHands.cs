using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Small, camera-relative arm swing driven by actual planar travel.</summary>
public sealed class ChildWalkingHands : MonoBehaviour
{
    [SerializeField] private GameObject modelRoot;
    [SerializeField] private AnimationClip cycle;
    [SerializeField] private Transform rightArmRoot;
    [SerializeField] private Transform rightWrist;
    [SerializeField] private Transform leftArmRoot;
    [SerializeField] private Transform leftWrist;
    private FirstPersonController player;
    private Camera view;
    private Renderer[] renderers;
    private Scene2RadioKitchenSequence chores;
    private Vector3 previousPosition;
    private float amount;
    private float phase;
    private Vector3 rightReference, leftReference;
    private Vector3 rightRootPosition, leftRootPosition;
    [SerializeField, Min(.5f)] private float metresPerCycle = 3f;
    public bool IsVisible { get; private set; }
    public float StridePhase => phase;
    public float MotionAmount => amount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.Act2_Home && scene.name != SceneNames.Act3_OutSide) return;
        var player = FindObjectOfType<FirstPersonController>();
        if (player == null || player.PlayerCamera == null || player.GetComponentInChildren<ChildWalkingHands>(true) != null) return;
        var prefab = Resources.Load<ChildWalkingHands>("Cutscenes/KuTiWalkingHands");
        if (prefab == null) return;
        var hands = Instantiate(prefab, player.PlayerCamera, false);
        hands.name = "Walking Hands";
        hands.player = player;
        hands.view = player.PlayerCamera.GetComponent<Camera>();
        hands.previousPosition = player.transform.position;
        hands.chores = FindObjectOfType<Scene2RadioKitchenSequence>();
        hands.cycle.SampleAnimation(hands.modelRoot, 0f);
        hands.rightReference = hands.view.transform.InverseTransformPoint(hands.rightWrist.position);
        hands.leftReference = hands.view.transform.InverseTransformPoint(hands.leftWrist.position);
        hands.rightRootPosition = hands.rightArmRoot.localPosition;
        hands.leftRootPosition = hands.leftArmRoot.localPosition;
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        Show(false);
    }

    private void LateUpdate()
    {
        if (player == null || view == null || cycle == null) return;
        Vector3 displacement = player.transform.position - previousPosition;
        previousPosition = player.transform.position;
        displacement.y = 0f;
        bool blocked = player.IsMovementLocked || player.LockWalkingOnly ||
            (GameManager.Instance != null && GameManager.Instance.InputBlocked) ||
            (chores != null && chores.CarryingWood);
        float distance = displacement.magnitude;
        if (blocked || distance > .75f)
        {
            amount = 0f;
            Show(false);
            return;
        }
        float speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
        bool moving = speed > .08f;
        if (moving) phase = Mathf.Repeat(phase + distance / metresPerCycle, 1f);
        amount = Mathf.MoveTowards(amount, moving ? 1f : 0f, Time.deltaTime * 5f);
        ApplyPose();
        Show(amount > .01f);
    }

    private void ApplyPose()
    {
        // Reset positional framing, then preserve the clip's actual forward/backward arm travel.
        rightArmRoot.localPosition = rightRootPosition;
        leftArmRoot.localPosition = leftRootPosition;
        cycle.SampleAnimation(modelRoot, phase * cycle.length);
        FrameArm(rightArmRoot, rightWrist, rightReference, 1f);
        FrameArm(leftArmRoot, leftWrist, leftReference, -1f);
    }

    private void FrameArm(Transform root, Transform wrist, Vector3 reference, float side)
    {
        Vector3 local = view.transform.InverseTransformPoint(wrist.position);
        Vector3 swing = local - reference;
        float halfHeight = reference.z * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad);
        float forwardStep = Mathf.Sin(phase * Mathf.PI * 2f) * side;
        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(forwardStep));
        float x = side * .80f * halfHeight * view.aspect + swing.x;
        float y = -(1.25f + .65f * (1f - reveal) + .55f * (1f - amount)) * halfHeight + swing.y;
        root.position += view.transform.right * (x - local.x) + view.transform.up * (y - local.y);
    }

#if UNITY_EDITOR
    public Vector4 SampleStrideForTests(float normalizedPhase)
    {
        phase = normalizedPhase;
        amount = 1f;
        ApplyPose();
        Vector3 right = view.transform.InverseTransformPoint(rightWrist.position);
        Vector3 left = view.transform.InverseTransformPoint(leftWrist.position);
        return new Vector4(right.y, right.z, left.y, left.z);
    }
#endif

    private void Show(bool visible)
    {
        IsVisible = visible;
        if (renderers == null) return;
        foreach (var renderer in renderers) renderer.enabled = visible;
    }
}
