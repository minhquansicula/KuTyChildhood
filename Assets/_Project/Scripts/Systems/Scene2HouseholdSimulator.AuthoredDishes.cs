using System.Collections.Generic;
using UnityEngine;

public sealed partial class Scene2HouseholdSimulator
{
    private bool authoredDishes, faucetOpen;
    private Scene2DishwashingLayout dishLayout;
    private readonly Dictionary<GameObject, bool> hiddenDishSources = new Dictionary<GameObject, bool>();
    private readonly Transform[] authoredHolders = new Transform[3];
    private readonly Transform[] authoredVisuals = new Transform[3];
    private readonly Vector3[] authoredHomes = new Vector3[3];
    private readonly Quaternion[] authoredRackRotations = new Quaternion[3];
    private readonly MeshCollider[][] dishQueries = new MeshCollider[3][];
    private readonly Vector3[] stainWorldSizes = new Vector3[6];
    private readonly Vector3[] latherSizes = new Vector3[8];
    private Bounds authoredBasinBounds, authoredRackBounds;
    private Transform authoredSoap, tapPick;
    private Quaternion toolHomeRotation;
    private LineRenderer faucetStream;
    private int authoredDishIndex;
    private float authoredPlaneHeight;
    private Vector3 authoredCameraFocus;
    private Transform tapSplash;
    private Vector3 tapLanding;
    private Vector3 AuthoredCameraDestination => dishCloseView && moving
        ? Vector3.Lerp(dishCameraPosition, moving.position - dishCameraRotation * Vector3.forward * .52f, dishZoomBlend)
        : dishCameraPosition;

