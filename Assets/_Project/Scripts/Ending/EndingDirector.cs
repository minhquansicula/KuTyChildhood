using System.Collections;
using UnityEngine;

/// <summary>
/// Directs the ~24 second multi-cut ending film ("Tỉnh Giấc Bình Minh Nơi Bàn Làm Việc"):
///   Act I (0.0s – 11.4s, HomeMemorySet):
///     - Shot 1 (0.0s – 3.8s): Close-up slider across the warm kitchen hearth & steaming rice pot.
///     - Shot 2 (3.8s – 7.8s): Sunlit home interior framed through wooden columns; Mother nods warmly with floating memory frames.
///     - Shot 3 (7.8s – 11.4s): Close-up on Mother's letter & vintage radio in warm sunlight as eyelids flutter and close.
///   Act II (11.4s – 24.0s, OfficeDawnSet):
///     - Shot 4 (11.4s – 18.2s): First-person awakening on the office desk at dawn; eyelids blink open, gaze falls on Mother's letter beside the keyboard, then lifts to the sunlit panoramic window.
///     - Shot 5 (18.2s – 24.0s): Rising from the office chair and stepping to the panoramic window as the finale music swells.
///   Act III (24.0s+, Epilogue Card):
///     - Letterbox retracts and EndingUI reveals over the living dawn panoramic window.
/// </summary>
public sealed class EndingDirector : MonoBehaviour
{
    [Header("World (authored)")]
    [SerializeField] private Transform worldRoot;
    [SerializeField] private EndingCameraRig rig;
    [SerializeField] private Light sun;
    [SerializeField] private Animator mother;
    [Header("Final card")]
    [SerializeField] private EndingUI finalCard;
    [Header("Timing")]
    [SerializeField] private float skipHoldSeconds = 1f;

    private sealed class ShotPath
    {
        public Transform a, b, mid;
        public Vector3 Position(float t)
        {
            if (a == null || b == null) return Vector3.zero;
            Vector3 control = mid != null ? mid.position : (a.position + b.position) * .5f;
            float u = 1f - t;
            return u * u * a.position + 2f * u * t * control + t * t * b.position;
        }
        public Quaternion Rotation(float t) =>
            a == null || b == null ? Quaternion.identity : Quaternion.Slerp(a.rotation, b.rotation, t);
    }

    private static readonly Quaternion HomeSunRotation = Quaternion.Euler(28f, 315f, 0f);
    private static readonly Quaternion OfficeSunRotation = Quaternion.Euler(13f, -96f, 0f);

    private GameObject homeSet, officeSet;
    private EndingCinematicUI ui;
    private EndingAudioMix mix;
    private EndingAtmosphere atmosphere;
    private EndingMemoryFrames memory;
    private ShotPath shot1, shot2, shot3, shot4A, shot4B, shot5, finalShot;
    private Transform homeFocus1, motherFocus, letterFocus, archAnchor;
    private Transform officeLetterFocus, officeWindowFocus, officeHorizonFocus;
    private Coroutine film;
    private bool finalStarted;
    private float holdTime;
    private float finalClock;
    private bool filmRunning;
    private float startTime;

    public bool FinalStarted => finalStarted;

    // ---------- lifecycle ----------
    private void Start()
    {
        if (worldRoot == null) { enabled = false; return; }
        if (rig == null) rig = FindObjectOfType<EndingCameraRig>();
        if (finalCard == null) finalCard = FindObjectOfType<EndingUI>();
        if (sun == null) sun = RenderSettings.sun;
        BindAnchors();

        Cursor.visible = false;
        mix = gameObject.AddComponent<EndingAudioMix>();
        mix.Initialize();

        ui = gameObject.AddComponent<EndingCinematicUI>();
        ui.Build(rig.Camera, sun);
        ui.SetVeil(1f);
        ui.SetBars(0f);
        ui.SetEyelids(0f);

        atmosphere = gameObject.AddComponent<EndingAtmosphere>();
        atmosphere.Initialize(rig.Camera.transform, sun);
        atmosphere.Prewarm();

        ActivateSet(true);
        ApplyPose(shot1, 0f, 30f);
        film = StartCoroutine(Film());
    }

