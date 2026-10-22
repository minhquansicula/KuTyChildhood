using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Quiet physical feedback: a short pour, paper leaving the printer and a report sliding onto the desk.</summary>
public sealed class OfficeSceneEffects : MonoBehaviour
{
    private OfficePhase phase;
    private ParticleSystem waterParticles;
    private ParticleSystem deskDust;
    private LineRenderer waterStream;
    private Light printerLight;
    private Transform printerSheet, deliveredSheet;
    private Vector3 printerRest, reportRest, tap;
    private Material paperMaterial;
    private Texture2D reportTexture;
    private Mesh paperMesh;
    private Coroutine waterRoutine, pickupRoutine, reportRoutine;
    private float printStarted;

    private void Start()
    {
        phase = GetComponent<OfficeSceneController>()?.Phase ?? OfficePhase.Opening;
        Transform printer = null, water = null;
        foreach (var item in FindObjectsOfType<OfficeMicroInteractable>(true))
        {
            if (item.Kind == OfficeMicroInteractionKind.PrinterPaper && !printer) printer = item.transform;
            if (item.Kind == OfficeMicroInteractionKind.WaterCooler && !water) water = item.transform;
        }
        paperMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Warm office paper" };
        paperMaterial.SetColor("_BaseColor", new Color(.86f, .85f, .78f));
        paperMaterial.SetFloat("_Smoothness", .08f);
        reportTexture = ReportTexture();
        paperMaterial.SetTexture("_BaseMap", reportTexture);
        paperMesh = new Mesh { name = "Two-sided report sheet" };
        paperMesh.vertices = new[] { new Vector3(-.105f, 0, -.148f), new Vector3(.105f, 0, -.148f),
            new Vector3(.105f, 0, .148f), new Vector3(-.105f, 0, .148f),
            new Vector3(-.105f, -.0008f, -.148f), new Vector3(.105f, -.0008f, -.148f),
            new Vector3(.105f, -.0008f, .148f), new Vector3(-.105f, -.0008f, .148f) };
        paperMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
            Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        paperMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7 };
        paperMesh.RecalculateNormals();
        paperMesh.RecalculateBounds();

