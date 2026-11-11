using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class Scene2SpinningTop : InteractableBase
{
    private enum Stage { WaitingForWake, Ready, Pulling, Spinning, Follow, Complete }

    private Stage stage;
    private Camera view;
    private FirstPersonController player;
    private Transform top;
    private LineRenderer cord;
    private Transform kitchen;
    private HouseholdWorkSession session;
    private Material wood, stripe, thread;
    private Mesh topMesh;
    private Vector3 tablePoint, dragStart;
    private float pull;
    private bool dragging;
    private GameManager followInputOwner;
    private bool followLockedWithoutManager;

    public Transform LookTarget => top;

    public static Scene2SpinningTop Create(Camera camera)
    {
        var bed = GameObject.Find("BedBlock");
        var exit = GameObject.Find("BedExitPoint");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (bed == null || exit == null || camera == null || shader == null) return null;

        var root = new GameObject("Bedroom spinning top");
        root.layer = LayerMask.NameToLayer("Interactable");
        var toy = root.AddComponent<Scene2SpinningTop>();
        toy.view = camera;
        toy.player = camera.GetComponentInParent<FirstPersonController>();
        var stove = FindObjectOfType<Scene2KitchenHandsOn>();
        toy.kitchen = stove != null && stove.hearth != null ? stove.hearth : GameObject.Find("FoodPrepTable")?.transform;
        Vector3 away = Vector3.ProjectOnPlane(exit.transform.position - bed.transform.position, Vector3.up).normalized;
        if (away.sqrMagnitude < .01f) away = Vector3.right;
        var seat = GameObject.Find("cho_conquay")?.transform;
        Vector3 seatPosition = seat != null ? seat.position : new Vector3(11.2135f, .5341403f, 5.1777f);
        toy.Build(shader, seatPosition, away, exit.transform.position.y);
        return toy;
    }

    private void Build(Shader shader, Vector3 seatPosition, Vector3 away, float floor)
    {
        wood = new Material(shader) { color = new Color(.29f, .14f, .065f) };
        stripe = new Material(shader) { color = new Color(.59f, .19f, .085f) };
        thread = new Material(shader) { color = new Color(.74f, .64f, .46f) };
        transform.SetPositionAndRotation(new Vector3(seatPosition.x, floor, seatPosition.z), Quaternion.LookRotation(away));
        tablePoint = seatPosition + Vector3.up * .002f;
        if (Physics.Raycast(seatPosition + Vector3.up * .14f, Vector3.down, out var hit, .24f,
            ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > .55f)
            tablePoint.y = hit.point.y + .002f;
        top = new GameObject("Wooden top").transform;
        top.SetParent(transform, false);
        top.position = tablePoint;
        top.gameObject.layer = gameObject.layer;
        BuildWoodenTop();
        var target = top.gameObject.AddComponent<SphereCollider>();
        target.center = Vector3.up * .055f;
        target.radius = .09f;
        target.isTrigger = true;

        cord = new GameObject("Cotton pull cord").AddComponent<LineRenderer>();
        cord.transform.SetParent(transform, false);
        cord.sharedMaterial = thread;
        cord.startWidth = cord.endWidth = .003f;
        cord.numCapVertices = 3;
        cord.numCornerVertices = 3;
        DrawCord();
    }

    private void BuildWoodenTop()
    {
        var model = Resources.Load<GameObject>("Cutscenes/WoodenSpinningTop");
        if (model != null)
        {
            Instantiate(model, top, false);
            return;
        }

        const int sides = 32;
        var profile = new[]
        {
            new Vector2(0f, 0f), new Vector2(.005f, .007f), new Vector2(.017f, .018f),
            new Vector2(.028f, .029f), new Vector2(.033f, .039f), new Vector2(.034f, .044f),
            new Vector2(.032f, .050f), new Vector2(.021f, .057f), new Vector2(.009f, .059f),
            new Vector2(.008f, .073f), new Vector2(.006f, .078f), new Vector2(0f, .078f)
        };
        var vertices = new Vector3[profile.Length * sides];
        var timber = new System.Collections.Generic.List<int>();
        var paint = new System.Collections.Generic.List<int>();
        for (int ring = 0; ring < profile.Length; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                float angle = side * 2f * Mathf.PI / sides;
                int index = ring * sides + side;
                vertices[index] = new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y,
                    Mathf.Sin(angle) * profile[ring].x);
                if (ring == 0) continue;
                int next = ring * sides + (side + 1) % sides;
                var triangles = ring == 5 || ring == 7 ? paint : timber;
                triangles.Add(index - sides); triangles.Add(index); triangles.Add(next);
                triangles.Add(index - sides); triangles.Add(next); triangles.Add(next - sides);
            }
        }
        topMesh = new Mesh { name = "Turned wooden top", vertices = vertices, subMeshCount = 2 };
        topMesh.SetTriangles(timber, 0);
        topMesh.SetTriangles(paint, 1);
        topMesh.RecalculateNormals();
        top.gameObject.AddComponent<MeshFilter>().sharedMesh = topMesh;
        top.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { wood, stripe };
    }

    public void EnablePlay()
    {
        if (stage == Stage.WaitingForWake) stage = Stage.Ready;
    }

    public override string GetPromptText()
    {
        if (stage != Stage.Ready) return "";
        Vector3 offset = top.position - view.transform.position;
        return offset.x * offset.x + offset.z * offset.z < 1.4f * 1.4f && Mathf.Abs(offset.y) < 1.9f
            ? "[E] Cầm con quay" : "";
    }

    protected override void OnInteract()
    {
        if (string.IsNullOrEmpty(GetPromptText())) return;
        stage = Stage.Pulling;
        session = new HouseholdWorkSession(this, view);
        StartCoroutine(ShowToy());
    }

    private IEnumerator ShowToy()
    {
        Vector3 direction = Vector3.ProjectOnPlane(view.transform.position - tablePoint, Vector3.up).normalized;
        Vector3 eye = tablePoint + direction * .43f + Vector3.up * .38f;
        Vector3 raised = tablePoint + Vector3.up * .15f;
        for (float time = 0f; time < .68f; time += Time.deltaTime)
        {
            if (stage != Stage.Pulling) yield break;
            session.MoveTo(eye, Quaternion.LookRotation(tablePoint + Vector3.up * .03f - eye), time / .32f);
            top.position = Vector3.Lerp(tablePoint, raised, Mathf.Sin(time / .68f * Mathf.PI));
            DrawCord();
            yield return null;
        }
        top.position = tablePoint;
        DrawCord();
        dragging = false;
    }

    private void Update()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        if (stage != Stage.Pulling) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Input.mousePosition;
            dragging = true;
        }
        if (dragging)
        {
            pull = Mathf.Clamp01((Input.mousePosition - dragStart).magnitude / Mathf.Max(1f, Screen.height * .25f));
            DrawCord();
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                if (pull >= .24f) Launch();
                else { pull = 0f; DrawCord(); }
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) && stage == Stage.Pulling) { pull = .7f; Launch(); }
    }

    private void Launch()
    {
        stage = Stage.Spinning;
        StopAllCoroutines();
        DrawCord(1f);
        StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        float speed = Mathf.Lerp(620f, 1050f, pull);
        Vector3 edgeDirection = player != null
            ? Vector3.ProjectOnPlane(player.transform.position - tablePoint, Vector3.up).normalized
            : transform.forward;
        top.position = tablePoint;
        for (float time = 0f; time < 3f; time += Time.deltaTime)
        {
            top.rotation = Quaternion.Euler(3f * Mathf.Sin(time * 11f), time * speed, 2f);
            yield return null;
        }
        Vector3 edge = tablePoint + edgeDirection * .19f;
        for (float time = 0f; time < 1.25f; time += Time.deltaTime)
        {
            top.position = Vector3.Lerp(tablePoint, edge, Mathf.SmoothStep(0f, 1f, time / 1.25f));
            top.Rotate(Vector3.up, speed * .55f * Time.deltaTime, Space.Self);
            yield return null;
        }
        Vector3 floor = new Vector3(edge.x, transform.position.y + .015f, edge.z);
        for (float time = 0f; time < .46f; time += Time.deltaTime)
        {
            float t = Mathf.Clamp01(time / .46f);
            top.position = Vector3.Lerp(edge, floor, t * t) + edgeDirection * (.28f * t);
            top.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
            yield return null;
        }
        AudioManager.Instance?.PlaySFX(SfxId.MarbleHit);
        session.Close();
        session = null;
        if (kitchen == null || player == null) { Finish(); yield break; }
        yield return FollowTop();
        Finish();
    }

    private IEnumerator FollowTop()
    {
        Vector3 approach = Vector3.ProjectOnPlane(top.position - kitchen.position, Vector3.up).normalized;
        Vector3 rest = kitchen.position + approach * .72f;
        rest.y = transform.position.y + .025f;
        var route = new NavMeshPath();
        Vector3[] points = NavMesh.SamplePosition(top.position, out var start, 1.2f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(rest, out var end, 1.2f, NavMesh.AllAreas) &&
            NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, route) &&
            route.status == NavMeshPathStatus.PathComplete ? route.corners : new[] { rest };

        stage = Stage.Follow;
        followInputOwner = GameManager.Instance;
        if (followInputOwner != null) followInputOwner.AcquireInput(this, showCursor: false);
        else if (!player.IsMovementLocked)
        {
            player.LockMovement(true);
            followLockedWithoutManager = true;
        }
        try
        {
            float stuck = 0f;
            float elapsed = 0f;
            foreach (Vector3 corner in points)
            {
                Vector3 target = new Vector3(corner.x, rest.y, corner.z);
                while ((top.position - target).sqrMagnitude > .0025f)
                {
                    float dt = Time.deltaTime;
                    if (dt <= 0f) { yield return null; continue; }
                    elapsed += dt;
                    if (elapsed > 35f) yield break;
                    Vector3 offset = Vector3.ProjectOnPlane(top.position - player.transform.position, Vector3.up);
                    if (offset.sqrMagnitude < 1.45f * 1.45f)
                        top.position = Vector3.MoveTowards(top.position, target, .62f * dt);
                    top.Rotate(Vector3.up, 265f * dt, Space.Self);
                    Vector3 previous = player.transform.position;
                    if (offset.sqrMagnitude > .68f * .68f)
                        player.WalkToward(top.position - offset.normalized * .64f, .88f, dt);
                    Vector3 look = top.position - view.transform.position;
                    float pitch = Mathf.Atan2(-look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg;
                    float currentPitch = Mathf.DeltaAngle(0f, view.transform.localEulerAngles.x);
                    player.SetPitch(Mathf.MoveTowards(currentPitch, Mathf.Clamp(pitch, 8f, 36f), 50f * dt));
                    bool blocked = offset.sqrMagnitude >= 1.45f * 1.45f &&
                        (player.transform.position - previous).sqrMagnitude < .000001f;
                    stuck = blocked ? stuck + dt : 0f;
                    if (stuck > 2f) yield break;
                    yield return null;
                }
            }
            top.rotation = Quaternion.Euler(0f, 30f, 76f);
            yield return new WaitForSeconds(.3f);
        }
        finally
        {
            ReleaseFollowInput();
        }
    }

    private void ReleaseFollowInput()
    {
        if (followInputOwner != null) followInputOwner.ReleaseInput(this);
        followInputOwner = null;
        if (followLockedWithoutManager && player != null) player.LockMovement(false);
        followLockedWithoutManager = false;
    }

    private void DrawCord() => DrawCord(pull);

    private void DrawCord(float extension)
    {
        int wraps = Mathf.RoundToInt(Mathf.Lerp(36f, 2f, extension));
        const int tailPoints = 12;
        cord.positionCount = wraps + tailPoints;
        for (int i = 0; i < wraps; i++)
        {
            float angle = i * .57f;
            cord.SetPosition(i, top.position + new Vector3(Mathf.Cos(angle) * .034f, .046f - i * .0003f, Mathf.Sin(angle) * .034f));
        }
        Vector3 end = tablePoint - transform.forward * (.13f + .2f * extension);
        for (int i = 0; i < tailPoints; i++)
        {
            float t = (i + 1f) / tailPoints;
            Vector3 point = Vector3.Lerp(cord.GetPosition(wraps - 1), end, t);
            point += transform.right * ((1f - extension) * .024f * Mathf.Sin(t * Mathf.PI * 2f));
            point.y = Mathf.Max(tablePoint.y + .003f, point.y - Mathf.Sin(t * Mathf.PI) * .011f);
            cord.SetPosition(wraps + i, point);
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || GameManager.Instance?.IsPaused == true) return;
        string message = stage == Stage.Pulling ? "Giữ chuột trái, kéo dây rồi thả   ·   Space để quay   ·   Esc để thoát" :
            stage == Stage.Follow ? "Con quay lăn về phía bếp." : null;
        if (message != null) GUI.Box(new Rect(Screen.width * .2f, Screen.height * .84f, Screen.width * .6f, 32f), message);
    }

    private void Cancel()
    {
        StopAllCoroutines();
        session?.Close();
        session = null;
        stage = Stage.Ready;
        pull = 0f;
        dragging = false;
        top.position = tablePoint;
        top.rotation = Quaternion.identity;
        DrawCord();
    }

    private void Finish()
    {
        stage = Stage.Complete;
        cord.enabled = false;
        if (top != null) top.gameObject.SetActive(false);
        KitchenFlowManager.Instance?.ShowChecklist();
    }

    private void OnDisable()
    {
        session?.Close();
        ReleaseFollowInput();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused || stage != Stage.Pulling || !dragging) return;
        dragging = false;
        pull = 0f;
        DrawCord();
    }

    private void OnDestroy()
    {
        Destroy(wood);
        Destroy(stripe);
        Destroy(thread);
        Destroy(topMesh);
    }
}
