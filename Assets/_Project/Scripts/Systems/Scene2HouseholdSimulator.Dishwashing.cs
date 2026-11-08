using System.Linq;
using UnityEngine;

public sealed partial class Scene2HouseholdSimulator
{
    private bool importedDishes;
    private Transform spongeVisual;
    private Transform washBasin, dryingRack, soapVisual;
    private Quaternion dishRestRotation, spongeRestRotation;
    private float basinRadius = .18f, bowlRadius = .065f, bowlLift = .14f, rinseBowlHeight = .055f;
    private Vector3 spongeRestScale;
    private Bounds dishWorkBounds;
    private Vector3 dishCameraPosition;
    private Quaternion dishCameraRotation;
    private MeshCollider fixtureSurfaceQuery;
    private Collider fixtureBox;
    private bool dishCloseView;
    private float dishZoomBlend;
    private Vector3 DishCameraDestination
    {
        get
        {
            Vector3 center = dishVisual.TransformPoint(Vector3.up * (dishwashingAssets.bowlSize.y * .5f));
            Vector3 direction = -(dishCameraRotation * Vector3.forward);
            return Vector3.Lerp(dishCameraPosition, center + direction * Mathf.Max(.5f, bowlRadius * 7), dishZoomBlend);
        }
    }
    private void ToggleDishCloseView()
    {
        dishCloseView = !dishCloseView;
        if (dishCloseView && held < 0)
        {
            held = 1; pointerValid = false;
            Vector3 position = moving.localPosition + Vector3.right * (bowlRadius * 1.4f);
            position.y = DishContactHeight + .001f; tool.localPosition = position;
        }
        else if (!dishCloseView && held == 1) ReleasePointer();
    }
    private readonly Transform[] waterRipples = new Transform[3];
    private float DishContactHeight => world.InverseTransformPoint(stains[state.flipped ? 3 : 0].position).y;

