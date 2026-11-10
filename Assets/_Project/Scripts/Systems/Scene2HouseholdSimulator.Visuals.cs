using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed partial class Scene2HouseholdSimulator
{
    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private Transform moving, tool, content, receiverContent, dishVisual, letterPaper, letterFlap;
    private readonly Transform[] pieces = new Transform[3];
    private readonly Vector3[] homes = new Vector3[3], slots = new Vector3[3];
    private readonly Transform[] stains = new Transform[6], bubbles = new Transform[8], racked = new Transform[3];
    private Vector3 movingHome, toolHome, sourceZone, targetZone, rinseZone, rackZone;
    private LineRenderer pourStream;
    private readonly LineRenderer[] tableGuides = new LineRenderer[3];
    private int tableHover = -1;
    private bool tableDropReady;
    private float nextScrubSound, letterReadSeconds;

    private void BuildWorld()
    {
        // After the meal is cleared the same dining surface can be used to read the letter.
        for (int i = finishedWorlds.Count - 1; i >= 0; i--)
            if (!finishedWorlds[i] || Vector3.Distance(finishedWorlds[i].transform.position, station.surface.position) < .75f)
            { if (finishedWorlds[i]) Destroy(finishedWorlds[i]); finishedWorlds.RemoveAt(i); }
        pickers.Clear();
        EnsureTableInteractionStorage();
        System.Array.Clear(tableGuides, 0, tableGuides.Length);
        System.Array.Clear(tablePreviews, 0, tablePreviews.Length);
        System.Array.Clear(tablePreviewMaterials, 0, tablePreviewMaterials.Length);
        System.Array.Clear(tablePieceRenderers, 0, tablePieceRenderers.Length);
        tableIgnoreUntilUp = false;
        tableHover = -1; tableDropReady = false;
        importedDishes = false;
        authoredDishes = false;
        dishLayout = GetComponent<Scene2DishwashingLayout>();
        importedTransfer = false;
        moving = tool = content = receiverContent = dishVisual = letterPaper = letterFlap = null;
        pourStream = null;
        System.Array.Clear(pieces, 0, pieces.Length);
        System.Array.Clear(stains, 0, stains.Length);
        System.Array.Clear(bubbles, 0, bubbles.Length);
        System.Array.Clear(racked, 0, racked.Length);
        world = new GameObject("Simulator · " + state.chore).transform;
        world.SetPositionAndRotation(station.surface.position, station.surface.rotation);
        world.localScale = Vector3.one;
        world.SetParent(transform, true);
        if (station.idleDressing && !(state.chore == HouseholdChore.WashDishes && dishLayout && dishLayout.IsReady)) station.idleDressing.gameObject.SetActive(false);
        // The dishwashing station shares the existing kitchen/floor. Never cover the
        // legacy stone washing area with a second procedural tabletop and four legs.
        if (!station.existingFixture && !station.transferJar) BuildWorkstand();
        if (state.chore == HouseholdChore.WashDishes) BuildDishes();
        else if (state.chore == HouseholdChore.FetchWater || state.chore == HouseholdChore.MeasureRice) BuildTransfer();
        else if (state.chore == HouseholdChore.ReadLetter) BuildLetter();
        else BuildPlacement();
        if (!loop)
        {
            loop = new GameObject("Simulator work audio").AddComponent<AudioSource>();
            loop.transform.SetParent(transform, false);
            loop.playOnAwake = false; loop.loop = true; loop.volume = .13f; loop.spatialBlend = .65f;
            loop.minDistance = 1; loop.maxDistance = 7; loop.dopplerLevel = 0;
            KuTyAudioMix.Route(loop, KuTyAudioBus.SFX);
            var kitchen = GetComponent<Scene2KitchenHandsOn>();
            waterClip = kitchen ? kitchen.waterClip : null;
            loop.clip = waterClip;
        }
        loop.transform.position = world.position;
        Physics.SyncTransforms();
    }

    private void BuildWorkstand()
    {
        Material wood = MaterialFor("warm wood", new Color(.32f, .19f, .095f));
        var top = Prop("Work cloth", PrimitiveType.Cube, world, new Vector3(0, -.025f, 0), new Vector3(1.25f, .045f, .76f),
            MaterialFor("woven cloth", new Color(.37f, .39f, .28f)));
        top.gameObject.AddComponent<BoxCollider>();
        float legHeight = Mathf.Max(.1f, world.position.y - .05f);
        for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
            Prop("Workstand leg", PrimitiveType.Cube, world, new Vector3(x * .53f, -legHeight * .5f - .025f, z * .27f),
                new Vector3(.055f, legHeight, .055f), wood);
        for (int i = 0; i < 12; i++)
            Prop("Cloth stitching", PrimitiveType.Cube, world, new Vector3(-.56f + i * .1f, .001f, -.34f),
                new Vector3(.035f, .002f, .003f), wood);
    }

    private void BuildDishes()
    {
        if (dishLayout && dishLayout.IsReady) { BuildAuthoredDishes(); return; }
        if (dishwashingAssets && dishwashingAssets.IsReady) { BuildImportedDishes(); return; }
        rinseZone = new Vector3(-.37f, .03f, .10f);
        rackZone = new Vector3(.39f, .03f, .12f);
        Bowl("Rinse basin", world, rinseZone, 1.6f, MaterialFor("basin enamel", new Color(.23f, .46f, .47f)));
        Disk("Fresh water", world, rinseZone + Vector3.up * .065f, .30f, WaterMaterial());
        for (int i = 0; i < 5; i++)
            Prop("Bamboo drying rail", PrimitiveType.Cylinder, world, rackZone + new Vector3((i - 2) * .045f, .03f, 0),
                new Vector3(.012f, .16f, .012f), MaterialFor("bamboo", new Color(.61f, .42f, .19f))).localRotation = Quaternion.Euler(90, 0, 0);
        movingHome = new Vector3(-.03f, .10f, -.03f);
        moving = Group("Hold and turn the bowl", world, movingHome);
        dishVisual = Bowl("Ceramic bowl", moving, Vector3.zero, 1, Ceramic());
        AddPick(moving, new Vector3(0, .045f, 0), new Vector3(.29f, .13f, .29f));
        toolHome = new Vector3(.17f, .035f, -.24f);
        tool = Prop("Sponge", PrimitiveType.Cube, world, toolHome, new Vector3(.09f, .035f, .06f),
            MaterialFor("sponge", new Color(.69f, .61f, .27f)));
        AddPick(tool, Vector3.zero, new Vector3(1.3f, 1.5f, 1.5f));
        var soap = Prop("Soap", PrimitiveType.Cube, world, new Vector3(-.30f, .025f, -.25f), new Vector3(.10f, .045f, .065f),
            MaterialFor("soap", new Color(.81f, .85f, .64f)));
        AddPick(soap, Vector3.zero, new Vector3(1.2f, 1.3f, 1.3f));
        AddPick(world, rinseZone + Vector3.up * .06f, new Vector3(.32f, .12f, .32f));
        AddPick(world, rackZone + Vector3.up * .05f, new Vector3(.29f, .12f, .32f));
        for (int side = 0; side < 2; side++)
            for (int i = 0; i < 3; i++)
            {
                float angle = (i + .45f) * Mathf.PI * 2 / 3;
                stains[side * 3 + i] = Prop("Food stain", PrimitiveType.Sphere, moving,
                    new Vector3(Mathf.Cos(angle) * .064f, .028f, Mathf.Sin(angle) * .064f),
                    new Vector3(.055f, .004f, .035f), MaterialFor("food stain", new Color(.31f, .16f, .055f)));
            }
        for (int i = 0; i < bubbles.Length; i++)
        {
            float a = i * 2.39996f;
            bubbles[i] = Prop("Soap foam", PrimitiveType.Sphere, moving,
                new Vector3(Mathf.Cos(a) * .07f, .038f, Mathf.Sin(a) * .07f), Vector3.one * (.014f + i % 3 * .004f),
                MaterialFor("foam", new Color(.90f, .94f, .86f)));
        }
        for (int i = 0; i < 3; i++) racked[i] = Bowl("Clean bowl " + (i + 1), world, rackZone + new Vector3(0, .05f + i * .033f, 0), .8f, Ceramic());
        WorldLabel("THAU NƯỚC", rinseZone + new Vector3(0, 0, .23f));
        WorldLabel("GIÁ CHÉN", rackZone + new Vector3(0, 0, .23f));
        WorldLabel("XÀ PHÒNG", new Vector3(-.30f, .005f, -.32f));
    }

    private void BuildTransfer()
    {
        if (waterRiceAssets && waterRiceAssets.IsReady) { BuildImportedTransfer(); return; }
        bool water = state.chore == HouseholdChore.FetchWater;
        sourceZone = new Vector3(-.34f, .03f, .06f);
        targetZone = new Vector3(.34f, .03f, .06f);
        Bowl(water ? "Water jar mouth" : "Rice jar mouth", world, sourceZone, 1.6f,
            MaterialFor("earthenware", new Color(.40f, .22f, .12f)));
        Disk("Supply", world, sourceZone + Vector3.up * .07f, .3f, water ? WaterMaterial() : Rice());
        Bowl("Receiving pot", world, targetZone, 1.6f, Ceramic());
        receiverContent = Disk("Poured contents", world, targetZone + Vector3.up * .025f, .29f, water ? WaterMaterial() : Rice());
        movingHome = new Vector3(-.02f, .11f, -.22f);
        moving = Group("Scoop", world, movingHome);
        Bowl("Scoop cup", moving, Vector3.zero, .65f, MaterialFor("scoop wood", new Color(.55f, .31f, .12f)));
        Prop("Scoop handle", PrimitiveType.Cube, moving, new Vector3(0, .02f, -.14f), new Vector3(.028f, .025f, .18f),
            MaterialFor("scoop wood", new Color(.55f, .31f, .12f)));
        content = Disk("Scoop contents", moving, new Vector3(0, .035f, 0), .14f, water ? WaterMaterial() : Rice());
        AddPick(moving, new Vector3(0, .025f, -.035f), new Vector3(.19f, .10f, .30f));
        AddPick(world, sourceZone, new Vector3(.34f, .16f, .34f));
        AddPick(world, targetZone, new Vector3(.34f, .16f, .34f));
        pourStream = new GameObject("Pour stream").AddComponent<LineRenderer>();
        pourStream.transform.SetParent(world, false); pourStream.positionCount = 3;
        pourStream.sharedMaterial = water ? WaterMaterial() : Rice();
        pourStream.startWidth = .012f; pourStream.endWidth = .019f;
        pourStream.numCapVertices = 3; pourStream.enabled = false;
        WorldLabel(water ? "MÚC NƯỚC" : "MÚC GẠO", sourceZone + new Vector3(0, 0, .24f));
        WorldLabel(water ? "NỒI NƯỚC" : "ĐONG 3 CA", targetZone + new Vector3(0, 0, .24f));
    }

    private void BuildPlacement()
    {
        var authored = GetComponent<Scene2AuthoredTableLayout>();
        if (state.chore == HouseholdChore.SetTable && authored && authored.IsReady)
        {
            for (int i = 0; i < 3; i++)
            {
                homes[i] = authored.sourceLocal[i]; slots[i] = authored.targetLocal[i];
                pieces[i] = Group("Table dish " + (i + 1), world, homes[i]);
                var source = authored.dishes[i];
                foreach (var sourceMesh in source.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = sourceMesh.GetComponent<MeshRenderer>();
                    if (!renderer || !sourceMesh.sharedMesh) continue;
                    var visual = new GameObject(sourceMesh.name + " · existing mesh");
                    visual.transform.SetParent(pieces[i], false);
                    visual.transform.localRotation = Quaternion.Inverse(authored.useFoodAssets ? world.rotation : source.rotation) * sourceMesh.transform.rotation;
                    var scale = sourceMesh.transform.lossyScale;
                    visual.transform.localScale = scale;
                    visual.transform.localPosition = Quaternion.Inverse(authored.useFoodAssets ? world.rotation : source.rotation) * (sourceMesh.transform.position - source.position);
                    visual.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
                    visual.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
                Bounds bounds = Scene2DishwashingLayout.VisualBounds(pieces[i]);
                if (authored.useFoodAssets)
                {
                    Vector3 center = world.TransformPoint(homes[i]);
                    pieces[i].position += new Vector3(center.x - bounds.center.x, 0, center.z - bounds.center.z);
                }
                pieces[i].position += Vector3.up * (world.TransformPoint(homes[i]).y + .003f - bounds.min.y);
                // The group itself sits on the plane; child offset preserves the source pivot/scale.
                Vector3 shift = pieces[i].localPosition - homes[i];
                foreach (Transform child in pieces[i]) child.localPosition += shift;
                pieces[i].localPosition = homes[i];
                var hit = pieces[i].gameObject.AddComponent<BoxCollider>(); hit.isTrigger = true;
                bounds = Scene2DishwashingLayout.VisualBounds(pieces[i]);
                hit.center = pieces[i].InverseTransformPoint(bounds.center); hit.size = bounds.size + Vector3.one * .012f;
                pickers.Add(hit);
                var guide = new GameObject("Table placement guide " + i).AddComponent<LineRenderer>();
                guide.transform.SetParent(world, false);
                guide.useWorldSpace = false; guide.loop = true; guide.positionCount = 48;
                guide.widthMultiplier = .003f;
                guide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                guide.receiveShadows = false;
                var shader = Shader.Find("Sprites/Default");
                if (shader)
                {
                    var material = new Material(shader);
                    ownedAssets.Add(material); guide.sharedMaterial = material;
                }
                for (int p = 0; p < guide.positionCount; p++)
                {
                    float angle = p * Mathf.PI * 2 / guide.positionCount;
                    guide.SetPosition(p, slots[i] + new Vector3(Mathf.Cos(angle) * .16f, .008f, Mathf.Sin(angle) * .16f));
                }
                tableGuides[i] = guide;
                BuildTablePreview(i);
                if (authored.useFoodAssets) HideTableSource(source);
            }
            return;
        }
        bool table = state.chore == HouseholdChore.SetTable;
        bool cabinet = state.chore == HouseholdChore.PutAwayDishes;
        string[] names = table ? new[] { "CHÉN", "ĐŨA", "NỒI CƠM" } : cabinet ? new[] { "CHÉN", "ĐĨA", "ĐŨA" } : new[] { "ÁO", "KHĂN", "KỶ VẬT" };
        for (int i = 0; i < 3; i++)
        {
            homes[i] = new Vector3(-.39f + i * .38f, .04f, -.22f);
            slots[i] = new Vector3(-.39f + i * .38f, .04f, .18f);
            Disk("Placement mark", world, slots[i] - Vector3.up * .035f, .26f,
                MaterialFor("placement mark", new Color(.58f, .57f, .36f)));
            WorldLabel(names[i], slots[i] + new Vector3(0, .0f, .14f));
            pieces[i] = Group(names[i], world, homes[i]);
            if (table && i == 0 || cabinet && i == 0)
            {
                if (dishwashingAssets && dishwashingAssets.bowl) Art(dishwashingAssets.bowl, "Bowl", pieces[i], Vector3.zero);
                else Bowl("Bowl", pieces[i], Vector3.zero, .8f, Ceramic());
            }
            else if (table && i == 1 || cabinet && i == 2)
            {
                for (int c = 0; c < 2; c++) Prop("Chopstick", PrimitiveType.Cube, pieces[i], new Vector3((c - .5f) * .022f, .01f, 0),
                    new Vector3(.009f, .009f, .23f), MaterialFor("bamboo", new Color(.61f, .42f, .19f)));
            }
            else if (table)
            {
                Bowl("Rice serving pot", pieces[i], Vector3.zero, 1, MaterialFor("pot metal", new Color(.22f, .24f, .24f)));
                Disk("Cooked rice", pieces[i], Vector3.up * .06f, .21f, Rice());
            }
            else if (cabinet)
            {
                if (dishwashingAssets && dishwashingAssets.plate) Art(dishwashingAssets.plate, "Plate", pieces[i], Vector3.zero);
                else Disk("Plate", pieces[i], Vector3.up * .015f, .22f, Ceramic());
            }
            else
            {
                Prop("Folded keepsake", i == 2 ? PrimitiveType.Sphere : PrimitiveType.Cube, pieces[i], Vector3.up * .02f,
                    i == 2 ? new Vector3(.09f, .06f, .09f) : new Vector3(.20f, .045f, .14f),
                    MaterialFor("keepsake " + i, i == 0 ? new Color(.26f, .43f, .49f) : i == 1 ? new Color(.71f, .55f, .36f) : new Color(.48f, .29f, .19f)));
            }
            AddPick(pieces[i], Vector3.up * .03f, new Vector3(.26f, .14f, .25f));
        }
    }

    private void BuildLetter()
    {
        movingHome = new Vector3(0, .03f, 0);
        moving = Group("Mother's envelope", world, movingHome);
        var paper = MaterialFor("letter paper", new Color(.87f, .82f, .66f));
        Prop("Envelope", PrimitiveType.Cube, moving, Vector3.zero, new Vector3(.36f, .005f, .23f), paper);
        letterFlap = Prop("Envelope flap", PrimitiveType.Cube, moving, new Vector3(0, .008f, .04f), new Vector3(.32f, .004f, .13f), paper);
        letterPaper = Prop("Folded letter", PrimitiveType.Cube, moving, new Vector3(0, .015f, -.02f), new Vector3(.30f, .003f, .18f), paper);
        AddPick(moving, new Vector3(0, .02f, 0), new Vector3(.52f, .10f, .45f));
        WorldLabel("GỬI TÝ CỦA MẸ", new Vector3(0, .04f, -.06f));
    }

    private void RefreshVisuals()
    {
        if (!world || state == null) return;
        RefreshTableGuides();
        if (state.chore == HouseholdChore.WashDishes)
        {
            if (authoredDishes) { RefreshAuthoredDishes(); return; }
            for (int i = 0; i < 6; i++) if (stains[i])
            {
                stains[i].gameObject.SetActive(i / 3 == (state.flipped ? 1 : 0) && state.stains[i] > .02f && !state.Complete);
                if (!importedDishes)
                {
                    Vector3 stainPosition = stains[i].localPosition;
                    stainPosition.y = state.flipped ? .083f : .028f;
                    stains[i].localPosition = stainPosition;
                }
                stains[i].localScale = (importedDishes ? new Vector3(.023f, .0016f, .018f) : new Vector3(.055f, .004f, .035f)) * Mathf.Sqrt(Mathf.Max(.01f, state.stains[i]));
            }
            // A separate visible underside cue; the bowl remains safely pickable at either orientation.
            if (dishVisual)
            {
                dishVisual.localRotation = (importedDishes ? dishRestRotation : Quaternion.identity) * Quaternion.Euler(state.flipped ? 180 : 0, 0, 0);
                dishVisual.localPosition = Vector3.up * (state.flipped ? (importedDishes ? dishwashingAssets.bowlSize.y * dishVisual.localScale.y : .08f) : 0);
            }
            for (int i = 0; i < bubbles.Length; i++) if (bubbles[i])
            {
                bubbles[i].gameObject.SetActive(state.soaped && state.rinse < 1 && !state.Complete && (!importedDishes || i / 4 == (state.flipped ? 1 : 0)));
                if (!importedDishes)
                {
                    Vector3 p = bubbles[i].localPosition; p.y = state.flipped ? .092f : .038f;
                    bubbles[i].localPosition = p;
                }
                else bubbles[i].localScale = Vector3.one * (.006f + i % 3 * .002f) * (1 - state.rinse);
            }
            if (importedDishes) RefreshImportedDishes();
            for (int i = 0; i < racked.Length; i++) if (racked[i]) racked[i].gameObject.SetActive(i < state.delivered);
            if (moving) moving.gameObject.SetActive(!state.Complete);
        }
        else if (state.chore == HouseholdChore.FetchWater || state.chore == HouseholdChore.MeasureRice)
        {
            if (importedTransfer) { RefreshImportedTransfer(); return; }
            if (content) content.gameObject.SetActive(state.fill > .015f);
            if (receiverContent)
            {
                float fill = state.chore == HouseholdChore.FetchWater ? state.transferred : (state.delivered + state.transferred) / 3f;
                receiverContent.gameObject.SetActive(fill > .01f);
                Vector3 p = targetZone + Vector3.up * Mathf.Lerp(.012f, .075f, fill); receiverContent.localPosition = p;
            }
            if (pourStream)
            {
                bool pouring = held == 0 && tilt > .35f && state.fill > .005f && InZone(moving.localPosition, targetZone, .2f);
                pourStream.enabled = pouring;
                if (pouring)
                {
                    Vector3 from = moving.TransformPoint(new Vector3(.07f, .03f, 0));
                    Vector3 to = world.TransformPoint(targetZone + Vector3.up * .07f);
                    pourStream.SetPosition(0, from); pourStream.SetPosition(1, Vector3.Lerp(from, to, .5f) + Vector3.down * .025f); pourStream.SetPosition(2, to);
                }
            }
        }
        else if (state.chore == HouseholdChore.ReadLetter)
        {
            letterFlap.localRotation = Quaternion.Euler(state.letterStep >= 1 ? -150 : 0, 0, 0);
            letterPaper.localScale = new Vector3(.30f, .003f, state.letterStep >= 2 ? .36f : .18f);
            letterPaper.localPosition = new Vector3(0, .018f, state.letterStep >= 1 ? -.11f : -.02f);
        }
    }

    private Transform Group(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
    }
    private Transform Prop(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        var collider = go.GetComponent<Collider>(); if (collider) { collider.enabled = false; Destroy(collider); }
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }
    private void AddPick(Transform parent, Vector3 center, Vector3 size)
    {
        var collider = parent.gameObject.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.center = center; collider.size = size;
        pickers.Add(collider);
    }
    private Transform Disk(string name, Transform parent, Vector3 position, float diameter, Material mat) =>
        Prop(name, PrimitiveType.Cylinder, parent, position, new Vector3(diameter, .002f, diameter), mat);

    private Transform Bowl(string name, Transform parent, Vector3 position, float scale, Material mat)
    {
        var root = Group(name, parent, position); root.localScale = Vector3.one * scale;
        var mesh = root.gameObject.AddComponent<MeshFilter>(); mesh.sharedMesh = BowlMesh();
        root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
        // Thin blue rim makes the cup silhouette and inside/outside orientation readable.
        var rim = Group("Glazed rim", root, Vector3.zero);
        rim.gameObject.AddComponent<MeshFilter>().sharedMesh = RimMesh();
        rim.gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor("blue glaze", new Color(.14f, .28f, .43f));
        return root;
    }
    private Mesh BowlMesh()
    {
        if (meshes.TryGetValue("bowl", out var cached)) return cached;
        return Lathe("bowl", new[] { new Vector2(.001f, 0), new Vector2(.065f, 0), new Vector2(.11f, .035f),
            new Vector2(.14f, .08f), new Vector2(.132f, .08f), new Vector2(.103f, .039f), new Vector2(.060f, .012f), new Vector2(.001f, .012f) });
    }
    private Mesh RimMesh()
    {
        if (meshes.TryGetValue("rim", out var cached)) return cached;
        return Lathe("rim", new[] { new Vector2(.132f, .078f), new Vector2(.14f, .078f), new Vector2(.14f, .082f), new Vector2(.132f, .082f), new Vector2(.132f, .078f) });
    }
    private Mesh Lathe(string key, Vector2[] profile)
    {
        const int segments = 40;
        var vertices = new Vector3[profile.Length * (segments + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(profile.Length - 1) * segments * 6];
        int cursor = 0;
        for (int p = 0; p < profile.Length; p++) for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            int v = p * (segments + 1) + i;
            vertices[v] = new Vector3(Mathf.Cos(angle) * profile[p].x, profile[p].y, Mathf.Sin(angle) * profile[p].x);
            uv[v] = new Vector2((float)i / segments, (float)p / (profile.Length - 1));
            if (p == profile.Length - 1 || i == segments) continue;
            int next = v + segments + 1;
            triangles[cursor++] = v; triangles[cursor++] = next; triangles[cursor++] = v + 1;
            triangles[cursor++] = v + 1; triangles[cursor++] = next; triangles[cursor++] = next + 1;
        }
        var mesh = new Mesh { name = "Household " + key, vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes[key] = mesh; ownedAssets.Add(mesh); return mesh;
    }
    private Material MaterialFor(string key, Color color)
    {
        if (materials.TryGetValue(key, out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "Simulator " + key, color = color };
        mat.SetFloat("_Smoothness", key.Contains("glaze") || key.Contains("ceramic") ? .65f : .2f);
        materials[key] = mat; ownedAssets.Add(mat); return mat;
    }
    private Material Ceramic() => MaterialFor("ceramic", new Color(.83f, .85f, .74f));
    private Material Rice() => MaterialFor("rice", new Color(.89f, .84f, .67f));
    private Material WaterMaterial() => MaterialFor("water", new Color(.34f, .62f, .66f));
    private void WorldLabel(string text, Vector3 position)
    {
        var label = new GameObject(text, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(world, false); label.transform.localPosition = position + Vector3.up * .004f;
        label.transform.localRotation = Quaternion.Euler(90, 0, 0);
        label.rectTransform.sizeDelta = new Vector2(.35f, .075f);
        label.fontSize = .55f; label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        if (GameUiTheme.Current) label.font = GameUiTheme.Current.font;
        label.color = new Color(.94f, .88f, .67f);
    }
}
