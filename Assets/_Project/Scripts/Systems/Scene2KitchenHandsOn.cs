using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene 2's three physical kitchen actions. The existing flow owns all progression.</summary>
public sealed class Scene2KitchenHandsOn : MonoBehaviour
{
    [Header("World bindings (authored by Scene2KitchenSetup)")]
    public Transform worldRoot, washRoot, fireRoot, cookRoot;
    public Transform washView, fireView, cookView;
    public Transform ricePot, water, spoon, drainHandle, hearth, blowTube, fuelLog, lid;
    public Transform[] kindling = new Transform[3];
    public Collider waterHit, drainHit, hearthHit, blowHit, fuelHit, lidHit;
    public Collider[] kindlingHits = new Collider[3];
    public Renderer waterRenderer;
    public Renderer fireStoveRenderer;
    public Light fireGlow, cookGlow;
    public DialogueSequence motherLines;
    public AudioClip waterClip, fireClip, simmerClip;
    public Transform motherVoiceAnchor;
    [Header("Imported asset articulation")]
    public bool importedKitchen;
    public Transform[] kindlingSlots = new Transform[0];
    public Collider[] retainedColliders = new Collider[0];
    public Renderer[] emberRenderers = new Renderer[0];
    public Transform fuelAxis, drainAnchor, steamAnchor;
    public Vector3 potTiltAxis = Vector3.forward;
    public float potTiltDegrees = -22f, washRadius = .18f;

    HouseholdChoreFlow flow;
    Camera view;
    HouseholdChore task;
    Vector3 cameraHome;
    Quaternion cameraRotation;
    float cameraFov, cameraNear, moveTime;
    Vector3 cameraFrom;
    Quaternion rotationFrom;
    bool crosshair, dragging, rinsing, needsDrain, fireLit, finishedRice;
    int draggedLog = -1, kindlingCount, rinseRound, lastIndex = -1;
    readonly bool[] placed = new bool[3];
    readonly Vector3[] logHomes = new Vector3[3];
    readonly Quaternion[] logRotations = new Quaternion[3];
    Vector3 spoonHome, fuelHome, lidHome, waterScale;
    Vector3 lidUp, lidSide;
    Quaternion potHome, waterWorldHome, blowHome;
    float washTurns, lastWashAngle, drainAmount, breath, ember, fuel = .85f, heat = .8f, cooked, lidLift;
    Vector2 pointerDown;
    float talkRemaining, talkCooldown;
    string instructions, lastMotherId;
    CanvasGroup captions;
    TextMeshProUGUI motherText, controlText;
    AudioSource motherVoice, workAudio;
    ParticleSystem splashes, drainStream, embers, steam;
    MaterialPropertyBlock waterColor, stoveColor;

    public bool IsWorking { get; private set; }
    public bool MotherSpeaking => talkRemaining > 0f || resumeMother || (motherVoice && motherVoice.isPlaying);
    public void SilenceMother()
    {
        if (motherVoice) motherVoice.Stop();
        talkRemaining = 0f;
        if (motherText) motherText.text = "";
        resumeMother = false;
    }
    private float idleSeconds, progressSample;
    private GameManager pauseOwner;
    private bool resumeMother, resumeWork;

    private float GestureProgress => task == HouseholdChore.WashRice
        ? rinseRound * 3f + Mathf.Abs(washTurns) + drainAmount
        : task == HouseholdChore.LightFire ? kindlingCount * 2f + ember : cooked + lidLift;

    private void PauseAudio(bool paused)
    {
        if (paused)
        {
            resumeMother = motherVoice && motherVoice.isPlaying;
            resumeWork = workAudio && workAudio.isPlaying;
            if (resumeMother) motherVoice.Pause();
            if (resumeWork) workAudio.Pause();
            ReleasePointer();
        }
        else
        {
            if (resumeMother && motherVoice) motherVoice.UnPause();
            if (resumeWork && workAudio && IsWorking) workAudio.UnPause();
            resumeMother = resumeWork = false;
        }
    }

    private void ReleasePointer()
    {
        dragging = rinsing = false;
        breath = 0f;
        if (draggedLog >= 0 && !placed[draggedLog])
        { kindling[draggedLog].localPosition = logHomes[draggedLog]; kindling[draggedLog].localRotation = logRotations[draggedLog]; }
        draggedLog = -1;
    }

