using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Scene2HouseholdSimulator
{
    private bool tableHelpOpen, tableLargeText;
    private float tableCompletionTime, tableLiftTime, tableLiftFrom, tableHoverTime;
    private int tableRawHover = -1;
    private Vector2 tablePointerScreen;
    private GameObject tableActions;
    private Button tableHelpButton, tableTextButton;
    private Renderer tableToneRenderer;
    private Material[] tableSourceMaterials, tableToneMaterials;
    private System.Collections.Generic.Dictionary<Texture, Texture2D> tableQuietTextures;
    private AudioSource tableContactSource;
    private AudioClip tableContactClip;
    private bool TableActivity => state != null && state.chore == HouseholdChore.SetTable;
    private Vector3 TableCameraDestination => world.TransformPoint(new Vector3(0, 1.065f, -.83f));
    private Quaternion TableCameraRotation => Quaternion.LookRotation(world.TransformPoint(new Vector3(0, .015f, -.13f)) - TableCameraDestination, world.up);
    private float TableCameraFov => view.aspect < 1.6f ? 52f : 48f;
#if UNITY_EDITOR
    // Editor evidence harness supplies the SAME ProcessPointer edges. Not compiled into players.
    // Camera, UI, pause/focus callbacks, save, validation and rewards still run normally.
    [System.NonSerialized] public bool EditorTablePointerPlayback;
#endif

    private void ConfigureTablePresentation()
    {
        tableHelpOpen = false; tableCompletionTime = 0;
        foreach (var label in new[] { title, instructions, hint })
        {
            label.transform.parent.gameObject.SetActive(true);
            label.alignment = TextAlignmentOptions.Center;
            GameUiPresentation.Card(label.transform.parent.GetComponent<Image>());
        }
        var header = (RectTransform)title.transform.parent;
        header.anchorMin = header.anchorMax = header.pivot = new Vector2(.5f, 1);
        header.anchoredPosition = new Vector2(0, -24); header.sizeDelta = new Vector2(760, 90);
        foreach (var label in new[] { instructions, hint })
        {
            var rect = (RectTransform)label.transform.parent;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
        }
        GameUiPresentation.Text(title, 26, GameUiPresentation.Paper);
        GameUiPresentation.Text(instructions, 28, GameUiPresentation.Paper);
        GameUiPresentation.Text(hint, 22, GameUiPresentation.Paper);
        if (tableActions) tableActions.SetActive(TableActivity);
        if (!TableActivity) return;
        captions.blocksRaycasts = captions.interactable = true;
        if (!captions.GetComponent<GraphicRaycaster>()) captions.gameObject.AddComponent<GraphicRaycaster>();
        if (!tableActions)
        {
            tableActions = new GameObject("Table quiet controls", typeof(RectTransform));
            tableActions.transform.SetParent(header.parent, false);
            var row = (RectTransform)tableActions.transform;
            row.anchorMin = row.anchorMax = row.pivot = Vector2.one;
            row.anchoredPosition = new Vector2(-28, -24); row.sizeDelta = new Vector2(210, 54);
            tableHelpButton = TableButton("?", -158, () => tableHelpOpen = !tableHelpOpen);
            tableTextButton = TableButton("Aa", -62, () => tableLargeText = !tableLargeText);
        }
        header.anchorMin = header.anchorMax = header.pivot = new Vector2(0, 1);
        header.anchoredPosition = new Vector2(28, -24); header.sizeDelta = new Vector2(430, 62);
        var footer = (RectTransform)hint.transform.parent;
        footer.anchorMin = footer.anchorMax = footer.pivot = Vector2.zero;
        footer.anchoredPosition = new Vector2(28, 22); footer.sizeDelta = new Vector2(860, 52);
        header.GetComponent<Image>().raycastTarget = true;
        footer.GetComponent<Image>().raycastTarget = true;
        foreach (var label in new[] { title, instructions, hint }) label.alignment = TextAlignmentOptions.MidlineLeft;
        SetTableArtTone(true);
        BuildTableContactAudio();
    }

    private Button TableButton(string caption, float x, UnityEngine.Events.UnityAction click)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(tableActions.transform, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.anchoredPosition = new Vector2(x, -27); rect.sizeDelta = new Vector2(84, 54);
        GameUiPresentation.Card(go.GetComponent<Image>()); go.GetComponent<Image>().raycastTarget = true;
        var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(click);
        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(go.transform, false); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        GameUiPresentation.Text(label, 26, GameUiPresentation.Paper); label.alignment = TextAlignmentOptions.Center; label.text = caption;
        return button;
    }

    private void UpdateTableCaption(int placed)
    {
        float scale = tableLargeText ? 1.22f : 1;
        title.fontSize = 30 * scale; instructions.fontSize = 26 * scale; hint.fontSize = 21 * scale;
        title.text = state.Complete ? "Bàn đã gọn." : "Dọn mâm  ·  " + placed + "/3";
        hint.text = "Chuột phải · Đặt lại     Esc · Rời đi     ? · Hướng dẫn";
        ((RectTransform)title.transform.parent).sizeDelta = new Vector2(Mathf.Clamp(title.GetPreferredValues(title.text).x + 36, 250, 430), 62);
        ((RectTransform)hint.transform.parent).sizeDelta = new Vector2(Mathf.Min(940, hint.GetPreferredValues(hint.text).x + 36), 52);
        string action = state.Complete ? "" : tableSettleTime > 0 ? "Đặt nhẹ xuống bàn…" : held >= 0 ?
            tableDropReady ? (tableDragging ? "Thả để đặt " : "Bấm để đặt ") + TablePieceName(held).ToLowerInvariant() : "Đưa " + TablePieceName(held).ToLowerInvariant() + " tới hình bóng." :
            tableHover >= 0 && tableHover < 3 && (state.placedMask & (1 << tableHover)) == 0 ? "Cầm " + TablePieceName(tableHover).ToLowerInvariant() :
            placed == 0 ? "Bấm một món để cầm." : idleTime > 9 ? "Cầm món còn lại để dọn tiếp." : "";
        if (messageTime > 0 && !state.Complete && held < 0 && tableHover < 0) action = message;
        if (held >= 0 && !tableDropReady && tableValidation.reason != null && tableValidation.reason.Contains("chắn")) action = "Đưa vật vòng qua chỗ bị chắn.";
        if (tableHelpOpen) action = "Bấm để cầm · Bấm lần nữa để đặt\nHoặc giữ, kéo đến hình bóng rồi thả\nChuột phải đặt lại · Aa đổi cỡ chữ";
        instructions.text = action;
        var card = (RectTransform)instructions.transform.parent;
        card.gameObject.SetActive(!string.IsNullOrEmpty(action));
        card.GetComponent<Image>().raycastTarget = tableHelpOpen;
        var safe = (RectTransform)card.parent;
        card.anchorMin = card.anchorMax = new Vector2(.5f, .5f); card.pivot = new Vector2(0, 1);
        card.sizeDelta = new Vector2(tableHelpOpen ? 800 : 510, tableHelpOpen ? 158 : 86);
        if (!tableHelpOpen)
        {
            float width = Mathf.Clamp(instructions.GetPreferredValues(action).x + 36, 200, 510);
            card.sizeDelta = new Vector2(width, Mathf.Clamp(instructions.GetPreferredValues(action, width - 36, 0).y + 20, 64, 160));
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, tablePointerScreen, null, out var cursor);
        Vector2 pos = tableHelpOpen ? new Vector2(-card.sizeDelta.x / 2, safe.rect.yMin + 250) : cursor + new Vector2(22, -20);
        int active = held >= 0 ? held : tableHover;
        if (!tableHelpOpen && active >= 0 && active < 3 && pieces[active])
        {
            // Keep the action close to the object, but never paint a tooltip across its visible face.
            Bounds bounds = Scene2DishwashingLayout.VisualBounds(pieces[active]);
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (float x in TableBoundsSigns) foreach (float y in TableBoundsSigns) foreach (float z in TableBoundsSigns)
            {
                Vector3 screen = view.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, screen, null, out var local);
                minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
            }
            float above = maxY + 18 + card.sizeDelta.y;
            pos.y = above <= safe.rect.yMax - 104 ? above : minY - 18;
        }
        pos.x = Mathf.Clamp(pos.x, safe.rect.xMin + 24, safe.rect.xMax - card.sizeDelta.x - 24);
        pos.y = Mathf.Clamp(pos.y, safe.rect.yMin + 92 + card.sizeDelta.y, safe.rect.yMax - 104);
        card.anchoredPosition = pos;
        hint.transform.parent.GetComponent<Image>().color = new Color(.055f, .105f, .095f, .78f);
    }
    private static readonly float[] TableBoundsSigns = { -1, 1 };

    private void SetTableArtTone(bool enabled)
    {
        if (enabled && !tableToneRenderer)
        {
            var layout = GetComponent<Scene2AuthoredTableLayout>();
            if (!layout || !layout.IsReady) return;
            tableToneRenderer = layout.table; tableSourceMaterials = tableToneRenderer.sharedMaterials;
            tableToneMaterials = new Material[tableSourceMaterials.Length];
            for (int i = 0; i < tableSourceMaterials.Length; i++)
            {
                var source = tableSourceMaterials[i];
                if (!source) continue;
                var copy = new Material(source) { name = source.name + " (table activity trial)" }; ownedAssets.Add(copy);
                string color = copy.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                if (copy.HasProperty(color)) copy.SetColor(color, copy.GetColor(color) * new Color(.80f, .85f, .88f, 1));
                if (copy.HasProperty("_BaseMap") && copy.GetTexture("_BaseMap")) copy.SetTexture("_BaseMap", QuietTableTexture(copy.GetTexture("_BaseMap")));
                if (copy.HasProperty("_BumpScale")) copy.SetFloat("_BumpScale", .25f);
                copy.DisableKeyword("_METALLICSPECGLOSSMAP");
                if (copy.HasProperty("_Metallic")) copy.SetFloat("_Metallic", 0);
                if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", .12f);
                if (copy.HasProperty("_Glossiness")) copy.SetFloat("_Glossiness", .12f);
                if (copy.HasProperty("_SpecColor")) copy.SetColor("_SpecColor", new Color(.12f, .12f, .12f));
                tableToneMaterials[i] = copy;
            }
        }
        if (tableToneRenderer) tableToneRenderer.sharedMaterials = enabled ? tableToneMaterials : tableSourceMaterials;
    }

    private Texture2D QuietTableTexture(Texture source)
    {
        if (tableQuietTextures == null) tableQuietTextures = new System.Collections.Generic.Dictionary<Texture, Texture2D>();
        if (tableQuietTextures.TryGetValue(source, out var cached)) return cached;
        // GPU copy handles non-readable imports. Only this transient copy is edited, never the asset.
        var target = RenderTexture.GetTemporary(512, 512, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var copy = new Texture2D(512, 512, TextureFormat.RGBA32, false) { name = "Table activity low-contrast texture trial", wrapMode = source.wrapMode };
        try
        {
            Graphics.Blit(source, target); RenderTexture.active = target;
            copy.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            var pixels = copy.GetPixels(); Color mean = Color.clear;
            foreach (var pixel in pixels) mean += pixel;
            mean /= pixels.Length;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color softened = Color.Lerp(mean, pixels[i], .55f);
                Color grey = new Color(softened.grayscale, softened.grayscale, softened.grayscale, softened.a);
                pixels[i] = Color.Lerp(grey, softened, .78f);
            }
            copy.SetPixels(pixels); copy.Apply(false, true);
            ownedAssets.Add(copy); tableQuietTextures[source] = copy; return copy;
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
    }

    private void RestoreTableArtTone()
    {
        if (!tableToneRenderer) return;
        var current = tableToneRenderer.sharedMaterials;
        for (int i = 0; i < current.Length && i < tableToneMaterials.Length; i++)
            if (current[i] == tableToneMaterials[i]) current[i] = tableSourceMaterials[i];
        tableToneRenderer.sharedMaterials = current;
        tableToneRenderer = null; tableSourceMaterials = tableToneMaterials = null;
    }

    private void BuildTableContactAudio()
    {
        // Honest placeholder: a quiet synthesized contact, NOT recorded ceramic/wood foley.
        if (!tableContactClip)
        {
            const int rate = 24000; var samples = new float[4800];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = .24f * Mathf.Exp(-t * 42) * (Mathf.Sin(t * 6100) + .32f * Mathf.Sin(t * 13900));
            }
            tableContactClip = AudioClip.Create("Synthesized ceramic contact (placeholder)", samples.Length, 1, rate, false);
            tableContactClip.SetData(samples, 0); ownedAssets.Add(tableContactClip);
        }
        tableContactSource = new GameObject("Quiet dish contact").AddComponent<AudioSource>();
        tableContactSource.transform.SetParent(world, false); tableContactSource.playOnAwake = false;
        tableContactSource.volume = .24f; tableContactSource.spatialBlend = 0;
        KuTyAudioMix.Route(tableContactSource, KuTyAudioBus.SFX);
    }
}
