using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Scene2HouseholdSimulator
{
    private HouseholdActivityPointer washingUiPointer;
    private bool washingCarryLatched, washingDown, washingPickedOnDown, washingDrag, washingIgnoreUntilUp;
    private Vector2 washingPress;
    private Vector3 washingGrabOffset;
    private float washingFlipTime;
    private Quaternion washingFlipFrom;
    private GameObject washingActionRow;
    private Button washingFlipButton, washingCloseButton, washingResetButton, washingLeaveButton;
    private Transform washingRackPreview, washingTargetGuide;
    private Material washingPreviewMaterial;
    private Transform washingSourceGuide;
    private MaterialPropertyBlock washingHighlight;
    private static readonly float[] RackSampleX = { 0f, -.7f, .7f };
    private static readonly float[] RackSampleY = { .8f, .4f, 0f, -.5f };
    private System.Collections.Generic.List<MeshCollider> authoredRackQueries;
    private System.Collections.Generic.List<Vector3> washingRackSamples;
    private System.Collections.Generic.List<MeshCollider> washingPreviewQueries;
    private bool washingRackReady;

    private void ResetWashingPointer()
    {
        washingCarryLatched = washingDown = washingPickedOnDown = washingDrag = false;
        washingFlipTime = 0;
        washingGrabOffset = Vector3.zero;
        washingRackReady = false;
        washingRackSettleTime = 0;
        if (washingScrubAudio) washingScrubAudio.Stop();
        if (tool && washingToolRestScale != Vector3.zero) tool.localScale = washingToolRestScale;
    }

    private void FlipWashingDish()
    {
        if (!IsWorking || !authoredDishes || !moving || !state.wetted || !state.soaped || state.Scrubbed ||
            washingFlipTime > 0 || manager && manager.IsPaused) return;
        Quaternion from = moving.localRotation;
        ReleasePointer();
        state.flipped = !state.flipped;
        washingFlipFrom = from; washingFlipTime = .28f;
        Touch(); Tell(state.flipped ? "Chà mặt ngoài của món." : "Chà mặt trong của món.");
    }

    private void ConfigureWashingControls()
    {
        captions.blocksRaycasts = captions.interactable = authoredDishes;
        if (authoredDishes && !captions.GetComponent<GraphicRaycaster>()) captions.gameObject.AddComponent<GraphicRaycaster>();
        if (authoredDishes && !washingActionRow)
        {
            var safe = title.transform.parent.parent;
            washingActionRow = new GameObject("Washing actions", typeof(RectTransform));
            washingActionRow.transform.SetParent(safe, false);
            var rect = (RectTransform)washingActionRow.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 14); rect.sizeDelta = new Vector2(1100, 52);
            washingFlipButton = WashingButton("Lật món", -414, FlipWashingDish);
            washingCloseButton = WashingButton("Nhìn gần [F]", -138, () => { if (IsWorking && authoredDishes) ToggleDishCloseView(); });
            washingResetButton = WashingButton("Đặt lại", 138, () => { ReleasePointer(); washingIgnoreUntilUp = true; Tell("Đã đặt món và miếng rửa lại an toàn."); });
            washingLeaveButton = WashingButton("Rời đi [ESC]", 414, End);
        }
        if (washingActionRow) washingActionRow.SetActive(authoredDishes);
        ((RectTransform)instructions.transform.parent).anchoredPosition = new Vector2(0, authoredDishes ? 156 : 112);
        ((RectTransform)hint.transform.parent).anchoredPosition = new Vector2(0, authoredDishes ? 76 : 24);
        ((RectTransform)hint.transform.parent).sizeDelta = new Vector2(1260, authoredDishes ? 72 : 80);
        washingIgnoreUntilUp = false;
    }

    private Button WashingButton(string caption, float x, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(washingActionRow.transform, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(264, 52); rect.anchoredPosition = new Vector2(x, 0);
        var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); GameUiPresentation.Button(button);
        button.onClick.AddListener(action);
        var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        if (GameUiTheme.Current) text.font = GameUiTheme.Current.font;
        GameUiPresentation.Text(text, 24, GameUiTheme.Current ? GameUiTheme.Current.text : Color.black); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center; text.text = caption;
        return button;
    }

    private void RefreshWashingControls()
    {
        if (!washingActionRow || !authoredDishes) return;
        bool available = IsWorking && moving && !state.Complete && washingRackSettleTime <= 0 && !(manager && manager.IsPaused);
        washingFlipButton.interactable = available && state.wetted && state.soaped && !state.Scrubbed && washingFlipTime <= 0;
        washingCloseButton.interactable = available && held != 0 && (dishCloseView || state.wetted && state.soaped && !state.Scrubbed) && washingFlipTime <= 0;
        washingResetButton.interactable = available && (held >= 0 || washingFlipTime > 0);
        washingLeaveButton.interactable = IsWorking && !(manager && manager.IsPaused);
    }

    private void BuildWashingGuides()
    {
        var guide = new GameObject("Washing current target").AddComponent<LineRenderer>();
        guide.transform.SetParent(world, false); guide.useWorldSpace = true; guide.loop = true; guide.positionCount = 48;
        guide.widthMultiplier = .0025f;
        washingPreviewMaterial = new Material(Shader.Find("Sprites/Default")); ownedAssets.Add(washingPreviewMaterial);
        var guideMaterial = new Material(Shader.Find("Sprites/Default")); ownedAssets.Add(guideMaterial);
        guide.sharedMaterial = guideMaterial; guide.startColor = guide.endColor = new Color(.88f, .84f, .64f, .55f);
        guide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        washingTargetGuide = guide.transform;
        var sourceGuide = new GameObject("Current dish highlight").AddComponent<LineRenderer>();
        sourceGuide.transform.SetParent(world, false); sourceGuide.useWorldSpace = true; sourceGuide.loop = true; sourceGuide.positionCount = 48;
        sourceGuide.widthMultiplier = .0025f; sourceGuide.sharedMaterial = guideMaterial;
        sourceGuide.startColor = sourceGuide.endColor = new Color(.88f, .84f, .64f, .55f);
        sourceGuide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        washingSourceGuide = sourceGuide.transform;
        washingRackPreview = new GameObject("Clean dish rack preview").transform;
        washingRackPreview.SetParent(world, false);
        washingRackSamples = new System.Collections.Generic.List<Vector3>();
        washingPreviewQueries = new System.Collections.Generic.List<MeshCollider>();
        foreach (var point in dishLayout.dishes[authoredDishIndex].points)
            washingRackSamples.Add(moving.InverseTransformPoint(dishVisual.TransformPoint(point)));
        foreach (var query in dishQueries[authoredDishIndex])
        {
            var mesh = query.GetComponent<MeshFilter>();
            var visual = new GameObject("Rack silhouette").transform; visual.SetParent(washingRackPreview, false);
            visual.localPosition = moving.InverseTransformPoint(mesh.transform.position);
            visual.localRotation = Quaternion.Inverse(moving.rotation) * mesh.transform.rotation;
            visual.localScale = mesh.transform.lossyScale;
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var surface = visual.gameObject.AddComponent<MeshCollider>(); surface.sharedMesh = mesh.sharedMesh;
            surface.gameObject.layer = 2; washingPreviewQueries.Add(surface);
            var renderer = visual.gameObject.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.GetComponent<MeshRenderer>().sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = washingPreviewMaterial;
            renderer.sharedMaterials = materials; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void RefreshWashingGuides()
    {
        if (!washingTargetGuide || !moving) return;
        bool soap = state.wetted && !state.soaped;
        bool scrub = state.wetted && state.soaped && !state.Scrubbed;
        Vector3 center = state.DishReady ? authoredRackBounds.center : soap ? Scene2DishwashingLayout.VisualBounds(authoredSoap).center :
            scrub ? moving.position : state.Scrubbed ? dishLayout.outlet.position : authoredBasinBounds.center;
        float radius = soap ? .065f : scrub ? .12f : state.DishReady ? .18f : state.Scrubbed ? .13f : Mathf.Min(authoredBasinBounds.extents.x, authoredBasinBounds.extents.z) * .8f;
        var line = washingTargetGuide.GetComponent<LineRenderer>();
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / line.positionCount;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, .014f, Mathf.Sin(angle) * radius));
        }
        line.enabled = IsWorking && !state.Complete && !scrub && (soap || held == 0) && washingRackSettleTime <= 0;
        if (washingSourceGuide)
        {
            var sourceLine = washingSourceGuide.GetComponent<LineRenderer>();
            var bounds = dishQueries[authoredDishIndex][0].bounds;
            foreach (var query in dishQueries[authoredDishIndex]) bounds.Encapsulate(query.bounds);
            float dishRadius = Mathf.Max(bounds.extents.x, bounds.extents.z) + .018f;
            for (int i = 0; i < sourceLine.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / sourceLine.positionCount;
                sourceLine.SetPosition(i, new Vector3(bounds.center.x + Mathf.Cos(angle) * dishRadius, bounds.max.y + .006f, bounds.center.z + Mathf.Sin(angle) * dishRadius));
            }
            sourceLine.enabled = IsWorking && !state.Complete && !scrub && held < 0 && !soap;
        }
        HighlightWashingDish(IsWorking && !state.Complete);
        if (washingRackPreview)
        {
            washingRackPreview.gameObject.SetActive(IsWorking && state.DishReady && held == 0 && washingRackSettleTime <= 0);
            washingRackPreview.localPosition = slots[authoredDishIndex]; washingRackPreview.localRotation = authoredRackRotations[authoredDishIndex];
        }
        washingPreviewMaterial.color = washingRackReady ? new Color(.60f, .82f, .70f, .24f) : new Color(.88f, .82f, .64f, .22f);
    }

    private bool CanRackAuthoredDish()
    {
        if (!moving || !state.DishReady || !InAuthoredRack(moving.localPosition)) return false;
        // A slatted rack can hide the pivot while the actual ceramic remains visible above/between rails.
        // Test authored mesh-contact anchors (also works with non-readable imported meshes),
        // never waive solid occlusion or rely on an empty bounds corner.
        if (washingRackSamples == null) return false;
        foreach (var local in washingRackSamples)
        {
            Vector3 target = world.TransformPoint(slots[authoredDishIndex] + authoredRackRotations[authoredDishIndex] * local);
            Vector3 screen = view.WorldToViewportPoint(target);
            if (screen.z > view.nearClipPlane && screen.x > 0 && screen.x < 1 && screen.y > 0 && screen.y < 1 &&
                !AuthoredDishOccluded(new Ray(view.transform.position, (target - view.transform.position).normalized), Vector3.Distance(view.transform.position, target))) return true;
        }
        // Small bowls may show only a rim above a slat. Resolve candidate screen rays against
        // the preview's exact mesh, so a visible rim counts but empty bounds corners never do.
        washingRackPreview.localPosition = slots[authoredDishIndex];
        washingRackPreview.localRotation = authoredRackRotations[authoredDishIndex];
        washingRackPreview.gameObject.SetActive(true); Physics.SyncTransforms();
        var bounds = Scene2DishwashingLayout.VisualBounds(washingRackPreview);
        foreach (float x in RackSampleX)
        foreach (float y in RackSampleY)
        {
            Vector3 candidate = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, 0));
            Vector3 screen = view.WorldToViewportPoint(candidate);
            if (screen.z <= view.nearClipPlane || screen.x <= 0 || screen.x >= 1 || screen.y <= 0 || screen.y >= 1) continue;
            Ray ray = new Ray(view.transform.position, (candidate - view.transform.position).normalized);
            foreach (var query in washingPreviewQueries)
                if (query && query.Raycast(ray, out var hit, 5f) && !AuthoredDishOccluded(ray, hit.distance)) return true;
        }
        return false;
    }

    private void HighlightWashingDish(bool active)
    {
        if (authoredDishIndex < 0 || dishQueries[authoredDishIndex] == null) return;
        if (washingHighlight == null) washingHighlight = new MaterialPropertyBlock();
        foreach (var query in dishQueries[authoredDishIndex])
        {
            var renderer = query.GetComponent<MeshRenderer>();
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                washingHighlight.Clear();
                if (active && materials[i])
                {
                    string property = materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (materials[i].HasProperty(property))
                    {
                        Color color = materials[i].GetColor(property);
                        washingHighlight.SetColor(property, new Color(color.r * 1.15f, color.g * 1.15f, color.b * 1.05f, color.a));
                    }
                }
                renderer.SetPropertyBlock(washingHighlight, i);
            }
        }
    }
}
