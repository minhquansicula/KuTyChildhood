using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum InteractionVfxKind { Dust, Steam, Droplet, Ember, Grain, Bubble }

/// <summary>Small material-specific particle presets shared by office and household interactions.</summary>
public static class InteractionVfx
{
    private static readonly Dictionary<InteractionVfxKind, Material> Materials = new Dictionary<InteractionVfxKind, Material>();
    private static readonly Dictionary<InteractionVfxKind, Texture2D> Masks = new Dictionary<InteractionVfxKind, Texture2D>();
    private static readonly Dictionary<InteractionVfxKind, Sprite> Sprites = new Dictionary<InteractionVfxKind, Sprite>();

    public static ParticleSystem Create(Transform parent, string name, InteractionVfxKind kind)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        bool steam = kind == InteractionVfxKind.Steam;
        bool water = kind == InteractionVfxKind.Droplet;
        bool ember = kind == InteractionVfxKind.Ember;
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 3;
        main.maxParticles = steam ? 28 : 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.Automatic;
        main.startLifetime = new ParticleSystem.MinMaxCurve(steam ? 1.3f : water ? .14f : .35f,
            steam ? 2.3f : water ? .24f : .85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.015f, steam ? .08f : .11f);
        main.startSize = new ParticleSystem.MinMaxCurve(steam ? .065f : water ? .006f : .004f,
            steam ? .12f : water ? .013f : kind == InteractionVfxKind.Bubble ? .024f : .011f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = steam ? new Color(.94f, .94f, .89f, .28f) : water ? new Color(.75f, .88f, .91f, .6f) :
            ember ? new Color(1f, .47f, .13f, .8f) : kind == InteractionVfxKind.Grain ? new Color(.88f, .82f, .66f, .65f) :
            kind == InteractionVfxKind.Bubble ? new Color(.88f, .96f, .96f, .35f) : new Color(.78f, .73f, .60f, .24f);
        main.gravityModifier = water ? .6f : kind == InteractionVfxKind.Grain ? .35f : 0;
        var emission = system.emission;
        emission.rateOverTime = 0;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = steam ? .025f : water ? .006f : .045f;
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-.025f, .025f);
        velocity.z = new ParticleSystem.MinMaxCurve(-.02f, .02f);
        velocity.y = new ParticleSystem.MinMaxCurve(steam ? .13f : water ? -.35f : .03f,
            steam ? .22f : water ? -.27f : ember ? .35f : .09f);
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, steam ? .25f : .08f),
                new GradientAlphaKey(.65f, .55f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, steam ?
            new AnimationCurve(new Keyframe(0, .45f), new Keyframe(.6f, 1.1f), new Keyframe(1, 1.65f)) :
            new AnimationCurve(new Keyframe(0, .6f), new Keyframe(.15f, 1), new Keyframe(1, .35f)));
        if (steam || kind == InteractionVfxKind.Dust)
        {
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = steam ? .035f : .012f;
            noise.frequency = .6f;
            noise.scrollSpeed = .15f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = MaterialFor(kind);
        renderer.renderMode = water ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = 1.5f;
        renderer.velocityScale = .025f;
        renderer.maxParticleSize = steam ? .10f : .035f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        return system;
    }

    public static void Rate(ParticleSystem system, float value)
    { if (system) { var emission = system.emission; emission.rateOverTime = Mathf.Max(0, value); } }

    public static Material MaterialFor(InteractionVfxKind kind)
    {
        if (Materials.TryGetValue(kind, out var cached) && cached) return cached;
        var shader = Resources.Load<Shader>("Effects/MemoryParticle");
        if (!shader) throw new System.InvalidOperationException("The bundled MemoryParticle shader is missing.");
        var material = new Material(shader) { name = "FX " + kind, hideFlags = HideFlags.DontSave };
        material.SetTexture("_BaseMap", MaskFor(kind));
        material.SetFloat("_Brightness", kind == InteractionVfxKind.Ember ? 1.65f : 1f);
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        material.SetFloat("_SoftFade", pipeline && pipeline.supportsCameraDepthTexture ? .065f : 0);
        Materials[kind] = material;
        return material;
    }

    public static Sprite SpriteFor(InteractionVfxKind kind)
    {
        if (Sprites.TryGetValue(kind, out var cached) && cached) return cached;
        var texture = MaskFor(kind);
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100);
        sprite.name = "Soft " + kind; sprite.hideFlags = HideFlags.DontSave;
        Sprites[kind] = sprite;
        return sprite;
    }

    public static Texture2D MaskFor(InteractionVfxKind kind)
    {
        if (Masks.TryGetValue(kind, out var cached) && cached) return cached;
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        { name = "Soft mask " + kind, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
            float radius = Mathf.Sqrt(u * u + v * v);
            float edge = 1 - Mathf.SmoothStep(.72f, 1f, radius);
            float alpha;
            if (kind == InteractionVfxKind.Steam)
            {
                float noise = Mathf.PerlinNoise(u * 2.2f + 3.17f, v * 2.2f + 8.31f);
                alpha = Mathf.Exp(-(u * u + v * v) * 3.2f) * edge * Mathf.Lerp(.35f, 1, noise);
            }
            else if (kind == InteractionVfxKind.Bubble)
            {
                float ring = Mathf.Exp(-Mathf.Pow((radius - .62f) * 17, 2)) * .55f;
                float glint = Mathf.Exp(-((u + .30f) * (u + .30f) + (v - .34f) * (v - .34f)) * 80);
                alpha = (ring + glint) * edge;
            }
            else if (kind == InteractionVfxKind.Grain)
                alpha = (1 - Mathf.SmoothStep(.45f, .7f, Mathf.Sqrt(u * u * 3.5f + v * v))) * edge;
            else alpha = Mathf.Exp(-(u * u + v * v) * (kind == InteractionVfxKind.Droplet ? 6 : 9)) * edge;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Masks[kind] = texture;
        return texture;
    }
}