    private void BindAnchors()
    {
        Transform home = worldRoot.Find("HomeMemorySet");
        Transform office = worldRoot.Find("OfficeDawnSet");
        if (home != null) homeSet = home.gameObject;
        if (office != null) officeSet = office.gameObject;

        Transform anchors = worldRoot.Find("Anchors");
        if (anchors == null) anchors = worldRoot;
        Transform Get(string n) => anchors.Find(n);

        shot1 = new ShotPath { a = Get("S1_Start"), mid = Get("S1_Mid"), b = Get("S1_End") };
        shot2 = new ShotPath { a = Get("S2_Start"), mid = Get("S2_Mid"), b = Get("S2_End") };
        shot3 = new ShotPath { a = Get("S3_Start"), mid = Get("S3_Mid"), b = Get("S3_End") };
        shot4A = new ShotPath { a = Get("S4_Start"), mid = null, b = Get("S4_Mid") };
        shot4B = new ShotPath { a = Get("S4_Mid"), mid = null, b = Get("S4_End") };
        shot5 = new ShotPath { a = Get("S5_Start"), mid = Get("S5_Mid"), b = Get("S5_End") };
        finalShot = new ShotPath { a = Get("Final_Start"), mid = Get("Final_Mid"), b = Get("Final_End") };

        homeFocus1 = Get("HomeFocus1");
        motherFocus = Get("MotherFocus");
        letterFocus = Get("LetterFocus");
        archAnchor = Get("ArchCenter");
        officeLetterFocus = Get("OfficeLetterFocus");
        officeWindowFocus = Get("OfficeWindowFocus");
        officeHorizonFocus = Get("OfficeHorizonFocus");
    }

    private void ActivateSet(bool homeActive)
    {
        if (homeSet != null) homeSet.SetActive(homeActive);
        if (officeSet != null) officeSet.SetActive(!homeActive);
        if (sun != null)
        {
            sun.transform.rotation = homeActive ? HomeSunRotation : OfficeSunRotation;
            sun.color = homeActive ? new Color(1f, .88f, .68f) : new Color(1f, .82f, .55f);
            sun.intensity = homeActive ? 2.2f : 2.6f;
        }
        RenderSettings.fog = true;
        RenderSettings.fogColor = homeActive ? new Color(.95f, .83f, .66f) : new Color(.96f, .82f, .62f);
        RenderSettings.fogDensity = homeActive ? .0045f : .0035f;
    }

