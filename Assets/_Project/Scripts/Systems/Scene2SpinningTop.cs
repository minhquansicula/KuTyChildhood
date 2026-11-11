using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class Scene2SpinningTop : InteractableBase
{
    private enum Stage { WaitingForWake, Ready, Entering, Winding, Aiming, Spinning, Follow, WaitingAtKitchen, MeetingMother, Complete }
    private const float WindingTurns = 3f;
    private const float TopScale = 1.65f;
    private const float KitchenStopShort = 1.5f;
    private const float KitchenRollSpeed = 1.35f;

    private Stage stage;
    private Camera view;
    private FirstPersonController player;
    private Transform top;
    private LineRenderer cord;
    private Transform cordKnot;
    private Transform kitchen;
    private HouseholdWorkSession session;
    private Material wood, stripe, thread, importedMaterial;
    private Mesh topMesh;
    private Vector3 tablePoint, cordEnd, throwDirection;
    private Vector2 dragStart;
    private float pull;
    private float windingAngle, lastMouseAngle;
    private float cordAnchorAngle;
    private int windingDirection;
    private bool dragging, windingDrag;

    public Transform LookTarget => top;
    public float WindingProgress => Mathf.Clamp01(Mathf.Abs(windingAngle) / (WindingTurns * Mathf.PI * 2f));
    public bool ReadyToThrow => stage == Stage.Aiming;
    public bool AwaitingKitchenVisit => stage == Stage.WaitingAtKitchen;

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
        thread = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? shader)
            { color = new Color(.76f, .65f, .49f) };
        transform.SetPositionAndRotation(new Vector3(seatPosition.x, floor, seatPosition.z), Quaternion.LookRotation(away));
        tablePoint = seatPosition + Vector3.up * .002f;
        if (Physics.Raycast(seatPosition + Vector3.up * .14f, Vector3.down, out var hit, .24f,
            ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > .55f)
            tablePoint.y = hit.point.y + .002f;
        top = new GameObject("Wooden top").transform;
        top.SetParent(transform, false);
        top.position = tablePoint;
        top.localScale = Vector3.one * TopScale;
        top.gameObject.layer = gameObject.layer;
        BuildWoodenTop();
        var target = top.gameObject.AddComponent<SphereCollider>();
        target.center = Vector3.up * .047f;
        target.radius = .065f;
        target.isTrigger = true;

        cord = new GameObject("Cotton pull cord").AddComponent<LineRenderer>();
        cord.transform.SetParent(transform, false);
        cord.sharedMaterial = thread;
        cord.startWidth = cord.endWidth = .004f;
        cord.numCapVertices = 6;
        cord.numCornerVertices = 6;
        var knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot.name = "Cotton cord knot";
        knot.transform.SetParent(cord.transform, false);
        knot.transform.localScale = new Vector3(.011f, .008f, .014f);
        knot.GetComponent<Collider>().enabled = false;
        knot.GetComponent<Renderer>().sharedMaterial = thread;
        cordKnot = knot.transform;
        ResetCordEnd();
        DrawCord();
    }

    private void BuildWoodenTop()
    {
        var model = Resources.Load<GameObject>("Cutscenes/WoodenSpinningTop");
        if (model != null)
        {
            var visual = Instantiate(model, top, false);
            visual.name = "WoodenSpinningTop visual";
            var baseColor = Resources.Load<Texture2D>("Cutscenes/WoodenSpinningTop_BaseColor");
            if (baseColor != null)
            {
                importedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                importedMaterial.name = "Wooden spinning top wood and metal";
                importedMaterial.SetTexture("_BaseMap", baseColor);
                importedMaterial.SetFloat("_Smoothness", .36f);
                var normal = Resources.Load<Texture2D>("Cutscenes/WoodenSpinningTop_Normal");
                if (normal != null)
                {
                    importedMaterial.SetTexture("_BumpMap", normal);
                    importedMaterial.EnableKeyword("_NORMALMAP");
                }
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = importedMaterial;
            }
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
        stage = Stage.Entering;
        session = new HouseholdWorkSession(this, view);
        StartCoroutine(ShowToy());
    }

    private IEnumerator ShowToy()
    {
        Vector3 direction = Vector3.ProjectOnPlane(view.transform.position - tablePoint, Vector3.up).normalized;
        if (direction.sqrMagnitude < .01f) direction = -transform.forward;
        ResetCordEnd(direction);
        Vector3 eye = tablePoint + direction * .50f + Vector3.up * .64f;
        Vector3 focus = tablePoint + direction * .035f + Vector3.up * .025f;
        Quaternion facing = Quaternion.LookRotation(focus - eye);
        Vector3 raised = tablePoint + Vector3.up * .12f;
        for (float time = 0f; time < .68f; time += Time.deltaTime)
        {
            if (stage != Stage.Entering) yield break;
            session.MoveTo(eye, facing, time / .45f, 54f);
            top.position = Vector3.Lerp(tablePoint, raised, Mathf.Sin(time / .68f * Mathf.PI));
            DrawCord();
            yield return null;
        }
        session.MoveTo(eye, facing, 1f, 54f);
        top.position = tablePoint;
        stage = Stage.Winding;
        DrawCord();
        dragging = false;
    }

    private void Update()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        if (stage == Stage.WaitingAtKitchen) { CheckKitchenArrival(); return; }
        if (stage != Stage.Entering && stage != Stage.Winding && stage != Stage.Aiming) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
        if (stage == Stage.Entering) return;
        if (stage == Stage.Winding)
        {
            if (Input.GetMouseButtonDown(0)) BeginWinding(Input.mousePosition);
            if (windingDrag && Input.GetMouseButton(0)) TrackWinding(Input.mousePosition);
            if (windingDrag && Input.GetMouseButtonUp(0)) EndWinding();
            return;
        }
        if (Input.GetMouseButtonDown(0) && PointsAtKnot(Input.mousePosition))
        {
            dragStart = Input.mousePosition;
            dragging = true;
        }
        if (dragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - dragStart;
            pull = Mathf.Clamp01(delta.magnitude / Mathf.Max(1f, Screen.height * .28f));
            Vector3 direction = view.transform.right * delta.x +
                Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized * delta.y;
            if (direction.sqrMagnitude > .01f) throwDirection = direction.normalized;
            if (TryGetCordOffset(Input.mousePosition, out var offset))
                cordEnd = tablePoint + offset + Vector3.up * .009f;
            DrawCord();
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                if (pull >= .3f) Launch();
                else { pull = 0f; DrawCord(); }
            }
        }
    }

    private bool PointsAtKnot(Vector2 mouse)
    {
        Vector3 point = view.WorldToScreenPoint(cordEnd);
        float tolerance = Mathf.Max(12f, Screen.height * .024f);
        return point.z > 0f && (mouse - (Vector2)point).sqrMagnitude <= tolerance * tolerance;
    }

    private bool TryGetCordOffset(Vector2 mouse, out Vector3 offset)
    {
        var plane = new Plane(Vector3.up, tablePoint + Vector3.up * .009f);
        var ray = view.ScreenPointToRay(mouse);
        offset = Vector3.zero;
        if (!plane.Raycast(ray, out float distance)) return false;
        offset = Vector3.ProjectOnPlane(ray.GetPoint(distance) - tablePoint, Vector3.up);
        return true;
    }

    private void BeginWinding(Vector2 mouse)
    {
        if (stage != Stage.Winding || !PointsAtKnot(mouse) || !TryGetCordOffset(mouse, out var offset)) return;
        lastMouseAngle = Mathf.Atan2(offset.z, offset.x);
        windingDrag = true;
    }

    private void TrackWinding(Vector2 mouse)
    {
        if (!windingDrag || !TryGetCordOffset(mouse, out var offset) || offset.sqrMagnitude < .065f * .065f) return;
        float angle = Mathf.Atan2(offset.z, offset.x);
        float delta = Mathf.DeltaAngle(lastMouseAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        lastMouseAngle = angle;
        if (Mathf.Abs(delta) > .52f || Mathf.Abs(delta) < .003f) return;
        if (windingDirection == 0 && Mathf.Abs(delta) > .025f) windingDirection = delta > 0 ? 1 : -1;
        windingAngle = Mathf.Clamp(windingAngle + delta * windingDirection, 0f, WindingTurns * Mathf.PI * 2f);
        float reach = Mathf.Lerp(.32f, .145f, WindingProgress);
        cordEnd = tablePoint + offset.normalized * Mathf.Clamp(offset.magnitude, .072f, reach) + Vector3.up * .009f;
        DrawCord();
    }

    private void EndWinding()
    {
        windingDrag = false;
        if (WindingProgress < 1f) return;
        stage = Stage.Aiming;
        pull = 0f;
        throwDirection = transform.forward;
        AudioManager.Instance?.PlaySFX(SfxId.MarbleHit);
        DrawCord();
    }

    private void ResetCordEnd() => ResetCordEnd(-transform.forward);

    private void ResetCordEnd(Vector3 direction)
    {
        Vector3 offset = direction * .18f;
        cordEnd = tablePoint + offset + Vector3.up * .009f;
        cordAnchorAngle = Mathf.Atan2(offset.z, offset.x);
    }

    private void Launch()
    {
        if (stage != Stage.Aiming || WindingProgress < 1f || pull < .3f) return;
        stage = Stage.Spinning;
        StopAllCoroutines();
        cord.enabled = false;
        cordKnot.gameObject.SetActive(false);
        StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        float speed = Mathf.Lerp(620f, 1050f, pull);
        Vector3 edgeDirection = throwDirection.sqrMagnitude > .01f ? throwDirection : transform.forward;
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
        session?.Close();
        session = null;
        if (kitchen == null) { Finish(); yield break; }
        yield return RollToKitchen();
    }

    private IEnumerator RollToKitchen()
    {
        var doorway = GameObject.Find("KitchenToyStop");
        Vector3 approach = Vector3.ProjectOnPlane(top.position - kitchen.position, Vector3.up).normalized;
        Vector3 rest = doorway ? doorway.transform.position : kitchen.position + approach * .72f;
        rest.y = transform.position.y + .025f;
        var route = new NavMeshPath();
        Vector3[] points = NavMesh.SamplePosition(top.position, out var start, 1.2f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(rest, out var end, 1.2f, NavMesh.AllAreas) &&
            NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, route) &&
            route.status == NavMeshPathStatus.PathComplete ? route.corners : new[] { rest };

        int lastCorner = points.Length - 1;
        float remaining = doorway ? 0f : KitchenStopShort;
        while (lastCorner >= 0)
        {
            Vector3 previous = lastCorner > 0 ? points[lastCorner - 1] : top.position;
            previous.y = rest.y;
            Vector3 endPoint = points[lastCorner];
            endPoint.y = rest.y;
            float length = Vector3.Distance(previous, endPoint);
            if (length > remaining)
            {
                points[lastCorner] = Vector3.MoveTowards(endPoint, previous, remaining);
                break;
            }
            remaining -= length;
            lastCorner--;
        }

        stage = Stage.Follow;
        for (int i = 0; i <= lastCorner; i++)
        {
            Vector3 corner = points[i];
            Vector3 target = new Vector3(corner.x, rest.y, corner.z);
            while ((top.position - target).sqrMagnitude > .0025f)
            {
                float dt = Time.deltaTime;
                top.position = Vector3.MoveTowards(top.position, target, KitchenRollSpeed * dt);
                top.Rotate(Vector3.up, 495f * dt, Space.Self);
                yield return null;
            }
        }
        top.rotation = Quaternion.Euler(0f, 30f, 76f);
        yield return new WaitForSeconds(.3f);
        stage = Stage.WaitingAtKitchen;
        HouseholdChoreFlow.Instance?.UI?.Notify("Con quay dừng trước cửa bếp. Mẹ đang gọi con.", 6f);
        CheckKitchenArrival();
    }

    private void CheckKitchenArrival()
    {
        if (player == null || kitchen == null) { Finish(); return; }
        Vector3 gap = Vector3.ProjectOnPlane(player.transform.position - top.position, Vector3.up);
        if (gap.sqrMagnitude <= 1.35f * 1.35f) StartCoroutine(MeetMother());
    }

    private IEnumerator MeetMother()
    {
        stage = Stage.MeetingMother;
        var flow = HouseholdChoreFlow.Instance;
        if (flow && !flow.ChecklistVisible)
        {
            flow.GetComponent<Scene2MotherCompanion>()?.LookAtToy(top.position);
            yield return new WaitForSeconds(.65f);
            flow.UI?.Notify("Mẹ: Mẹ gọi con xuống phụ bếp, ai dè con quay xuống trước.", 3.6f);
            yield return new WaitForSeconds(3.6f);
        }
        Finish();
    }

    private void DrawCord() => DrawCord(pull);

    private void DrawCord(float extension)
    {
        float wrappedAngle = windingAngle * (1f - extension);
        int wraps = 1 + Mathf.CeilToInt(wrappedAngle * 5f);
        const int tailPoints = 12;
        cord.positionCount = wraps + tailPoints;
        for (int i = 0; i < wraps; i++)
        {
            float arc = wraps == 1 ? 0f : wrappedAngle * i / (wraps - 1);
            float angle = cordAnchorAngle + windingDirection * arc;
            float height = .033f + arc * .00072f;
            float radius = Mathf.Lerp(.030f, .044f, Mathf.InverseLerp(.033f, .047f, height));
            cord.SetPosition(i, top.position + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
        }
        Vector3 end = cordEnd;
        for (int i = 0; i < tailPoints; i++)
        {
            float t = (i + 1f) / tailPoints;
            Vector3 point = Vector3.Lerp(cord.GetPosition(wraps - 1), end, t);
            float slack = windingDrag || dragging ? 0f : .028f * (1f - WindingProgress);
            point += transform.right * (slack * Mathf.Sin(t * Mathf.PI * 2f));
            point.y = Mathf.Max(tablePoint.y + .003f, point.y - Mathf.Sin(t * Mathf.PI) * .011f);
            cord.SetPosition(wraps + i, point);
        }
        cordKnot.position = end;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || GameManager.Instance?.IsPaused == true) return;
        string message = stage == Stage.Winding ?
            (WindingProgress >= 1f ? "Thả đầu dây để chuẩn bị giật dây." :
                "Giữ nút thắt ở đầu dây và kéo quanh thân con quay.") :
            stage == Stage.Aiming ? "Nắm lại đầu dây, kéo mạnh ra ngoài rồi thả." :
            stage == Stage.Follow ? "Con quay lăn về phía bếp. Tự bước theo nó nhé." :
            stage == Stage.WaitingAtKitchen ? "Con quay đã tới bếp. Đến gần mẹ để xem chuyện gì xảy ra." : null;
        if (message != null) GUI.Box(new Rect(Screen.width * .16f, Screen.height * .84f, Screen.width * .68f, 38f), message);
    }

    private void Cancel()
    {
        StopAllCoroutines();
        session?.Close();
        session = null;
        stage = Stage.Ready;
        pull = 0f;
        windingAngle = 0f;
        windingDirection = 0;
        dragging = windingDrag = false;
        top.position = tablePoint;
        top.rotation = Quaternion.identity;
        cord.enabled = true;
        cordKnot.gameObject.SetActive(true);
        ResetCordEnd();
        DrawCord();
    }

    private void Finish()
    {
        stage = Stage.Complete;
        cord.enabled = false;
        if (top != null) top.gameObject.SetActive(false);
        var flow = HouseholdChoreFlow.Instance;
        if (flow != null)
        {
            if (!flow.ChecklistVisible) Scene2Checkpoint.MarkKitchenIntroSeen();
            flow.ShowChecklist("Con tới đúng lúc quá. Lại đây phụ mẹ nhóm lửa nhé.");
        }
        else KitchenFlowManager.Instance?.ShowChecklist();
    }

    private void OnDisable() => session?.Close();

    private void OnApplicationFocus(bool focused)
    {
        if (focused || (stage != Stage.Winding && stage != Stage.Aiming)) return;
        dragging = windingDrag = false;
        pull = 0f;
        DrawCord();
    }

    private void OnDestroy()
    {
        Destroy(wood);
        Destroy(stripe);
        Destroy(thread);
        Destroy(importedMaterial);
        Destroy(topMesh);
    }
}
