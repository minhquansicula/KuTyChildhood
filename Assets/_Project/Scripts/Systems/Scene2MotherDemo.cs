using UnityEngine;

/// <summary>MomUser standing presentation only. Never owns chores, rewards, saves or navigation.</summary>
[DisallowMultipleComponent]
public sealed class Scene2MotherDemo : MonoBehaviour
{
    // Keep these serialized bindings: the existing scene already points at the verified MomUser model.
    [SerializeField] private Animator animator;
    [SerializeField] private Scene2MotherCompanion companion;
    [SerializeField] private Scene2RadioKitchenSequence radio;
    private static readonly int Idle = Animator.StringToHash("Base Layer.Mother_Idle_Standing");
    private static readonly int Call = Animator.StringToHash("Base Layer.Mother_CallChild");
    private static readonly int Talk = Animator.StringToHash("Base Layer.Mother_TalkIdle");
    private static readonly int Nod = Animator.StringToHash("Base Layer.Mother_NodApprove");
    private Scene2RadioKitchenSequence boundRadio;
    private HouseholdChoreFlow boundFlow;
    private Scene2KitchenHandsOn kitchen;
    private GameManager pauseOwner;
    private Camera playerCamera;
    private int state;
    private float oneShotRemaining, nextCall, nextApproval, approvalExpires;
    private float speedBeforePause = 1f;
    private bool ready, approvalPending, pausedAnimator;

    public Animator Animator => animator;
    public bool IsCalling => state == Call;
    public bool IsNodding => state == Nod;
    public bool IsTalking => state == Talk;

    public void Configure(Animator modelAnimator, Scene2MotherCompanion mother, Scene2RadioKitchenSequence sequence)
    {
        Unbind();
        animator = modelAnimator;
        companion = mother;
        radio = sequence;
        if (isActiveAndEnabled) Bind();
    }

    private void OnEnable() => Bind();

    private void Start()
    {
        if (!animator || !animator.runtimeAnimatorController || !animator.HasState(0, Idle))
        {
            Debug.LogWarning("Mother presentation: the standing animation/controller is missing.", this);
            enabled = false;
            return;
        }
        animator.applyRootMotion = false;
        ready = true;
        state = Idle;
        animator.Play(Idle, 0, 0f);
        Bind();
    }

    private void Bind()
    {
        if (boundRadio != radio)
        {
            if (boundRadio) boundRadio.OnMotherCalled -= Greet;
            boundRadio = radio;
            if (boundRadio) boundRadio.OnMotherCalled += Greet;
        }
        var flow = companion ? companion.GetComponent<HouseholdChoreFlow>() : null;
        if (!flow && radio) flow = radio.GetComponent<HouseholdChoreFlow>();
        if (boundFlow != flow)
        {
            if (boundFlow) boundFlow.OnChoreCompleted -= OnChoreCompleted;
            boundFlow = flow;
            if (boundFlow) boundFlow.OnChoreCompleted += OnChoreCompleted;
        }
        kitchen = flow ? flow.GetComponent<Scene2KitchenHandsOn>() : null;
        if (pauseOwner != GameManager.Instance)
        {
            if (pauseOwner) pauseOwner.OnPauseChanged -= PauseAnimation;
            pauseOwner = GameManager.Instance;
            if (pauseOwner) pauseOwner.OnPauseChanged += PauseAnimation;
        }
        if (pauseOwner && pauseOwner.IsPaused) PauseAnimation(true);
    }

    /// <summary>One call gesture, only for an authored call event; never used as a talk loop.</summary>
    public void Greet()
    {
        if (!ready || !isActiveAndEnabled || GameManager.Instance?.IsPaused == true ||
            Time.time < nextCall || IsCalling || (companion && companion.IsConversing) ||
            !animator || !animator.HasState(0, Call)) return;
        PlayOneShot(Call, "Mother_CallChild", 3.4f);
        nextCall = Time.time + oneShotRemaining + 8f;
    }

    /// <summary>Queue a visual acknowledgment. No progress or reward is changed.</summary>
    public void RequestApproval()
    {
        if (!ready || !isActiveAndEnabled || GameManager.Instance?.IsPaused == true ||
            Time.time < nextApproval || IsNodding || !animator || !animator.HasState(0, Nod)) return;
        if (!approvalPending) approvalExpires = Time.time + 10f;
        approvalPending = true;
    }

    private void OnChoreCompleted(HouseholdChore chore, int pieces)
    {
        if (chore == HouseholdChore.ReadLetter) { approvalPending = false; return; }
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerCamera || (playerCamera.transform.position - transform.position).sqrMagnitude > 36f) return;
        var hit = GetComponent<Collider>();
        Vector3 focus = hit ? hit.bounds.center : transform.position + Vector3.up;
        if (Physics.Linecast(playerCamera.transform.position, focus, ~0, QueryTriggerInteraction.Ignore)) return;
        RequestApproval();
    }

    private bool MotherSpeaking => (companion && companion.IsPresentingReply) ||
        (radio && radio.MotherSpeaking) || (kitchen && kitchen.MotherSpeaking);

    private void Update()
    {
        if (!ready || GameManager.Instance?.IsPaused == true || !animator || !animator.isActiveAndEnabled) return;
        if ((IsCalling || IsNodding) && oneShotRemaining > 0f)
        {
            oneShotRemaining -= Time.deltaTime * Mathf.Abs(animator.speed);
            if (oneShotRemaining > 0f) return;
        }
        if (approvalPending && Time.time > approvalExpires) approvalPending = false;
        if (MotherSpeaking)
        {
            ChangeState(animator.HasState(0, Talk) ? Talk : Idle);
            return;
        }
        if (approvalPending && !(companion && companion.IsConversing))
        {
            approvalPending = false;
            nextApproval = Time.time + 6f;
            PlayOneShot(Nod, "Mother_NodApprove", 1.8f);
            return;
        }
        ChangeState(Idle);
    }

    private void ChangeState(int next)
    {
        if (state == next) return;
        state = next;
        animator.CrossFadeInFixedTime(next, .2f, 0, 0f);
    }

    private void PlayOneShot(int next, string clipName, float fallbackSeconds)
    {
        oneShotRemaining = fallbackSeconds;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) { oneShotRemaining = clip.length; break; }
        ChangeState(next);
    }

    private void PauseAnimation(bool paused)
    {
        if (!animator) return;
        if (paused && !pausedAnimator)
        {
            speedBeforePause = animator.speed;
            animator.speed = 0f;
            pausedAnimator = true;
        }
        else if (!paused && pausedAnimator)
        {
            animator.speed = speedBeforePause;
            pausedAnimator = false;
        }
    }

    private void Unbind()
    {
        if (boundRadio) boundRadio.OnMotherCalled -= Greet;
        if (boundFlow) boundFlow.OnChoreCompleted -= OnChoreCompleted;
        if (pauseOwner) pauseOwner.OnPauseChanged -= PauseAnimation;
        boundRadio = null;
        boundFlow = null;
        pauseOwner = null;
        PauseAnimation(false);
    }

    private void OnDisable()
    {
        Unbind();
        approvalPending = false;
        oneShotRemaining = 0f;
        if (ready && animator && animator.isActiveAndEnabled) animator.Play(Idle, 0, 0f);
        state = Idle;
    }
}
