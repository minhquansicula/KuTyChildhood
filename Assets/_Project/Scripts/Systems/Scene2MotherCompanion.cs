using UnityEngine;

/// <summary>Presentation only: never completes chores, changes checkpoints or gives money.
/// Assign an authored actor to replace the runtime greybox; movement is a separate validated component.</summary>
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
    private float ambientRemaining;
    private Scene2ChoreGuidance guidance;

    public bool IsConversing => conversing;
    // Caption presentation window, not a claim that an unrecorded line has audio.
    public bool IsPresentingReply => conversing && replyRemaining > 0f || ambientRemaining > 0f;
    public Transform Actor => actor;
    public bool CanTalk => isActiveAndEnabled && actor && flow && flow.ChecklistVisible &&
        flow.ActiveIndex < HouseholdChoreFlow.Order.Length &&
        HouseholdChoreFlow.Order[flow.ActiveIndex] != HouseholdChore.ReadLetter &&
        GameManager.Instance?.InputBlocked != true && DialogueUI.Instance?.IsShowing != true &&
        flow.Voice?.Voice?.IsPlaying != true && !OtherMotherSpeaking() && ambientRemaining <= 0f &&
        PlayerCanReach() && Time.time >= nextConversation;

    private bool PlayerCanReach()
    {
        var camera = Camera.main;
        if (!camera || !actor || Vector3.ProjectOnPlane(camera.transform.position - actor.position, Vector3.up).sqrMagnitude > 3.2f * 3.2f) return false;
        var collider = actor.GetComponent<Collider>();
        Vector3 target = collider ? collider.bounds.center : actor.position + Vector3.up;
        foreach (var hit in Physics.RaycastAll(camera.transform.position, (target - camera.transform.position).normalized,
            Vector3.Distance(target, camera.transform.position), ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(actor) && !hit.transform.IsChildOf(camera.transform.root)) return false;
        return true;
    }

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
        guidance = GetComponent<Scene2ChoreGuidance>();
        if (guidance) guidance.OnMotherReaction += PresentAmbientReaction;
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
            reply = SmallTalk(flow.CurrentChore, conversationIndex++);
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

    public static string SmallTalk(HouseholdChore? chore, int index)
    {
        string[] lines;
        switch (chore)
        {
            case HouseholdChore.ArrangeKeepsakes:
            case HouseholdChore.TuneRadio:
                lines = new[] { "Mấy món trong rương, món nào cũng có một chuyện hồi con còn bé.",
                    "Ngày trước chưa có nhiều đồ chơi, con cứ theo mẹ quanh nhà suốt.", "Sáng nghe tiếng radio, tự nhiên mẹ thấy căn nhà vui hơn." }; break;
            case HouseholdChore.CarryWood:
            case HouseholdChore.FetchWater:
            case HouseholdChore.MeasureRice:
                lines = new[] { "Có con phụ một tay, mẹ đỡ chạy ra chạy vào. Mình làm thong thả thôi nhé.",
                    "Nước trong lu mát đấy. Con cầm gáo cho chắc, không cần vội đâu.", "Mẹ đong gạo hay thêm một chút. Cứ sợ con đi chơi về lại đói." }; break;
            case HouseholdChore.WashRice:
            case HouseholdChore.LightFire:
            case HouseholdChore.CookRice:
                lines = new[] { "Hồi bằng tuổi con, mẹ cũng được bà chỉ vo gạo, nhóm bếp như thế này.",
                    "Làm bếp nhớ nhẹ tay, cẩn thận nước nóng nhé. Có mẹ ở đây rồi.", "Ngửi thấy mùi cơm chưa con? Cơm nhà mình chỉ cần nóng, cả nhà ngồi cùng là ngon." }; break;
            default:
                lines = new[] { "Bữa cơm có con ngồi cùng, mẹ thấy vui hơn nhiều.",
                    "Chén bát mình làm từ từ thôi. Xong việc rồi con nghỉ tay, đừng cố quá.",
                    "Sau này có đi đâu xa, con nhớ ăn cơm đúng bữa. Về nhà lúc nào mẹ cũng mừng." }; break;
        }
        return lines[Mathf.Abs(index % lines.Length)];
    }

    private void PresentAmbientReaction(string text)
    {
        if (!conversing && PlayerCanReach()) ambientRemaining = Mathf.Clamp((text?.Length ?? 0) / 12f, 1.8f, 7f);
    }

    private void PresentReply(string text)
    {
        // Stop talking while the player considers choices instead of looping forever.
        replyRemaining = Mathf.Clamp((text?.Length ?? 0) / 12f, 1.8f, 9f);
    }

    private void Update()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        ambientRemaining = Mathf.Max(0, ambientRemaining - Time.deltaTime);
        if (!conversing ||
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

    private void OnDisable() { EndConversation(); ambientRemaining = 0; }
    private void OnDestroy()
    {
        EndConversation();
        if (guidance) guidance.OnMotherReaction -= PresentAmbientReaction;
        if (placeholder) Destroy(placeholder);
    }
}
