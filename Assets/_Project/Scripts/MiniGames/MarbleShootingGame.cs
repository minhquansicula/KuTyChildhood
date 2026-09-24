using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MarbleShootingGame : MonoBehaviour
{
    public enum PlayState { Inactive, Aiming, Simulating, ShowingResult, Retrying, Completed }
    public PlayState State { get; private set; }
    public bool IsCompleted => State == PlayState.Completed;
    [SerializeField] private int targetMarblesToKnock = 3;
    [SerializeField] private int maxShots = 5;
    [SerializeField] private float minForce = 0.5f;
    [SerializeField] private float maxForce = 3f;
    [SerializeField] private float minimumDrag = 0.08f;
    [SerializeField] private float simulationTimeout = 8f;
    [SerializeField] private Rigidbody playerMarble;
    [SerializeField] private List<Rigidbody> targetMarbles = new List<Rigidbody>();
    [SerializeField] private Transform shootPosition;
    [SerializeField] private Collider ringCollider;
    [SerializeField] private Transform gameCameraPosition;
    [SerializeField] private Transform gameCameraLookAt;
    [SerializeField] private LineRenderer aimLine;
    private readonly Dictionary<Rigidbody, Pose> originalPoses = new Dictionary<Rigidbody, Pose>();
    private readonly HashSet<Rigidbody> knocked = new HashSet<Rigidbody>();
    private Camera gameCamera;
    private Transform cameraParent;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private bool cameraSaved;
    private bool dragging;
    private Vector3 dragStart;
    private int shots;
    private float simulationTime;
    private float settledTime;
    public event System.Action<int, int> OnShotFired;
    public event System.Action<int, int> OnMarbleKnocked;
    public event System.Action<bool> OnGameEnded;

    private void Awake()
    {
        foreach (var marble in targetMarbles)
            if (marble != null) originalPoses[marble] = new Pose(marble.position, marble.rotation);
        if (aimLine != null) { aimLine.positionCount = 2; aimLine.enabled = false; }
    }
    public void StartGame()
    {
        if (State != PlayState.Inactive) return;
        if (QuestManager.Instance != null && !QuestManager.Instance.CanPlayMarbles) return;
        gameCamera = Camera.main;
        if (playerMarble == null || shootPosition == null || ringCollider == null ||
            gameCameraPosition == null || gameCamera == null || originalPoses.Count < targetMarblesToKnock ||
            targetMarblesToKnock <= 0 || maxShots <= 0 || maxForce <= minForce)
        { Debug.LogError("Marble game setup is incomplete.", this); return; }
        GameManager.Instance?.AcquireInput(this);
        cameraParent = gameCamera.transform.parent;
        cameraPosition = gameCamera.transform.localPosition;
        cameraRotation = gameCamera.transform.localRotation;
        cameraSaved = true;
        gameCamera.transform.SetParent(null);
        gameCamera.transform.SetPositionAndRotation(gameCameraPosition.position, gameCameraPosition.rotation);
        if (gameCameraLookAt != null) gameCamera.transform.LookAt(gameCameraLookAt);
        ResetAllMarbles();
        State = PlayState.Aiming;
    }
    private void Update()
    {
        if (State == PlayState.Inactive || State == PlayState.Completed) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { CancelGame(); return; }
        if (State == PlayState.Aiming) HandleAiming();
        else if (State == PlayState.Simulating)
        {
            simulationTime += Time.deltaTime;
            settledTime = AllStopped() ? settledTime + Time.deltaTime : 0f;
            if ((simulationTime > 0.2f && settledTime >= 0.35f) || simulationTime >= simulationTimeout)
            {
                StopBodies();
                EvaluateShot();
            }
        }
    }
    private bool MouseOnPlane(out Vector3 point)
    {
        point = Vector3.zero;
        if (gameCamera == null || playerMarble == null) return false;
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, playerMarble.position);
        if (!plane.Raycast(ray, out float distance)) return false;
        point = ray.GetPoint(distance);
        return true;
    }
    private void HandleAiming()
    {
        if (Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
            MouseOnPlane(out Vector3 start) && Vector3.Distance(start, playerMarble.position) < 0.65f)
        { dragging = true; dragStart = start; }
        if (!dragging) return;
        if (Input.GetMouseButton(1)) { ClearAim(); return; }
        if (!MouseOnPlane(out Vector3 point))
        {
            if (Input.GetMouseButtonUp(0)) ClearAim();
            return;
        }
        Vector3 drag = dragStart - point;
        drag.y = 0;
        float force = Mathf.Lerp(minForce, maxForce, Mathf.Clamp01(drag.magnitude / 2.5f));
        if (aimLine != null)
        {
            aimLine.enabled = drag.magnitude >= minimumDrag;
            aimLine.SetPosition(0, playerMarble.position);
            aimLine.SetPosition(1, playerMarble.position + drag.normalized * force);
        }
        MarbleAimUI.Instance?.UpdateForceIndicator(Mathf.InverseLerp(minForce, maxForce, force));
        if (!Input.GetMouseButtonUp(0)) return;
        ClearAim();
        if (drag.magnitude < minimumDrag) return;
        Shoot(drag.normalized, force);
    }
    private void Shoot(Vector3 direction, float force)
    {
        if (State != PlayState.Aiming) return;
        shots++;
        simulationTime = settledTime = 0;
        State = PlayState.Simulating;
        playerMarble.WakeUp();
        playerMarble.AddForce(direction * force, ForceMode.Impulse);
        AudioManager.Instance?.PlaySFX("marble_shoot");
        OnShotFired?.Invoke(shots, maxShots);
        MarbleAimUI.Instance?.UpdateShots(shots, maxShots);
    }
    private bool AllStopped()
    {
        if (playerMarble.velocity.sqrMagnitude > 0.0025f) return false;
        foreach (var body in targetMarbles)
            if (body != null && body.velocity.sqrMagnitude > 0.0025f) return false;
        return true;
    }
    private void EvaluateShot()
    {
        Vector3 center = ringCollider.bounds.center;
        float radius = Mathf.Min(ringCollider.bounds.extents.x, ringCollider.bounds.extents.z);
        foreach (var body in targetMarbles)
        {
            if (body == null || knocked.Contains(body)) continue;
            Vector3 offset = body.position - center;
            bool fellOff = offset.y < -1f;
            offset.y = 0;
            if (offset.magnitude > radius || fellOff)
            {
                knocked.Add(body);
                OnMarbleKnocked?.Invoke(knocked.Count, targetMarblesToKnock);
            }
        }
        MarbleAimUI.Instance?.UpdateKnocked(knocked.Count, targetMarblesToKnock);
        if (knocked.Count >= targetMarblesToKnock)
        {
            State = PlayState.ShowingResult;
            Finish(true);
        }
        else if (shots >= maxShots)
        {
            State = PlayState.Retrying;
            OnGameEnded?.Invoke(false);
            StartCoroutine(Retry());
        }
        else { ResetPlayer(); State = PlayState.Aiming; }
    }
    private IEnumerator Retry()
    {
        // State changes before waiting, so extra input cannot spend another shot.
        yield return new WaitForSeconds(1.2f);
        ResetAllMarbles();
        State = PlayState.Aiming;
    }
    public void ResetAllMarbles()
    {
        ClearAim();
        shots = 0;
        knocked.Clear();
        foreach (var entry in originalPoses)
        {
            if (entry.Key == null) continue;
            StopBody(entry.Key);
            entry.Key.position = entry.Value.position;
            entry.Key.rotation = entry.Value.rotation;
        }
        ResetPlayer();
        MarbleAimUI.Instance?.Show(shots, maxShots, 0, targetMarblesToKnock);
    }
    private void ResetPlayer()
    {
        StopBody(playerMarble);
        playerMarble.position = shootPosition.position;
        playerMarble.rotation = shootPosition.rotation;
    }
    private static void StopBody(Rigidbody body)
    {
        if (body == null) return;
        body.velocity = body.angularVelocity = Vector3.zero;
        body.Sleep();
    }
    private void StopBodies()
    {
        StopBody(playerMarble);
        foreach (var body in targetMarbles) StopBody(body);
    }
    private void ClearAim()
    {
        dragging = false;
        if (aimLine != null) aimLine.enabled = false;
        MarbleAimUI.Instance?.UpdateForceIndicator(0);
    }
    private void Finish(bool won)
    {
        StopAllCoroutines();
        ClearAim();
        StopBodies();
        RestoreCamera();
        MarbleAimUI.Instance?.Hide();
        State = won ? PlayState.Completed : PlayState.Inactive;
        GameManager.Instance?.ReleaseInput(this);
        if (won)
        {
            OnGameEnded?.Invoke(true);
        }
    }
    public void CancelGame() { if (State != PlayState.Inactive && State != PlayState.Completed) Finish(false); }
    private void RestoreCamera()
    {
        if (!cameraSaved || gameCamera == null) return;
        gameCamera.transform.SetParent(cameraParent);
        gameCamera.transform.localPosition = cameraPosition;
        gameCamera.transform.localRotation = cameraRotation;
        cameraSaved = false;
    }
    private void OnDisable() { CancelGame(); }
}
