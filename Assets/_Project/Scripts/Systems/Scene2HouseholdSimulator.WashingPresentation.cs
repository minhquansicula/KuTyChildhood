using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Scene2HouseholdSimulator
{
    private bool washingHelpOpen, washingLargeText;
    private Button washingHelpButton, washingTextButton;
    private float washingRackSettleTime;
    private Vector3 washingRackFrom;
    private Quaternion washingRackFromRotation;
    private Material washingDirtMaterial, washingFoamMaterial;
    private MaterialPropertyBlock washingSurfaceBlock;
    private AudioSource washingScrubAudio, washingContactAudio;
    private AudioClip washingScrubClip, washingContactClip;
    private Vector3 washingToolRestScale;
    private float washingBasinWaterY;
    private float AuthoredWashingOverviewFov => view.aspect < 1.5f ? 76f : 64f;
#if UNITY_EDITOR
    [System.NonSerialized] public bool EditorWashingPointerPlayback;
#endif

    private void ConfigureWashingPresentation()
    {
        if (!authoredDishes) return; // Opt-in only: no changes to SetTable or other activities.
        washingHelpOpen = false;
        var header = (RectTransform)title.transform.parent;
        header.anchorMin = header.anchorMax = header.pivot = new Vector2(0, 1);
        header.anchoredPosition = new Vector2(24, -24); header.sizeDelta = new Vector2(480, 62);
        var action = (RectTransform)instructions.transform.parent;
        action.anchorMin = action.anchorMax = action.pivot = Vector2.zero;
        action.anchoredPosition = new Vector2(24, 88);
        var mode = (RectTransform)hint.transform.parent;
        mode.anchorMin = mode.anchorMax = mode.pivot = new Vector2(0, 1);
        mode.anchoredPosition = new Vector2(24, -94); mode.sizeDelta = new Vector2(430, 52);
        foreach (var text in new[] { title, instructions, hint })
        {
            text.transform.parent.gameObject.SetActive(true); text.alignment = TextAlignmentOptions.MidlineLeft;
            GameUiPresentation.Card(text.transform.parent.GetComponent<Image>());
            text.transform.parent.GetComponent<Image>().raycastTarget = true;
        }
        var row = (RectTransform)washingActionRow.transform;
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(1, 0); row.anchoredPosition = new Vector2(-24, 14);
        if (!washingHelpButton) washingHelpButton = WashingButton("?", 0, () => washingHelpOpen = !washingHelpOpen);
        if (!washingTextButton) washingTextButton = WashingButton("Aa", 0, () => washingLargeText = !washingLargeText);
        foreach (var button in new[] { washingFlipButton, washingCloseButton, washingResetButton, washingLeaveButton, washingHelpButton, washingTextButton })
        {
            var image = button.GetComponent<Image>(); GameUiPresentation.Card(image); image.raycastTarget = true;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(); label.color = GameUiPresentation.Paper;
        }
        BuildWashingAudio();
    }

    private void UpdateWashingPresentation()
    {
        if (!authoredDishes) return;
        RefreshWashingControls();
        float scale = washingLargeText ? 1.22f : 1;
        title.fontSize = 30 * scale; instructions.fontSize = 27 * scale; hint.fontSize = 22 * scale;
        title.text = "Rửa chén  ·  " + Mathf.Min(3, state.delivered + 1) + "/3";
        string item = dishLayout.dishes[Mathf.Min(2, state.delivered)].label.ToLowerInvariant();
        string action = washingRackSettleTime > 0 ? "Đặt nhẹ lên giá…" : washingFlipTime > 0 ? "Đang lật món…" : !state.wetted ?
            held == 0 ? "Đưa món vào thau nước." : "Cầm " + item + "." : !state.soaped ? "Bấm bánh xà phòng." : !state.Scrubbed ?
            FaceClean() ? "Lật món để chà mặt còn lại." : "Giữ chuột, chà qua vết bẩn." : !state.DishReady ?
            !faucetOpen ? "Mở vòi nước." : held == 0 ? "Đưa món dưới dòng nước." : "Cầm món để tráng." :
            washingRackReady ? "Đặt được — bấm hoặc thả chuột." : held == 0 ? "Đưa món tới hình bóng trên giá." : "Cầm món sạch để cất lên giá.";
        if (state.Complete) action = "Đã rửa xong.";
        if (idleTime > 8 || washingHelpOpen) action += "\n" + (!state.wetted ? "Bấm để cầm; cũng có thể giữ và kéo." : !state.soaped ? "Xà phòng ở cạnh miếng rửa." : !state.Scrubbed ? "Chà đúng vết bẩn; Lật món để đổi mặt, F để nhìn gần." : !state.DishReady ? "Đưa vào vùng nước rồi chờ hết bọt." : "Đưa lên giá rồi bấm lần nữa hoặc thả chuột.");
        if (washingHelpOpen) action = AuthoredDishCaption() + "\nBấm để cầm · Chuột phải đặt lại · Esc rời đi";
        instructions.text = action;
        float width = Mathf.Clamp(instructions.GetPreferredValues(action).x + 36, 360, washingHelpOpen ? 1180 : 820);
        ((RectTransform)instructions.transform.parent).sizeDelta = new Vector2(width, instructions.GetPreferredValues(action, width - 36, 0).y + 22);
        hint.text = messageTime > 0 && held < 0 ? message : held == 0 ? "Đang cầm món" : state.wetted && state.soaped && !state.Scrubbed ?
            "Miếng rửa  ·  " + (state.flipped ? "Mặt ngoài" : "Mặt trong") : "";
        hint.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(hint.text));
        ((RectTransform)hint.transform.parent).sizeDelta = new Vector2(Mathf.Clamp(hint.GetPreferredValues(hint.text).x + 36, 280, 730), hint.GetPreferredValues(hint.text, 694, 0).y + 22);
        LayoutWashingActions();
    }

    private bool FaceClean()
    {
        int side = state.flipped ? 3 : 0;
        return state.stains[side] <= .02f && state.stains[side + 1] <= .02f && state.stains[side + 2] <= .02f;
    }

    private void LayoutWashingActions()
    {
        var buttons = new[] { washingFlipButton, washingCloseButton, washingResetButton, washingHelpButton, washingTextButton, washingLeaveButton };
        washingFlipButton.gameObject.SetActive(washingFlipButton.interactable);
        washingCloseButton.gameObject.SetActive(washingCloseButton.interactable);
        washingResetButton.gameObject.SetActive(held == 0 || washingFlipTime > 0 || washingRackSettleTime > 0);
        float width = 0;
        foreach (var button in buttons) if (button && button.gameObject.activeSelf) width += 198;
        ((RectTransform)washingActionRow.transform).sizeDelta = new Vector2(width, 54);
        float x = -width / 2 + 93;
        foreach (var button in buttons)
        {
            if (!button || !button.gameObject.activeSelf) continue;
            var rect = (RectTransform)button.transform; rect.sizeDelta = new Vector2(186, 54); rect.anchoredPosition = new Vector2(x, 0);
            button.GetComponentInChildren<TextMeshProUGUI>().fontSize = washingLargeText ? 28 : 24;
            x += 198;
        }
    }

    private void TickWashingRackSettle(float dt)
    {
        washingRackSettleTime = Mathf.Max(0, washingRackSettleTime - Mathf.Min(dt, .05f));
        float t = Mathf.SmoothStep(0, 1, 1 - washingRackSettleTime / .2f);
        moving.localPosition = Vector3.Lerp(washingRackFrom, slots[authoredDishIndex], t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .035f);
        moving.localRotation = Quaternion.Slerp(washingRackFromRotation, authoredRackRotations[authoredDishIndex], t);
        if (washingRackSettleTime <= 0)
        {
            int index = state.delivered;
            if (state.RackDish())
            {
                authoredHolders[index].localPosition = slots[index]; authoredHolders[index].localRotation = authoredRackRotations[index];
                pickers[0].enabled = false; washingContactAudio?.PlayOneShot(washingContactClip); Touch();
                held = -1; pointerValid = false; ResetWashingPointer(); SelectAuthoredDish();
                Tell(moving ? "Cầm món tiếp theo khi sẵn sàng." : "Đã rửa xong.");
                if (!moving) StopAuthoredTap();
            }
        }
        RefreshAuthoredDishes();
    }

    private Transform WashingSurfacePatch(string name, Vector3 point, Vector3 normal, Vector2 size, bool foam)
    {
        EnsureWashingSurfaceMaterials();
        var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
        root.SetParent(world, false); root.SetPositionAndRotation(point + normal * .001f, Quaternion.FromToRotation(Vector3.up, normal));
        int variant = name[name.Length - 1] - '0';
        root.rotation = Quaternion.AngleAxis(variant * 47, normal) * root.rotation;
        if (!foam) size = new Vector2(size.x * (1.12f + variant % 3 * .08f), size.y * .72f);
        const int grid = 5; var vertices = new Vector3[grid * grid]; var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length]; var colors = new Color[vertices.Length]; var triangles = new int[(grid - 1) * (grid - 1) * 6]; int tri = 0;
        for (int z = 0; z < grid; z++) for (int x = 0; x < grid; x++)
        {
            int i = z * grid + x; uv[i] = new Vector2(x / (grid - 1f), z / (grid - 1f));
            Vector3 projected = root.TransformPoint(new Vector3((uv[i].x - .5f) * size.x, 0, (uv[i].y - .5f) * size.y));
            bool found = false; RaycastHit surface = default; float nearest = .09f;
            foreach (var query in dishQueries[authoredDishIndex])
                if (query && query.Raycast(new Ray(projected + normal * .04f, -normal), out var hit, nearest) && Vector3.Dot(hit.normal, normal) > .35f)
                { nearest = hit.distance; surface = hit; found = true; }
            vertices[i] = root.InverseTransformPoint(found ? surface.point + surface.normal * (foam ? .0017f : .0011f) : projected);
            normals[i] = Vector3.up; colors[i] = new Color(1, 1, 1, found ? 1 : 0);
            if (x < grid - 1 && z < grid - 1)
            {
                triangles[tri++] = i; triangles[tri++] = i + grid; triangles[tri++] = i + 1;
                triangles[tri++] = i + 1; triangles[tri++] = i + grid; triangles[tri++] = i + grid + 1;
            }
        }
        var mesh = new Mesh { name = name + " projected onto ceramic" }; mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles; mesh.RecalculateBounds();
        ownedAssets.Add(mesh); root.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.GetComponent<MeshRenderer>(); renderer.sharedMaterial = foam ? washingFoamMaterial : washingDirtMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        return root;
    }

    private void EnsureWashingSurfaceMaterials()
    {
        if (washingDirtMaterial) return;
        foreach (bool foam in new[] { false, true })
        {
            var texture = new Texture2D(96, 96, TextureFormat.RGBA32, false) { name = foam ? "Soft soap film" : "Irregular food smear", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[96 * 96];
            for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++)
            {
                float u = x / 95f, v = y / 95f;
                float noise = Mathf.PerlinNoise(u * 5.2f + 3.7f, v * 4.1f + 8.2f);
                float r = new Vector2((u - .5f) * 2, (v - .5f) * 2).magnitude;
                float alpha = Mathf.Clamp01((1 - r + (noise - .5f) * .65f) * 4) * (foam ? .25f + noise * .45f : .45f + noise * .55f);
                pixels[y * 96 + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(false, true); ownedAssets.Add(texture);
            var material = new Material(Shader.Find("Sprites/Default")); material.mainTexture = texture; ownedAssets.Add(material);
            material.color = foam ? new Color(.90f, .94f, .88f, .35f) : new Color(.25f, .14f, .055f, .82f);
            if (foam) washingFoamMaterial = material; else washingDirtMaterial = material;
        }
    }

    private void WashingSurfaceFade(Transform patch, bool foam, float amount)
    {
        if (washingSurfaceBlock == null) washingSurfaceBlock = new MaterialPropertyBlock();
        Color color = foam ? washingFoamMaterial.color : washingDirtMaterial.color; color.a *= amount;
        washingSurfaceBlock.Clear(); washingSurfaceBlock.SetColor("_Color", color); patch.GetComponent<Renderer>().SetPropertyBlock(washingSurfaceBlock);
    }

    private void BuildWashingAudio()
    {
        if (!washingScrubClip)
        {
            const int rate = 24000; var scrub = new float[rate]; var contact = new float[3600]; uint seed = 17;
            float filtered = 0;
            for (int i = 0; i < scrub.Length; i++) { seed = seed * 1664525 + 1013904223; filtered = Mathf.Lerp(filtered, (seed / (float)uint.MaxValue) * 2 - 1, .18f); scrub[i] = filtered * .28f; }
            for (int i = 0; i < contact.Length; i++) { float t = i / (float)rate; contact[i] = .22f * Mathf.Exp(-t * 45) * (Mathf.Sin(t * 5800) + .3f * Mathf.Sin(t * 12300)); }
            washingScrubClip = AudioClip.Create("Synthesized wet scrub placeholder", rate, 1, rate, false); washingScrubClip.SetData(scrub, 0); ownedAssets.Add(washingScrubClip);
            washingContactClip = AudioClip.Create("Synthesized rack contact placeholder", contact.Length, 1, rate, false); washingContactClip.SetData(contact, 0); ownedAssets.Add(washingContactClip);
        }
        washingScrubAudio = new GameObject("Contact-driven scrub audio").AddComponent<AudioSource>(); washingScrubAudio.transform.SetParent(world, false);
        washingScrubAudio.playOnAwake = false; washingScrubAudio.loop = true; washingScrubAudio.clip = washingScrubClip; washingScrubAudio.volume = .12f;
        washingContactAudio = new GameObject("Quiet rack contact").AddComponent<AudioSource>(); washingContactAudio.transform.SetParent(world, false);
        washingContactAudio.playOnAwake = false; washingContactAudio.volume = .24f;
        KuTyAudioMix.Route(washingScrubAudio, KuTyAudioBus.SFX); KuTyAudioMix.Route(washingContactAudio, KuTyAudioBus.SFX);
    }
}
