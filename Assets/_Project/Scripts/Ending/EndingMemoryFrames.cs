using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The "memory pieces" beat of the ending: one glowing keepsake card per chore the player actually finished
/// rises out of the rice, chimes as it settles into an arch above the mother, then streams into the house.
/// </summary>
public sealed class EndingMemoryFrames : MonoBehaviour
{
    private struct Keepsake
    {
        public HouseholdChore chore;
        public string sprite;
        public Keepsake(HouseholdChore chore, string sprite) { this.chore = chore; this.sprite = sprite; }
    }

    private static readonly Keepsake[] Keepsakes =
    {
        new Keepsake(HouseholdChore.ArrangeKeepsakes, "Keepsake_Box"),
        new Keepsake(HouseholdChore.TuneRadio, "Radio_Body"),
        new Keepsake(HouseholdChore.CarryWood, "Wood_Bundle"),
        new Keepsake(HouseholdChore.FetchWater, "Water_Jar"),
        new Keepsake(HouseholdChore.MeasureRice, "Rice_Jar"),
        new Keepsake(HouseholdChore.WashRice, "Rice_Washing_Pot"),
        new Keepsake(HouseholdChore.LightFire, "Fire_Hearth"),
        new Keepsake(HouseholdChore.CookRice, "Cook_Pot"),
        new Keepsake(HouseholdChore.SetTable, "Meal_Tray_Empty"),
        new Keepsake(HouseholdChore.WashDishes, "Clean_Dish"),
        new Keepsake(HouseholdChore.PutAwayDishes, "Dish_Cabinet"),
        new Keepsake(HouseholdChore.SortTools, "Tool_Hoe"),
        new Keepsake(HouseholdChore.GrindCorn, "Corn_Mill_Base"),
        new Keepsake(HouseholdChore.ReadLetter, "Letter_Envelope"),
    };

    private sealed class Frame
    {
        public Transform root, card;
        public SpriteRenderer halo, border, panel, picture;
        public Vector3 start, end;
        public float delay, phase, arrivedAt = -1f, arc;
        public int index;
    }

    private readonly List<Frame> frames = new List<Frame>();
    private Texture2D circleTex;
    private Sprite whiteSprite;
    private Material haloMaterial;
    private bool built;

    public int Count => frames.Count;

    public static IEnumerable<string> EarnedSpriteNames()
    {
        foreach (var k in Keepsakes)
            if (EndingSummary.IsCompleted(k.chore)) yield return k.sprite;
    }

    private Sprite CreateSoftCircleSprite()
    {
        const int size = 64;
        circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "EndingSoftCircle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = (y + .5f) / size * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        circleTex.SetPixels(pixels);
        circleTex.Apply(false, false);
        return Sprite.Create(circleTex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
    }

    /// <summary>Spawns the frames (hidden) around the field in front of the camera.</summary>
    public void Build(Vector3 cameraPosition, Vector3 cameraForward, Vector3 archCenter, float archRadius)
    {
        if (built) return;
        built = true;
        whiteSprite = CreateSoftCircleSprite();
        var shader = Resources.Load<Shader>("Effects/MemoryParticle");
        if (shader != null)
        {
            haloMaterial = new Material(shader) { name = "Ending halo", hideFlags = HideFlags.DontSave };
            haloMaterial.SetTexture("_BaseMap", circleTex);
            haloMaterial.SetFloat("_Brightness", 1.25f);
        }

        Vector3 flat = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
        if (flat.sqrMagnitude < .01f) flat = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, flat).normalized;

        var list = new List<Keepsake>();
        foreach (var k in Keepsakes) if (EndingSummary.IsCompleted(k.chore)) list.Add(k);
        int n = list.Count;
        var rng = new System.Random(2024);
        for (int i = 0; i < n; i++)
        {
            var sprite = Resources.Load<Sprite>("Scene2/MiniGames/" + list[i].sprite);
            var frame = new Frame { index = i };
            frame.root = new GameObject("Memory " + list[i].sprite).transform;
            frame.root.SetParent(transform, false);

            frame.halo = Layer(frame.root, "Halo", whiteSprite, new Color(1f, .84f, .48f, 0f), haloMaterial, .18f, .18f, 0);
            frame.border = Layer(frame.root, "Border", whiteSprite, new Color(1f, .88f, .58f, .22f), null, .11f, .11f, 1);
            frame.panel = Layer(frame.root, "Panel", whiteSprite, new Color(1f, .96f, .86f, .28f), null, .085f, .085f, 2);
            if (sprite != null)
            {
                frame.picture = Layer(frame.root, "Picture", sprite, new Color(1f, .98f, .92f, .92f), null, 0f, 0f, 3);
                float maxSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                float fit = .072f / Mathf.Max(.001f, maxSide);
                frame.picture.transform.localScale = Vector3.one * fit;
                frame.picture.transform.localPosition = Vector3.zero;
            }

            // Arch above the mother: the first keepsake sits at the left, the last at the right.
            float k = n <= 1 ? .5f : i / (float)(n - 1);
            float angle = Mathf.Lerp(Mathf.PI * .94f, Mathf.PI * .06f, k);
            frame.end = archCenter + right * (Mathf.Cos(angle) * archRadius) + Vector3.up * (Mathf.Sin(angle) * archRadius * .62f);
            frame.start = archCenter - flat * Mathf.Lerp(.18f, .42f, (float)rng.NextDouble()) +
                          right * Mathf.Lerp(-.58f, .58f, (float)rng.NextDouble()) +
                          Vector3.down * Mathf.Lerp(.42f, .68f, (float)rng.NextDouble());
            frame.delay = i * .11f;
            frame.phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            frame.arc = Mathf.Lerp(.16f, .32f, (float)rng.NextDouble());
            frame.root.position = frame.start;
            frame.root.localScale = Vector3.zero;
            frames.Add(frame);
        }
    }