    private void OnDestroy()
    {
        if (pauseOwner) pauseOwner.OnPauseChanged -= PauseAudio;
    }
    public HouseholdChore CurrentTask => task;
    public int RinseRound => rinseRound;
    public int KindlingCount => kindlingCount;
    public bool FireLit => fireLit;
    public float Heat => heat;
    public float Cooked => cooked;
    public bool NeedsDrain => needsDrain;

    void Start()
    {
        flow = GetComponent<HouseholdChoreFlow>();
        view = Camera.main;
        if (!HasBindings)
        {
            Debug.LogWarning("Scene 2 physical kitchen bindings are incomplete; using household mini-games instead.", this);
            enabled = false;
            return;
        }
        waterColor = new MaterialPropertyBlock(); stoveColor = new MaterialPropertyBlock();
        // HouseholdChoreFlow adds solids to visible meshes in Awake. These props
        // are manipulated in a close view and retain only their authored triggers.
        foreach (var c in worldRoot.GetComponentsInChildren<Collider>(true))
            if (!c.isTrigger && System.Array.IndexOf(retainedColliders, c) < 0) c.enabled = false;
        for (int i = 0; i < 3; i++) { logHomes[i] = kindling[i].localPosition; logRotations[i] = kindling[i].localRotation; }
        spoonHome = spoon.localPosition; fuelHome = fuelLog.localPosition; lidHome = lid.localPosition;
        waterScale = water.localScale; potHome = ricePot.localRotation;
        waterWorldHome = water.rotation; blowHome = blowTube.localRotation;
        lidUp = lid.parent.InverseTransformDirection(Vector3.up);
        lidSide = lid.parent.InverseTransformDirection(cookView.right);
        motherVoice = kitchenAudio("Mother at the stove", false);
        motherVoice.transform.position = motherVoiceAnchor ? motherVoiceAnchor.position : hearth.position + Vector3.up;
        workAudio = kitchenAudio("Kitchen work", true);
        splashes = InteractionVfx.Create(washRoot, "Rice washing drops", InteractionVfxKind.Droplet);
        splashes.transform.position = water.position;
        drainStream = InteractionVfx.Create(washRoot, "Draining water", InteractionVfxKind.Droplet);
        drainStream.transform.position = drainAnchor ? drainAnchor.position : drainHandle.position;
        embers = InteractionVfx.Create(hearth, "Warm embers", InteractionVfxKind.Ember);
        steam = InteractionVfx.Create(cookRoot, "Rice steam", InteractionVfxKind.Steam);
        steam.transform.position = steamAnchor ? steamAnchor.position : lid.position;
        splashes.Play(); embers.Play(); steam.Play();
        BuildCaptions();
        RestoreWorld();
        pauseOwner = GameManager.Instance;
        if (pauseOwner) pauseOwner.OnPauseChanged += PauseAudio;
    }

    AudioSource kitchenAudio(string title, bool loop)
    {
        var host = new GameObject(title);
        host.transform.SetParent(worldRoot, false);
        var audio = host.AddComponent<AudioSource>();
        audio.playOnAwake = false; audio.loop = loop; audio.spatialBlend = .7f;
        audio.dopplerLevel = 0f; audio.minDistance = 2f; audio.maxDistance = 12f;
        audio.volume = .55f;
        KuTyAudioMix.Route(audio, loop ? KuTyAudioBus.SFX : KuTyAudioBus.Voice);
        return audio;
    }

    private bool HasBindings => worldRoot && washRoot && fireRoot && cookRoot && washView && fireView && cookView &&
        ricePot && water && spoon && drainHandle && hearth && blowTube && fuelLog && lid && waterRenderer && fireGlow &&
        waterHit && drainHit && hearthHit && blowHit && fuelHit && lidHit &&
        kindling != null && kindling.Length == 3 && kindling[0] && kindling[1] && kindling[2] &&
        kindlingHits != null && kindlingHits.Length == 3 && kindlingHits[0] && kindlingHits[1] && kindlingHits[2];

    public bool Handles(HouseholdChore chore) => isActiveAndEnabled && HasBindings &&
        (chore == HouseholdChore.WashRice || chore == HouseholdChore.LightFire || chore == HouseholdChore.CookRice);

    public string PromptFor(HouseholdChore chore) => flow != null && flow.IsCurrent(chore) && !IsWorking
        ? chore == HouseholdChore.WashRice ? "[E] Ngồi vo gạo cùng mẹ" :
          chore == HouseholdChore.LightFire ? "[E] Xếp củi và nhóm bếp" : "[E] Canh nồi cơm giúp mẹ" : "";

