using UnityEngine;

public sealed partial class Scene2KitchenHandsOn
{
    private HouseholdActivityPointer kitchenUiPointer;
    private bool breathDelivered, lidAutoOpening, lidClickCandidate, kitchenPointerNeedsRelease, woodDropReady;
    private float woodSettleTime;
    private Vector3 woodSettleFrom;
    private Quaternion woodSettleRotation;
    private Transform[] woodPreviews;
    private Material woodPreviewMaterial, kitchenGuideMaterial;
    private MaterialPropertyBlock kitchenHighlight;
    private LineRenderer[] woodSourceGuides;

    private void BuildKitchenUx()
    {
        woodPreviews = new Transform[3];
        woodSourceGuides = new LineRenderer[3];
        woodPreviewMaterial = new Material(Shader.Find("Sprites/Default"));
        var guideMaterial = new Material(Shader.Find("Sprites/Default"));
        kitchenGuideMaterial = guideMaterial;
        for (int i = 0; i < 3; i++)
        {
            var outline = new GameObject("Pickable source wood " + i).AddComponent<LineRenderer>();
            outline.transform.SetParent(fireRoot, false); outline.useWorldSpace = true; outline.loop = true; outline.positionCount = 4;
            outline.widthMultiplier = .004f; outline.sharedMaterial = guideMaterial;
            outline.startColor = outline.endColor = new Color(1, .84f, .38f);
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            woodSourceGuides[i] = outline;
            var ghost = new GameObject("Source wood placement preview " + i).transform;
            ghost.SetParent(fireRoot, false);
            if (i < kindlingSlots.Length && kindlingSlots[i]) ghost.SetPositionAndRotation(kindlingSlots[i].position, kindlingSlots[i].rotation);
            else ghost.position = hearth.position + new Vector3(0, i * .045f, (i - 1) * .065f);
            foreach (var mesh in kindling[i].GetComponentsInChildren<MeshFilter>())
            {
                var visual = new GameObject("Source wood silhouette").transform; visual.SetParent(ghost, false);
                visual.localPosition = kindling[i].InverseTransformPoint(mesh.transform.position);
                visual.localRotation = Quaternion.Inverse(kindling[i].rotation) * mesh.transform.rotation;
                visual.localScale = mesh.transform.lossyScale;
                visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var renderer = visual.gameObject.AddComponent<MeshRenderer>();
                var materials = new Material[mesh.GetComponent<MeshRenderer>().sharedMaterials.Length];
                for (int m = 0; m < materials.Length; m++) materials[m] = woodPreviewMaterial;
                renderer.sharedMaterials = materials; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            woodPreviews[i] = ghost; ghost.gameObject.SetActive(false);
        }
    }

    private void TickWoodSettling(float seconds)
    {
        if (draggedLog < 0) { woodSettleTime = 0; return; }
        woodSettleTime = Mathf.Max(0, woodSettleTime - seconds);
        float t = Mathf.SmoothStep(0, 1, 1 - woodSettleTime / .18f);
        var target = woodPreviews[draggedLog];
        kindling[draggedLog].SetPositionAndRotation(Vector3.Lerp(woodSettleFrom, target.position, t), Quaternion.Slerp(woodSettleRotation, target.rotation, t));
        if (woodSettleTime <= 0) { int item = draggedLog; draggedLog = -1; woodDropReady = false; PlaceKindling(item); }
    }

    private void RefreshKitchenUx()
    {
        if (woodPreviews != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (woodPreviews[i]) woodPreviews[i].gameObject.SetActive(IsWorking && task == HouseholdChore.LightFire && draggedLog == i && !placed[i]);
                HighlightKitchenObject(kindling[i], IsWorking && task == HouseholdChore.LightFire && !placed[i]);
                if (woodSourceGuides != null && woodSourceGuides[i])
                {
                    var outline = woodSourceGuides[i];
                    outline.enabled = IsWorking && task == HouseholdChore.LightFire && !placed[i] && draggedLog != i;
                    if (outline.enabled)
                    {
                        var bounds = Scene2DishwashingLayout.VisualBounds(kindling[i]); bounds.Expand(.02f);
                        float y = bounds.max.y + .002f;
                        outline.SetPosition(0, new Vector3(bounds.min.x, y, bounds.min.z));
                        outline.SetPosition(1, new Vector3(bounds.max.x, y, bounds.min.z));
                        outline.SetPosition(2, new Vector3(bounds.max.x, y, bounds.max.z));
                        outline.SetPosition(3, new Vector3(bounds.min.x, y, bounds.max.z));
                    }
                }
            }
            woodPreviewMaterial.color = woodDropReady ? new Color(.45f, 1, .65f, .35f) : new Color(1, .84f, .38f, .30f);
        }
        HighlightKitchenObject(lid, IsWorking && task == HouseholdChore.CookRice && cooked >= 12);
        HighlightKitchenObject(fuelLog, IsWorking && task == HouseholdChore.CookRice && cooked < 12);
    }

    private void HighlightKitchenObject(Transform target, bool active)
    {
        if (!target) return;
        if (kitchenHighlight == null) kitchenHighlight = new MaterialPropertyBlock();
        foreach (var renderer in target.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                kitchenHighlight.Clear();
                if (active && materials[i])
                {
                    string property = materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (materials[i].HasProperty(property))
                    {
                        Color color = materials[i].GetColor(property);
                        kitchenHighlight.SetColor(property, new Color(color.r * 1.18f, color.g * 1.18f, color.b * 1.08f, color.a));
                    }
                }
                renderer.SetPropertyBlock(kitchenHighlight, i);
            }
        }
    }
}
