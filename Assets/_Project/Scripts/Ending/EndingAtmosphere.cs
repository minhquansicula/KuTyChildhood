using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Floating golden dust, pollen and soft bokeh that stay around the film camera. Particles simulate in world
/// space so the camera flies *through* them, which sells the depth of the 3D set.
/// </summary>
public sealed class EndingAtmosphere : MonoBehaviour
{
    private Transform follow;
    private Transform rootDust, rootBokeh;
    private ParticleSystem dust, bokeh, pollen;
    private Material material;
    private Texture2D softMask;
    private Vector3 windDirection = new Vector3(1f, .12f, .35f).normalized;

    public void Initialize(Transform cameraTransform, Light sun)
    {
        follow = cameraTransform;
        var shader = Resources.Load<Shader>("Effects/MemoryParticle");
        if (shader == null) { enabled = false; return; }
        softMask = InteractionVfx.MaskFor(InteractionVfxKind.Dust);
        material = new Material(shader) { name = "Ending FX", hideFlags = HideFlags.DontSave };
        material.SetTexture("_BaseMap", softMask);
        material.SetFloat("_Brightness", 2.4f);
        material.SetFloat("_SoftFade", 0f);

        dust = Create("Golden dust", new Vector3(26f, 9f, 34f), 12f, 320,
            .035f, .11f, new Color(1f, .86f, .52f, .75f), 24f, .35f);
        pollen = Create("Rice pollen", new Vector3(30f, 3.2f, 36f), 5f, 200,
            .02f, .05f, new Color(1f, .93f, .66f, .9f), 40f, .8f);
        bokeh = Create("Warm bokeh", new Vector3(22f, 8f, 30f), 16f, 90,
            .35f, .85f, new Color(1f, .78f, .42f, .16f), 5f, .12f);
    }

    private ParticleSystem Create(string title, Vector3 box, float centerAhead, int maxParticles,
        float sizeMin, float sizeMax, Color color, float rate, float speed)
    {
        var go = new GameObject(title);
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 13f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, speed * .4f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, new Color(color.r, color.g * .92f, color.b * .8f, color.a * .6f));
        main.gravityModifier = -.004f;
        main.prewarm = false;
        var emission = system.emission;
        emission.rateOverTime = rate;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        shape.position = new Vector3(0f, box.y * .35f, centerAhead);
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(windDirection.x * speed * .6f, windDirection.x * speed);
        velocity.y = new ParticleSystem.MinMaxCurve(-.02f, .14f);
        velocity.z = new ParticleSystem.MinMaxCurve(windDirection.z * speed * .3f, windDirection.z * speed * .7f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = .35f;
        noise.frequency = .22f;
        noise.scrollSpeed = .12f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        var life = system.colorOverLifetime;
        life.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(.8f, .7f), new GradientAlphaKey(0f, 1f) });
        life.color = gradient;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.maxParticleSize = sizeMax > .3f ? 1.2f : .5f;
        system.Play();
        return system;
    }

    /// <summary>Warm up the volume so the very first frame already has dust in the air.</summary>
    public void Prewarm()
    {
        foreach (var system in new[] { dust, pollen, bokeh })
        {
            if (system == null) continue;
            system.Simulate(8f, true, true);
            system.Play();
        }
    }

    public void SetIntensity(float value)
    {
        value = Mathf.Clamp01(value);
        SetRate(dust, 24f * value);
        SetRate(pollen, 40f * value);
        SetRate(bokeh, 5f * value);
    }

    private static void SetRate(ParticleSystem system, float rate)
    {
        if (system == null) return;
        var emission = system.emission;
        emission.rateOverTime = rate;
    }

    private void LateUpdate()
    {
        if (follow == null) return;
        var flatForward = Vector3.ProjectOnPlane(follow.forward, Vector3.up);
        if (flatForward.sqrMagnitude < .01f) flatForward = Vector3.forward;
        transform.SetPositionAndRotation(follow.position, Quaternion.LookRotation(flatForward.normalized, Vector3.up));
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
