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
    private bool useHomeFraming;
    private PlayerInteraction interaction;
    private Transform[] rightFingerBones;
    private Quaternion[] rightFingerRestRotations;
    private float reachTime = 1f;
    private const float ReachDuration = .46f;
    public bool IsReaching => reachTime < ReachDuration;
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
        hands.useHomeFraming = scene.name == SceneNames.Act2_Home;
        hands.player = player;
        hands.view = player.PlayerCamera.GetComponent<Camera>();
        hands.previousPosition = player.transform.position;
        hands.chores = FindObjectOfType<Scene2RadioKitchenSequence>();
        hands.rightFingerBones = hands.rightWrist.GetComponentsInChildren<Transform>();
        hands.rightFingerRestRotations = new Quaternion[hands.rightFingerBones.Length];
        var skin = hands.GetComponentInChildren<SkinnedMeshRenderer>();
        var bones = skin.bones;
        var bindPoses = skin.sharedMesh.bindposes;
        for (int i = 0; i < hands.rightFingerBones.Length; i++)
        {
            var bone = hands.rightFingerBones[i];
            int index = System.Array.IndexOf(bones, bone);
            int parent = System.Array.IndexOf(bones, bone.parent);
            hands.rightFingerRestRotations[i] = index >= 0 && parent >= 0 ?
                (bindPoses[parent] * bindPoses[index].inverse).rotation : bone.localRotation;
        }
        hands.interaction = player.GetComponent<PlayerInteraction>();
        if (hands.interaction != null) hands.interaction.InteractionStarted += hands.OnInteraction;
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
            reachTime = ReachDuration;
            amount = 0f;
            Show(false);
            return;
        }
        float speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
        bool moving = speed > .08f;
        if (moving) phase = Mathf.Repeat(phase + distance / metresPerCycle, 1f);
        amount = Mathf.MoveTowards(amount, moving ? 1f : 0f, Time.deltaTime * 5f);
        if (IsReaching) reachTime += Time.deltaTime;
        ApplyPose();
        Show(amount > .01f || IsReaching);
    }

    private void ApplyPose()
    {
        // Reset positional framing, then preserve the clip's actual forward/backward arm travel.
        rightArmRoot.localPosition = rightRootPosition;
        leftArmRoot.localPosition = leftRootPosition;
        cycle.SampleAnimation(modelRoot, (IsReaching ? .25f : phase) * cycle.length);
        FrameArm(rightArmRoot, rightWrist, rightReference, 1f);
        FrameArm(leftArmRoot, leftWrist, leftReference, -1f);
        if (IsReaching)
        {
            Quaternion openPalm = rightWrist.rotation;
            Vector3 fingerCenter = Vector3.zero;
            int fingerCount = 0;
            for (int i = 0; i < rightWrist.childCount; i++)
            {
                var child = rightWrist.GetChild(i);
                if (child.name.Contains("Thumb")) continue;
                fingerCenter += child.position;
                fingerCount++;
            }
            Vector3 palmAxis = fingerCount > 0 ? fingerCenter / fingerCount - rightWrist.position : rightWrist.up;
            openPalm = Quaternion.AngleAxis(35f * ReachLift(), palmAxis.normalized) * openPalm;
            for (int i = 0; i < rightFingerBones.Length; i++)
                if (rightFingerBones[i] != rightWrist)
                    rightFingerBones[i].localRotation = rightFingerRestRotations[i];
            // Aim the forearm from the lower corner, then constrain the independent hand pose.
            Vector3 wristScreen = view.WorldToViewportPoint(rightWrist.position);
            Vector3 elbowScreen = view.WorldToViewportPoint(rightWrist.parent.position);
            Vector2 currentDirection = new Vector2((elbowScreen.x - wristScreen.x) * view.aspect,
                elbowScreen.y - wristScreen.y);
            Vector2 desiredDirection = new Vector2((.907f - wristScreen.x) * view.aspect,
                Mathf.Min(-.03f, .062f - wristScreen.y));
            float elbowTurn = Vector2.SignedAngle(currentDirection, desiredDirection);
            rightArmRoot.RotateAround(rightWrist.position, view.transform.forward, elbowTurn * ReachLift());
            Vector3 forearmAxis = (rightWrist.position - rightWrist.parent.position).normalized;
            rightWrist.rotation = openPalm;
            // Preserve the reference palm facing while limiting the wrist bend.
            Vector3 handAxis = palmAxis.normalized;
            // palmAxis was sampled before rotating the arm, so it remains the reference direction.
            float bend = Vector3.Angle(forearmAxis, handAxis);
            if (bend > 35f)
            {
                Vector3 safeAxis = Vector3.Slerp(forearmAxis, handAxis, 35f / bend).normalized;
                rightWrist.rotation = Quaternion.FromToRotation(handAxis, safeAxis) * rightWrist.rotation;
            }
            Transform middle = null;
            foreach (var finger in rightFingerBones)
                if (finger.name.EndsWith("Middle1")) { middle = finger; break; }
            if (middle != null && middle.childCount > 0)
            {
                Vector3 middleTip = middle.GetChild(0).position * 2f - middle.position;
                foreach (var finger in rightFingerBones)
                {
                    if (finger.childCount == 0 || !finger.name.EndsWith("1") ||
                        finger == middle || finger.name.Contains("Thumb")) continue;
                    Vector3 tip = finger.GetChild(0).position * 2f - finger.position;
                    Vector3 relaxedTip = Vector3.Lerp(tip, middleTip, .12f);
                    Quaternion close = Quaternion.FromToRotation(tip - finger.position, relaxedTip - finger.position);
                    finger.rotation = Quaternion.RotateTowards(Quaternion.identity, close, 3f) * finger.rotation;
                }
            }
        }
    }

    private void FrameArm(Transform root, Transform wrist, Vector3 reference, float side)
    {
        Vector3 local = view.transform.InverseTransformPoint(wrist.position);
        Vector3 swing = local - reference;
        float halfHeight = reference.z * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad);
        float forwardStep = Mathf.Sin(phase * Mathf.PI * 2f) * side;
        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(forwardStep));
        float reach = useHomeFraming ? reveal * amount : 0f;
        float lowerEdgeOffset = 1.25f + .65f * (1f - reveal) + .55f * (1f - amount);
        float x = side * (.80f - .05f * reach) * halfHeight * view.aspect + swing.x;
        float y = (.30f * reach - lowerEdgeOffset) * halfHeight + swing.y;
        if (IsReaching)
        {
            float lift = ReachLift();
            x = side * .84f * halfHeight * view.aspect;
            y = (side > 0f ? -1.85f + 1.00f * lift : -2.1f) * halfHeight;
        }
        // Convert the correction from camera-local units, including the player's scale.
        root.position += view.transform.TransformVector(new Vector3(x - local.x, y - local.y, 0f));
    }

    private float ReachLift()
    {
        float t = Mathf.Clamp01(reachTime / ReachDuration);
        return t < .38f ? Mathf.SmoothStep(0f, 1f, t / .38f) :
            1f - Mathf.SmoothStep(0f, 1f, (t - .38f) / .62f);
    }

    private void OnInteraction(IInteractable target)
    {
        if (IsReaching || player == null || player.IsMovementLocked || player.LockWalkingOnly ||
            (chores != null && chores.CarryingWood)) return;
        reachTime = 0f;
    }

    private void OnDestroy()
    {
        if (interaction != null) interaction.InteractionStarted -= OnInteraction;
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
