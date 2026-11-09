using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Private room navigation, baked from the authored layout and guarded by live physics. Never owns progress.</summary>
[DefaultExecutionOrder(-20), DisallowMultipleComponent]
public sealed class Scene2MotherHomeMovement : MonoBehaviour
{
    [Serializable] public sealed class ActivityPoint
    {
        public HouseholdChore chore;
        public Transform anchor;
    }
    [SerializeField] private Scene2MotherCompanion companion;
    [SerializeField] private NavMeshData navigation;
    [SerializeField] private ActivityPoint[] activities = Array.Empty<ActivityPoint>();
    [SerializeField, Min(.05f)] private float walkSpeed = .36f;
    private readonly List<Vector3> destinations = new List<Vector3>();
    private readonly Dictionary<HouseholdChore, Vector3> chorePlaces = new Dictionary<HouseholdChore, Vector3>();
    private NavMeshPath path;
    private NavMeshData data;
    private NavMeshDataInstance surface;
    private NavMeshQueryFilter filter;
    private HouseholdChoreFlow flow;
    private Scene2MotherDemo presentation;
    private Camera player;
    private Vector3 home;
    private float radius, height, nextTrip, blockedTime;
    private int corner, trip, progress;
    private bool ready;
    public bool IsMoving { get; private set; }
    public bool NavigationReady => ready;
    public int ReachablePlaces => destinations.Count;
    public float DistanceWalked { get; private set; }
    public Vector3 Home => home;
    public Vector3 Destination { get; private set; }
    public float WalkSpeed => walkSpeed;
    private void Awake() => path = new NavMeshPath();

    public void Configure(Scene2MotherCompanion mother, ActivityPoint[] points, float speed, NavMeshData roomNavigation)
    {
        companion = mother; activities = points ?? Array.Empty<ActivityPoint>(); walkSpeed = Mathf.Max(.05f, speed); navigation = roomNavigation;
    }

