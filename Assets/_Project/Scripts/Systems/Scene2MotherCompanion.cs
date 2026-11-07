using UnityEngine;

/// <summary>Presentation only: never completes chores, changes checkpoints or gives money.
/// Assign an authored actor to replace the runtime greybox. No unverified walking paths.</summary>
public sealed class Scene2MotherCompanion : MonoBehaviour
{
    [Header("Authored mother actor (MomUser in Scene 2)")]
    [SerializeField] private Transform actor;
    [SerializeField] private Transform head;
    [SerializeField] private Animator animator;
    [SerializeField] private bool showPlaceholder = true;
    [Header("Optional authored standing points; movement is intentionally not automatic")]
    [SerializeField] private Transform standingPoint;
    private HouseholdChoreFlow flow;
    private GameObject placeholder;
    private Quaternion headRest;
    private bool conversing;
    private int openedFrame;
    private int conversationIndex;
    private float nextConversation;
    private float replyRemaining;

    public bool IsConversing => conversing;
    // Caption presentation window, not a claim that an unrecorded line has audio.
    public bool IsPresentingReply => conversing && replyRemaining > 0f;
    public Transform Actor => actor;
    public bool CanTalk => isActiveAndEnabled && actor && flow && flow.ChecklistVisible &&
        flow.ActiveIndex < HouseholdChoreFlow.Order.Length &&
        HouseholdChoreFlow.Order[flow.ActiveIndex] != HouseholdChore.ReadLetter &&
        GameManager.Instance?.InputBlocked != true && DialogueUI.Instance?.IsShowing != true &&
        flow.Voice?.Voice?.IsPlaying != true && !OtherMotherSpeaking() && Time.time >= nextConversation;

    private bool OtherMotherSpeaking()
    {
        var kitchen = GetComponent<Scene2KitchenHandsOn>();
        var radio = GetComponent<Scene2RadioKitchenSequence>();
        return (kitchen && (kitchen.IsWorking || kitchen.MotherSpeaking)) ||
            (radio && (radio.CarryingWood || radio.MotherSpeaking));
    }

    private void Start()
    {
        flow = GetComponent<HouseholdChoreFlow>();
        if (!actor && showPlaceholder)
        {
            var sequence = GetComponent<Scene2RadioKitchenSequence>();
            Transform spot = standingPoint ? standingPoint : sequence ? sequence.kitchenSpot : null;
            if (!spot)
            {
                Debug.LogWarning("Mother companion: assign a standing point or actor before enabling interactions.", this);
                return;
            }
            placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "Mother_Placeholder_ReplaceWithModel";
            Vector3 feet = spot.position;
            var kitchen = GetComponent<Scene2KitchenHandsOn>();
            // The voice anchor is above the stove, not a standing point. Use the
            // authored approach view's horizontal location for the temporary actor.
            if (!standingPoint && kitchen && kitchen.fireView && kitchen.hearth)
            {
                feet = kitchen.fireView.position;
                feet.y = kitchen.hearth.position.y;
            }
            else if (!standingPoint)
            {
                Destroy(placeholder);
                placeholder = null;
                Debug.LogWarning("Mother companion: a floor-level standing point is required.", this);
                return;
            }
            placeholder.transform.SetPositionAndRotation(feet + Vector3.up * .8f, spot.rotation);
            placeholder.transform.localScale = new Vector3(.45f, .8f, .45f);
            placeholder.transform.SetParent(transform, true);
            actor = placeholder.transform;
            // A trigger is focusable without blocking the child's movement.
            placeholder.GetComponent<Collider>().isTrigger = true;
        }
        if (!actor) return;
        if (!animator) animator = actor.GetComponentInChildren<Animator>();
        if (!head && animator && animator.isHuman) head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head) headRest = head.localRotation;
        var interaction = actor.GetComponent<Scene2MotherInteractable>();
        if (!interaction) interaction = actor.gameObject.AddComponent<Scene2MotherInteractable>();
        interaction.Configure(this);
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
            foreach (Collider collider in actor.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = layer;
        if (layer >= 0) actor.gameObject.layer = layer;
    }

