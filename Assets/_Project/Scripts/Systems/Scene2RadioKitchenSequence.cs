using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Physical radio tuning and a small, persistent journey from the bedroom to the kitchen.
/// HouseholdChoreFlow alone owns rewards, ordering and checkpoints.</summary>
public sealed class Scene2RadioKitchenSequence : MonoBehaviour
{
    [Header("Authored world props")]
    public Transform radioRig;
    public Transform tuningKnob;
    public Collider knobCollider;
    public Transform needle;
    public Transform closeView;
    public Transform kitchenSpot;
    public event System.Action OnMotherCalled;
    public Transform woodBundle;
    public Transform woodPickup;
    public Collider pickupCollider;
    public Transform woodRest;
    public AudioClip stationClip;
    public AudioClip staticClip;
    public AudioClip motherClip;
    [TextArea] public string motherText = HouseholdChoreFlow.WakeupText;

    const float Station = .68f;
    const float Band = .075f;
    const float LockSeconds = 1.3f;
    HouseholdChoreFlow flow;
    Camera view;
    AudioSource stationAudio, noiseAudio, motherAudio;
    CanvasGroup captionGroup;
    TextMeshProUGUI caption;
    GameObject captionRoot;
    Vector3 savedCameraPosition;
    Quaternion savedCameraRotation;
    Vector3 startViewPosition;
    Quaternion startViewRotation;
    float savedFov, savedNear, moveTime;
    float frequency = .12f, lockTime, captionTime;
    float lastPointerAngle;
    bool dragging, savedCrosshair;
    bool radioOn;
    Coroutine callRoutine;
    GameManager pauseOwner;
    bool resumeStation, resumeNoise, resumeMother;
    float motherRemaining;
    int lastProgress = -1;
    readonly RaycastHit[] placementHits = new RaycastHit[32];

    public bool IsTuning { get; private set; }
    public bool CarryingWood { get; private set; }
    public bool RadioOn => radioOn;
    public bool MotherSpeaking => motherRemaining > 0f || resumeMother || (motherAudio && motherAudio.isPlaying);
    public float Frequency => frequency;
    public float Signal => Mathf.Clamp01(1f - Mathf.Abs(frequency - Station) / .24f);
    public Transform GuidanceTarget => CarryingWood ? woodRest : null;

    void Start()
    {
        flow = GetComponent<HouseholdChoreFlow>();
        view = Camera.main;
        if (!radioRig || !tuningKnob || !knobCollider || !needle || !closeView || !woodBundle || !woodPickup || !woodRest)
        {
            Debug.LogWarning("Scene 2 radio/wood bindings are incomplete; using household mini-games instead.", this);
            enabled = false;
            return;
        }
        // The legacy flow adds solid colliders to all imported visible meshes in Awake.
        // Carried props and the radio's controls must remain non-solid after that pass.
        foreach (var collider in (radioRig.parent ? radioRig.parent : radioRig).GetComponentsInChildren<Collider>(true))
            if (collider != knobCollider && collider != pickupCollider) collider.enabled = false;
        stationAudio = MakeSource(radioRig, stationClip, true, 1.2f, 13f);
        stationAudio.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 4200f;
        noiseAudio = MakeSource(radioRig, staticClip, true, 1.2f, 8f);
        motherAudio = MakeSource(kitchenSpot, motherClip, false, 5f, 28f);
        motherAudio.spatialBlend = .8f;
        KuTyAudioMix.Route(motherAudio, KuTyAudioBus.Voice);
        MakeCaption();
        // A loaded checkpoint reconstructs the visible world without replaying mother's call.
        RestoreWorld();
        BindPause();
    }

    void BindPause()
    {
        if (pauseOwner) pauseOwner.OnPauseChanged -= PauseAudio;
        pauseOwner = GameManager.Instance;
        if (pauseOwner) pauseOwner.OnPauseChanged += PauseAudio;
    }

    void OnEnable()
    {
        if (stationAudio) { BindPause(); RestoreWorld(); }
    }

    void PauseAudio(bool paused)
    {
        if (paused)
        {
            resumeStation = stationAudio && stationAudio.isPlaying;
            resumeNoise = noiseAudio && noiseAudio.isPlaying;
            resumeMother = motherAudio && motherAudio.isPlaying;
            if (resumeStation) stationAudio.Pause();
            if (resumeNoise) noiseAudio.Pause();
            if (resumeMother) motherAudio.Pause();
            dragging = false;
        }
        else
        {
            if (resumeStation && stationAudio) stationAudio.UnPause();
            if (resumeNoise && noiseAudio && IsTuning) noiseAudio.UnPause();
            if (resumeMother && motherAudio) motherAudio.UnPause();
            resumeStation = resumeNoise = resumeMother = false;
        }
    }

