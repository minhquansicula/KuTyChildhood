using UnityEngine;

/// <summary>Small, reusable visual beats for existing Scene 1 interactions.</summary>
public sealed class OfficeSceneEffects : MonoBehaviour
{
    private OfficePhase phase;
    private ParticleSystem printerParticles;
    private ParticleSystem waterParticles;
    private ParticleSystem reportParticles;
    private Light printerLight;
    private Material particleMaterial;
    private float nextPrinterEmit;

    private void Start()
    {
        OfficeSceneController office = GetComponent<OfficeSceneController>();
        phase = office != null ? office.Phase : OfficePhase.Opening;

        Transform printer = null;
        Transform water = null;
        foreach (OfficeMicroInteractable item in FindObjectsOfType<OfficeMicroInteractable>(true))
        {
            if (item.Kind == OfficeMicroInteractionKind.PrinterPaper && printer == null) printer = item.transform;
            else if (item.Kind == OfficeMicroInteractionKind.WaterCooler && water == null) water = item.transform;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                        Shader.Find("Particles/Standard Unlit");
        if (shader != null) particleMaterial = new Material(shader);

        if (printer != null)
        {
            Vector3 position = EffectPosition(printer);
            printerParticles = CreateParticles("PrinterPaperFlecks", position, new Color(.76f, .9f, 1f, .8f), .025f);
            var lamp = new GameObject("PrinterStatusGlow");
            lamp.transform.SetParent(transform, false);
            lamp.transform.position = position + Vector3.up * .08f;
            printerLight = lamp.AddComponent<Light>();
            printerLight.type = LightType.Point;
            printerLight.color = new Color(.67f, .89f, 1f);
            printerLight.range = 1.15f;
            printerLight.intensity = 0f;
            printerLight.shadows = LightShadows.None;
            printerLight.enabled = false;
        }
        if (water != null)
            waterParticles = CreateParticles("WaterCoolerDroplets", EffectPosition(water),
                new Color(.5f, .84f, 1f, .72f), .034f);
        OfficeBlockoutDoorInteractable bossDoor = FindObjectOfType<OfficeBlockoutDoorInteractable>();
        if (bossDoor != null)
            reportParticles = CreateParticles("ReportDeliveredMotes", EffectPosition(bossDoor.transform),
                new Color(1f, .8f, .44f, .75f), .025f);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (phase == OfficePhase.PrintingReport && printerParticles != null && Time.time >= nextPrinterEmit)
        {
            printerParticles.Emit(2);
            nextPrinterEmit = Time.time + .22f;
        }
        if (printerLight == null) return;
        float target = phase == OfficePhase.PrintingReport ? .34f + .06f * Mathf.Sin(Time.time * 11f) : 0f;
        printerLight.intensity = Mathf.MoveTowards(printerLight.intensity, target, Time.deltaTime * 1.2f);
        printerLight.enabled = printerLight.intensity > .01f;
    }

    public void SetPhase(OfficePhase next)
    {
        phase = next;
        if (next == OfficePhase.CollectPrint) printerParticles?.Emit(12);
        if (next == OfficePhase.PrintingReport) nextPrinterEmit = 0f;
    }

    public void PlayWater() => waterParticles?.Emit(16);
    public void PlayPaperPickup() => printerParticles?.Emit(14);
    public void PlayReportDelivered() => reportParticles?.Emit(18);

    private ParticleSystem CreateParticles(string name, Vector3 position, Color color, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = .55f;
        main.startSpeed = .55f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .09f;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
        return system;
    }

    private static Vector3 EffectPosition(Transform target)
    {
        Collider collider = target.GetComponent<Collider>();
        return collider != null ? collider.bounds.center + Vector3.up * .2f : target.position + Vector3.up * .6f;
    }

    private void OnDisable()
    {
        if (printerLight != null) printerLight.enabled = false;
        printerParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        waterParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        reportParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (particleMaterial != null) Destroy(particleMaterial);
    }
}