    private SpriteRenderer Layer(Transform parent, string title, Sprite sprite, Color color, Material material,
        float width, float height, int order)
    {
        var go = new GameObject(title);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, order * -.002f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 100 + order;
        if (material != null) renderer.sharedMaterial = material;
        if (width > 0f) go.transform.localScale = new Vector3(width, height, 1f);
        return renderer;
    }

    /// <summary>Rises out of the field and settles into the arch. Fires <paramref name="onArrive"/> per frame.</summary>
    public IEnumerator FlyIn(Transform cam, EndingAudioMix mix, float flight = 3.1f)
    {
        float clock = 0f;
        float total = (frames.Count > 0 ? frames[frames.Count - 1].delay : 0f) + flight + .6f;
        while (clock < total)
        {
            clock += Time.unscaledDeltaTime;
            foreach (var f in frames)
            {
                float u = Mathf.Clamp01((clock - f.delay) / flight);
                if (u <= 0f) { f.root.localScale = Vector3.zero; continue; }
                float e = Mathf.SmoothStep(0f, 1f, u);
                Vector3 side = Vector3.Cross(Vector3.up, (f.end - f.start).normalized);
                Vector3 pos = Vector3.Lerp(f.start, f.end, e) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * f.arc) +
                              side * (Mathf.Sin(u * Mathf.PI * 2f + f.phase) * .12f * (1f - e));
                f.root.position = pos;
                Face(f.root, cam);
                float pop = Mathf.SmoothStep(0f, 1f, u / .22f);
                float pulse = 1f;
                if (u >= 1f)
                {
                    if (f.arrivedAt < 0f)
                    {
                        f.arrivedAt = clock;
                        if (mix != null) mix.Bell(f.index, .42f);
                    }
                    pulse += .22f * Mathf.Exp(-(clock - f.arrivedAt) * 5f);
                }
                f.root.localScale = Vector3.one * (pop * pulse);
                SetHalo(f, u >= 1f ? .5f + .5f * Mathf.Exp(-(clock - f.arrivedAt) * 3.5f) : Mathf.Clamp01(u * 1.4f) * .35f);
            }
            yield return null;
        }
    }

    /// <summary>Holds the arch with a gentle float so the shot never goes static.</summary>
    public void Hover(Transform cam)
    {
        float t = Time.unscaledTime;
        foreach (var f in frames)
        {
            if (f.arrivedAt < 0f) continue;
            Vector3 bob = Vector3.up * (Mathf.Sin(t * .9f + f.phase) * .045f);
            f.root.position = f.end + bob;
            Face(f.root, cam);
            SetHalo(f, .42f + .12f * Mathf.Sin(t * 1.3f + f.phase));
        }
    }

    /// <summary>The arch pours through the doorway of the house as light.</summary>
    public IEnumerator StreamInto(Transform cam, Vector3 door, float duration = 2.2f)
    {
        float clock = 0f;
        float total = duration + frames.Count * .1f;
        var from = new List<Vector3>();
        foreach (var f in frames) from.Add(f.root.position);
        while (clock < total)
        {
            clock += Time.unscaledDeltaTime;
            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                float u = Mathf.Clamp01((clock - i * .1f) / duration);
                float e = u * u * (3f - 2f * u);
                Vector3 mid = Vector3.Lerp(from[i], door, e) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * .7f);
                f.root.position = mid;
                Face(f.root, cam);
                f.root.localScale = Vector3.one * Mathf.Lerp(1f, .12f, e);
                float fade = 1f - Mathf.SmoothStep(.55f, 1f, u);
                SetAlpha(f, fade);
                SetHalo(f, Mathf.Lerp(.9f, 0f, e));
            }
            yield return null;
        }
        foreach (var f in frames) f.root.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        foreach (var f in frames) f.root.gameObject.SetActive(false);
    }

    private static void Face(Transform t, Transform cam)
    {
        Vector3 away = t.position - cam.position;
        if (away.sqrMagnitude > .001f) t.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    private static void SetHalo(Frame f, float alpha)
    {
        if (f.halo == null) return;
        var c = f.halo.color;
        c.a = Mathf.Clamp01(alpha);
        f.halo.color = c;
    }

    private static void SetAlpha(Frame f, float alpha)
    {
        foreach (var r in new[] { f.border, f.panel, f.picture })
        {
            if (r == null) continue;
            var c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }

    private void OnDestroy()
    {
        if (haloMaterial != null) Destroy(haloMaterial);
        if (whiteSprite != null) Destroy(whiteSprite);
        if (circleTex != null) Destroy(circleTex);
    }
}
