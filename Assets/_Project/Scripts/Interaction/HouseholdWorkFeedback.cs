using UnityEngine;

/// <summary>Temporary animated props and particles for a chore; never moves imported meshes or colliders.</summary>
public sealed class HouseholdWorkFeedback
{
    private readonly HouseholdChore chore;
    private readonly GameObject root;
    private readonly Transform tool;
    private readonly ParticleSystem particles;
    private readonly Light fireLight;
    private readonly Material toolMaterial;
    private readonly Material particleMaterial;
    private readonly Vector3 origin;
    private readonly Transform swayingVisual;
    private readonly Quaternion visualRotation;
    private float intensity;
    private float pulse;

    public HouseholdWorkFeedback(HouseholdChore task, Transform anchor)
    {
        chore = task;
        origin = PositionFor(anchor);
        if (task == HouseholdChore.WashRice && anchor != null && anchor.GetComponent<Collider>() == null)
        {
            swayingVisual = anchor;
            visualRotation = anchor.localRotation;
        }
        root = new GameObject("HouseholdWorkEffect_" + task);
        root.transform.position = origin;

        var kind = task == HouseholdChore.WashDishes || task == HouseholdChore.FoldBlanket || task == HouseholdChore.ReadLetter ? PrimitiveType.Cube
            : task == HouseholdChore.LightFire ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
        var toolObject = GameObject.CreatePrimitive(kind);
        toolObject.name = task == HouseholdChore.WashDishes ? "AnimatedSponge" : "AnimatedWorkTool";
        toolObject.transform.SetParent(root.transform, false);
        toolObject.transform.localScale = task == HouseholdChore.WashDishes
            ? new Vector3(.18f, .055f, .12f)
            : task == HouseholdChore.LightFire ? new Vector3(.12f, .12f, .12f)
            : new Vector3(.14f, .055f, .14f);
        if (task == HouseholdChore.FoldBlanket) toolObject.transform.localScale = new Vector3(.5f, .035f, .35f);
        if (task == HouseholdChore.ReadLetter) toolObject.transform.localScale = new Vector3(.23f, .012f, .16f);
        if (task == HouseholdChore.CarryWood) toolObject.transform.localScale = new Vector3(.09f, .25f, .09f);
        var collider = toolObject.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        tool = toolObject.transform;
        var toolShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        toolMaterial = new Material(toolShader);
        toolMaterial.color = ToolColor(task);
        toolObject.GetComponent<Renderer>().sharedMaterial = toolMaterial;
        if (task == HouseholdChore.LightFire || task == HouseholdChore.CookRice)
        {
            var glow = new GameObject("AnimatedFireGlow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = Vector3.down * .12f;
            fireLight = glow.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, .38f, .1f);
            fireLight.range = 1.2f;
            fireLight.intensity = task == HouseholdChore.LightFire ? .35f : .22f;
        }

        var particleObject = new GameObject("WorkParticles");
        particleObject.transform.SetParent(root.transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true;
        main.startLifetime = task == HouseholdChore.CookRice ? 1.4f : .65f;
        main.startSpeed = task == HouseholdChore.CookRice ? .35f : 1.1f;
        main.startSize = task == HouseholdChore.LightFire ? .035f : .025f;
        main.startColor = ParticleColor(task);
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .13f;
        var emission = particles.emission;
        emission.rateOverTime = 7f;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = task == HouseholdChore.LightFire || task == HouseholdChore.CookRice ? .7f : -.35f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            particleMaterial = new Material(particleShader);
            renderer.sharedMaterial = particleMaterial;
        }
        particles.Play();
    }

    public void Tick()
    {
        if (root == null) return;
        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 2f);
        var t = Time.unscaledTime;
        float rate = chore == HouseholdChore.WashDishes || chore == HouseholdChore.WashRice ? 9f : 4f;
        float size = .045f + intensity * .035f + pulse * .085f;
        tool.localPosition = new Vector3(Mathf.Sin(t * rate) * size, .1f + Mathf.Cos(t * rate * .8f) * size,
            Mathf.Cos(t * rate) * size);
        tool.localRotation = Quaternion.Euler(15f * Mathf.Sin(t * rate), t * 25f, 18f * Mathf.Cos(t * rate));
        if (chore == HouseholdChore.GrindCorn)
        {
            tool.localPosition = new Vector3(Mathf.Cos(t * 4f) * .2f, .1f, Mathf.Sin(t * 4f) * .2f);
            tool.localRotation = Quaternion.Euler(0f, t * 180f, 90f);
        }
        if (chore == HouseholdChore.TuneRadio) tool.localRotation = Quaternion.Euler(90f, 0f, Mathf.Sin(t * 3f) * 90f);
        if (swayingVisual != null)
            swayingVisual.localRotation = visualRotation * Quaternion.Euler(0f, 0f,
                Mathf.Sin(t * rate) * (2f + 4f * intensity));
        if (fireLight != null) fireLight.intensity = .25f + intensity * .6f + pulse * .9f
            + .08f * Mathf.Sin(t * 13f);
        var emission = particles.emission;
        emission.rateOverTime = 7f + intensity * 24f + pulse * 45f;
    }

    public void Pulse() { pulse = 1f; intensity = Mathf.Min(1f, intensity + .13f); }
    public void SetIntensity(float value) => intensity = Mathf.Clamp01(value);

    public void Dispose()
    {
        if (swayingVisual != null) swayingVisual.localRotation = visualRotation;
        if (root != null) Object.Destroy(root);
        if (toolMaterial != null) Object.Destroy(toolMaterial);
        if (particleMaterial != null) Object.Destroy(particleMaterial);
    }

    private static Vector3 PositionFor(Transform anchor)
    {
        if (anchor == null) return Vector3.zero;
        var renderer = anchor.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds.center + Vector3.up * Mathf.Min(.4f, renderer.bounds.extents.y);
        return anchor.position + Vector3.up * .7f;
    }

    private static Color ToolColor(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.WashDishes: return new Color(.8f, .72f, .3f);
            case HouseholdChore.LightFire: return new Color(.38f, .2f, .08f);
            case HouseholdChore.CookRice: return new Color(.57f, .55f, .5f);
            default: return new Color(.63f, .41f, .22f);
        }
    }

    private static Color ParticleColor(HouseholdChore task)
    {
        switch (task)
        {
            case HouseholdChore.LightFire: return new Color(1f, .38f, .06f, .85f);
            case HouseholdChore.CookRice: return new Color(.95f, .95f, .87f, .5f);
            case HouseholdChore.GrindCorn:
            case HouseholdChore.MeasureRice: return new Color(.95f, .86f, .65f, .9f);
            case HouseholdChore.FoldBlanket:
            case HouseholdChore.ArrangeKeepsakes:
            case HouseholdChore.SortTools:
            case HouseholdChore.CarryWood:
            case HouseholdChore.TuneRadio:
            case HouseholdChore.ReadLetter:
            case HouseholdChore.SetTable:
            case HouseholdChore.PutAwayDishes: return new Color(1f, .78f, .4f, .65f);
            default: return new Color(.63f, .86f, 1f, .7f);
        }
    }
}