    public void BeginConversation()
    {
        if (!CanTalk || flow.UI == null) return;
        conversing = true;
        openedFrame = Time.frameCount;
        GameManager.Instance?.AcquireInput(this);
        flow.UI.ShowGame("Nói chuyện với mẹ", "Mẹ đây, con muốn hỏi gì?", false, EndConversation);
        PresentReply("Mẹ đây, con muốn hỏi gì?");
        flow.UI.ShowChoices(new[] { "Con làm gì tiếp?", "Nói chuyện với mẹ", "Con làm tiếp nhé" }, Select);
    }

    private void Select(int choice)
    {
        if (!conversing || GameManager.Instance?.IsPaused == true) return;
        if (choice == 2) { EndConversation(); return; }
        string reply;
        if (choice == 0) reply = HintForCurrentChore();
        else if (choice == 1)
        {
            string[] lines = {
                "Có con ở nhà, mẹ làm gì cũng thấy vui hơn.",
                "Làm từ từ thôi con. Chưa quen thì mẹ chỉ cho.",
                "Đi chơi đâu cũng nhớ về ăn cơm với mẹ nha."
            };
            reply = lines[conversationIndex++ % lines.Length];
        }
        else reply = "Ừ, con phụ mẹ một tay nhé. " + HintForCurrentChore();
        flow.UI.ShowGame("Mẹ", reply, false, EndConversation);
        PresentReply(reply);
        flow.UI.ShowChoices(new[] { "Hỏi việc tiếp theo", "Nói thêm với mẹ", "Con làm tiếp nhé" }, i =>
        {
            if (i == 2) EndConversation(); else Select(i);
        });
    }

    public string HintForCurrentChore()
    {
        if (!flow || flow.ActiveIndex >= HouseholdChoreFlow.Order.Length) return "Xong việc rồi, con nghỉ tay đi nhé.";
        return flow.CurrentGuidance;
    }

    private void PresentReply(string text)
    {
        // Stop talking while the player considers choices instead of looping forever.
        replyRemaining = Mathf.Clamp((text?.Length ?? 0) / 12f, 1.8f, 9f);
    }

    private void Update()
    {
        if (!conversing || GameManager.Instance?.IsPaused == true ||
            PauseUI.LastResumeFrame == Time.frameCount || Time.frameCount == openedFrame) return;
        replyRemaining = Mathf.Max(0f, replyRemaining - Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Escape)) { EndConversation(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) EndConversation();
    }

    private void LateUpdate()
    {
        if (!head || !actor || GameManager.Instance?.IsPaused == true) return;
        Camera camera = Camera.main;
        Quaternion target = headRest;
        if (camera)
        {
            Vector3 offset = camera.transform.position - head.position;
            if (offset.sqrMagnitude < 16f && offset.sqrMagnitude > .01f &&
                Vector3.Angle(actor.forward, offset) < 65f &&
                !Physics.Linecast(head.position, camera.transform.position, out _, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 local = actor.InverseTransformDirection(offset.normalized);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -40f, 40f);
                float pitch = Mathf.Clamp(-Mathf.Asin(local.y) * Mathf.Rad2Deg, -20f, 20f);
                target = headRest * Quaternion.Euler(pitch, yaw, 0);
            }
        }
        head.localRotation = Quaternion.Slerp(head.localRotation, target, Time.deltaTime * 4f);
    }

    public void EndConversation()
    {
        if (!conversing) return;
        conversing = false;
        replyRemaining = 0f;
        flow?.UI?.HideGame();
        GameManager.Instance?.ReleaseInput(this);
        nextConversation = Time.time + .4f;
    }

    private void OnDisable() => EndConversation();
    private void OnDestroy()
    {
        EndConversation();
        if (placeholder) Destroy(placeholder);
    }
}