        if (water)
        {
            Bounds bounds = BoundsFor(water, new Vector3(.59f, 2.15f, .62f));
            Renderer cooler = null;
            float nearest = 2.25f;
            foreach (var candidate in FindObjectsOfType<MeshRenderer>())
            {
                if (candidate.name != "Mesh_Cooler_01") continue;
                float distance = (candidate.bounds.center - bounds.center).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                cooler = candidate;
            }
            if (cooler) bounds = cooler.bounds;
            // Cold tap: aisle side, below the bottle, rather than the interaction volume's centre.
            tap = new Vector3(bounds.center.x + bounds.size.x * (cooler ? .27f : .15f),
                bounds.min.y + bounds.size.y * (cooler ? .68f : .40f), bounds.min.z + (cooler ? .045f : -.012f));
            waterParticles = InteractionVfx.Create(transform, "WaterCoolerDroplets", InteractionVfxKind.Droplet);
            waterParticles.transform.position = tap;
            var streamObject = new GameObject("WaterCoolerPour");
            streamObject.transform.SetParent(transform, false);
            waterStream = streamObject.AddComponent<LineRenderer>();
            waterStream.sharedMaterial = InteractionVfx.MaterialFor(InteractionVfxKind.Droplet);
            waterStream.useWorldSpace = true;
            waterStream.positionCount = 4;
            for (int i = 0; i < 4; i++) waterStream.SetPosition(i, tap + new Vector3(.002f * i, -.06f * i, -.001f * i));
            waterStream.startColor = new Color(.84f, .92f, .94f, .55f);
            waterStream.endColor = new Color(.78f, .88f, .92f, .15f);
            waterStream.widthMultiplier = .006f;
            waterStream.numCapVertices = 3;
            waterStream.shadowCastingMode = ShadowCastingMode.Off;
            waterStream.receiveShadows = false;
            waterStream.enabled = false;
        }
        if (printer)
        {
            var top = GameObject.Find("Mesh_Printer_Top_01")?.GetComponent<Renderer>();
            printerRest = top ? top.bounds.center + new Vector3(0, -.305f, -.24f) : printer.position + new Vector3(0, 1.28f, -.32f);
            printerSheet = Sheet("PrintedReportSheet", printerRest);
            var lamp = new GameObject("PrinterStatusGlow");
            lamp.transform.SetParent(transform, false);
            lamp.transform.position = printerRest + new Vector3(.22f, .08f, .10f);
            printerLight = lamp.AddComponent<Light>();
            printerLight.color = new Color(.55f, .72f, .55f);
            printerLight.range = .35f;
            printerLight.intensity = 0;
            printerLight.shadows = LightShadows.None;
            printerLight.enabled = false;
        }
        var desk = GameObject.Find("BossDesk_SLOT");
        if (desk)
        {
            float surface = desk.transform.position.y + .79f;
            foreach (var collider in desk.GetComponentsInChildren<Collider>())
                if (!collider.isTrigger && collider.bounds.max.y < 1.5f) surface = Mathf.Max(surface, collider.bounds.max.y);
            // Authored art can sit above the old blockout collider and outside the slot hierarchy.
            float nearestTop = 1.44f;
            Bounds? tabletop = null;
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                var bounds = renderer.bounds;
                if (!renderer.enabled || bounds.size.x < 1.2f || bounds.size.x > 4f ||
                    bounds.size.z < .55f || bounds.size.z > 2f || bounds.max.y > 1.4f || bounds.max.y < .5f) continue;
                Vector3 offset = bounds.center - desk.transform.position;
                float distance = offset.x * offset.x + offset.z * offset.z;
                if (distance >= nearestTop) continue;
                nearestTop = distance;
                surface = bounds.max.y;
                tabletop = bounds;
            }
            reportRest = desk.transform.position + new Vector3(.62f, surface - desk.transform.position.y + .006f, -.20f);
            if (tabletop.HasValue)
            {
                var bounds = tabletop.Value;
                reportRest = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, .68f), surface + .006f, bounds.min.z + .23f);
            }
            deliveredSheet = Sheet("DeliveredReportSheet", reportRest);
            deliveredSheet.localScale = Vector3.one * 1.15f;
            deskDust = InteractionVfx.Create(transform, "DeskImpactDust", InteractionVfxKind.Dust);
            deskDust.transform.position = new Vector3(desk.transform.position.x, surface + .015f, desk.transform.position.z + .10f);
        }
    }

    private void Update()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        if (phase == OfficePhase.PrintingReport && printerSheet && pickupRoutine == null)
        {
            float t = Mathf.Clamp01((Time.time - printStarted) / 2.6f);
            printerSheet.gameObject.SetActive(true);
            printerSheet.position = printerRest + Vector3.forward * Mathf.Lerp(.20f, 0, Mathf.SmoothStep(0, 1, t));
        }
        if (!printerLight) return;
        float target = phase == OfficePhase.PrintingReport ? .10f + .018f * Mathf.PerlinNoise(Time.time * 1.7f, 7) : 0;
        printerLight.intensity = Mathf.MoveTowards(printerLight.intensity, target, Time.deltaTime * .4f);
        printerLight.enabled = printerLight.intensity > .005f;
    }

    public void SetPhase(OfficePhase next)
    {
        phase = next;
        if (next == OfficePhase.PrintingReport) printStarted = Time.time;
        if (next == OfficePhase.CollectPrint && printerSheet)
        { printerSheet.position = printerRest; printerSheet.gameObject.SetActive(true); }
    }
    public void PlayWater()
    {
        if (!waterParticles || !isActiveAndEnabled) return;
        if (waterRoutine != null) StopCoroutine(waterRoutine);
        waterRoutine = StartCoroutine(PourWater());
    }
    private IEnumerator PourWater()
    {
        InteractionVfx.Rate(waterParticles, 38);
        waterParticles.Play();
        float elapsed = 0;
        waterStream.enabled = true;
        while (elapsed < .65f)
        {
            if (GameManager.Instance?.IsPaused == true) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float envelope = Mathf.SmoothStep(0, 1, elapsed / .12f) * Mathf.SmoothStep(0, 1, (.65f - elapsed) / .14f);
            waterStream.widthMultiplier = .006f * envelope;
            yield return null;
        }
        waterStream.enabled = false;
        waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        waterRoutine = null;
    }
    public void PlayPaperPickup()
    {
        if (!printerSheet || !isActiveAndEnabled) return;
        if (pickupRoutine != null) StopCoroutine(pickupRoutine);
        pickupRoutine = StartCoroutine(PickupPaper());
    }
    private IEnumerator PickupPaper()
    {
        Vector3 from = printerSheet.position;
        for (float t = 0; t < 1; t += Time.deltaTime / .32f)
        {
            if (GameManager.Instance?.IsPaused == true) { yield return null; continue; }
            printerSheet.position = from + new Vector3(0, Mathf.Sin(t * Mathf.PI) * .075f, -t * .18f);
            printerSheet.rotation = Quaternion.Euler(-12f * Mathf.Sin(t * Mathf.PI), 0, 0);
            yield return null;
        }
        printerSheet.gameObject.SetActive(false);
        printerSheet.rotation = Quaternion.identity;
        pickupRoutine = null;
    }
    public void PlayReportDelivered()
    {
        if (!deliveredSheet || !isActiveAndEnabled) return;
        if (reportRoutine != null) StopCoroutine(reportRoutine);
        reportRoutine = StartCoroutine(SlideReport());
    }
    private IEnumerator SlideReport()
    {
        deliveredSheet.gameObject.SetActive(true);
        for (float t = 0; t < 1; t += Time.deltaTime / .38f)
        {
            if (GameManager.Instance?.IsPaused == true) { yield return null; continue; }
            float eased = 1 - Mathf.Pow(1 - t, 3);
            deliveredSheet.position = reportRest + new Vector3(0, .025f * (1 - eased), -.22f * (1 - eased));
            deliveredSheet.rotation = Quaternion.Euler(0, Mathf.Lerp(-8, 0, eased), 0);
            yield return null;
        }
        deliveredSheet.SetPositionAndRotation(reportRest, Quaternion.identity);
        reportRoutine = null;
    }
    public void PlayDeskImpact() { if (deskDust && isActiveAndEnabled) deskDust.Emit(4); }

    private Transform Sheet(string name, Vector3 position)
    {
        var sheet = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        sheet.transform.SetParent(transform, false);
        sheet.transform.position = position;
        sheet.GetComponent<MeshFilter>().sharedMesh = paperMesh;
        var renderer = sheet.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = paperMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        sheet.SetActive(false);
        return sheet.transform;
    }
    private static Texture2D ReportTexture()
    {
        const int width = 128, height = 181;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        { name = "Printed report ink", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            Color color = new Color(.98f, .97f, .93f);
            if (x >= 12 && x <= 93 && y >= 158 && y <= 163) color = new Color(.18f, .20f, .18f);
            else if (x >= 10 && x <= 118 && y >= 22 && y <= 145 && ((y - 22) % 12 == 0 || (x - 10) % 22 == 0))
                color = new Color(.64f, .65f, .61f);
            else if (x >= 15 && x <= 110 && y >= 24 && y <= 138 && (y - 26) % 12 < 2 && x % 22 > 6 && x % 22 < 16)
                color = new Color(.38f, .40f, .37f);
            pixels[y * width + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
    private static Bounds BoundsFor(Transform target, Vector3 fallbackSize)
    {
        var collider = target.GetComponent<Collider>();
        return collider ? collider.bounds : new Bounds(target.position + Vector3.up * fallbackSize.y * .5f, fallbackSize);
    }
    private void OnDisable()
    {
        StopAllCoroutines(); waterRoutine = pickupRoutine = reportRoutine = null;
        if (printerLight) printerLight.enabled = false;
        if (waterStream) waterStream.enabled = false;
        if (waterParticles) waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (deskDust) deskDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private void OnDestroy()
    {
        if (paperMaterial) Destroy(paperMaterial);
        if (paperMesh) Destroy(paperMesh);
        if (reportTexture) Destroy(reportTexture);
    }
}
