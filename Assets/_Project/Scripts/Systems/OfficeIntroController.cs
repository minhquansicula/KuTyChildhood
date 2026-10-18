using System;
using UnityEngine;
using UnityEngine.Events;

// One local input owner. The legacy PlayerInteraction requires HUDController/task UI,
// so this independent scene uses an occluding raycast without adding that dependency.
[DefaultExecutionOrder(-100)]
public sealed class OfficeIntroController : MonoBehaviour
{
    public enum IntroState { Initialize, OpeningDialogue, ReturnToDesk, SeatingTransition, ReadyAtDesk }
    [Serializable] public class Line
    {
        public string speaker;
        [TextArea] public string text;
        public float duration = 5;
        public AudioClip voice;
        public bool slam;
    }
    [Header("Player and anchors")]
    public FirstPersonController motor;
    public CharacterController body;
    public Camera view;
    public Transform spawn, seat, chairRoot;
    [Header("Presentation")]
    public OfficeIntroHUD hud;
    public OfficeDoorInteractable door;
    public AudioSource voice, deskSlam, chairSound;
    public AudioSource[] ambience;
    public Line[] dialogue;
    [Header("Tuning")]
    public float interactionDistance = 2.7f, seatingDuration = .85f;
    public float seatedEyeHeight = 1.2f, lookSensitivity = 2f;
    public Vector2 openingPitch = new Vector2(-15,20), seatedPitch = new Vector2(-25,30);
    public float openingYaw = 30, seatedYaw = 55;
    public UnityEvent readyAtDesk = new UnityEvent();
    public IntroState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool ExitedBossRoom { get; private set; }
    public int DialogueIndex => lineIndex;
    public float LineElapsed => elapsed;
    public event Action OpeningFinished, PlayerExitedBossRoom, Seated, ReadyAtDesk;
    float elapsed, yaw, pitch, centerYaw, completionElapsed;
    int lineIndex;
    Vector3 seatStart, eyeStart;
    Quaternion seatRotationStart, eyeRotationStart;
    bool initialized, previousMotorEnabled, previousBodyEnabled, previousLocked, previousWalkingOnly;
    CursorLockMode previousCursor;
    bool previousCursorVisible;
    float[] ambientVolumes;
    AudioSource[] ownedAudio;
    readonly RaycastHit[] interactionHits = new RaycastHit[32];

