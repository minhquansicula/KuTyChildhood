using UnityEngine;

/// <summary>Material-specific work feedback; placement, radio and letter actions do not spray confetti.</summary>
public sealed class HouseholdWorkFeedback
{
    private readonly HouseholdChore chore;
    private readonly GameObject root;
    private readonly ParticleSystem particles;
    private readonly ParticleSystem secondary;
    private readonly Light fireLight;
    private readonly Transform swayingVisual;
    private readonly Quaternion visualRotation;
    private float intensity, pulse;

    public HouseholdWorkFeedback(HouseholdChore task, Transform anchor)
    {
        chore = task;
        root = new GameObject("HouseholdWorkEffect_" + task);
        root.transform.position = PositionFor(anchor, task);
        if (anchor && anchor.GetComponentInChildren<Collider>() == null && task == HouseholdChore.GrindCorn)
        { swayingVisual = anchor; visualRotation = anchor.localRotation; }

        InteractionVfxKind? kind = null;
        switch (task)
        {
            case HouseholdChore.CookRice: kind = InteractionVfxKind.Steam; break;
            case HouseholdChore.LightFire: kind = InteractionVfxKind.Ember; break;
            case HouseholdChore.WashRice:
            case HouseholdChore.FetchWater: kind = InteractionVfxKind.Droplet; break;
            case HouseholdChore.WashDishes: kind = InteractionVfxKind.Bubble; break;
            case HouseholdChore.GrindCorn:
            case HouseholdChore.MeasureRice: kind = InteractionVfxKind.Grain; break;
            case HouseholdChore.FoldBlanket:
            case HouseholdChore.CarryWood:
            case HouseholdChore.ArrangeKeepsakes: kind = InteractionVfxKind.Dust; break;
        }
        if (kind.HasValue)
        {
            particles = InteractionVfx.Create(root.transform, "WorkParticles", kind.Value);
            particles.Play();
        }
        if (task == HouseholdChore.LightFire)
        {
            secondary = InteractionVfx.Create(root.transform, "KindlingSmoke", InteractionVfxKind.Steam);
            var color = secondary.main;
            color.startColor = new Color(.43f, .40f, .35f, .12f);
            secondary.Play();
        }
        if (task == HouseholdChore.LightFire || task == HouseholdChore.CookRice)
        {
            var glow = new GameObject("FireWorkGlow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = task == HouseholdChore.CookRice ? Vector3.down * .35f : Vector3.zero;
            fireLight = glow.AddComponent<Light>();
            fireLight.color = new Color(1f, .56f, .24f);
            fireLight.range = 1.0f;
            fireLight.intensity = .08f;
            fireLight.shadows = LightShadows.None;
        }
    }

    public void Tick()
    {
        if (!root || GameManager.Instance?.IsPaused == true) return;
        pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 2.8f);
        float rate = chore == HouseholdChore.CookRice ? 1.5f + intensity * 3 :
            chore == HouseholdChore.LightFire ? intensity * 1.3f + pulse * 5 : pulse * 7;
        InteractionVfx.Rate(particles, rate);
        InteractionVfx.Rate(secondary, chore == HouseholdChore.LightFire ? .5f + intensity : 0);
        if (swayingVisual)
            swayingVisual.localRotation = visualRotation * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 4) * intensity * 1.3f);
        if (fireLight)
            fireLight.intensity = .08f + intensity * .18f + pulse * .10f + Mathf.PerlinNoise(Time.time * 2.1f, 3.7f) * .04f;
    }
    public void Pulse()
    {
        pulse = 1;
        if (particles) particles.Emit(chore == HouseholdChore.CookRice ? 2 : chore == HouseholdChore.LightFire ? 3 : 4);
    }
    public void SetIntensity(float value) => intensity = Mathf.Clamp01(value);
    public void Dispose()
    {
        if (swayingVisual) swayingVisual.localRotation = visualRotation;
        if (root) Object.Destroy(root);
    }
    private static Vector3 PositionFor(Transform anchor, HouseholdChore task)
    {
        if (!anchor) return Vector3.zero;
        var collider = anchor.GetComponent<Collider>();
        Bounds bounds = collider ? collider.bounds : new Bounds(anchor.position, Vector3.one * .4f);
        bool foundVisual = false;
        foreach (var visual in anchor.GetComponentsInChildren<Renderer>())
        {
            if (!visual.enabled || visual is ParticleSystemRenderer || visual is LineRenderer) continue;
            if (!foundVisual) { bounds = visual.bounds; foundVisual = true; }
            else bounds.Encapsulate(visual.bounds);
        }
        if (bounds.size.magnitude > 4) return anchor.position + Vector3.up * .25f;
        var point = bounds.center;
        point.y = task == HouseholdChore.LightFire ? Mathf.Lerp(bounds.min.y, bounds.max.y, .22f) : bounds.max.y + .025f;
        return point;
    }
}
