using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    private Camera reachCamera;
    private UniversalAdditionalCameraData viewData;
    private int reachLayer;
    private bool viewIncludedReachLayer;
    private int[] walkingLayers;
    private Scene2RadioKitchenSequence chores;
    private Scene2FoodCarry foodCarry;
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
        bool isHome = scene.name == SceneNames.Act2_Home;
        var prefab = Resources.Load<ChildWalkingHands>(isHome ? "Cutscenes/HomeWalkingHands" : "Cutscenes/KuTiWalkingHands");
        if (prefab == null) return;
        var hands = Instantiate(prefab, player.PlayerCamera, false);
        hands.name = "Walking Hands";
        hands.useHomeFraming = isHome;
        hands.player = player;
        hands.view = player.PlayerCamera.GetComponent<Camera>();
        if (isHome) hands.CreateReachCamera();
        hands.previousPosition = player.transform.position;
        hands.chores = FindObjectOfType<Scene2RadioKitchenSequence>();
        hands.foodCarry = player.GetComponent<Scene2FoodCarry>();
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
        bool blocked = (foodCarry != null && foodCarry.IsHandlingItem) || player.IsMovementLocked || player.LockWalkingOnly ||
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
        float targetAmount = moving ? 1f : 0f;
        if (useHomeFraming)
            targetAmount = Mathf.InverseLerp(.08f, 2f, speed);
        float blendRate = useHomeFraming && targetAmount < amount ? 3.5f : 5f;
        amount = Mathf.MoveTowards(amount, targetAmount, Time.deltaTime * blendRate);
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
            // The textured home mesh keeps the wrist and fingers in its skinned pose.
            // Rotate the whole arm for framing so the wrist follows the forearm.
            if (useHomeFraming)
            {
                // Keep the clip's forward wrist angle and add a small turn to the right.
                rightArmRoot.RotateAround(rightWrist.position, view.transform.up, 12f * ReachLift());
                return;
            }

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
            AimReachArm();
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

    private void AimReachArm()
    {
        Vector3 wristScreen = view.WorldToViewportPoint(rightWrist.position);
        Vector3 elbowScreen = view.WorldToViewportPoint(rightWrist.parent.position);
        Vector2 currentDirection = new Vector2((elbowScreen.x - wristScreen.x) * view.aspect,
            elbowScreen.y - wristScreen.y);
        Vector2 desiredDirection = new Vector2((.907f - wristScreen.x) * view.aspect,
            Mathf.Min(-.03f, .062f - wristScreen.y));
        float elbowTurn = Vector2.SignedAngle(currentDirection, desiredDirection);
        rightArmRoot.RotateAround(rightWrist.position, view.transform.forward, elbowTurn * ReachLift());
    }

    private void FrameArm(Transform root, Transform wrist, Vector3 reference, float side)
    {
        Vector3 local = view.transform.InverseTransformPoint(wrist.position);
        Vector3 swing = local - reference;
        float halfHeight = reference.z * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad);
        float forwardStep = Mathf.Sin(phase * Mathf.PI * 2f) * side;
        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(forwardStep));
        float reach = useHomeFraming ? reveal * amount : 0f;
        float lowerEdgeOffset = (useHomeFraming ? 1.37f : 1.25f) + .65f * (1f - reveal) + .55f * (1f - amount);
        float x = side * (useHomeFraming ? .815f - .025f * reach : .80f - .05f * reach) * halfHeight * view.aspect + swing.x;
        float y = ((useHomeFraming ? .17f : 0f) * reach - lowerEdgeOffset) * halfHeight + swing.y;
        float forwardOffset = 0f;
        if (useHomeFraming && !IsReaching)
        {
            // Let the wrists trail the stride slightly instead of moving along a straight line.
            float trailingSwing = Mathf.Sin(phase * Mathf.PI * 2f - .35f) * side;
            float walkBlend = Mathf.SmoothStep(0f, 1f, amount);
            x -= side * .035f * trailingSwing * walkBlend * halfHeight * view.aspect;
            forwardOffset = .018f * trailingSwing * walkBlend;
            Vector3 forearm = wrist.position - wrist.parent.position;
            root.RotateAround(wrist.position, forearm.normalized, side * trailingSwing * walkBlend * 4f);
        }
        if (IsReaching)
        {
            float lift = ReachLift();
            float reachSide = useHomeFraming && side > 0f ? .84f - .12f * lift : .84f;
            x = side * reachSide * halfHeight * view.aspect;
            bool homeRightReach = useHomeFraming && side > 0f;
            y = (side > 0f ? -1.85f + (homeRightReach ? 1.06f : 1f) * lift : -2.1f) * halfHeight;
            if (homeRightReach) forwardOffset = .06f * lift;
        }
        // Convert the correction from camera-local units, including the player's scale.
        root.position += view.transform.TransformVector(new Vector3(x - local.x, y - local.y, forwardOffset));
    }

    private float ReachLift()
    {
        float t = Mathf.Clamp01(reachTime / ReachDuration);
        return t < .38f ? Mathf.SmoothStep(0f, 1f, t / .38f) :
            1f - Mathf.SmoothStep(0f, 1f, (t - .38f) / .62f);
    }

    private void OnInteraction(IInteractable target)
    {
        if (target is Scene2FoodInteractable || target is Scene2FoodCarry)
        {
            reachTime = ReachDuration;
            return;
        }
        TryReach();
    }

    public bool TryReach()
    {
        if (IsReaching || (foodCarry != null && foodCarry.IsHandlingItem) || player == null || player.IsMovementLocked || player.LockWalkingOnly ||
            (GameManager.Instance != null && GameManager.Instance.InputBlocked) ||
            (chores != null && chores.CarryingWood)) return false;
        reachTime = 0f;
        return true;
    }

    private void OnDestroy()
    {
        if (interaction != null) interaction.InteractionStarted -= OnInteraction;
        if (reachCamera != null && viewData != null)
        {
            viewData.cameraStack?.Remove(reachCamera);
            if (view != null && viewIncludedReachLayer) view.cullingMask |= 1 << reachLayer;
        }
    }

    private void CreateReachCamera()
    {
        reachLayer = LayerMask.NameToLayer("FirstPersonReach");
        if (view == null || reachLayer < 0) return;
        viewData = view.GetUniversalAdditionalCameraData();
        var stack = viewData.cameraStack;
        if (stack == null) return;

        walkingLayers = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) walkingLayers[i] = renderers[i].gameObject.layer;

        var cameraObject = new GameObject("Reach Hands Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
        reachCamera = cameraObject.AddComponent<Camera>();
        reachCamera.CopyFrom(view);
        reachCamera.enabled = false;
        reachCamera.cullingMask = 1 << reachLayer;
        var reachData = reachCamera.GetUniversalAdditionalCameraData();
        reachData.renderType = CameraRenderType.Overlay;
        reachData.renderShadows = false;
        viewIncludedReachLayer = (view.cullingMask & (1 << reachLayer)) != 0;
        view.cullingMask &= ~(1 << reachLayer);
        stack.Add(reachCamera);
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
        bool overlay = visible && IsReaching && reachCamera != null;
        if (reachCamera != null)
        {
            reachCamera.enabled = overlay;
            if (overlay)
            {
                reachCamera.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                reachCamera.fieldOfView = view.fieldOfView;
                reachCamera.nearClipPlane = view.nearClipPlane;
                reachCamera.farClipPlane = view.farClipPlane;
            }
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
            if (reachCamera != null)
                renderers[i].gameObject.layer = overlay ? reachLayer : walkingLayers[i];
        }
    }
}