    private void Update()
    {
        if (!finalStarted) Cursor.visible = false;
        if (finalStarted || ui == null) return;

        bool holding = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return) ||
                       Input.GetKey(KeyCode.KeypadEnter) || Input.GetMouseButton(0);
        holdTime = holding ? holdTime + Time.unscaledDeltaTime : Mathf.Max(0f, holdTime - Time.unscaledDeltaTime * 2f);
        ui.ShowSkip(holding || (filmRunning && Time.unscaledTime - startTime < 7f), holdTime / skipHoldSeconds);
        if (holdTime >= skipHoldSeconds) SkipToEnd();
    }

    // ---------- the film ----------
    private IEnumerator Film()
    {
        filmRunning = true;
        startTime = Time.unscaledTime;
        float clock = 0f;

        bool wind = false;
        bool line1A = false, line1B = false;
        bool line2A = false, line2B = false;
        bool line3A = false, line3B = false;
        bool line4 = false, line5 = false;
        bool beat1 = false, beat2 = false, switchedToOffice = false, riserPlayed = false, musicStarted = false;

        const float Shot1End = 5.2f;
        const float Shot2End = 10.2f;
        const float Shot3End = 16.4f;
        const float Shot4MidTime = 19.2f;
        const float Shot4End = 21.6f;
        const float Shot5End = 26.5f;

        while (clock < Shot5End)
        {
            clock += Time.unscaledDeltaTime;

            // Intro veil & cinematic letterbox bars
            float introVeil = 1f - Mathf.SmoothStep(0f, 1f, clock / 1.2f);
            ui.SetBars(Mathf.SmoothStep(0f, 1f, (clock - .15f) / 1.4f));

            if (!wind && clock > .1f) { wind = true; mix.FadeWind(.24f, 2.5f); }

            // ==================== ACT I: HOME MEMORY SET (0.0s - 16.4s) ====================
            if (clock < Shot1End)
            {
                // Shot 1 (0.0s - 5.2s): Kitchen Hearth & Steaming Rice Pot Close-up
                if (!line1A && clock > .25f)
                {
                    line1A = true;
                    mix.PlayVoiceLine("act2_ty_light_fire_001", .95f);
                    ui.ShowCaption("Bếp bắt lửa, cả nhà bắt đầu ấm.", .35f, 1.75f, .30f);
                }
                if (!line1B && clock > 2.75f)
                {
                    line1B = true;
                    mix.PlayVoiceLine("act2_ty_cook_rice_001", .95f);
                    ui.ShowCaption("Mùi cơm chín khiến con biết sắp được gọi về.", .35f, 1.75f, .30f);
                }

                float t = Mathf.SmoothStep(0f, 1f, clock / Shot1End);
                ApplyPose(shot1, t, Mathf.Lerp(30f, 28.5f, t));
                rig.SetFocus(DistanceTo(homeFocus1, 1.65f));
                rig.SetSway(.55f);
                rig.SetExposure(.15f);
                rig.SetBloom(1.1f);
                ui.UpdateSun(.35f);

                // Soft warm cross-dissolve at end of Shot 1
                float cutVeil = clock > Shot1End - .32f ? Mathf.SmoothStep(0f, .85f, (clock - (Shot1End - .32f)) / .32f) : 0f;
                ui.SetVeil(Mathf.Max(introVeil, cutVeil));
            }
            else if (clock < Shot2End)
            {
                // Shot 2 (5.2s - 10.2s): Sunlit Wooden Window looking out at the village rice fields
                if (!line2A && clock > Shot1End + .22f)
                {
                    line2A = true;
                    mix.PlayVoiceLine("act2_ty_set_table_001", .95f);
                    ui.ShowCaption("Mâm cơm đủ người là bữa ngon nhất.", .35f, 1.40f, .28f);
                }
                if (!line2B && clock > Shot1End + 2.25f)
                {
                    line2B = true;
                    mix.PlayVoiceLine("act2_ty_put_away_dishes_001", .95f);
                    ui.ShowCaption("Tiếng chén khẽ chạm nhau, việc nhà đã xong.", .35f, 1.95f, .30f);
                }

                float local = clock - Shot1End;
                float duration = Shot2End - Shot1End;
                float t = Mathf.SmoothStep(0f, 1f, local / duration);
                ApplyPose(shot2, t, Mathf.Lerp(32f, 30f, t));
                rig.SetFocus(DistanceTo(motherFocus, 2.6f));
                rig.SetSway(.6f);
                rig.SetExposure(.2f);
                rig.SetBloom(1.15f);
                ui.UpdateSun(.45f);

                float fadeInVeil = local < .35f ? Mathf.Lerp(.85f, 0f, local / .35f) : 0f;
                float fadeOutVeil = local > duration - .32f ? Mathf.SmoothStep(0f, .85f, (local - (duration - .32f)) / .32f) : 0f;
                ui.SetVeil(Mathf.Max(fadeInVeil, fadeOutVeil));
            }
            else if (clock < Shot3End)
            {
                // Shot 3 (10.2s - 16.4s): Close-up on Mother's Letter & Vintage Radio -> Mother's Voice + Eyelids Close
                if (!line3A && clock > Shot2End + .25f)
                {
                    line3A = true;
                    mix.PlayVoiceLine("act2_mom_letter_001", .98f);
                    ui.ShowCaption("“ Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. ”", .40f, 2.35f, .30f);
                }
                if (!line3B && clock > Shot2End + 3.40f)
                {
                    line3B = true;
                    ui.ShowCaption("“ Nhà mình luôn có một chỗ chờ con. ”", .35f, 2.00f, .35f);
                }

                float local = clock - Shot2End;
                float duration = Shot3End - Shot2End;
                float t = Mathf.SmoothStep(0f, 1f, local / duration);
                ApplyPose(shot3, t, Mathf.Lerp(29f, 27f, t));
                rig.SetFocus(DistanceTo(letterFocus, 1.05f));
                rig.SetSway(.45f);
                rig.SetExposure(.22f);
                rig.SetBloom(1.25f);
                ui.UpdateSun(.3f);

                float fadeInVeil = local < .35f ? Mathf.Lerp(.85f, 0f, local / .35f) : 0f;
                ui.SetVeil(fadeInVeil);

                // Eyelid drowsy flutter (14.2s - 14.9s) then full close (14.9s - 16.25s)
                float closure = 0f;
                if (clock >= 14.2f && clock < 14.9f)
                {
                    float b = (clock - 14.2f) / .7f;
                    closure = Mathf.Sin(b * Mathf.PI) * .58f;
                }
                else if (clock >= 14.9f)
                {
                    closure = Mathf.SmoothStep(0f, 1f, (clock - 14.9f) / 1.35f);
                }
                ui.SetEyelids(closure);

                if (!beat1 && clock > 15.1f) { beat1 = true; mix.Heartbeat(.8f); }
                if (!beat2 && clock > 16.05f) { beat2 = true; mix.Heartbeat(.65f); }
            }
            // ==================== ACT II: OFFICE DAWN SET (16.4s - 26.5s) ====================
            else if (clock < Shot4End)
            {
                if (!switchedToOffice)
                {
                    switchedToOffice = true;
                    ui.SetVeil(0f);
                    ui.SetEyelids(1f);
                    ActivateSet(false);
                    atmosphere.SetIntensity(.85f);
                }

                if (!line4 && clock > Shot3End + .60f)
                {
                    line4 = true;
                    mix.PlayVoiceLine("ending_ty_wake_001", .95f);
                    ui.ShowCaption("Ngoài khung cửa sổ, cơn mưa đêm đã tạnh... Nắng sớm đã lên rồi.", .45f, 3.25f, .40f);
                }
                if (!riserPlayed && clock > Shot4End - 1.8f)
                    { riserPlayed = true; mix.Riser(); }

                // Eyelids waking up: crack open, quick morning blink, open fully
                float awakeElapsed = clock - Shot3End;
                float closure = 0f;
                if (awakeElapsed < .18f)
                    closure = 1f;
                else if (awakeElapsed < .95f)
                    closure = Mathf.Lerp(1f, .38f, Mathf.SmoothStep(0f, 1f, (awakeElapsed - .18f) / .77f));
                else if (awakeElapsed < 1.35f)
                    closure = Mathf.Lerp(.38f, .82f, Mathf.SmoothStep(0f, 1f, (awakeElapsed - .95f) / .4f));
                else if (awakeElapsed < 2.25f)
                    closure = Mathf.Lerp(.82f, 0f, Mathf.SmoothStep(0f, 1f, (awakeElapsed - 1.35f) / .9f));
                ui.SetEyelids(closure);

                if (clock < Shot4MidTime)
                {
                    // Shot 4A: Lifting head from the office desk, looking down at Mother's letter beside the keyboard
                    float u = Mathf.SmoothStep(0f, 1f, (clock - Shot3End) / (Shot4MidTime - Shot3End));
                    ApplyPose(shot4A, u, Mathf.Lerp(50f, 43f, u));
                    rig.SetFocus(DistanceTo(officeLetterFocus, .85f));
                    rig.SetSway(.75f);
                    rig.SetExposure(Mathf.Lerp(.45f, .18f, u));
                    rig.SetBloom(Mathf.Lerp(1.65f, 1.25f, u));
                    ui.UpdateSun(.55f);
                }
                else
                {
                    // Shot 4B: Seated in chair, lifting gaze from the letter toward the sunlit East Panoramic Window
                    float u = Mathf.SmoothStep(0f, 1f, (clock - Shot4MidTime) / (Shot4End - Shot4MidTime));
                    ApplyPose(shot4B, u, Mathf.Lerp(43f, 39f, u));
                    float focus = Mathf.Lerp(DistanceTo(officeLetterFocus, .95f), DistanceTo(officeWindowFocus, 3.2f), u);
                    rig.SetFocus(focus);
                    rig.SetSway(.65f);
                    rig.SetExposure(.22f);
                    rig.SetBloom(1.3f);
                    ui.UpdateSun(.75f);
                }
            }
            else
            {
                // Shot 5 (21.6s - 26.5s): Rising from the office chair & stepping toward the dawn panoramic window
                ui.SetEyelids(0f);
                if (!musicStarted)
                {
                    musicStarted = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayFinaleMusic();
                    mix.FadeWind(.18f, 2.5f);
                }
                if (!line5 && clock > Shot4End + .35f)
                {
                    line5 = true;
                    mix.PlayVoiceLine("ending_ty_horizon_001", .98f);
                    ui.ShowCaption("Dù đi đâu làm gì... trong lòng vẫn luôn có một bếp lửa ấm để trở về.", .45f, 3.30f, .45f);
                }

                float local = clock - Shot4End;
                float duration = Shot5End - Shot4End;
                float u = Mathf.SmoothStep(0f, 1f, local / duration);

                // Base path from chair to window + subtle human footstep bob
                Vector3 pos = shot5.Position(u);
                float stepEnvelope = Mathf.Sin(u * Mathf.PI);
                pos.y += Mathf.Sin(local * 4.4f) * .018f * stepEnvelope;
                Quaternion rot = shot5.Rotation(u);
                rig.SetPose(pos, rot, Mathf.Lerp(39f, 36f, u));

                rig.SetFocus(Mathf.Lerp(DistanceTo(officeWindowFocus, 2.5f), DistanceTo(officeHorizonFocus, 14f), u));
                rig.SetSway(.65f);
                rig.SetExposure(.26f);
                rig.SetBloom(1.4f);
                ui.UpdateSun(.85f);
            }

            yield return null;
        }

        filmRunning = false;
        yield return StartCoroutine(BeginFinal(false));
    }

    // ---------- final ----------
    private IEnumerator BeginFinal(bool skipped)
    {
        if (finalStarted) yield break;
        finalStarted = true;
        ui.HideSkip();

        if (skipped)
        {
            yield return Fade(v => ui.SetFlash(v, new Color(1f, .93f, .74f)), 0f, 1f, .22f);
        }

        ActivateSet(false);
        if (memory != null) memory.HideAll();
        ui.HideCaption();
        ui.SetEyelids(0f);
        ui.SetDim(0f);
        ui.SetVeil(0f);
        mix.StopVoice();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayFinaleMusic();
        mix.FadeWind(.20f, 2.5f);

        ApplyPose(finalShot, 0f, 36f);
        rig.SetBloom(1.45f);
        rig.SetExposure(.26f);

        StartCoroutine(Fade(v => ui.SetBars(v), ui != null ? 1f : 0f, 0f, 1.5f));
        Cursor.visible = true;
        if (finalCard != null) finalCard.BeginFinalReveal();

        if (skipped)
        {
            yield return Fade(v => ui.SetFlash(v, new Color(1f, .93f, .74f)), 1f, 0f, 1.2f);
        }

        StartCoroutine(FinalDrift());
    }

    private IEnumerator FinalDrift()
    {
        finalClock = 0f;
        while (true)
        {
            finalClock += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(finalClock / 36f));
            ApplyPose(finalShot, t, Mathf.Lerp(36f, 34f, t));
            rig.SetFocus(DistanceTo(officeHorizonFocus, 14f));
            rig.SetSway(.5f);
            ui.UpdateSun(.7f);
            yield return null;
        }
    }

    public void SkipToEnd()
    {
        if (finalStarted) return;
        if (film != null) StopCoroutine(film);
        StopAllCoroutines();
        filmRunning = false;
        mix.SilenceAll(.2f);
        StartCoroutine(BeginFinal(true));
    }

    // ---------- helpers ----------
    private void ApplyPose(ShotPath shot, float t, float fov)
    {
        if (shot == null || shot.a == null || shot.b == null || rig == null) return;
        rig.SetPose(shot.Position(t), shot.Rotation(t), fov);
    }

    private float DistanceTo(Transform target, float fallback)
    {
        if (target == null || rig == null || rig.Camera == null) return fallback;
        return Vector3.Distance(rig.Camera.transform.position, target.position);
    }

    private static IEnumerator Fade(System.Action<float> apply, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
            yield return null;
        }
        apply(to);
    }
}