    private Transform AuthoredDish(string name) => station.idleDressing ? station.idleDressing.Find(name) : null;
    private void ApplyDishPose(Transform instance, string sourceName)
    {
        var source = AuthoredDish(sourceName);
        if (!source) return;
        instance.SetPositionAndRotation(source.position, source.rotation);
        instance.localScale = source.lossyScale;
    }
    private Bounds LocalVisualBounds(Transform root, Transform relativeTo)
    {
        Bounds result = default; bool first = true;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 p = relativeTo.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
            }
        }
        return result;
    }
    private void PickVisual(Transform root)
    {
        Bounds bounds = LocalVisualBounds(root, root);
        AddPick(root, bounds.center, bounds.size + Vector3.one * .012f);
    }
    private bool DishInRinse(Vector3 point) => InZone(point, rinseZone, basinRadius);
    private bool DishInRack(Vector3 point)
    {
        if (!dryingRack) return InZone(point, rackZone, .18f);
        Vector3 p = dryingRack.InverseTransformPoint(world.TransformPoint(point));
        return Mathf.Abs(p.x) < .24f && Mathf.Abs(p.z) < .17f;
    }

    private Transform Art(GameObject prefab, string name, Transform parent, Vector3 position)
    {
        var instance = Instantiate(prefab, parent, false).transform;
        instance.name = name; instance.localPosition = position;
        return instance;
    }

    private void BuildImportedDishes()
    {
        importedDishes = true;
        dishCloseView = false; dishZoomBlend = 0;
        rinseZone = new Vector3(-.35f, .005f, .07f);
        rackZone = new Vector3(.365f, .005f, .10f);
        var basin = Art(dishwashingAssets.basin, "Aluminium wash basin", world, rinseZone);
        ApplyDishPose(basin, "WashBasin"); washBasin = basin; rinseZone = basin.localPosition;
        basinRadius = .18f * Mathf.Min(basin.lossyScale.x, basin.lossyScale.z);
        Transform water = basin.GetComponentsInChildren<Transform>(true).First(t => t.name == "WaterSurface");
        rinseBowlHeight = world.InverseTransformPoint(water.position).y - .06f;
        var waterMat = MaterialFor("dish water", new Color(.24f, .48f, .48f, .58f));
        waterMat.SetFloat("_Smoothness", .92f); waterMat.SetFloat("_Metallic", .05f);
        waterMat.SetFloat("_Surface", 1); waterMat.SetFloat("_Blend", 0);
        waterMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.SetFloat("_ZWrite", 0); waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        waterMat.SetOverrideTag("RenderType", "Transparent"); waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Disk("Clean rinse water", basin, basin.InverseTransformPoint(water.position), .385f, waterMat);
        for (int i = 0; i < waterRipples.Length; i++)
        {
            var ring = Group("Water ripple", basin, basin.InverseTransformPoint(water.position) + Vector3.up * .003f);
            var line = ring.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 48; line.widthMultiplier = .0014f;
            line.sharedMaterial = MaterialFor("water glint", new Color(.57f, .72f, .66f));
            for (int p = 0; p < 48; p++)
            {
                float angle = p * Mathf.PI * 2 / 48;
                line.SetPosition(p, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }
            waterRipples[i] = ring;
        }
        var rack = Art(dishwashingAssets.rack, "Bamboo drying rack", world, rackZone);
        ApplyDishPose(rack, "DryingRack"); dryingRack = rack; rackZone = rack.localPosition;
        movingHome = new Vector3(-.015f, .012f, -.07f);
        moving = Group("Hold and turn the bowl", world, movingHome);
        dishVisual = Art(dishwashingAssets.bowl, "Ceramic bowl", moving, Vector3.zero);
        ApplyDishPose(dishVisual, "DishBowl");
        moving.localPosition = world.InverseTransformPoint(dishVisual.position);
        dishVisual.localPosition = Vector3.zero;
        movingHome = moving.localPosition; dishRestRotation = dishVisual.localRotation;
        bowlRadius = .065f * Mathf.Max(dishVisual.lossyScale.x, dishVisual.lossyScale.z);
        bowlLift = Mathf.Max(movingHome.y + .12f, rinseBowlHeight + .10f);
        // Five cheap query triggers only: no 105-piece compound physics or sealed-mouth convex hulls.
        PickVisual(moving);
        toolHome = new Vector3(.17f, .005f, -.24f);
        tool = Group("Sponge", world, toolHome);
        spongeVisual = Art(dishwashingAssets.sponge, "Sponge visual", tool, Vector3.zero);
        ApplyDishPose(spongeVisual, "ScrubSponge");
        tool.localPosition = world.InverseTransformPoint(spongeVisual.position); spongeVisual.localPosition = Vector3.zero;
        toolHome = tool.localPosition; spongeRestRotation = spongeVisual.localRotation; spongeRestScale = spongeVisual.localScale;
        PickVisual(tool);
        var soap = Art(dishwashingAssets.soap, "Soap", world, new Vector3(-.30f, .005f, -.25f));
        ApplyDishPose(soap, "SoapBar"); soapVisual = soap;
        PickVisual(soap); PickVisual(basin); PickVisual(rack);
        dishWorkBounds = LocalVisualBounds(basin, world);
        foreach (var item in new[] { rack, moving, tool, soap }) dishWorkBounds.Encapsulate(LocalVisualBounds(item, world));
        dishWorkBounds.Expand(new Vector3(.18f, .30f, .18f));
        FrameDishLayout();
        fixtureSurfaceQuery = null; fixtureBox = null;
        if (station.existingFixture && station.existingFixture.TryGetComponent<MeshFilter>(out var fixtureMesh) && fixtureMesh.sharedMesh)
        {
            // The old closed BoxCollider covers its bowls and free ledges. Keep it solid
            // for walking, but use the actual visible mesh when testing a pointer ray.
            var query = Group("Legacy fixture exact pointer surface", world, Vector3.zero);
            query.SetPositionAndRotation(fixtureMesh.transform.position, fixtureMesh.transform.rotation);
            query.localScale = fixtureMesh.transform.lossyScale;
            fixtureSurfaceQuery = query.gameObject.AddComponent<MeshCollider>(); fixtureSurfaceQuery.sharedMesh = fixtureMesh.sharedMesh;
            fixtureBox = station.existingFixture.GetComponent<BoxCollider>();
            query.gameObject.layer = 2; // IgnoreRaycast for ordinary player focus; queried explicitly below.
        }
        for (int i = 0; i < 6; i++)
        {
            stains[i] = Prop("Food stain " + (i + 1), PrimitiveType.Sphere, dishVisual,
                dishwashingAssets.stainPoints[i], new Vector3(.023f, .0016f, .018f),
                MaterialFor("food stain", new Color(.31f, .16f, .055f)));
            stains[i].localRotation = Quaternion.FromToRotation(Vector3.up, dishwashingAssets.stainNormals[i]);
        }
        for (int i = 0; i < bubbles.Length; i++)
        {
            int patch = i / 4 * 3 + i % 3;
            bubbles[i] = Prop("Soap foam", PrimitiveType.Sphere, dishVisual,
                dishwashingAssets.stainPoints[patch] + dishwashingAssets.stainNormals[patch] * .004f,
                Vector3.one * .008f, MaterialFor("foam", new Color(.90f, .94f, .86f)));
        }
        for (int i = 0; i < 3; i++)
        {
            var slot = rack.GetComponentsInChildren<Transform>(true).First(t => t.name == "RackSlot_0" + (i + 1));
            racked[i] = Art(dishwashingAssets.bowl, "Clean bowl " + (i + 1), slot, Vector3.zero);
            // Attachment rotation was baked through the FBX hierarchy; do not apply Blender's 180 degrees twice.
            racked[i].localRotation = Quaternion.identity;
            // Slot spacing follows the authored rack; the bowl itself must retain its real size.
            racked[i].localScale = new Vector3(dishVisual.lossyScale.x / slot.lossyScale.x,
                dishVisual.lossyScale.y / slot.lossyScale.y, dishVisual.lossyScale.z / slot.lossyScale.z);
        }
        // Instructions belong to the HUD, not fixed labels drawn through the user's props.
    }

    private void FrameDishLayout()
    {
        Vector3 focus = world.TransformPoint(dishWorkBounds.center);
        Vector3 direction = world.TransformDirection(new Vector3(0, .8f, -.65f).normalized);
        dishCameraRotation = Quaternion.LookRotation(-direction, world.up);
        float distance = 1f;
        float tangent = Mathf.Tan(48 * .5f * Mathf.Deg2Rad);
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = world.TransformPoint(dishWorkBounds.center + Vector3.Scale(dishWorkBounds.extents, new Vector3(x, y, z)));
            Vector3 p = Quaternion.Inverse(dishCameraRotation) * (corner - focus);
            distance = Mathf.Max(distance, Mathf.Abs(p.y) / tangent - p.z, Mathf.Abs(p.x) / (tangent * Mathf.Min(view.aspect, 16f / 9f)) - p.z);
        }
        dishCameraPosition = focus + direction * (distance + .12f);
    }

    private int ContactPatch(Vector3 point)
    {
        int side = state.flipped ? 3 : 0, result = -1;
        float nearest = Mathf.Pow(.024f * dishVisual.lossyScale.x, 2);
        for (int i = side; i < side + 3; i++)
        {
            Vector3 contact = world.InverseTransformPoint(stains[i].position);
            float distance = new Vector2(point.x - contact.x, point.z - contact.z).sqrMagnitude;
            if (distance < nearest) { nearest = distance; result = i; }
        }
        return result;
    }

    private void RefreshImportedDishes()
    {
        if (spongeVisual)
        {
            spongeVisual.localRotation = spongeRestRotation * (held == 1 ? Quaternion.Euler(180, 0, 0) : Quaternion.identity);
            spongeVisual.localPosition = held == 1 ? Vector3.up * (.03f * spongeRestScale.y) : Vector3.zero;
        }
        for (int i = 0; i < waterRipples.Length; i++) if (waterRipples[i])
        {
            bool rinsing = held == 0 && DishInRinse(moving.localPosition);
            waterRipples[i].gameObject.SetActive(rinsing);
            waterRipples[i].localScale = Vector3.one * Mathf.Lerp(.035f, .185f, Mathf.Repeat(Time.time * .65f + i / 3f, 1));
        }
    }
}
