using UnityEngine;

/// <summary>Follows the office dialogue owner; never starts its own voice/timeline.</summary>
[DefaultExecutionOrder(-90), RequireComponent(typeof(Animator))]
public sealed class BossOfficeAnimation : MonoBehaviour
{
    public OfficeIntroController intro;
    public Vector3 openingViewOffset = new Vector3(-.6f, 0f, 0f);
    private Animator animator;
    private string currentState;
    private float returnElapsed;
    private bool awaitingImpact;
    private OfficeSceneController office;
    private float speechRemaining;
    public float SpeechSecondsRemaining => speechRemaining;
    public int ImpactCount { get; private set; }
    public float LastImpactLineTime { get; private set; }
    public string CurrentState => currentState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
    }
    private void Start()
    {
        SetState("Boss_Idle", 0);
        office = OfficeSceneController.Instance;
        if (office) office.SpeakerChanged += OnSceneSpeakerChanged;
    }
    private void OnDestroy()
    {
        if (office) office.SpeakerChanged -= OnSceneSpeakerChanged;
    }

    private void OnSceneSpeakerChanged(string speaker, float seconds)
    {
        if (intro && intro.State == OfficeIntroController.IntroState.OpeningDialogue) return;
        if (speaker == "SẾP")
        {
            speechRemaining = Mathf.Max(.1f, seconds);
            awaitingImpact = false;
            SetState("Boss_Talk_Command", .12f);
        }
        else if (speechRemaining > 0f) ReturnToIdle();
    }
    private void Update()
    {
        bool paused = (intro && intro.IsPaused) || (GameManager.Instance && GameManager.Instance.IsPaused);
        animator.speed = paused ? 0f : 1f;
        if (paused) return;
        if (speechRemaining > 0f)
        {
            speechRemaining -= Time.deltaTime;
            if (speechRemaining <= 0f) ReturnToIdle();
            return;
        }
        if (currentState == "Boss_ReturnIdle")
        {
            returnElapsed += Time.deltaTime;
            if (returnElapsed >= 1.2f) SetState("Boss_Idle", .08f);
        }
        else if (intro && intro.State != OfficeIntroController.IntroState.OpeningDialogue &&
                 currentState != "Boss_Idle") ReturnToIdle();
    }

    public void BeginLine(int index)
    {
        string id = intro && intro.Sequence != null ? intro.Sequence.GetLine(index)?.id : null;
        if (id == "act1_intro_slam_001" || (id == null && index == 2))
        {
            awaitingImpact = true;
            // No blend delay: the authored contact event must stay at 14/30 seconds.
            SetState("Boss_DeskSlam", 0f);
        }
        else if (id == "act1_intro_boss_003" || (id == null && index == 3)) SetState("Boss_Talk_Command", .12f);
        else if (id == "act1_intro_ty_001" || (id == null && index == 4)) ReturnToIdle();
        else if (id == "act1_intro_boss_001" || id == "act1_intro_boss_002" || index < 2)
        {
            if (currentState != "Boss_Talk_Stern") SetState("Boss_Talk_Stern", .12f);
        }
    }

    public void ReturnToIdle()
    {
        speechRemaining = 0f;
        awaitingImpact = false;
        returnElapsed = 0f;
        SetState("Boss_ReturnIdle", .1f);
    }
    private void SetState(string state, float fade)
    {
        currentState = state;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (fade <= 0f) animator.Play(hash, 0, 0f);
        else animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
    }

    // Authored animation event at the actual contact frame (14/30 seconds).
    public void OnDeskImpact()
    {
        if (!awaitingImpact || !intro || intro.IsPaused) return;
        awaitingImpact = false;
        ImpactCount++;
        LastImpactLineTime = intro.LineElapsed;
        if (intro.deskSlam && intro.deskSlam.clip) intro.deskSlam.Play();
        if (office) office.GetComponent<OfficeSceneEffects>()?.PlayDeskImpact();
    }
}