    AudioSource MakeSource(Transform at, AudioClip clip, bool loop, float near, float far)
    {
        var host = new GameObject(loop ? "Radio sound" : "Mother in kitchen");
        host.transform.SetParent(at != null ? at : transform, false);
        var source = host.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = near;
        source.maxDistance = far;
        source.volume = 0f;
        KuTyAudioMix.Route(source, KuTyAudioBus.SFX);
        return source;
    }

    public bool Handles(HouseholdChore chore) => isActiveAndEnabled &&
        ((chore == HouseholdChore.TuneRadio && radioRig && tuningKnob && knobCollider && needle && closeView) ||
         (chore == HouseholdChore.CarryWood && woodBundle && woodRest && woodPickup));

    public string PromptFor(HouseholdChore chore)
    {
        if (flow == null || !flow.IsCurrent(chore) || IsTuning) return "";
        if (chore == HouseholdChore.TuneRadio) return "[E] Ghé lại chỉnh radio";
        return CarryingWood ? "" : "[E] Nhấc bó củi mang vào bếp";
    }

    public void InteractWith(HouseholdChore chore)
    {
        if (flow == null || !flow.IsCurrent(chore) || GameManager.Instance?.InputBlocked == true) return;
        if (chore == HouseholdChore.TuneRadio) BeginTuning();
        else if (chore == HouseholdChore.CarryWood && !CarryingWood) PickUpWood();
    }

    void BeginTuning()
    {
        if (!view || IsTuning) return;
        IsTuning = true;
        dragging = false;
        lockTime = moveTime = 0f;
        savedCameraPosition = view.transform.localPosition;
        savedCameraRotation = view.transform.localRotation;
        startViewPosition = view.transform.position;
        startViewRotation = view.transform.rotation;
        savedFov = view.fieldOfView;
        savedNear = view.nearClipPlane;
        savedCrosshair = HUDController.Instance != null && HUDController.Instance.CrosshairVisible;
        HUDController.Instance?.SetCrosshairVisible(false);
        GameManager.Instance?.AcquireInput(this);
        view.nearClipPlane = .025f;
        flow.Voice.Stop();
        noiseAudio.Play();
        if (!stationAudio.isPlaying) stationAudio.Play();
        Say("Giữ chuột trái trên núm rồi xoay · Lăn chuột để chỉnh nhỏ\nNghe tiếng rè dịu đi và giữ sóng rõ một lát · ESC  Rời radio", 0f);
        UpdateRadioVisuals();
    }

    void Update()
    {
        if (flow == null || GameManager.Instance?.IsPaused == true || PauseUI.LastResumeFrame == Time.frameCount) return;
        // Restarting the chapter must also reset its world props and any pending call.
        if (lastProgress >= 2 && flow.ActiveIndex < 2) ResetWorld();
        lastProgress = flow.ActiveIndex;
        motherRemaining = Mathf.Max(0f, motherRemaining - Time.deltaTime);
        if (captionTime > 0f)
        {
            captionTime -= Time.deltaTime;
            if (captionTime <= 0f && !IsTuning) captionGroup.alpha = 0f;
        }
        if (IsTuning) TickTuning();
        else if (CarryingWood)
        {
            if (!flow.IsCurrent(HouseholdChore.CarryWood)) { RestoreWorld(); return; }
            if (GameManager.Instance?.InputBlocked != true && Input.GetKeyDown(KeyCode.Q)) ReturnWood();
        }
        if (!IsTuning && stationAudio != null)
            stationAudio.volume = Mathf.MoveTowards(stationAudio.volume,
                radioOn ? (MotherSpeaking || flow.Voice?.Voice?.IsPlaying == true ||
                    GetComponent<Scene2KitchenHandsOn>()?.MotherSpeaking == true ||
                    GetComponent<Scene2MotherCompanion>()?.IsConversing == true ? .055f : .17f) : 0f, Time.deltaTime * .3f);
    }