    void Start()
    {
        if (!motor || !body || !view || !spawn || !seat || !chairRoot || !hud || !door || !door.leaf ||
            !hud.subtitle || !hud.objective || !hud.prompt || !hud.crosshair || !hud.resumeButton ||
            !hud.pausePanel || !hud.subtitlePanel || dialogue == null || dialogue.Length == 0)
        { Debug.LogError("OfficeIntroController: missing required Player/Anchor/Door/HUD/Dialogue reference.", this); enabled = false; return; }
        previousMotorEnabled = motor.enabled; previousBodyEnabled = body.enabled;
        previousLocked = motor.IsMovementLocked; previousWalkingOnly = motor.LockWalkingOnly;
        previousCursor = Cursor.lockState; previousCursorVisible = Cursor.visible;
        ownedAudio = GetComponentsInChildren<AudioSource>();
        ambience = ambience ?? new AudioSource[0];
        ambientVolumes = new float[ambience.Length];
        for (int i=0;i<ambience.Length;i++) if (ambience[i])
        { ambientVolumes[i]=ambience[i].volume; if (ambience[i].clip) ambience[i].Play(); }
        initialized = true;
        motor.TeleportTo(spawn.position, spawn.rotation);
        view.transform.localPosition = Vector3.up * 1.65f;
        motor.SetPitch(0); motor.LockMovement(true); motor.enabled = false;
        centerYaw = spawn.eulerAngles.y; yaw = 0; pitch = 0;
        door.Initialize();
        hud.resumeButton.onClick.AddListener(Resume);
        hud.SetPaused(false); hud.SetPrompt(""); hud.objective.text = "";
        hud.crosshair.gameObject.SetActive(false);
        State = IntroState.OpeningDialogue; lineIndex=0; BeginLine(); SetCursor(true);
    }
    void BeginLine()
    {
        elapsed = 0;
        Line line = dialogue[lineIndex];
        hud.SetSubtitle(line.speaker, line.text);
        if (voice) { voice.Stop(); voice.clip=line.voice; if (line.voice) voice.Play(); }
        if (line.slam && deskSlam && deskSlam.clip) deskSlam.Play();
    }
    void Update()
    {
        if (!initialized) return;
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!IsPaused);
        if (IsPaused) return;
        // Respect other systems' input ownership if this scene is later loaded additively.
        if (GameManager.Instance && GameManager.Instance.InputBlocked) { SetPaused(true); return; }
        float dt=Time.deltaTime;
        door.Tick(dt, body);
        if (State==IntroState.OpeningDialogue || State==IntroState.ReadyAtDesk) LimitedLook();
        if (State==IntroState.OpeningDialogue)
        {
            elapsed+=dt;
            Line line=dialogue[lineIndex];
            float duration=line.voice ? line.voice.length : Mathf.Max(.1f,line.duration);
            if (elapsed>=duration)
            {
                if (++lineIndex<dialogue.Length) BeginLine();
                else
                {
                    hud.SetSubtitle("",""); State=IntroState.ReturnToDesk;
                    motor.SetPitch(pitch); motor.LockMovement(false); motor.LockWalkingOnly=false; motor.enabled=true;
                    hud.objective.text="Quay lại bàn làm việc."; hud.crosshair.gameObject.SetActive(true);
                    OpeningFinished?.Invoke();
                }
            }
        }
        else if (State==IntroState.ReturnToDesk)
        {
            hud.SetPrompt(GetInteractionPrompt());
            if (Input.GetKeyDown(KeyCode.E)) TryInteract();
            if (!ExitedBossRoom && body.transform.position.x > door.transform.position.x+.35f)
            { ExitedBossRoom=true; PlayerExitedBossRoom?.Invoke(); }
        }
        else if (State==IntroState.SeatingTransition)
        {
            elapsed+=dt;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.1f,seatingDuration)));
            body.transform.SetPositionAndRotation(Vector3.Lerp(seatStart,seat.position,t),Quaternion.Slerp(seatRotationStart,seat.rotation,t));
            view.transform.localPosition=Vector3.Lerp(eyeStart,Vector3.up*seatedEyeHeight,t);
            view.transform.localRotation=Quaternion.Slerp(eyeRotationStart,Quaternion.Euler(8,0,0),t);
            if (elapsed>=seatingDuration)
            {
                State=IntroState.ReadyAtDesk; pitch=8; yaw=0; centerYaw=seat.eulerAngles.y;
                motor.SetPitch(pitch); completionElapsed=0;
                hud.objective.text="Đã về chỗ làm việc.";
                Seated?.Invoke(); readyAtDesk.Invoke(); ReadyAtDesk?.Invoke();
            }
        }
        else if (State==IntroState.ReadyAtDesk && (completionElapsed+=dt)>=2) hud.objective.text="";
        for(int i=0;i<ambience.Length;i++) if(ambience[i])
        {
            float factor=ExitedBossRoom ? 1 : door.State==OfficeDoorInteractable.DoorState.Closed ? .3f : .6f;
            ambience[i].volume=Mathf.MoveTowards(ambience[i].volume,ambientVolumes[i]*factor,dt*.05f);
        }
    }
    void LimitedLook()
    {
        float limit=State==IntroState.OpeningDialogue ? openingYaw : seatedYaw;
        Vector2 bounds=State==IntroState.OpeningDialogue ? openingPitch : seatedPitch;
        yaw=Mathf.Clamp(yaw+Input.GetAxis("Mouse X")*lookSensitivity,-limit,limit);
        pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*lookSensitivity,bounds.x,bounds.y);
        body.transform.rotation=Quaternion.Euler(0,centerYaw+yaw,0);
        view.transform.localRotation=Quaternion.Euler(pitch,0,0);
    }
    int Target()
    {
        if (IsPaused || State!=IntroState.ReturnToDesk) return 0;
        int count=Physics.RaycastNonAlloc(view.transform.position,view.transform.forward,interactionHits,interactionDistance,~0,QueryTriggerInteraction.Ignore);
        if(count==interactionHits.Length) return 0; // Fail closed if a very dense asset fills the buffer.
        RaycastHit hit=default;
        float nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var candidate=interactionHits[i];
            if(candidate.transform.IsChildOf(body.transform) || candidate.distance>=nearest) continue;
            nearest=candidate.distance; hit=candidate;
        }
        if(!hit.collider) return 0;
        if(hit.transform.IsChildOf(door.transform) && door.State==OfficeDoorInteractable.DoorState.Closed) return 1;
        if(hit.transform.IsChildOf(chairRoot) && ExitedBossRoom) return 2;
        return 0;
    }
    public string GetInteractionPrompt()
    {
        int target=Target();
        if(target==1) return door.PlayerBlocksSweep(body) ? "Lùi lại một chút để mở cửa." : "[E] Mở cửa";
        return target==2 ? "[E] Ngồi xuống" : "";
    }
    public bool TryInteract()
    {
        int target=Target();
        if(target==1) return door.TryOpen(body);
        if(target!=2) return false;
        motor.LockMovement(true); motor.enabled=false; body.enabled=false;
        seatStart=body.transform.position; seatRotationStart=body.transform.rotation;
        eyeStart=view.transform.localPosition; eyeRotationStart=view.transform.localRotation;
        elapsed=0; State=IntroState.SeatingTransition;
        hud.SetPrompt(""); hud.crosshair.gameObject.SetActive(false);
        if(chairSound && chairSound.clip) chairSound.Play();
        return true;
    }
    public void Resume() { SetPaused(false); }
    public void SetPaused(bool value)
    {
        if(!initialized || IsPaused==value) return;
        if(!value && GameManager.Instance && GameManager.Instance.InputBlocked) return;
        IsPaused=value; motor.IsMovementLocked=value || State!=IntroState.ReturnToDesk;
        hud.SetPaused(value); hud.SetPrompt(""); SetCursor(!value);
        foreach(var source in ownedAudio) if(source) { if(value) source.Pause(); else source.UnPause(); }
    }
    void OnApplicationFocus(bool focused) { if(!focused) SetPaused(true); }
    void SetCursor(bool locked) { Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None; Cursor.visible=!locked; }
    void OnDisable()
    {
        if(!initialized) return;
        foreach(var source in ownedAudio) if(source) source.Stop();
        if(hud)
        {
            if(hud.resumeButton) hud.resumeButton.onClick.RemoveListener(Resume);
            hud.SetSubtitle("",""); hud.SetPrompt(""); hud.SetPaused(false);
        }
        // Restore to a clear spawn before re-enabling collision (seat overlaps the chair).
        if(body && motor && spawn && view)
        {
            motor.TeleportTo(spawn.position,spawn.rotation); body.enabled=previousBodyEnabled;
            view.transform.localPosition=Vector3.up*1.65f; motor.SetPitch(0);
            motor.enabled=previousMotorEnabled; motor.IsMovementLocked=previousLocked; motor.LockWalkingOnly=previousWalkingOnly;
        }
        Cursor.lockState=previousCursor; Cursor.visible=previousCursorVisible; initialized=false;
    }
}