    public void Begin(HouseholdChore chore)
    {
        if (!Handles(chore) || flow == null || !flow.IsCurrent(chore) || IsWorking || !view ||
            GameManager.Instance?.InputBlocked == true) return;
        task = chore;
        idleSeconds = 0f;
        progressSample = GestureProgress;
        IsWorking = true;
        dragging = rinsing = false; draggedLog = -1; moveTime = 0f;
        cameraHome = view.transform.localPosition; cameraRotation = view.transform.localRotation;
        cameraFov = view.fieldOfView; cameraNear = view.nearClipPlane;
        cameraFrom = view.transform.position; rotationFrom = view.transform.rotation;
        crosshair = HUDController.Instance != null && HUDController.Instance.CrosshairVisible;
        HUDController.Instance?.SetCrosshairVisible(false);
        GameManager.Instance?.AcquireInput(this);
        flow.Voice.Stop();
        view.nearClipPlane = .025f;
        captions.alpha = 1f;
        workAudio.transform.position = chore == HouseholdChore.WashRice ? water.position : chore == HouseholdChore.LightFire ? hearth.position : lid.position;
        workAudio.clip = chore == HouseholdChore.WashRice ? waterClip : chore == HouseholdChore.LightFire ? fireClip : simmerClip;
        workAudio.volume = .12f; if (workAudio.clip) workAudio.Play();
        if (chore == HouseholdChore.WashRice)
        {
            instructions = "Giữ phím F (hoặc rê chuột tròn) để vo gạo. Khi nước đục, bấm Space / Enter (hoặc kéo quai nồi) để chắt.\nESC  Đứng dậy · Tiếp tục được khi quay lại";
            Speak(needsDrain ? "wash_drain" : "wash_start", true);
        }
        else if (chore == HouseholdChore.LightFire)
        {
            instructions = "Bấm F hoặc kéo củi vào miệng bếp. Sau đó bấm / giữ phím F hoặc Space (hoặc click giữ ống thổi) để nhóm lửa.\nESC  Đứng dậy · Tiếp tục được khi quay lại";
            Speak(kindlingCount < 3 ? "fire_start" : "fire_blow", true);
        }
        else
        {
            instructions = "Dùng phím A / D (hoặc kéo củi) để chỉnh lửa. Giữ phím F để tự giữ lửa đều.\nKhi cơm cạn, bấm Space / Enter (hoặc kéo núm vung lên) · ESC  Đứng dậy";
            Speak("cook_start", true);
        }
        UpdateControls();
    }