    void TickTuning()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { EndTuning(); return; }
        moveTime += Time.deltaTime;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveTime / .4f));
        view.transform.SetPositionAndRotation(Vector3.Lerp(startViewPosition, closeView.position, blend),
            Quaternion.Slerp(startViewRotation, closeView.rotation, blend));
        view.fieldOfView = Mathf.Lerp(savedFov, 48f, blend);
        if (blend < 1f) return;
        if (!Application.isFocused) { dragging = false; return; }

        bool overKnob = knobCollider.Raycast(view.ScreenPointToRay(Input.mousePosition), out _, 3f);
        Vector3 center = view.WorldToScreenPoint(tuningKnob.position);
        Vector2 offset = (Vector2)Input.mousePosition - (Vector2)center;
        float pointerAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (Input.GetMouseButtonDown(0) && overKnob)
        { dragging = true; lastPointerAngle = pointerAngle; }
        if (!Input.GetMouseButton(0)) dragging = false;
        if (dragging && offset.sqrMagnitude > 25f)
        {
            float delta = Mathf.DeltaAngle(lastPointerAngle, pointerAngle);
            AdjustFrequency(-delta / 270f);
            lastPointerAngle = pointerAngle;
        }
        if (overKnob) AdjustFrequency(Input.mouseScrollDelta.y * .018f);
        AdvanceSignalLock(Time.deltaTime);
    }

    void AdjustFrequency(float delta)
    {
        frequency = Mathf.Clamp01(frequency + delta);
        UpdateRadioVisuals();
    }

    void UpdateRadioVisuals()
    {
        tuningKnob.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(135f, -135f, frequency));
        Vector3 p = needle.localPosition;
        p.x = Mathf.Lerp(-.20f, .12f, frequency);
        needle.localPosition = p;
        if (noiseAudio) noiseAudio.volume = Mathf.Lerp(.2f, .008f, Signal);
        if (stationAudio) stationAudio.volume = Mathf.Lerp(.005f, .24f, Signal);
    }

    void AdvanceSignalLock(float deltaTime)
    {
        bool clear = Mathf.Abs(frequency - Station) <= Band;
        lockTime = clear ? lockTime + deltaTime : 0f;
        Say((clear ? "Đúng sóng rồi… giữ nguyên núm một lát." : "Tiếng rè… xoay chậm để tìm tiếng nhạc.") +
            "\n<color=#EDC781>" + Mathf.Lerp(88f, 108f, frequency).ToString("F1") +
            " MHz</color>   ·   Giữ chuột trái và xoay núm / lăn chuột   ·   ESC  Rời radio", 0f);
        if (lockTime < LockSeconds) return;
        EndTuning();
        if (!flow.Complete(HouseholdChore.TuneRadio, false)) return;
        radioOn = true;
        stationAudio.Play();
        Say("Tý: Đúng tiếng radio buổi sáng của nhà mình rồi…", 3f);
        callRoutine = StartCoroutine(CallFromKitchen());
    }

    public void EndTuning()
    {
        if (!IsTuning) return;
        IsTuning = dragging = false;
        if (view)
        {
            view.transform.localPosition = savedCameraPosition;
            view.transform.localRotation = savedCameraRotation;
            view.fieldOfView = savedFov;
            view.nearClipPlane = savedNear;
        }
        if (noiseAudio) noiseAudio.Stop();
        if (!radioOn && stationAudio) stationAudio.Stop();
        if (captionGroup) captionGroup.alpha = 0f;
        GameManager.Instance?.ReleaseInput(this);
        HUDController.Instance?.SetCrosshairVisible(savedCrosshair);
    }

    IEnumerator CallFromKitchen()
    {
        yield return new WaitForSeconds(3.2f);
        // Wait for other voiced chores/modals instead of talking over them.
        while (flow.Voice?.Voice?.IsPlaying == true || GameManager.Instance?.InputBlocked == true ||
            DialogueUI.Instance?.IsShowing == true || GetComponent<Scene2KitchenHandsOn>()?.MotherSpeaking == true)
            yield return null;
        // Do not replay a stale call if the player already delivered the wood.
        if (!flow.IsCurrent(HouseholdChore.CarryWood)) { callRoutine = null; yield break; }
        motherAudio.volume = .85f;
        OnMotherCalled?.Invoke();
        if (motherClip) motherAudio.Play();
        motherRemaining = motherClip ? motherClip.length + .5f : 6f;
        Say("<color=#EDC781>Mẹ · từ phía bếp</color>\n" + motherText, motherRemaining);
        yield return new WaitForSeconds(motherClip ? motherClip.length + .8f : 6f);
        if (flow.IsCurrent(HouseholdChore.CarryWood))
            Say("Mang giúp mẹ bó củi trong giỏ vào cạnh bếp nhé.\nĐến giỏ củi và nhấn E để nhấc lên.", 7f);
        callRoutine = null;
    }

    void PickUpWood()
    {
        if (!view) return;
        CarryingWood = true;
        woodBundle.SetParent(view.transform, false);
        woodBundle.localPosition = new Vector3(.27f, -.28f, .65f);
        woodBundle.localRotation = Quaternion.Euler(-12f, -15f, 15f);
        Say("Đem bó củi tới cạnh bếp. Nhìn vào chỗ đặt và nhấn E.\nWASD  Di chuyển   ·   Q  Trả củi về giỏ", 7f);
        RefreshGuidance();
    }

    public bool CanPlaceWood()
    {
        if (!CarryingWood || IsTuning || flow == null || !flow.IsCurrent(HouseholdChore.CarryWood) || !view) return false;
        Vector3 delta = woodRest.position - view.transform.position;
        if (delta.magnitude > 2.6f || Vector3.Dot(view.transform.forward, delta.normalized) < .72f) return false;
        // Require a clear path to the actual place point, even through doorway edges.
        int count = Physics.RaycastNonAlloc(view.transform.position, delta.normalized, placementHits,
            delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (placementHits[i].collider != null &&
                placementHits[i].collider.GetComponentInParent<FirstPersonController>() == null) return false;
        return count < placementHits.Length;
    }

    public void PlaceWood()
    {
        if (!CanPlaceWood()) return;
        CarryingWood = false;
        woodBundle.SetParent(woodRest, false);
        woodBundle.localPosition = Vector3.zero;
        woodBundle.localRotation = Quaternion.identity;
        if (!flow.Complete(HouseholdChore.CarryWood, false)) { ReturnWood(); return; }
        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        Say("Tý: Để củi ở đây, lát nữa mẹ nhóm bếp.\nGiờ mình múc nước phụ mẹ.", 6f);
        RefreshGuidance();
    }

    public void ReturnWood()
    {
        CarryingWood = false;
        woodBundle.SetParent(woodPickup, false);
        woodBundle.localPosition = Vector3.zero;
        woodBundle.localRotation = Quaternion.identity;
        Say("Đã đặt bó củi lại vào giỏ. Bạn có thể nhấc lên lần nữa.", 4f);
        RefreshGuidance();
    }

    void RefreshGuidance() => GetComponent<Scene2QuestHUD>()?.RefreshWorldObjective();

    void RestoreWorld()
    {
        if (flow == null) return;
        radioOn = flow.IsCompleted(HouseholdChore.TuneRadio);
        frequency = radioOn ? Station : .12f;
        UpdateRadioVisuals();
        if (radioOn && !stationAudio.isPlaying) stationAudio.Play();
        if (!radioOn) stationAudio.Stop();
        CarryingWood = false;
        woodBundle.SetParent(flow.IsCompleted(HouseholdChore.CarryWood) ? woodRest : woodPickup, false);
        woodBundle.localPosition = Vector3.zero;
        woodBundle.localRotation = Quaternion.identity;
        lastProgress = flow.ActiveIndex;
    }

    void ResetWorld()
    {
        EndTuning();
        if (callRoutine != null) StopCoroutine(callRoutine);
        callRoutine = null;
        motherAudio.Stop();
        motherRemaining = 0f;
        captionGroup.alpha = 0f;
        RestoreWorld();
    }

    void Say(string text, float seconds)
    {
        if (!caption) return;
        caption.text = text;
        captionTime = seconds;
        captionGroup.alpha = 1f;
    }

    void MakeCaption()
    {
        captionRoot = new GameObject("Radio kitchen subtitles", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        captionRoot.transform.SetParent(transform, false);
        var canvas = captionRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
        var scaler = captionRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        captionGroup = captionRoot.GetComponent<CanvasGroup>();
        captionGroup.blocksRaycasts = captionGroup.interactable = false;
        captionGroup.alpha = 0f;
        var panel = new GameObject("Subtitle backdrop", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(captionRoot.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 65f);
        rect.sizeDelta = new Vector2(1020f, 112f);
        panel.GetComponent<Image>().color = new Color(.035f, .04f, .035f, .86f);
        caption = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        caption.transform.SetParent(panel.transform, false);
        caption.rectTransform.anchorMin = Vector2.zero;
        caption.rectTransform.anchorMax = Vector2.one;
        caption.rectTransform.offsetMin = new Vector2(24f, 12f);
        caption.rectTransform.offsetMax = new Vector2(-24f, -12f);
        caption.font = GameUiTheme.Current.font;
        caption.fontSize = 25f;
        caption.color = new Color(.98f, .95f, .86f);
        caption.alignment = TextAlignmentOptions.Center;
        caption.raycastTarget = false;
    }

    void OnDisable()
    {
        EndTuning();
        if (pauseOwner) pauseOwner.OnPauseChanged -= PauseAudio;
        resumeStation = resumeNoise = resumeMother = false;
        motherRemaining = 0f;
        if (callRoutine != null) StopCoroutine(callRoutine);
        // Unity objects can already be destroyed when a sibling world rig unloads.
        // ?. only checks CLR null and would still call Stop on a destroyed source.
        if (stationAudio) stationAudio.Stop();
        if (noiseAudio) noiseAudio.Stop();
        if (motherAudio) motherAudio.Stop();
        if (captionGroup) captionGroup.alpha = 0f;
        if (CarryingWood && woodBundle && woodPickup) ReturnWood();
        GameManager.Instance?.ReleaseInput(this);
    }
}