    private void Start()
    {
        if (!companion || companion.Actor != transform) return;
        flow = companion.GetComponent<HouseholdChoreFlow>();
        presentation = GetComponent<Scene2MotherDemo>();
        if (presentation && presentation.Animator)
        {
            // The user positioned the visible model independently of its old empty
            // interaction root. Rebase ONLY at runtime: keep the model's authored
            // world pose while bringing navigation, body, focus and voice to it.
            var visual = presentation.Animator.transform;
            Vector3 authoredPosition = visual.position;
            transform.position = new Vector3(authoredPosition.x, transform.position.y, authoredPosition.z);
            visual.position = authoredPosition;
        }
        home = transform.position;
        var capsule = GetComponent<CapsuleCollider>();
        height = capsule ? capsule.height * Mathf.Abs(transform.lossyScale.y) : 2.5f;
        radius = capsule ? capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) : .42f;
        if (capsule)
        {
            // Retain the authored interaction trigger; use a separate runtime body
            // so the child cannot simply walk through the standing mother.
            var body = new GameObject("Mother walking body (runtime)");
            body.transform.SetParent(transform, false); body.layer = gameObject.layer;
            var solid = body.AddComponent<CapsuleCollider>();
            solid.center = capsule.center; solid.height = capsule.height; solid.radius = capsule.radius;
        }
        BuildNavigation();
        progress = flow ? flow.ActiveIndex : 0;
        nextTrip = Time.time + 8f;
    }

    private void BuildNavigation()
    {
        Physics.SyncTransforms();
        if (!navigation) return;
        // Own a private instance, not global NavMesh.RemoveAllNavMeshData or an agent warp.
        data = Instantiate(navigation);
        surface = NavMesh.AddNavMeshData(data);
        filter = new NavMeshQueryFilter { agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID, areaMask = NavMesh.AllAreas };
        ready = Sample(home, out _) && surface.valid;
        if (ready) RefreshDestinations();
        if (!ready) Debug.LogWarning("Mother: no safe floor navigation at the authored standing point; remaining in place.", this);
    }

    private bool Sample(Vector3 position, out Vector3 result)
    {
        result = position;
        if (!NavMesh.SamplePosition(position, out var hit, .7f, filter) || Mathf.Abs(hit.position.y - home.y) > .15f) return false;
        result = hit.position; result.y = home.y;
        return true;
    }

    private void RefreshDestinations()
    {
        destinations.Clear();
        chorePlaces.Clear();
        foreach (var point in activities)
        {
            if (point == null || !point.anchor || point.chore == HouseholdChore.ReadLetter) continue;
            // Approach existing furniture, never its centre and never a new authored layout.
            bool found = false;
            foreach (float distance in new[] { 1.2f, 1.8f, 2.4f })
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector3 candidate = point.anchor.position + Quaternion.Euler(0, i * 30f, 0) * Vector3.forward * distance;
                    candidate.y = home.y;
                    if (!Sample(candidate, out candidate) || Vector3.Distance(candidate, home) < .9f ||
                        !CanOccupy(candidate) || !TryPath(home, candidate, out float length) || length > 22f) continue;
                    if (!destinations.Exists(p => (p - candidate).sqrMagnitude < 1f)) destinations.Add(candidate);
                    chorePlaces[point.chore] = candidate;
                    found = true; break;
                }
                if (found) break;
            }
        }
        // A small kitchen circuit remains possible even when another room's low doorway is impassable.
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = home + Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * 2f;
            if (Sample(candidate, out candidate) && CanOccupy(candidate) && TryPath(home, candidate, out float length) && length > 1f && length < 8f &&
                !destinations.Exists(p => (p - candidate).sqrMagnitude < 1f)) destinations.Add(candidate);
        }
        path.ClearCorners(); corner = 0;
    }

    private bool TryPath(Vector3 from, Vector3 to, out float length)
    {
        length = 0;
        if (!Sample(from, out from) || !Sample(to, out to) || !NavMesh.CalculatePath(from, to, filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        var corners = path.corners;
        Vector3 previous = from;
        foreach (var corner in corners)
        {
            Vector3 point = corner; point.y = home.y;
            if ((point - previous).sqrMagnitude > .0001f && !ClearSegment(previous, point, 0)) return false;
            previous = point;
        }
        for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
        return corners.Length > 0;
    }

    /// <summary>Also used by regression checks: no teleport or straight-line fallback for unreachable goals.</summary>
    public bool TryVisit(Vector3 goal)
    {
        if (!ready || !CanOccupy(goal) || !TryPath(transform.position, goal, out _)) return false;
        Destination = goal; corner = path.corners.Length > 1 ? 1 : 0; blockedTime = 0;
        return true;
    }

    private bool IsOwn(Collider collider) => collider.transform.IsChildOf(transform);
    private void CapsuleAt(Vector3 feet, out Vector3 bottom, out Vector3 top)
    {
        bottom = feet + Vector3.up * (radius + .09f);
        top = feet + Vector3.up * Mathf.Max(radius + .09f, height - radius - .03f);
    }
    public bool CanOccupy(Vector3 feet)
    {
        CapsuleAt(feet, out var bottom, out var top);
        foreach (var hit in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
            if (!IsOwn(hit)) return false;
        return true;
    }

    private bool ClearStep(Vector3 to)
        => ClearSegment(transform.position, to, .035f);

    private bool ClearSegment(Vector3 from, Vector3 to, float padding)
    {
        Vector3 delta = to - from;
        CapsuleAt(from, out var bottom, out var top);
        foreach (var hit in Physics.CapsuleCastAll(bottom, top, radius, delta.normalized, delta.magnitude + padding, ~0, QueryTriggerInteraction.Ignore))
            if (!IsOwn(hit.collider)) return false;
        return CanOccupy(to);
    }

    private void Update()
    {
        IsMoving = false;
        if (!ready || !flow || !companion || !companion.isActiveAndEnabled || !presentation || !presentation.isActiveAndEnabled ||
            !flow.ChecklistVisible || GameManager.Instance?.IsPaused == true) return;
        if (!player) player = Camera.main;
        if (companion.IsConversing)
        {
            if (player) Face(player.transform.position, Time.deltaTime);
            nextTrip = Time.time + 5f;
            return;
        }
        if (GameManager.Instance?.InputBlocked == true || DialogueUI.Instance?.IsShowing == true ||
            flow.Voice?.Voice?.IsPlaying == true || companion.IsPresentingReply ||
            (presentation && (presentation.IsCalling || presentation.IsNodding || presentation.IsTalking)) ||
            flow.GetComponent<Scene2KitchenHandsOn>()?.MotherSpeaking == true ||
            flow.GetComponent<Scene2RadioKitchenSequence>()?.MotherSpeaking == true ||
            flow.CurrentChore == HouseholdChore.ReadLetter || flow.CurrentChore == null) return;
        if (flow.ActiveIndex != progress)
        {
            if (flow.ActiveIndex < progress) { path.ClearCorners(); corner = 0; nextTrip = Time.time + 8f; }
            progress = flow.ActiveIndex;
        }
        if (player && Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up).sqrMagnitude < 1.1f * 1.1f)
        {
            Face(player.transform.position, Time.deltaTime); nextTrip = Time.time + 2f; return;
        }
        var corners = path.corners;
        if (corner >= corners.Length)
        {
            if (Time.time < nextTrip || destinations.Count == 0) return;
            Vector3 goal = trip % 3 == 2 ? home : destinations[trip % destinations.Count];
            if (trip % 3 == 0 && flow.CurrentChore.HasValue && chorePlaces.TryGetValue(flow.CurrentChore.Value, out var preferred)) goal = preferred;
            trip++;
            if (!TryVisit(goal)) nextTrip = Time.time + 5f;
            return;
        }
        Vector3 target = corners[corner]; target.y = home.y;
        if (Vector3.Distance(transform.position, target) < .045f)
        {
            corner++;
            if (corner >= corners.Length) nextTrip = Time.time + 9f + trip % 5;
            return;
        }
        Face(target, Time.deltaTime);
        Vector3 direction = (target - transform.position).normalized;
        if (Vector3.Dot(transform.forward, direction) < .85f) return;
        Vector3 step = Vector3.MoveTowards(transform.position, target, walkSpeed * Mathf.Min(Time.deltaTime, .05f));
        if (!ClearStep(step))
        {
            blockedTime += Time.deltaTime;
            if (blockedTime > 3f) { path.ClearCorners(); corner = 0; nextTrip = Time.time + 4f; }
            return;
        }
        blockedTime = 0;
        DistanceWalked += Vector3.Distance(transform.position, step);
        transform.position = step; Physics.SyncTransforms(); IsMoving = true;
    }

    private void Face(Vector3 target, float dt)
    {
        var direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        if (direction.sqrMagnitude > .01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 95f * Mathf.Min(dt, .05f));
    }
    private void OnDisable() => IsMoving = false;
    private void OnDestroy()
    {
        if (surface.valid) surface.Remove();
        if (data) Destroy(data);
    }
}