    void Update()
    {
        if (!flow || GameManager.Instance?.IsPaused == true || PauseUI.LastResumeFrame == Time.frameCount) return;
        if (!captions || !motherText || !controlText)
        {
            if (captions) Destroy(captions.gameObject);
            BuildCaptions();
        }
        if (lastIndex != flow.ActiveIndex)
        {
            if (flow.ActiveIndex < lastIndex) { End(); RestoreWorld(); }
            else RefreshVisibility();
            lastIndex = flow.ActiveIndex;
        }
        talkCooldown = Mathf.Max(0f, talkCooldown - Time.deltaTime);
        if (talkRemaining > 0f)
        {
            talkRemaining -= Time.deltaTime;
            if (talkRemaining <= 0f) motherText.text = "";
        }
        if (captions) captions.alpha = IsWorking || talkRemaining > 0f ? 1f : 0f;
        if (motherText && motherText.transform.parent) motherText.transform.parent.gameObject.SetActive(talkRemaining > 0f);
        if (controlText && controlText.transform.parent) controlText.transform.parent.gameObject.SetActive(IsWorking);
        if (IsWorking)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { End(); return; }
            if (!Application.isFocused)
            {
                // Losing mouse focus must not leave a grabbed log or a held breath stuck.
                ReleasePointer();
            }
            Transform pose = task == HouseholdChore.WashRice ? washView : task == HouseholdChore.LightFire ? fireView : cookView;
            moveTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveTime / .4f));
            view.transform.SetPositionAndRotation(Vector3.Lerp(cameraFrom, pose.position, t), Quaternion.Slerp(rotationFrom, pose.rotation, t));
            view.fieldOfView = Mathf.Lerp(cameraFov, 48f, t);
            if (t >= 1f && Application.isFocused)
            {
                if (task == HouseholdChore.WashRice) TickWashing();
                else if (task == HouseholdChore.LightFire) TickFire();
                else TickCooking();
            }
            UpdateControls();
            if (Mathf.Abs(GestureProgress - progressSample) > .025f)
            {
                idleSeconds = 0f;
                progressSample = GestureProgress;
            }
            else idleSeconds += Time.deltaTime;
        }
        UpdateEffects();
    }

    bool Over(Collider hit) => hit && hit.enabled && hit.gameObject.activeInHierarchy &&
        hit.Raycast(view.ScreenPointToRay(Input.mousePosition), out _, 4f);

    void TickWashing()
    {
        if (needsDrain)
        {
            // Phím tắt chắt nước nhanh: Space, Enter, hoặc F
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F))
            {
                Drain(1f);
                return;
            }

            // Mouse manipulation stays attached to the actual handle; keys are the explicit fallback.
            if (Input.GetMouseButtonDown(0) && Over(drainHit))
            {
                dragging = true;
                pointerDown = Input.mousePosition;
            }
            if (dragging && Input.GetMouseButton(0))
            {
                float dragY = (pointerDown.y - Input.mousePosition.y) / (Screen.height * .14f);
                if (dragY > 0f) Drain(Mathf.Clamp01(dragY));
            }
            if (Input.GetMouseButtonUp(0) && needsDrain)
            {
                dragging = false;
                drainAmount = 0f;
                if (ricePot) ricePot.localRotation = potHome;
                if (importedKitchen && water) water.rotation = waterWorldHome;
            }
            return;
        }

        // Hỗ trợ giữ phím F để tự động vo gạo đều đặn
        if (Input.GetKey(KeyCode.F))
        {
            rinsing = false; // Reset the mouse-angle baseline when returning from keyboard assistance.
            washTurns += (washTurns < 0f ? -1f : 1f) * Time.deltaTime * 0.95f;
            float spoonAngle = Time.time * 7f;
            float spoonRadius = washRadius * 0.6f;
            if (spoon && water)
                spoon.position = water.position + new Vector3(Mathf.Cos(spoonAngle) * spoonRadius, 0.012f, Mathf.Sin(spoonAngle) * spoonRadius);
            InteractionVfx.Rate(splashes, 8f);
            if (Mathf.Abs(washTurns) >= 1.25f)
            {
                needsDrain = true;
                rinsing = false;
                InteractionVfx.Rate(splashes, 0f);
                Speak("wash_drain", true);
            }
            return;
        }

        // Rê chuột xoay quanh mặt nước
        if (Over(waterHit) && Input.GetMouseButton(0))
        {
            var plane = new Plane(Vector3.up, water.position);
            var ray = view.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float distance))
                WashAt(ray.GetPoint(distance));
        }
        else
        {
            rinsing = false;
            InteractionVfx.Rate(splashes, 0f);
        }
    }

    void WashAt(Vector3 point)
    {
        Vector3 offset = point - water.position;
        float radius = new Vector2(offset.x, offset.z).magnitude;
        if (radius < .025f || radius > washRadius) { rinsing = false; return; }
        float angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        if (rinsing)
        {
            // Preserve real circles; repeated back-and-forth shakes do not earn full turns.
            washTurns = Scene2CircularGesture.Accumulate(washTurns, lastWashAngle, angle);
        }
        lastWashAngle = angle;
        rinsing = true;
        if (spoon) spoon.position = point + Vector3.up * .012f;
        InteractionVfx.Rate(splashes, 8f);
        if (Mathf.Abs(washTurns) >= 1.25f)
        {
            needsDrain = true;
            rinsing = false;
            InteractionVfx.Rate(splashes, 0f);
            Speak("wash_drain", true);
        }
    }

    void Drain(float amount)
    {
        if (!needsDrain || !IsWorking || task != HouseholdChore.WashRice) return;
        drainAmount = Mathf.Clamp01(amount);
        ricePot.localRotation = Quaternion.AngleAxis(potTiltDegrees * drainAmount, potTiltAxis) * potHome;
        if (importedKitchen) water.rotation = waterWorldHome;
        if (drainAnchor) drainStream.transform.position = drainAnchor.position;
        water.localScale = new Vector3(waterScale.x, waterScale.y, waterScale.z) * (1f - .65f * drainAmount);
        if (!drainStream.isPlaying) drainStream.Play();
        InteractionVfx.Rate(drainStream, 22f * drainAmount);
        if (drainAmount < .95f) return;
        rinseRound++;
        needsDrain = dragging = rinsing = false; washTurns = drainAmount = 0f;
        water.localScale = waterScale; ricePot.localRotation = potHome; spoon.localPosition = spoonHome;
        if (importedKitchen) water.rotation = waterWorldHome;
        drainStream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        AudioManager.Instance?.PlaySFX(SfxId.WaterSplash);
        if (rinseRound >= 2) Finish("wash_done");
        else Speak("wash_again", true);
    }

    void TickFire()
    {
        if (kindlingCount < 3)
        {
            // Phím tắt đặt củi: Bấm F hoặc Space để tự động đặt thanh củi tiếp theo vào bếp
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!placed[i])
                    {
                        PlaceKindling(i);
                        break;
                    }
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
                for (int i = 0; i < 3; i++) if (!placed[i] && Over(kindlingHits[i])) { draggedLog = i; break; }
            if (draggedLog >= 0)
            {
                // Keep the wood attached to the pointer on a plane facing the player.
                var plane = new Plane(view.transform.forward, hearth.position);
                Ray ray = view.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out float d)) kindling[draggedLog].position = ray.GetPoint(d);
                if (Input.GetMouseButtonUp(0))
                {
                    int index = draggedLog; draggedLog = -1;
                    if (Over(hearthHit)) PlaceKindling(index);
                    else { kindling[index].localPosition = logHomes[index]; kindling[index].localRotation = logRotations[index]; }
                }
            }
            return;
        }

        // Đã đặt đủ 3 thanh củi: Thổi than
        // Phím tắt bấm một nhịp: Space, Enter, hoặc F
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F))
        {
            dragging = false;
            breath = 0f;
            Blow(0.6f);
            return;
        }

        // Giữ phím Space hoặc F để tự động thổi đều đặn nhịp nhàng
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.F))
        {
            breath += Time.deltaTime;
            blowTube.localRotation = blowHome * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 8f) * 3f);
            if (breath >= 0.55f)
            {
                breath = 0f;
                Blow(0.55f);
            }
            return;
        }

        // Mouse gestures still start on the visible blowing tube.
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.F)) breath = 0f;
        if (Input.GetMouseButtonDown(0) && Over(blowHit)) { dragging = true; breath = 0f; }
        if (dragging && Input.GetMouseButton(0)) breath += Time.deltaTime;
        if (dragging && Input.GetMouseButtonUp(0)) { dragging = false; Blow(breath); breath = 0f; }
        blowTube.localRotation = blowHome * Quaternion.Euler(0f, 0f, dragging ? Mathf.Sin(Time.time * 5f) * 2f : 0f);
    }

    void PlaceKindling(int index)
    {
        if (!IsWorking || task != HouseholdChore.LightFire || index < 0 || index >= 3 || placed[index]) return;
        placed[index] = true; kindlingCount++;
        SetPlacedWood(index);
        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        if (kindlingCount == 3) Speak("fire_blow", true);
    }

    void Blow(float seconds)
    {
        if (!IsWorking || task != HouseholdChore.LightFire || kindlingCount < 3) return;
        if (seconds < .2f) { Speak("fire_short"); return; }
        if (seconds > 1.1f) { ember = Mathf.Max(0f, ember - .15f); Speak("fire_gentle"); return; }
        // Người chơi giữ chuột hoặc bấm phím: tăng dần than hồng
        ember = Mathf.Clamp01(ember + 0.38f);
        if (embers) embers.Emit(14);
        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        if (ember >= 1f)
        {
            fireLit = true;
            Finish("fire_done");
        }
        else
        {
            Speak("fire_blow", true);
        }
    }

    void TickCooking()
    {
        if (cooked < 12f)
        {
            // Phím A/D hoặc mũi tên trái/phải để rút/đẩy củi chỉnh lửa
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                AdjustFuel(-Time.deltaTime * 0.45f);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                AdjustFuel(Time.deltaTime * 0.45f);

            // Giữ phím F để tự động duy trì mức lửa lý tưởng
            if (Input.GetKey(KeyCode.F))
            {
                if (heat < 0.48f) AdjustFuel(Time.deltaTime * 0.5f);
                else if (heat > 0.60f) AdjustFuel(-Time.deltaTime * 0.5f);
            }

            if (Input.GetMouseButtonDown(0) && Over(fuelHit)) { dragging = true; pointerDown = Input.mousePosition; }
            if (dragging && Input.GetMouseButton(0))
            {
                AdjustFuel((Input.mousePosition.x - pointerDown.x) / (Screen.width * .3f));
                pointerDown = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            Simmer(Time.deltaTime);
        }
        else
        {
            // Cơm đã cạn: bấm Space / Enter / F hoặc kéo núm vung lên
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F))
            {
                LiftLid(1f);
                return;
            }

            if (Input.GetMouseButtonDown(0) && Over(lidHit)) { dragging = true; pointerDown = Input.mousePosition; }
            if (dragging && Input.GetMouseButton(0)) LiftLid(Mathf.Clamp01((Input.mousePosition.y - pointerDown.y) / (Screen.height * .12f)));
            if (Input.GetMouseButtonUp(0) && IsWorking) { dragging = false; lid.localPosition = lidHome; }
        }
    }

    void AdjustFuel(float delta)
    {
        fuel = Mathf.Clamp01(fuel + delta);
        Vector3 inward = fuelAxis ? fuelLog.parent.InverseTransformDirection(fuelAxis.forward) : Vector3.left;
        fuelLog.localPosition = fuelHome + inward * ((fuel - .5f) * .32f);
    }

    void Simmer(float seconds)
    {
        if (!IsWorking || task != HouseholdChore.CookRice || cooked >= 12f) return;
        heat = Mathf.MoveTowards(heat, .15f + .8f * fuel + Mathf.Sin(Time.time * .6f) * .035f, seconds * .2f);
        if (heat >= .42f && heat <= .66f)
        {
            cooked = Mathf.Min(12f, cooked + seconds);
            if (cooked > 3f) Speak("cook_good");
        }
        else
        {
            cooked = Mathf.Max(0f, cooked - seconds * .15f);
            Speak(heat > .66f ? "cook_hot" : "cook_low");
        }
        if (cooked >= 12f) Speak("cook_check", true);
    }

    void LiftLid(float amount)
    {
        if (!IsWorking || task != HouseholdChore.CookRice || cooked < 12f) return;
        lidLift = Mathf.Clamp01(amount);
        lid.localPosition = lidHome + lidUp * (.22f * lidLift);
        if (lidLift >= .95f) { finishedRice = true; Finish("cook_done"); }
    }

    void Finish(string line)
    {
        HouseholdChore completed = task;
        End();
        if (!flow.Complete(completed, false)) return;
        RefreshVisibility();
        AudioManager.Instance?.PlaySFX(SfxId.TaskProgress);
        Speak(line, true);
    }

    public void End()
    {
        if (!IsWorking) return;
        SilenceMother();
        IsWorking = dragging = rinsing = false; breath = 0f;
        if (draggedLog >= 0 && !placed[draggedLog])
        {
            if (draggedLog < kindling.Length && kindling[draggedLog])
            {
                kindling[draggedLog].localPosition = logHomes[draggedLog];
                kindling[draggedLog].localRotation = logRotations[draggedLog];
            }
        }
        draggedLog = -1;
        if (ricePot) ricePot.localRotation = potHome;
        if (water)
        {
            water.localScale = waterScale;
            if (importedKitchen) water.rotation = waterWorldHome;
        }
        drainAmount = 0f;
        if (spoon) spoon.localPosition = spoonHome;
        if (lid) lid.localPosition = lidHome;
        if (blowTube) blowTube.localRotation = blowHome;
        if (view)
        {
            view.transform.localPosition = cameraHome;
            view.transform.localRotation = cameraRotation;
            view.fieldOfView = cameraFov;
            view.nearClipPlane = cameraNear;
        }
        GameManager.Instance?.ReleaseInput(this);
        HUDController.Instance?.SetCrosshairVisible(crosshair);
        if (workAudio) workAudio.Stop();
        InteractionVfx.Rate(splashes, 0f);
        if (drainStream) drainStream.Stop();
        if (controlText) controlText.text = "";
    }

    void RestoreWorld()
    {
        fireLit = flow.IsCompleted(HouseholdChore.LightFire);
        finishedRice = flow.IsCompleted(HouseholdChore.CookRice);
        rinseRound = flow.IsCompleted(HouseholdChore.WashRice) ? 2 : 0;
        washTurns = drainAmount = ember = cooked = lidLift = 0f;
        needsDrain = rinsing = false;
        heat = .8f; fuel = .85f; kindlingCount = fireLit ? 3 : 0;
        for (int i = 0; i < 3; i++)
        {
            placed[i] = fireLit;
            kindling[i].localPosition = logHomes[i];
            kindling[i].localRotation = logRotations[i];
            if (fireLit) SetPlacedWood(i);
        }
        AdjustFuel(0f); lid.localPosition = lidHome; water.localScale = waterScale; ricePot.localRotation = potHome;
        if (importedKitchen) water.rotation = waterWorldHome;
        blowTube.localRotation = blowHome;
        motherVoice.Stop(); talkRemaining = talkCooldown = 0f; motherText.text = "";
        RefreshVisibility(); lastIndex = flow.ActiveIndex;
    }

    void RefreshVisibility()
    {
        washRoot.gameObject.SetActive(flow.CurrentChore == HouseholdChore.WashRice);
        fireRoot.gameObject.SetActive(importedKitchen || flow.IsCompleted(HouseholdChore.WashRice));
        // The replacement stove remains part of the room before its chore is unlocked.
        cookRoot.gameObject.SetActive(true);
    }

    void SetPlacedWood(int index)
    {
        if (index < kindlingSlots.Length && kindlingSlots[index])
            kindling[index].SetPositionAndRotation(kindlingSlots[index].position, kindlingSlots[index].rotation);
        else kindling[index].position = hearth.position + new Vector3(0f, index * .045f, (index - 1) * .065f);
    }

    void UpdateEffects()
    {
        if (waterColor == null) waterColor = new MaterialPropertyBlock();
        if (stoveColor == null) stoveColor = new MaterialPropertyBlock();

        if (waterRenderer)
        {
            float cloud = Mathf.Clamp01(Mathf.Abs(washTurns) / 1.25f);
            waterColor.SetColor("_BaseColor", Color.Lerp(new Color(.6f, .72f, .69f, .34f), new Color(.87f, .84f, .73f, .85f), cloud));
            waterRenderer.SetPropertyBlock(waterColor);
        }
        if (fireGlow) fireGlow.intensity = fireLit ? .6f + Mathf.PerlinNoise(Time.time * 3f, 2f) * .18f : ember * .45f;
        if (cookGlow) cookGlow.intensity = fireLit ? .2f + heat * .35f : 0f;
        if (fireStoveRenderer)
        {
            stoveColor.SetColor("_BaseColor", Color.white * (fireLit ? 1f : .32f + ember * .35f));
            stoveColor.SetColor("_EmissionColor", fireLit ? Color.white : Color.black);
            fireStoveRenderer.SetPropertyBlock(stoveColor);
        }
        if (emberRenderers != null)
        {
            foreach (var renderer in emberRenderers)
            {
                if (!renderer) continue;
                stoveColor.Clear();
                stoveColor.SetColor("_EmissionColor", new Color(1f, .09f, .006f) * (fireLit ? .9f + heat : ember * .8f));
                renderer.SetPropertyBlock(stoveColor);
            }
        }
        if (embers) InteractionVfx.Rate(embers, fireLit ? 5f : ember * 4f);
        if (steam) InteractionVfx.Rate(steam, finishedRice ? 1.3f : IsWorking && task == HouseholdChore.CookRice ? heat * 8f : 0f);
        if (lid)
        {
            if (IsWorking && task == HouseholdChore.CookRice && cooked < 12f)
                lid.localPosition = lidHome + lidUp * (heat > .66f ? Mathf.Abs(Mathf.Sin(Time.time * 25f)) * .008f : 0f);
            else if (finishedRice) lid.localPosition = lidHome + lidSide * .57f + lidUp * .22f;
        }
        if (workAudio && IsWorking)
        {
            workAudio.volume = task == HouseholdChore.CookRice ? Mathf.Lerp(.06f, .24f, heat) : task == HouseholdChore.LightFire ? .08f + ember * .15f : rinsing ? .2f : .055f;
            workAudio.pitch = task == HouseholdChore.CookRice ? .75f + heat * .55f : 1f;
        }
    }

    void Speak(string id, bool immediate = false)
    {
        if (GameManager.Instance?.IsPaused == true || DialogueUI.Instance?.IsShowing == true ||
            flow.Voice?.Voice?.IsPlaying == true) return;
        var radio = GetComponent<Scene2RadioKitchenSequence>();
        if (radio && radio.MotherSpeaking) return;
        if (!motherLines || (!immediate && (talkCooldown > 0f || id == lastMotherId))) return;
        foreach (var line in motherLines.Lines)
        {
            if (line.id != id) continue;
            lastMotherId = id;
            talkRemaining = Mathf.Max(5f, line.ResolveDuration()); talkCooldown = talkRemaining + 3f;
            motherText.text = "<color=#EDC781>MẸ</color>  “" + line.text + "”";
            motherVoice.Stop(); motherVoice.clip = line.voiceClip;
            if (line.voiceClip) motherVoice.Play();
            captions.alpha = 1f;
            return;
        }
    }

    void UpdateControls()
    {
        string status = task == HouseholdChore.WashRice ? (needsDrain ? "Nước đã đục · Bấm Space / Enter (hoặc kéo quai nồi) để chắt" : "Lượt vo " + (rinseRound + 1) + "/2 · Giữ phím F hoặc rê chuột theo vòng tròn") :
            task == HouseholdChore.LightFire ? kindlingCount < 3 ? "Đã đặt " + kindlingCount + "/3 thanh củi · Bấm F hoặc kéo củi vào miệng bếp" : "Bấm / Giữ phím F hoặc Space để thổi than (" + Mathf.FloorToInt(ember * 100f) + "%)" :
            cooked >= 12f ? "Cơm đã cạn · Bấm Space / Enter (hoặc kéo núm vung) để mở" : heat > .66f ? "Lửa to · Bấm A hoặc kéo củi sang trái" : heat < .42f ? "Lửa yếu · Bấm D hoặc kéo củi sang phải" : "Cơm đang sôi lăn tăn (" + Mathf.FloorToInt(cooked / 12f * 100f) + "%) · Giữ F để giữ lửa";
        controlText.text = "<color=#EDC781>" + status + "</color>\n" + instructions;
        if (idleSeconds >= 7f)
            controlText.text += "\nGỢI Ý: " + (task == HouseholdChore.WashRice
                ? needsDrain ? "Bấm Space / Enter để chắt nước nhanh, hoặc kéo quai nồi xuống." : "Giữ phím F để tự động vo gạo, hoặc rê chuột tròn trên mặt nước."
                : task == HouseholdChore.LightFire
                    ? kindlingCount < 3 ? "Bấm phím F để đặt củi nhanh, hoặc kéo từng thanh củi vào miệng bếp." : "Bấm hoặc giữ phím F / Space để thổi bùng than hồng."
                    : cooked >= 12f ? "Bấm Space / Enter hoặc kéo núm vung lên." : "Bấm A / D để chỉnh lửa, hoặc giữ phím F để tự động giữ lửa ổn định.");
    }

    void BuildCaptions()
    {
        var go = new GameObject("Kitchen conversation", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        go.GetComponent<Canvas>().sortingOrder = 66;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        captions = go.GetComponent<CanvasGroup>(); captions.blocksRaycasts = captions.interactable = false; captions.alpha = 0f;
        motherText = Caption(go.transform, "Mother", new Vector2(0f, 820f), new Vector2(1060, 100), 26f);
        var motherPanel = (RectTransform)motherText.transform.parent;
        motherPanel.anchorMin = motherPanel.anchorMax = motherPanel.pivot = new Vector2(.5f, 1f);
        motherPanel.anchoredPosition = new Vector2(0f, -54f);
        controlText = Caption(go.transform, "Hands on instructions", new Vector2(0f, 50f), new Vector2(1120, 130), 22f);
    }

    TextMeshProUGUI Caption(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = panel.GetComponent<Image>(); image.color = new Color(.035f, .04f, .035f, .88f); image.raycastTarget = false;
        var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(panel.transform, false);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(24, 12); text.rectTransform.offsetMax = new Vector2(-24, -12);
        if (GameUiTheme.Current) text.font = GameUiTheme.Current.font;
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.98f, .95f, .86f); text.raycastTarget = false;
        return text;
    }

    void OnDisable()
    {
        End();
        if (motherVoice) motherVoice.Stop(); if (workAudio) workAudio.Stop();
        resumeMother = resumeWork = false;
        if (captions) captions.alpha = 0f;
        if (fireStoveRenderer) fireStoveRenderer.SetPropertyBlock(null);
        GameManager.Instance?.ReleaseInput(this);
    }
}
