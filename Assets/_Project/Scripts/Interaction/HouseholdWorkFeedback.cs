using UnityEngine;

/// <summary>Scene 2 work effects, kept separate from chore rules and imported colliders.</summary>
public sealed class HouseholdWorkFeedback
{
    private readonly HouseholdChore chore;
    private readonly GameObject root;
    private readonly ParticleSystem particles;
    private readonly Light fireLight;
    private readonly Material particleMaterial;
    private readonly Transform swayingVisual;
    private readonly Quaternion visualRotation;
    private float intensity;
    private float pulse;

    public HouseholdWorkFeedback(HouseholdChore task, Transform anchor)
    {
        chore = task;
        if (anchor != null && anchor.GetComponentInChildren<Collider>() == null &&
            (task == HouseholdChore.WashRice || task == HouseholdChore.FoldBlanket ||
             task == HouseholdChore.CarryWood || task == HouseholdChore.GrindCorn))
        {
            swayingVisual = anchor;
            visualRotation = anchor.localRotation;
        }

        root = new GameObject("HouseholdWorkEffect_" + task);
        root.transform.position = PositionFor(anchor);
        var particleObject = new GameObject("WorkParticles");
        particleObject.transform.SetParent(root.transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = task == HouseholdChore.CookRice ? 1.4f : .7f;
        main.startSpeed = IsRising(task) ? .32f : .8f;
        main.startSize = task == HouseholdChore.LightFire ? .028f : .022f;
        main.startColor = ParticleColor(task);
        main.maxParticles = 56;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = task == HouseholdChore.FoldBlanket ? .22f : .13f;
        var emission = particles.emission;
        emission.rateOverTime = AmbientRate(task);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = IsRising(task) ? .45f : -.35f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                        Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            particleMaterial = new Material(shader);
            renderer.sharedMaterial = particleMaterial;
        }

        if (task == HouseholdChore.LightFire || task == HouseholdChore.CookRice)
        {
            var glow = new GameObject("FireWorkGlow");
            glow.transform.SetParent(root.transform, false);
            fireLight = glow.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, .38f, .1f);
            fireLight.range = 1.25f;
            fireLight.intensity = .16f;
            fireLight.shadows = LightShadows.None;
        }
        particles.Play();
    }

    public void Tick()
    {
        if (root == null || GameManager.Instance?.IsPaused == true) return;
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 2.2f);
        var emission = particles.emission;
        emission.rateOverTime = AmbientRate(chore) + intensity * 12f + pulse * 20f;
        if (swayingVisual != null)
        {
            float angle = Mathf.Sin(Time.time * (chore == HouseholdChore.GrindCorn ? 6f : 3f)) *
                          (1.5f + 3f * intensity);
            swayingVisual.localRotation = visualRotation * Quaternion.Euler(0f, 0f, angle);
        }
        if (fireLight != null)
            fireLight.intensity = .16f + intensity * .45f + pulse * .4f + .045f * Mathf.Sin(Time.time * 12f);
    }

    public void Pulse()
    {
        pulse = 1f;
        intensity = Mathf.Min(1f, intensity + .12f);
        if (particles != null) particles.Emit(chore == HouseholdChore.ReadLetter ? 18 : 10);
    }

    public void SetIntensity(float value) => intensity = Mathf.Clamp01(value);

    public void Dispose()
    {
        if (swayingVisual != null) swayingVisual.localRotation = visualRotation;
        if (root != null) Object.Destroy(root);
        if (particleMaterial != null) Object.Destroy(particleMaterial);
    }

    private static bool IsRising(HouseholdChore task) =>
        task == HouseholdChore.LightFire || task == HouseholdChore.CookRice ||
        task == HouseholdChore.ReadLetter || task == HouseholdChore.TuneRadio;

    private static float AmbientRate(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.LightFire: return 7f;
            case HouseholdChore.CookRice: return 5f;
            case HouseholdChore.ReadLetter: return 3f;
            case HouseholdChore.WashRice:
            case HouseholdChore.FetchWater:
            case HouseholdChore.WashDishes: return 3f;
            case HouseholdChore.MeasureRice:
            case HouseholdChore.GrindCorn: return 2f;
            default: return .4f;
        }
    }

    private static Vector3 PositionFor(Transform anchor)
    {
        if (anchor == null) return Vector3.zero;
        Renderer renderer = anchor.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds.center + Vector3.up * Mathf.Min(.4f, renderer.bounds.extents.y);
        Collider collider = anchor.GetComponent<Collider>();
        if (collider != null) return collider.bounds.center + Vector3.up * .2f;
        return anchor.position + Vector3.up * .7f;
    }

    private static Color ParticleColor(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.LightFire: return new Color(1f, .38f, .06f, .8f);
            case HouseholdChore.CookRice: return new Color(.95f, .95f, .87f, .42f);
            case HouseholdChore.GrindCorn:
            case HouseholdChore.MeasureRice: return new Color(.95f, .86f, .65f, .82f);
            case HouseholdChore.ReadLetter: return new Color(1f, .8f, .43f, .68f);
            case HouseholdChore.FoldBlanket:
            case HouseholdChore.ArrangeKeepsakes:
            case HouseholdChore.SortTools:
            case HouseholdChore.CarryWood:
            case HouseholdChore.TuneRadio:
            case HouseholdChore.SetTable:
            case HouseholdChore.PutAwayDishes: return new Color(1f, .78f, .48f, .55f);
            default: return new Color(.63f, .86f, 1f, .65f);
        }
    }
}
