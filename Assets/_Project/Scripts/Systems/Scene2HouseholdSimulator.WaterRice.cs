using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Scene2HouseholdSimulator
{
    private bool importedTransfer;
    private Transform transferSource, transferReceiver, transferFillCenter, transferSpout;
    private WaterRiceFill scoopFill, sourceFill, receiverFill;
    private float transferSourceRadius, transferTargetRadius, transferTravelHeight;
    private bool transferPouring;
    private HouseholdJarSource activeTransferJar;
    private MeshCollider transferJarSurfaceQuery;
    private Vector2[] transferSourceProfile;
    private readonly Dictionary<MeshRenderer, bool> jarVisibility = new Dictionary<MeshRenderer, bool>();
    private static readonly Vector2[] LadleProfile = { new Vector2(0, .006f), new Vector2(.008f, .028f), new Vector2(.017f, .038f), new Vector2(.028f, .045f), new Vector2(.040f, .049f), new Vector2(.046f, .050f) };
    private static readonly Vector2[] CupProfile = { new Vector2(0, .029f), new Vector2(.003f, .034f), new Vector2(.018f, .036f), new Vector2(.047f, .038f), new Vector2(.077f, .040f), new Vector2(.093f, .041f) };
    private static readonly Vector2[] BucketProfile = { new Vector2(0, .109f), new Vector2(.003f, .121f), new Vector2(.021f, .125f), new Vector2(.095f, .131f), new Vector2(.185f, .138f), new Vector2(.275f, .146f), new Vector2(.34f, .151f) };
    private static readonly Vector2[] BasketProfile = { new Vector2(0, .089f), new Vector2(.003f, .108f), new Vector2(.01f, .127f), new Vector2(.026f, .142f), new Vector2(.054f, .156f), new Vector2(.093f, .168f), new Vector2(.132f, .175f), new Vector2(.171f, .179f) };
    private static Transform TransferMarker(Transform root, string suffix) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.EndsWith("_" + suffix));
    private WaterRiceFill CreateTransferFill(string name, Transform vessel, Transform marker, Vector2[] profile, float height, Material material)
    {
        var root = Group(name, world, Vector3.zero);
        var fill = root.gameObject.AddComponent<WaterRiceFill>(); fill.Configure(vessel, marker, profile, height, material); return fill;
    }
    private void BuildImportedTransfer()
    {
        importedTransfer = true; transferPouring = false;
        bool water = state.chore == HouseholdChore.FetchWater;
        sourceZone = new Vector3(-.34f, .005f, .06f); targetZone = new Vector3(.34f, .005f, .06f);
        activeTransferJar = station.transferJar && station.transferJar.IsReady ? station.transferJar : null;
        transferJarSurfaceQuery = null;
        transferSource = activeTransferJar ? BuildLegacyTransferJar() :
            Art(water ? waterRiceAssets.bucket : waterRiceAssets.basket, water ? "Water bucket" : "Rice basket", world, sourceZone);
        sourceZone = world.InverseTransformPoint(activeTransferJar ? activeTransferJar.Mouth : transferSource.position);
        if (activeTransferJar)
        {
            Bounds jar = activeTransferJar.original.bounds;
            targetZone = world.InverseTransformPoint(new Vector3(jar.max.x + waterRiceAssets.receiverRadius + .18f, .025f, jar.center.z + .25f));
        }
        transferReceiver = Art(waterRiceAssets.receiver, "Existing kitchen receiving pot", world, targetZone);
        movingHome = water ? new Vector3(.18f, .06f, -.22f) : new Vector3(.06f, .075f, -.22f);
        if (activeTransferJar) movingHome = targetZone + new Vector3(water ? .47f : .34f, water ? .06f : .075f, .26f);
        moving = Art(water ? waterRiceAssets.ladle : waterRiceAssets.cup, water ? "Coconut ladle" : "Aluminium measuring cup", world, movingHome);
        transferFillCenter = TransferMarker(moving, "FillCenter"); transferSpout = TransferMarker(moving, "PourSpout");
        Material material = water ? waterRiceAssets.waterMaterial : waterRiceAssets.riceMaterial;
        transferSourceProfile = activeTransferJar ? new[] { new Vector2(0, activeTransferJar.openingRadius * .90f),
            new Vector2(.12f, activeTransferJar.openingRadius * .96f), new Vector2(.20f, activeTransferJar.openingRadius) } : water ? BucketProfile : BasketProfile;
        sourceFill = CreateTransferFill("Source contents · profile fitted", transferSource, TransferMarker(transferSource, "ContentsRoot"),
            transferSourceProfile, activeTransferJar ? .12f : water ? .225f : .111f, material);
        scoopFill = CreateTransferFill("Carried contents · horizontal surface", moving, TransferMarker(moving, "ContentsRoot"),
            water ? LadleProfile : CupProfile, water ? .028f : .067f, material);
        var receiverFloor = Group("Receiver content floor", transferReceiver, waterRiceAssets.receiverFloor);
        receiverFill = CreateTransferFill("Received contents", transferReceiver, receiverFloor,
            new[] { new Vector2(0, waterRiceAssets.receiverRadius * .82f), new Vector2(waterRiceAssets.receiverHeight, waterRiceAssets.receiverRadius) },
            waterRiceAssets.receiverHeight, material);
        content = scoopFill.transform; receiverContent = receiverFill.transform;
        PickVisual(moving); PickVisual(transferSource); PickVisual(transferReceiver);
        transferSourceRadius = activeTransferJar ? activeTransferJar.openingRadius * .85f : water ? .125f : .15f;
        transferTargetRadius = waterRiceAssets.receiverRadius * .8f;
        transferTravelHeight = Mathf.Max(activeTransferJar ? sourceZone.y + .20f : water ? .56f : .36f, LocalVisualBounds(transferReceiver, world).max.y + .30f);
        pourStream = new GameObject(water ? "Water pour stream" : "Rice pour stream").AddComponent<LineRenderer>();
        pourStream.transform.SetParent(world, false); pourStream.positionCount = 3; pourStream.useWorldSpace = true;
        pourStream.sharedMaterial = material; pourStream.startWidth = water ? .006f : .012f; pourStream.endWidth = water ? .010f : .018f;
        pourStream.generateLightingData = true;
        pourStream.numCapVertices = 3; pourStream.enabled = false;
        dishWorkBounds = LocalVisualBounds(transferSource, world); dishWorkBounds.Encapsulate(LocalVisualBounds(transferReceiver, world));
        dishWorkBounds.Encapsulate(LocalVisualBounds(moving, world)); dishWorkBounds.Encapsulate(targetZone + new Vector3(.2f, transferTravelHeight - targetZone.y + .08f, 0));
        dishWorkBounds.Expand(new Vector3(.15f, .1f, .1f)); FrameDishLayout();
        sourceFill.SetLevel(1);
    }
    private Transform BuildLegacyTransferJar()
    {
        var original = activeTransferJar.original;
        if (!jarVisibility.ContainsKey(original)) jarVisibility.Add(original, original.enabled);
        var bounds = original.bounds;
        var root = Group("Existing " + (state.chore == HouseholdChore.FetchWater ? "water jar" : "rice jar"), world, Vector3.zero);
        root.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), Quaternion.identity);
        var body = JarPart("Original jar · open body", root, activeTransferJar.openBody);
        if (!activeTransferJar.openedLid)
        {
            // The old open jar is a one-sided shell. Render its existing back faces
            // instead of adding a cylindrical rim that can float above its irregular lip.
            var renderer = body.GetComponent<MeshRenderer>();
            var insideMaterials = renderer.sharedMaterials.Select(material =>
            {
                string key = "Double-sided existing jar " + material.GetInstanceID();
                if (!materials.TryGetValue(key, out var copy))
                {
                    copy = new Material(material) { name = key };
                    copy.SetFloat("_Cull", 0);
                    materials.Add(key, copy); ownedAssets.Add(copy);
                }
                return copy;
            }).ToArray();
            renderer.sharedMaterials = insideMaterials;
        }
        transferJarSurfaceQuery = body.gameObject.AddComponent<MeshCollider>(); transferJarSurfaceQuery.sharedMesh = activeTransferJar.openBody;
        body.gameObject.layer = 2;
        if (activeTransferJar.openedLid)
        {
            var lid = JarPart("Original jar · opened upper cap", root, activeTransferJar.openedLid);
            lid.position += new Vector3(bounds.extents.x + .55f, 0, -.55f);
            var lidBounds = lid.GetComponent<Renderer>().bounds;
            lid.position += Vector3.up * (.025f - lidBounds.min.y);
        }
        var floor = Group("LegacyJar_ContentsRoot", root, Vector3.zero);
        // This irregular open shell has a tall rear lip: keep water and the inner
        // lining recessed below the low front edge rather than using the highest lip.
        floor.position = activeTransferJar.Mouth - Vector3.up * (activeTransferJar.openedLid ? .20f : .32f);
        floor.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        BuildJarLiner(root, floor);
        original.enabled = false;
        return root;
    }
    private Transform JarPart(string name, Transform parent, Mesh mesh)
    {
        var original = activeTransferJar.original;
        var root = Group(name, parent, Vector3.zero); root.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        root.localScale = original.transform.lossyScale;
        root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.gameObject.AddComponent<MeshRenderer>().sharedMaterials = original.sharedMaterials;
        return root;
    }
    private void BuildJarLiner(Transform parent, Transform floor)
    {
        string key = "Existing jar liner " + activeTransferJar.GetInstanceID();
        if (!meshes.TryGetValue(key, out var mesh))
        {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
        float radius = activeTransferJar.openingRadius;
        bool openWater = !activeTransferJar.openedLid;
        float height = .20f;
        float bottomRadius = radius * .90f;
        float topRadius = radius * (openWater ? 1.16f : 1f);
        for (int i = 0; i < 48; i++)
        {
            float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
            Vector3 direction = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), next = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            int index = vertices.Count;
            vertices.Add(direction * bottomRadius); vertices.Add(next * bottomRadius);
            vertices.Add(direction * topRadius + Vector3.up * height); vertices.Add(next * topRadius + Vector3.up * height);
            triangles.AddRange(new[] { index, index + 1, index + 2, index + 1, index + 3, index + 2 });
            if (openWater) continue;
            index = vertices.Count;
            vertices.Add(direction * radius + Vector3.up * .20f); vertices.Add(next * radius + Vector3.up * .20f);
            vertices.Add(direction * radius / .86f + Vector3.up * .20f); vertices.Add(next * radius / .86f + Vector3.up * .20f);
            triangles.AddRange(new[] { index, index + 1, index + 2, index + 1, index + 3, index + 2 });
        }
        foreach (var v in vertices) uv.Add(new Vector2(v.x, v.z));
        mesh = new Mesh { name = "Temporary existing-jar inner wall and rim" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        ownedAssets.Add(mesh); meshes.Add(key, mesh);
        }
        var liner = Group("Jar inner wall", parent, Vector3.zero); liner.position = floor.position; liner.rotation = Quaternion.identity;
        liner.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        liner.gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor("jar inner clay", new Color(.13f, .075f, .035f));
        var bottom = Disk("Jar inner floor", parent, parent.InverseTransformPoint(floor.position), activeTransferJar.openingRadius * 1.8f,
            MaterialFor("jar inner clay", new Color(.13f, .075f, .035f)));
        bottom.rotation = Quaternion.identity;
    }
    private void RestoreCurrentTransferJar()
    {
        if (!activeTransferJar || !jarVisibility.TryGetValue(activeTransferJar.original, out bool visible)) return;
        activeTransferJar.original.enabled = visible; jarVisibility.Remove(activeTransferJar.original);
    }
    private void RestoreTransferJars()
    {
        foreach (var pair in jarVisibility) if (pair.Key) pair.Key.enabled = pair.Value;
        jarVisibility.Clear();
    }
    private void TickImportedTransfer(Vector3 point, int pick, bool down, bool pressed, bool up, float scroll, float dt)
    {
        if (down && pick == 0) { held = 0; tilt = 0; }
        transferPouring = false;
        if (held == 0 && pressed)
        {
            tilt = Mathf.Clamp01(tilt + scroll * .14f);
            // The exported root is the grip. Positive Z tilt lowers its -X pouring lip.
            moving.localRotation = Quaternion.Euler(0, 0, tilt * 100);
            moving.localPosition = new Vector3(point.x, transferTravelHeight, point.z);
            Vector3 mouth = world.InverseTransformPoint(transferFillCenter.position);
            if (InZone(mouth, sourceZone, transferSourceRadius) && tilt < .15f)
            {
                Vector3 position = moving.localPosition;
                position.y += sourceFill.SurfaceY - transferFillCenter.position.y - .012f;
                moving.localPosition = position;
                state.Scoop(dt); Touch(); PlayWater(state.chore == HouseholdChore.FetchWater);
                sourceFill.GetComponent<HouseholdWaterSurface>()?.Disturb(transferFillCenter.position, .75f);
            }
            else if (InZone(world.InverseTransformPoint(transferSpout.position), targetZone, transferTargetRadius) && tilt > .35f && state.fill > 0)
            {
                float before = state.fill; state.Pour(tilt, dt); transferPouring = state.fill < before;
                Touch(); PlayWater(state.chore == HouseholdChore.FetchWater && transferPouring);
                receiverFill.GetComponent<HouseholdWaterSurface>()?.Disturb(transferSpout.position, 1f);
            }
            else PlayWater(false);
        }
        if (up)
        {
            if (state.fill > .01f && !state.Complete) Tell("Đưa mép gáo/ca vào miệng nồi rồi lăn chuột để nghiêng đổ.");
            ReleasePointer(); transferPouring = false;
        }
        RefreshVisuals();
    }
    private void RefreshImportedTransfer()
    {
        sourceFill.SetLevel(1); scoopFill.SetLevel(state.fill);
        receiverFill.SetLevel(state.chore == HouseholdChore.FetchWater ? state.transferred : (state.delivered + state.transferred) / 3f);
        bool pouring = transferPouring && held == 0 && tilt > .35f;
        pourStream.enabled = pouring;
        if (pouring)
        {
            Vector3 from = transferSpout.position;
            Vector3 to = world.TransformPoint(targetZone + Vector3.up * .04f); to.y = Mathf.Max(to.y, receiverFill.SurfaceY);
            pourStream.SetPosition(0, from); pourStream.SetPosition(1, Vector3.Lerp(from, to, .5f) + Vector3.down * .02f); pourStream.SetPosition(2, to);
        }
    }
}
