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
    private static readonly Quaternion AuthoredScrubRotation = Quaternion.LookRotation(new Vector3(0, -1.15f, .8f), Vector3.up);
    private Quaternion AuthoredCameraRotation => Quaternion.Slerp(dishCameraRotation, AuthoredScrubRotation, dishZoomBlend);
    private Vector3 AuthoredCameraDestination => moving && dishZoomBlend > 0
        ? Vector3.Lerp(dishCameraPosition, moving.position - AuthoredScrubRotation * Vector3.forward * .56f, dishZoomBlend)
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
        authoredRackQueries = new List<MeshCollider>();
        world.SetPositionAndRotation(dishLayout.fixture.position, Quaternion.identity);
        // Rack solids can be broad boxes around an open frame. Refine them against its actual mesh,
        // rather than ignoring the rack or accepting a hidden target through the wooden rails.
        foreach (var mesh in dishLayout.rack.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.sharedMesh) continue;
            var query = new GameObject("Rack exact surface query").AddComponent<MeshCollider>();
            query.transform.SetParent(world, false); query.transform.SetPositionAndRotation(mesh.transform.position, mesh.transform.rotation);
            query.transform.localScale = mesh.transform.lossyScale; query.sharedMesh = mesh.sharedMesh;
            query.gameObject.layer = 2; authoredRackQueries.Add(query);
        }
        dishCloseView = false; dishZoomBlend = 0; faucetOpen = false;
        authoredBasinBounds = Scene2DishwashingLayout.VisualBounds(dishLayout.basin);
        var basinWater = FindDishAttachment(dishLayout.basin, "WaterSurface");
        washingBasinWaterY = basinWater ? basinWater.position.y : authoredBasinBounds.center.y;
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
        washingToolRestScale = tool.localScale;
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
        var fixtureBounds = Scene2DishwashingLayout.VisualBounds(dishLayout.fixture);
        authoredCameraFocus = new Vector3(Mathf.Lerp(fixtureBounds.center.x, area.center.x, .35f),
            authoredBasinBounds.center.y + .06f, Mathf.Lerp(fixtureBounds.center.z, authoredBasinBounds.center.z, .35f));
        // A seated working pose, in world metres. Do not zoom out to fit every
        // decorative tray corner: that is what raised the old camera above the player.
        // Faucet, soap, basin and rack remain reachable; F leans in over a dish's rim.
        Vector3 offset = new Vector3(0, .82f, -1.65f);
        Vector3 direction = offset.normalized;
        dishCameraRotation = Quaternion.LookRotation(-direction, Vector3.up);
        dishCameraPosition = authoredCameraFocus + offset;
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
        if (authoredDishIndex >= 0) HighlightWashingDish(false);
        if (washingTargetGuide) Destroy(washingTargetGuide.gameObject);
        if (washingRackPreview) Destroy(washingRackPreview.gameObject);
        if (washingSourceGuide) Destroy(washingSourceGuide.gameObject);
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
        Physics.SyncTransforms();
        for (int i = 0; i < 6; i++)
        {
            Vector3 point = dishVisual.TransformPoint(dish.points[i]);
            Vector3 normal = dishVisual.TransformDirection(dish.normals[i]).normalized;
            stains[i] = WashingSurfacePatch("Food residue " + i, point, normal, new Vector2(.052f, .042f), false);
            stains[i].SetParent(dishVisual, true);
            stainWorldSizes[i] = stains[i].localScale;
        }
        for (int i = 0; i < bubbles.Length; i++)
        {
            int patch = (i / 4) * 3 + i % 3;
            Vector3 normal = dishVisual.TransformDirection(dish.normals[patch]).normalized;
            Vector3 point = dishVisual.TransformPoint(dish.points[patch]);
            bubbles[i] = WashingSurfacePatch("Soap lather " + i, point, normal, new Vector2(.035f, .028f), true);
            bubbles[i].SetParent(dishVisual, true);
            latherSizes[i] = bubbles[i].localScale;
        }
        BuildWashingGuides();
        RefreshAuthoredDishes();
    }
    private bool DishUnderTap(Vector3 point)
    {
        Vector3 outlet = world.InverseTransformPoint(dishLayout.outlet.position);
        return faucetOpen && new Vector2(point.x - outlet.x, point.z - outlet.z).magnitude < .13f;
    }
    private bool InAuthoredBasin(Vector3 point)
    {
        Vector3 p = world.TransformPoint(point);
        return Mathf.Abs(p.x - authoredBasinBounds.center.x) < authoredBasinBounds.extents.x * .8f &&
            Mathf.Abs(p.z - authoredBasinBounds.center.z) < authoredBasinBounds.extents.z * .8f;
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
        if (washingScrubAudio && !(held == 1 && pressed)) washingScrubAudio.Stop();
        if (washingRackSettleTime > 0) { TickWashingRackSettle(dt); return; }
        if (washingFlipTime > 0)
        {
            washingFlipTime = Mathf.Max(0, washingFlipTime - Mathf.Min(dt, .05f));
            RefreshAuthoredDishes(); return;
        }
        if (washingUiPointer == null) washingUiPointer = new HouseholdActivityPointer();
        if (washingUiPointer.OverUi(screen))
        {
            if (down || pressed || up) { if (held >= 0 || washingDown) ReleasePointer(); washingIgnoreUntilUp = true; }
            return;
        }
        if (washingIgnoreUntilUp) { if (!down && !pressed) washingIgnoreUntilUp = false; return; }
        if (screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
        { pointerValid = false; if (up) ReleasePointer(); return; }
        Physics.SyncTransforms();
        Ray ray = view.ScreenPointToRay(screen);
        int pick = Pick(ray);
        if (down)
        {
            washingDown = true; washingPress = screen; washingDrag = false;
            washingPickedOnDown = held != 0;
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
                bool scrubMode = state.wetted && state.soaped && !state.Scrubbed;
                held = scrubMode && pick == 0 ? 1 : pick;
                washingCarryLatched = false;
                pointerValid = false;
            }
        }
        if (Mathf.Abs(scroll) > .01f && state.wetted && state.soaped && !state.Scrubbed)
        { FlipWashingDish(); RefreshAuthoredDishes(); return; }
        var plane = new Plane(Vector3.up, world.TransformPoint(Vector3.up * authoredPlaneHeight));
        if (!plane.Raycast(ray, out float distance)) { if (up) ReleasePointer(); RefreshAuthoredDishes(); return; }
        Vector3 point = world.InverseTransformPoint(ray.GetPoint(distance));
        point.x = Mathf.Clamp(point.x, dishWorkBounds.min.x, dishWorkBounds.max.x);
        point.z = Mathf.Clamp(point.z, dishWorkBounds.min.z, dishWorkBounds.max.z);
        if (down && held == 0 && washingPickedOnDown && moving)
        {
            // Preserve the actual grab point, not the perspective offset to the elevated carrying plane.
            washingGrabOffset = Vector3.zero;
            float nearest = 5f;
            foreach (var query in dishQueries[authoredDishIndex])
                if (query && query.Raycast(ray, out var contact, nearest))
                { nearest = contact.distance; washingGrabOffset = moving.localPosition - world.InverseTransformPoint(contact.point); }
            washingGrabOffset.y = 0;
        }
        float threshold = 10f * Mathf.Max(.75f, Mathf.Min(Screen.width / 1920f, Screen.height / 1080f));
        if (washingDown && pressed && (screen - washingPress).sqrMagnitude > threshold * threshold) washingDrag = true;
        if (held == 0 && (pressed || washingCarryLatched) && moving)
        {
            Vector3 candidate = point + washingGrabOffset;
            candidate.y = authoredPlaneHeight;
            if (!washingDown || washingDrag || !washingPickedOnDown)
            {
                // Stable water support uses real authored regions; it does not bypass visibility or occlusion.
                if (DishUnderTap(candidate) || DishUnderTap(point))
                {
                    Vector3 outlet = world.InverseTransformPoint(dishLayout.outlet.position);
                    candidate.x = outlet.x; candidate.z = outlet.z;
                }
                else if (InAuthoredBasin(candidate) || InAuthoredBasin(point))
                {
                    Vector3 basin = world.InverseTransformPoint(authoredBasinBounds.center);
                    candidate.x = basin.x; candidate.z = basin.z;
                    float halfHeight = Scene2DishwashingLayout.VisualBounds(dishVisual).extents.y;
                    float dockY = world.InverseTransformPoint(new Vector3(moving.position.x, washingBasinWaterY + halfHeight * .65f, moving.position.z)).y;
                    candidate.y = Mathf.MoveTowards(moving.localPosition.y, dockY, Mathf.Min(dt, .05f) * .9f);
                }
                moving.localPosition = candidate;
                if (DishUnderTap(candidate)) { state.Rinse(Mathf.Min(dt, .05f)); Touch(); dishWaterSurface?.Disturb(moving.position, .6f); }
                else if (InAuthoredBasin(candidate) && Scene2DishwashingLayout.VisualBounds(dishVisual).min.y <= washingBasinWaterY + .01f)
                {
                    bool alreadyWet = state.wetted;
                    state.Wet(Mathf.Min(dt, .05f)); Touch();
                    if (!alreadyWet) dishWaterSurface?.Disturb(moving.position, .45f);
                }
            }
        }
        if (held == 1 && pressed && moving)
        {
            bool scrubSound = false;
            bool contact = false;
            RaycastHit hit = default;
            float nearest = 5f;
            foreach (var query in dishQueries[authoredDishIndex])
                if (query && query.Raycast(ray, out var candidate, nearest)) { nearest = candidate.distance; hit = candidate; contact = true; }
            if (contact && !AuthoredDishOccluded(ray, hit.distance))
            {
                // Only the working copy is reduced at contact, so the hand/tool does not hide the entire stain.
                tool.localScale = washingToolRestScale * .68f;
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
                if (travel > .0002f && travel < .07f && state.Scrub(patch, Mathf.Min(travel, Mathf.Min(dt, .05f) * .24f)))
                {
                    Touch();
                    scrubSound = true;
                    if (washingScrubAudio && !washingScrubAudio.isPlaying) washingScrubAudio.Play();
                }
                else if (!state.wetted || !state.soaped) Tell("Làm ướt, rồi bấm bánh xà phòng trước khi chà.");
            }
            else pointerValid = false;
            if (!scrubSound && washingScrubAudio) washingScrubAudio.Stop();
        }
        washingRackReady = held == 0 && CanRackAuthoredDish();
        bool placeCarry = up && held == 0 && washingDown && (washingDrag || !washingPickedOnDown);
        if (placeCarry && moving && InAuthoredRack(moving.localPosition))
        {
            if (washingRackReady)
            {
                washingRackSettleTime = .2f; washingRackFrom = moving.localPosition; washingRackFromRotation = moving.localRotation;
                washingDown = washingCarryLatched = false;
                RefreshAuthoredDishes(); return;
            }
            else Tell(state.DishReady ? "Vị trí cất bị chắn — đưa món sang phần nhìn thấy của giá." : state.Scrubbed ? "Tráng dưới vòi đang mở đến khi hết bọt trước nhé." : "Còn vết bẩn — nhớ chà cả hai mặt.");
        }
        if (up)
        {
            if (held == 0 && washingDown && washingPickedOnDown && !washingDrag) washingCarryLatched = true;
            else ReleasePointer();
            washingDown = false;
        }
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
            if (authoredRackQueries != null && authoredRackQueries.Count > 0 && (hit.transform == dishLayout.rack || hit.transform.IsChildOf(dishLayout.rack)))
            {
                foreach (var query in authoredRackQueries)
                    if (query && query.Raycast(ray, out var exact, distance) && exact.distance < distance - .003f) return true;
                continue;
            }
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
        if (moving && washingRackSettleTime <= 0) moving.localRotation = washingFlipTime > 0 ? Quaternion.Slerp(washingFlipFrom,
            Quaternion.Euler(state.flipped ? 180 : 0, 0, 0), Mathf.SmoothStep(0, 1, 1 - washingFlipTime / .28f)) : Quaternion.Euler(state.flipped ? 180 : 0, 0, 0);
        for (int i = 0; i < 6; i++) if (stains[i])
        {
            stains[i].gameObject.SetActive(!state.Complete && i / 3 == (state.flipped ? 1 : 0) && state.stains[i] > .02f);
            stains[i].localScale = stainWorldSizes[i] * Mathf.Lerp(.82f, 1, state.stains[i]);
            WashingSurfaceFade(stains[i], false, Mathf.Clamp01(state.stains[i]));
        }
        for (int i = 0; i < bubbles.Length; i++) if (bubbles[i])
        {
            bubbles[i].gameObject.SetActive(!state.Complete && state.soaped && state.rinse < 1 && i / 4 == (state.flipped ? 1 : 0));
            bubbles[i].localScale = latherSizes[i] * Mathf.Lerp(.9f, 1, 1 - state.rinse);
            WashingSurfaceFade(bubbles[i], true, 1 - state.rinse);
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
        RefreshWashingGuides();
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
        int step = !state.wetted ? 0 : !state.soaped ? 1 : !state.Scrubbed ? 2 : state.rinse < 1 ? 3 : 4;
        float faceClean = 0; int offset = state.flipped ? 3 : 0;
        for (int i = offset; i < offset + 3; i++) faceClean += 1 - state.stains[i];
        string stage = step == 0 ? "Bấm cầm " + item.ToLowerInvariant() + ", đưa vào vòng sáng trong thau để làm ướt." :
            step == 1 ? "Bấm bánh xà phòng để chuẩn bị chà." :
            step == 2 ? (faceClean >= 2.94f ? "Mặt này sạch rồi — bấm Lật món để chà mặt còn lại." :
                "Giữ chuột trên vết bẩn để chà · " + (state.flipped ? "Mặt ngoài" : "Mặt trong") + " sạch " + Mathf.RoundToInt(faceClean / 3 * 100) + "%") :
            step == 3 ? (faucetOpen ? "Cầm món đưa dưới dòng nước · Đã tráng " + Mathf.RoundToInt(state.rinse * 100) + "%" : "Bấm vòi để mở nước, rồi đưa món vào vòng sáng.") :
            washingRackReady ? "Đặt được — bấm lần nữa hoặc thả chuột để cất lên giá." : "Cầm " + item.ToLowerInvariant() + " đưa đến hình bóng trên giá rồi đặt.";
        return GameUiPresentation.ActivitySteps(step, "Làm ướt", "Xà phòng", "Chà hai mặt", "Tráng", "Cất lên giá") + "\n" + stage;
    }
}
