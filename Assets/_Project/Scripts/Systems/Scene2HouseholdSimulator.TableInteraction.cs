using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class Scene2HouseholdSimulator
{
    // Transient gestures never enter checkpoints. placedMask retains its original item/slot IDs.
    private bool tablePointerDown, tableDragging, tablePickedThisGesture, tableIgnoreUntilUp;
    private Vector2 tablePressScreen;
    private Vector3 tableGrabOffset;
    private float tableSettleTime;
    private Vector3 tableSettleFrom;
    private Transform[] tablePreviews;
    private Material[] tablePreviewMaterials;
    private MeshRenderer[][] tablePieceRenderers;
    private MaterialPropertyBlock tableHighlight;
    private List<RaycastResult> activityUiHits;
    private TablePlacementValidation tableValidation;
    private readonly Dictionary<GameObject, bool> hiddenTableSources = new Dictionary<GameObject, bool>();

    private void HideTableSource(Transform source)
    {
        if (!hiddenTableSources.ContainsKey(source.gameObject)) hiddenTableSources.Add(source.gameObject, source.gameObject.activeSelf);
        source.gameObject.SetActive(false);
    }

    private void RestoreTableSources()
    {
        foreach (var source in hiddenTableSources) if (source.Key) source.Key.SetActive(source.Value);
        hiddenTableSources.Clear();
    }

    private struct TablePlacementValidation
    {
        public bool canCommit;
        public Vector3 pose;
        public string reason;
    }

    private bool PointerOverActivityUi(Vector2 screen)
    {
        if (!EventSystem.current) return false;
        EnsureTableInteractionStorage();
        activityUiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, activityUiHits);
        // Physics raycasters are world interactions, not UI blockers.
        foreach (var hit in activityUiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }

    private void EnsureTableInteractionStorage()
    {
        // Unity can retain an existing component across a hot reload before new transient fields exist.
        if (tablePreviews == null) tablePreviews = new Transform[3];
        if (tablePreviewMaterials == null) tablePreviewMaterials = new Material[3];
        if (tablePieceRenderers == null) tablePieceRenderers = new MeshRenderer[3][];
        if (tableHighlight == null) tableHighlight = new MaterialPropertyBlock();
        if (activityUiHits == null) activityUiHits = new List<RaycastResult>();
    }

    private void ResetTableGesture()
    {
        tablePointerDown = tableDragging = tablePickedThisGesture = false;
        tableSettleTime = 0;
        tableGrabOffset = Vector3.zero;
        tableValidation = default;
    }

    private void TickTablePointer(Vector2 screen, Vector3 point, int pick, bool down, bool pressed, bool up, float dt)
    {
        if (pick != tableRawHover) { tableRawHover = pick; tableHoverTime = 0; }
        else tableHoverTime += dt;
        tableHover = tableHoverTime >= .10f ? pick : -1;
        if (tableSettleTime > 0)
        {
            tableSettleTime = Mathf.Max(0, tableSettleTime - dt);
            float t = Mathf.SmoothStep(0, 1, 1 - tableSettleTime / .18f);
            pieces[held].localPosition = Vector3.Lerp(tableSettleFrom, tableValidation.pose, t);
            if (tableSettleTime == 0)
            {
                int item = held;
                // Commit the exact validation/pose that produced the green preview. No second radius rule.
                if (state.Place(item, tableValidation.canCommit))
                {
                    if (tableContactSource) { tableContactSource.pitch = item == 1 ? .88f : item == 0 ? .95f : 1.08f; tableContactSource.PlayOneShot(tableContactClip); }
                    Touch(); Tell("Đã đặt " + TablePieceName(item).ToLowerInvariant() + " gọn vào chỗ.");
                }
                ReleasePointer();
            }
            return;
        }
        if (down)
        {
            tablePointerDown = true; tableDragging = false;
            tablePressScreen = screen; tablePickedThisGesture = held < 0;
            if (held < 0 && pick >= 0 && pick < 3 && (state.placedMask & (1 << pick)) == 0)
            {
                held = pick;
                tableLiftTime = 0; tableLiftFrom = pieces[held].localPosition.y;
                tableGrabOffset = pieces[held].localPosition - point;
                tableGrabOffset.y = 0;
            }
        }
        if (held < 0) { RefreshTableGuides(); return; }
        float threshold = 10f * Mathf.Max(.75f, Mathf.Min(Screen.width / 1920f, Screen.height / 1080f));
        if (tablePointerDown && pressed && (screen - tablePressScreen).sqrMagnitude >= threshold * threshold)
            tableDragging = true;

        // A short first click only lifts; movement starts with a drag or after releasing that click.
        Vector3 candidate = pieces[held].localPosition;
        tableLiftTime = Mathf.Min(.16f, tableLiftTime + dt);
        float lift = Mathf.Lerp(tableLiftFrom, .045f, Mathf.SmoothStep(0, 1, tableLiftTime / .16f));
        if (tableDragging || !tablePointerDown || !tablePickedThisGesture)
            candidate = new Vector3(point.x + tableGrabOffset.x, lift, point.z + tableGrabOffset.z);
        else candidate.y = lift;
        bool blockedMove = TablePathBlocked(pieces[held].position + world.up * .05f, world.TransformPoint(candidate) + world.up * .05f);
        if (blockedMove) candidate = pieces[held].localPosition;
        tableValidation = ValidateTablePlacement(candidate, tableDropReady);
        if (blockedMove) { tableValidation.canCommit = false; tableValidation.reason = "Đưa vật vòng qua chỗ bị chắn."; }
        tableDropReady = tableValidation.canCommit;
        pieces[held].localPosition = tableDropReady ? tableValidation.pose + Vector3.up * lift : candidate;
        pieces[held].localRotation = Quaternion.identity;

        if (up && tablePointerDown)
        {
            bool place = tableDragging || !tablePickedThisGesture;
            tablePointerDown = tableDragging = false;
            if (place)
            {
                if (tableValidation.canCommit)
                {
                    tableSettleFrom = pieces[held].localPosition;
                    tableSettleTime = .18f;
                }
                else
                {
                    string reason = tableValidation.reason;
                    ReleasePointer(); Tell(reason + " Đồ đã được đặt lại an toàn.");
                }
            }
        }
        RefreshTableGuides();
    }

    private TablePlacementValidation ValidateTablePlacement(Vector3 candidate, bool wasReady)
    {
        var result = new TablePlacementValidation { pose = slots[held], reason = "Đưa " + TablePieceName(held).ToLowerInvariant() + " đến hình bóng cùng loại rồi đặt." };
        // Hysteresis is shared by preview and commit; the wider support area never awards progress.
        if (!InZone(candidate, result.pose, wasReady ? .185f : .16f)) return result;
        Vector3 target = world.TransformPoint(result.pose + Vector3.up * .10f);
        Vector3 viewport = view.WorldToViewportPoint(target);
        if (viewport.z <= view.nearClipPlane || viewport.x <= 0 || viewport.x >= 1 || viewport.y <= 0 || viewport.y >= 1)
        { result.reason = "Đưa vật về vị trí đang nhìn thấy trên bàn."; return result; }
        if (TablePathBlocked(view.transform.position, target) ||
            TablePathBlocked(pieces[held].position + world.up * .10f, target))
        { result.reason = "Đưa vật vòng qua chỗ bị chắn rồi đặt."; return result; }
        result.canCommit = true;
        return result;
    }

    private bool TablePathBlocked(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        if (delta.magnitude < .005f) return false;
        int count = Physics.RaycastNonAlloc(new Ray(from, delta.normalized), occlusionHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == occlusionHits.Length) return true;
        for (int i = 0; i < count; i++)
        {
            var hit = occlusionHits[i];
            if (!hit.collider || hit.transform.IsChildOf(world) || hit.collider.GetComponentInParent<FirstPersonController>()) continue;
            return true;
        }
        return false;
    }

    private void BuildTablePreview(int index)
    {
        tablePieceRenderers[index] = pieces[index].GetComponentsInChildren<MeshRenderer>();
        // Copy only renderer geometry: no colliders, scripts, source asset edits or replacement dishes.
        var ghost = new GameObject("Table pose preview " + index).transform;
        ghost.SetParent(world, false); ghost.localPosition = slots[index];
        var material = new Material(Shader.Find("Sprites/Default"));
        ownedAssets.Add(material); tablePreviewMaterials[index] = material;
        foreach (var mesh in pieces[index].GetComponentsInChildren<MeshFilter>())
        {
            var visual = new GameObject("Preview " + mesh.name).transform;
            visual.SetParent(ghost, false);
            visual.localPosition = pieces[index].InverseTransformPoint(mesh.transform.position);
            visual.localRotation = Quaternion.Inverse(pieces[index].rotation) * mesh.transform.rotation;
            visual.localScale = mesh.transform.localScale;
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var renderer = visual.gameObject.AddComponent<MeshRenderer>();
            int count = mesh.GetComponent<MeshRenderer>().sharedMaterials.Length;
            var materials = new Material[count];
            for (int i = 0; i < count; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        tablePreviews[index] = ghost;
    }

    private void HighlightTablePiece(int index, bool selected)
    {
        if (tablePieceRenderers[index] == null) return;
        foreach (var renderer in tablePieceRenderers[index])
        {
            if (!renderer) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                tableHighlight.Clear();
                if (selected && materials[i])
                {
                    string property = materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (materials[i].HasProperty(property))
                    {
                        Color color = materials[i].GetColor(property);
                        tableHighlight.SetColor(property, new Color(color.r * 1.18f, color.g * 1.18f, color.b * 1.08f, color.a));
                    }
                }
                renderer.SetPropertyBlock(tableHighlight, i);
            }
        }
    }
}