    private void HideDishSource(Transform source)
    {
        if (!hiddenDishSources.ContainsKey(source.gameObject)) hiddenDishSources.Add(source.gameObject, source.gameObject.activeSelf);
        source.gameObject.SetActive(false);
    }
    private void RestoreDishSources()
    {
        foreach (var pair in hiddenDishSources) if (pair.Key) pair.Key.SetActive(pair.Value);
        hiddenDishSources.Clear();
    }
    private Transform CloneDishProp(Transform source, string name)
    {
        var clone = Instantiate(source.gameObject, world).transform;
        clone.name = name;
        clone.SetPositionAndRotation(source.position, source.rotation);
        clone.localScale = source.lossyScale;
        clone.gameObject.SetActive(true);
        foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
        { behaviour.enabled = false; Destroy(behaviour); }
        foreach (var collider in clone.GetComponentsInChildren<Collider>(true))
        { collider.enabled = false; Destroy(collider); }
        return clone;
    }
    private void BuildAuthoredDishes()
    {
        authoredDishes = true;
        world.SetPositionAndRotation(dishLayout.fixture.position, Quaternion.identity);
        dishCloseView = false; dishZoomBlend = 0; faucetOpen = false;
        authoredBasinBounds = Scene2DishwashingLayout.VisualBounds(dishLayout.basin);
        authoredRackBounds = Scene2DishwashingLayout.VisualBounds(dishLayout.rack);
        rinseZone = world.InverseTransformPoint(dishLayout.outlet.position);
        rackZone = world.InverseTransformPoint(authoredRackBounds.center);
        authoredPlaneHeight = world.InverseTransformPoint(authoredBasinBounds.center).y + .16f;
        Bounds area = Scene2DishwashingLayout.VisualBounds(dishLayout.fixture);
        foreach (var source in new[] { dishLayout.basin, dishLayout.rack, dishLayout.faucet, dishLayout.sponge, dishLayout.soap })
            area.Encapsulate(Scene2DishwashingLayout.VisualBounds(source));
        for (int i = 0; i < 3; i++)
        {
            var source = dishLayout.dishes[i].source;
            var bounds = Scene2DishwashingLayout.VisualBounds(source);
            area.Encapsulate(bounds);
            var holder = Group("Dish " + (i + 1) + " · " + dishLayout.dishes[i].label, world, world.InverseTransformPoint(bounds.center));
            var visual = CloneDishProp(source, "Authored ceramic " + (i + 1));
            visual.SetParent(holder, true);
            authoredHolders[i] = holder; authoredVisuals[i] = visual; authoredHomes[i] = holder.localPosition;
            var queries = new List<MeshCollider>();
            foreach (var mesh in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                var query = mesh.gameObject.AddComponent<MeshCollider>();
                query.sharedMesh = mesh.sharedMesh;
                query.gameObject.layer = 2; // Explicit ray queries only; does not block E or the player's body.
                queries.Add(query);
            }
            dishQueries[i] = queries.ToArray();
            HideDishSource(source);
            // Lay dishes in separate lanes across the user's rack, not a generic distant drop circle.
            Vector3 slot = authoredRackBounds.center;
            slot.x += (i - 1) * Mathf.Min(.29f, authoredRackBounds.size.x * .30f);
            authoredRackRotations[i] = Quaternion.Euler(0, 0, 58f);
            holder.localRotation = authoredRackRotations[i];
            float tiltedHeight = Scene2DishwashingLayout.VisualBounds(visual).extents.y;
            holder.localRotation = Quaternion.identity;
            slot.y = authoredRackBounds.min.y + tiltedHeight + .055f;
            slots[i] = world.InverseTransformPoint(slot);
            if (i < state.delivered)
            {
                holder.localPosition = slots[i];
                holder.localRotation = authoredRackRotations[i];
            }
        }
        tool = CloneDishProp(dishLayout.sponge, "Working sponge");
        toolHome = tool.localPosition; toolHomeRotation = tool.rotation;
        authoredSoap = CloneDishProp(dishLayout.soap, "Working soap");
        HideDishSource(dishLayout.sponge); HideDishSource(dishLayout.soap);
        // Stable IDs: current dish, sponge, soap, faucet. The fixtures remain real scene objects.
        AddPick(authoredHolders[0], Vector3.zero, Vector3.one * .01f);
        PickVisual(tool); PickVisual(authoredSoap);
        tapPick = Group("Faucet interaction", world, world.InverseTransformPoint(Scene2DishwashingLayout.VisualBounds(dishLayout.faucet).center));
        AddPick(tapPick, Vector3.zero, Scene2DishwashingLayout.VisualBounds(dishLayout.faucet).size + Vector3.one * .025f);
        faucetStream = Group("Running tap water", world, Vector3.zero).gameObject.AddComponent<LineRenderer>();
        faucetStream.useWorldSpace = true; faucetStream.positionCount = 3;
        faucetStream.startWidth = .009f; faucetStream.endWidth = .014f; faucetStream.numCapVertices = 4;
        faucetStream.sharedMaterial = MaterialFor("tap stream", new Color(.53f, .77f, .82f));
        faucetStream.enabled = false;
        tapLanding = dishLayout.outlet.position;
        tapLanding.y = Scene2DishwashingLayout.VisualBounds(dishLayout.fixture).min.y + .015f;
        float landingDistance = 3f;
        foreach (var root in new[] { dishLayout.fixture, dishLayout.basin })
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                if (collider.Raycast(new Ray(dishLayout.outlet.position, Vector3.down), out var hit, landingDistance))
                { tapLanding = hit.point + Vector3.up * .002f; landingDistance = hit.distance; }
        tapSplash = Disk("Tap splash", world, world.InverseTransformPoint(tapLanding), .045f,
            MaterialFor("tap splash", new Color(.63f, .78f, .78f)));
        tapSplash.gameObject.SetActive(false);
        // Use the basin's real WaterSurface attachment if available, without cloning or moving its mesh.
        var waterAnchor = FindDishAttachment(dishLayout.basin, "WaterSurface");
        if (waterAnchor)
        {
            float diameter = Mathf.Min(authoredBasinBounds.size.x, authoredBasinBounds.size.z) * .78f;
            var water = Disk("Basin water", world, world.InverseTransformPoint(waterAnchor.position), diameter,
                waterRiceAssets && waterRiceAssets.waterMaterial ? waterRiceAssets.waterMaterial : WaterMaterial());
            if (water.GetComponent<Renderer>().sharedMaterial.shader.name == "KuTy/Household Water")
                dishWaterSurface = water.gameObject.AddComponent<HouseholdWaterSurface>();
        }
        area.Expand(new Vector3(.18f, .12f, .18f));
        dishWorkBounds = new Bounds(world.InverseTransformPoint(area.center), area.size);
        authoredCameraFocus = area.center;
        // Front is the open floor, matching the user's washing-area layout.
        // A steep view exposes the bowl's near interior; a shallow camera makes
        // the real rim occlude an otherwise valid dirt patch.
        Vector3 direction = new Vector3(0, 1.15f, -.45f).normalized;
        dishCameraRotation = Quaternion.LookRotation(-direction, Vector3.up);
        float distance = 1f;
        float tangent = Mathf.Tan(24f * Mathf.Deg2Rad);
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
        {
            Vector3 p = Quaternion.Inverse(dishCameraRotation) * Vector3.Scale(area.extents, new Vector3(x, y, z));
            distance = Mathf.Max(distance, Mathf.Abs(p.y) / (tangent * .75f) - p.z,
                Mathf.Abs(p.x) / (tangent * Mathf.Min(view.aspect, 16f / 9f) * .85f) - p.z);
        }
        dishCameraPosition = authoredCameraFocus + direction * (distance + .10f);
        authoredDishIndex = -1;
        SelectAuthoredDish();
    }
    private static Transform FindDishAttachment(Transform root, string name)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }
    private void SelectAuthoredDish()
    {
        if (state.delivered >= 3) { moving = dishVisual = null; pickers[0].enabled = false; return; }
        authoredDishIndex = state.delivered;
        moving = authoredHolders[authoredDishIndex]; dishVisual = authoredVisuals[authoredDishIndex];
        movingHome = authoredHomes[authoredDishIndex];
        if (pickers[0]) { pickers[0].enabled = false; Destroy(pickers[0]); }
        var bounds = LocalVisualBounds(dishVisual, moving);
        var pick = moving.gameObject.AddComponent<BoxCollider>();
        pick.isTrigger = true; pick.center = bounds.center; pick.size = bounds.size + Vector3.one * .015f;
        pickers[0] = pick;
        for (int i = 0; i < stains.Length; i++) if (stains[i]) { stains[i].gameObject.SetActive(false); Destroy(stains[i].gameObject); }
        for (int i = 0; i < bubbles.Length; i++) if (bubbles[i]) { bubbles[i].gameObject.SetActive(false); Destroy(bubbles[i].gameObject); }
        var dish = dishLayout.dishes[authoredDishIndex];
        for (int i = 0; i < 6; i++)
        {
            Vector3 point = dishVisual.TransformPoint(dish.points[i]);
            Vector3 normal = dishVisual.TransformDirection(dish.normals[i]).normalized;
            stains[i] = Prop("Food residue " + i, PrimitiveType.Sphere, world, Vector3.zero, new Vector3(.026f, .0018f, .021f),
                MaterialFor("authored food residue", new Color(.24f, .105f, .028f)));
            stains[i].SetPositionAndRotation(point + normal * .0009f, Quaternion.FromToRotation(Vector3.up, normal));
            stains[i].SetParent(dishVisual, true);
            stainWorldSizes[i] = stains[i].localScale;
        }
        for (int i = 0; i < bubbles.Length; i++)
        {
            int patch = (i / 4) * 3 + i % 3;
            bubbles[i] = Prop("Soap lather " + i, PrimitiveType.Sphere, world, Vector3.zero, Vector3.one * (.010f + i % 3 * .003f),
                MaterialFor("authored lather", new Color(.90f, .94f, .88f)));
            bubbles[i].position = stains[patch].position + stains[patch].up * .003f;
            bubbles[i].SetParent(dishVisual, true);
            latherSizes[i] = bubbles[i].localScale;
        }
        RefreshAuthoredDishes();
    }
    private bool DishUnderTap(Vector3 point)
    {
        Vector3 outlet = world.InverseTransformPoint(dishLayout.outlet.position);
        return faucetOpen && new Vector2(point.x - outlet.x, point.z - outlet.z).magnitude < .095f;
    }
    private bool InAuthoredBasin(Vector3 point)
    {
        Vector3 p = world.TransformPoint(point);
        return Mathf.Abs(p.x - authoredBasinBounds.center.x) < authoredBasinBounds.extents.x * .65f &&
            Mathf.Abs(p.z - authoredBasinBounds.center.z) < authoredBasinBounds.extents.z * .65f;
    }
    private bool InAuthoredRack(Vector3 point)
    {
        Vector3 p = world.TransformPoint(point);
        return Mathf.Abs(p.x - authoredRackBounds.center.x) < authoredRackBounds.extents.x + .035f &&
            Mathf.Abs(p.z - authoredRackBounds.center.z) < authoredRackBounds.extents.z + .035f;
    }
    private void ProcessAuthoredDishPointer(Vector2 screen, bool down, bool pressed, bool up, bool cancel, float scroll, float dt)
    {
        if (cancel) { ReleasePointer(); Tell("Đã đặt vật lại; vết bẩn đã chà vẫn được giữ."); return; }
        if (screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
        { pointerValid = false; if (up) ReleasePointer(); return; }
        Physics.SyncTransforms();
        Ray ray = view.ScreenPointToRay(screen);
        int pick = Pick(ray);
        if (down)
        {
            if (pick == 3)
            {
                faucetOpen = !faucetOpen;
                Tell(faucetOpen ? "Đã mở vòi. Đưa chén/dĩa vào dưới dòng nước." : "Đã khóa vòi nước.");
            }
            else if (pick == 2)
            {
                if (state.wetted) { state.soaped = true; state.rinse = 0; Touch(); Tell("Miếng rửa đã có xà phòng."); }
                else Tell("Làm ướt chén/dĩa trong thau hoặc dưới vòi trước nhé.");
            }
            else if (pick == 0 || pick == 1)
            {
                // Close-up is a scrubbing mode. Clicking the bowl with the equipped
                // sponge must not silently switch back to carrying the bowl.
                held = dishCloseView && pick == 0 ? 1 : pick;
                pointerValid = false;
            }
        }
        if (held == 0 && Mathf.Abs(scroll) > .01f)
        { state.flipped = !state.flipped; Touch(); Tell(state.flipped ? "Mặt ngoài / đáy." : "Mặt trong / lòng."); }
        var plane = new Plane(Vector3.up, world.TransformPoint(Vector3.up * authoredPlaneHeight));
        if (!plane.Raycast(ray, out float distance)) { if (up) ReleasePointer(); RefreshAuthoredDishes(); return; }
        Vector3 point = world.InverseTransformPoint(ray.GetPoint(distance));
        point.x = Mathf.Clamp(point.x, dishWorkBounds.min.x, dishWorkBounds.max.x);
        point.z = Mathf.Clamp(point.z, dishWorkBounds.min.z, dishWorkBounds.max.z);
        if (held == 0 && pressed && moving)
        {
            moving.localPosition = new Vector3(point.x, authoredPlaneHeight, point.z);
            if (DishUnderTap(point)) { state.Rinse(Mathf.Min(dt, .05f)); Touch(); dishWaterSurface?.Disturb(moving.position, .6f); }
            else if (InAuthoredBasin(point)) { state.Wet(Mathf.Min(dt, .05f)); Touch(); }
        }
        if (held == 1 && pressed && moving)
        {
            bool contact = false;
            RaycastHit hit = default;
            float nearest = 5f;
            foreach (var query in dishQueries[authoredDishIndex])
                if (query && query.Raycast(ray, out var candidate, nearest)) { nearest = candidate.distance; hit = candidate; contact = true; }
            if (contact && !AuthoredDishOccluded(ray, hit.distance))
            {
                tool.SetPositionAndRotation(hit.point + hit.normal * .010f, Quaternion.FromToRotation(Vector3.up, hit.normal) * toolHomeRotation);
                float travel = pointerValid ? Vector3.Distance(hit.point, lastPointerPoint) : 0;
                lastPointerPoint = hit.point; pointerValid = true;
                int patch = -1; float closest = .032f;
                int side = state.flipped ? 3 : 0;
                for (int i = side; i < side + 3; i++)
                {
                    float d = Vector3.Distance(hit.point, stains[i].position);
                    if (d < closest) { closest = d; patch = i; }
                }
                if (travel > .0002f && travel < .07f && state.Scrub(patch, travel))
                {
                    Touch();
                    if (Time.time >= nextScrubSound) { AudioManager.Instance?.PlaySFX(SfxId.TaskProgress); nextScrubSound = Time.time + .35f; }
                }
                else if (!state.wetted || !state.soaped) Tell("Làm ướt, rồi bấm bánh xà phòng trước khi chà.");
            }
            else pointerValid = false;
        }
        if (up && held == 0 && moving && InAuthoredRack(point))
        {
            if (state.DishReady)
            {
                int index = state.delivered;
                state.RackDish();
                authoredHolders[index].localPosition = slots[index];
                authoredHolders[index].localRotation = authoredRackRotations[index];
                pickers[0].enabled = false;
                AudioManager.Instance?.PlaySFX(SfxId.DishClean); Touch();
                Tell("Đã đặt đồ sạch lên giá · " + state.delivered + "/3");
                held = -1; pointerValid = false;
                SelectAuthoredDish();
                if (!moving) StopAuthoredTap();
            }
            else Tell(state.Scrubbed ? "Tráng dưới vòi đang mở đến khi hết bọt trước nhé." : "Còn vết bẩn — nhớ chà cả hai mặt.");
        }
        if (up) ReleasePointer();
        if (down || pressed && held >= 0) idleTime = 0;
        RefreshAuthoredDishes();
    }
    private bool AuthoredDishOccluded(Ray ray, float distance)
    {
        int count = Physics.RaycastNonAlloc(ray, occlusionHits, distance, ~0, QueryTriggerInteraction.Ignore);
        if (count == occlusionHits.Length) return true;
        for (int i = 0; i < count; i++)
        {
            var hit = occlusionHits[i];
            if (!hit.collider || hit.transform.IsChildOf(world) || hit.collider.GetComponentInParent<FirstPersonController>()) continue;
            if (hit.distance < distance - .003f) return true;
        }
        return false;
    }
    private void RefreshAuthoredDishes()
    {
        if (!authoredDishes || !world) return;
        for (int i = 0; i < 3; i++)
        {
            authoredHolders[i].gameObject.SetActive(true);
            if (i < state.delivered) continue;
            if (i != authoredDishIndex) authoredHolders[i].localPosition = authoredHomes[i];
        }
        if (moving) moving.localRotation = Quaternion.Euler(state.flipped ? 180 : 0, 0, 0);
        for (int i = 0; i < 6; i++) if (stains[i])
        {
            stains[i].gameObject.SetActive(!state.Complete && i / 3 == (state.flipped ? 1 : 0) && state.stains[i] > .02f);
            stains[i].localScale = stainWorldSizes[i] * Mathf.Sqrt(Mathf.Max(.01f, state.stains[i]));
        }
        for (int i = 0; i < bubbles.Length; i++) if (bubbles[i])
        {
            bubbles[i].gameObject.SetActive(!state.Complete && state.soaped && state.rinse < 1 && i / 4 == (state.flipped ? 1 : 0));
            bubbles[i].localScale = latherSizes[i] * Mathf.Lerp(.15f, 1, 1 - state.rinse);
        }
        if (faucetStream)
        {
            faucetStream.enabled = faucetOpen;
            if (faucetOpen)
            {
                Vector3 start = dishLayout.outlet.position;
                float endY = tapLanding.y;
                if (held == 0 && moving && DishUnderTap(moving.localPosition))
                    endY = Scene2DishwashingLayout.VisualBounds(dishVisual).max.y;
                Vector3 end = new Vector3(start.x, Mathf.Min(start.y - .02f, endY), start.z);
                faucetStream.SetPosition(0, start);
                faucetStream.SetPosition(1, Vector3.Lerp(start, end, .5f));
                faucetStream.SetPosition(2, end);
                if (tapSplash)
                {
                    tapSplash.position = end;
                    float pulse = 1 + Mathf.Sin(Time.time * 17) * .15f;
                    tapSplash.localScale = new Vector3(.045f * pulse, .002f, .045f * pulse);
                }
            }
        }
        if (tapSplash) tapSplash.gameObject.SetActive(faucetOpen);
        PlayWater(faucetOpen);
    }
    private void StopAuthoredTap()
    {
        faucetOpen = false;
        if (faucetStream) faucetStream.enabled = false;
        if (tapSplash) tapSplash.gameObject.SetActive(false);
        PlayWater(false);
    }
    private void CloseAuthoredDishes(bool cancel)
    {
        StopAuthoredTap();
        if (cancel) RestoreDishSources();
        else
        {
            foreach (var source in new[] { dishLayout.sponge, dishLayout.soap })
                if (hiddenDishSources.TryGetValue(source.gameObject, out bool active))
                { source.gameObject.SetActive(active); hiddenDishSources.Remove(source.gameObject); }
            if (tool) { tool.gameObject.SetActive(false); Destroy(tool.gameObject); }
            if (authoredSoap) { authoredSoap.gameObject.SetActive(false); Destroy(authoredSoap.gameObject); }
        }
    }
    private string AuthoredDishCaption()
    {
        string item = state.Complete ? "Hoàn tất" : dishLayout.dishes[Mathf.Min(state.delivered, 2)].label;
        string stage = !state.wetted ? "Kéo đồ vào thau hoặc dưới vòi đang mở để làm ướt" :
            !state.soaped ? "Bấm bánh xà phòng" : !state.Scrubbed ? "Cầm miếng rửa, giữ chuột và chà từng vết bẩn · Lăn chuột khi cầm đồ để lật mặt" :
            state.rinse < 1 ? "Mở vòi · Giữ đồ dưới dòng nước để tráng bọt: " + Mathf.RoundToInt(state.rinse * 100) + "%" : "Kéo đồ sạch lên giá rồi thả chuột";
        float clean = 0; foreach (float dirt in state.stains) clean += 1 - dirt;
        return "Đã xếp " + state.delivered + "/3 · " + item + " · Sạch " + Mathf.RoundToInt(clean / 6 * 100) + "% · " +
            (state.flipped ? "MẶT NGOÀI" : "MẶT TRONG") + "\n" + stage;
    }
}
