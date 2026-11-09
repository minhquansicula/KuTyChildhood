using System.Collections.Generic;
using UnityEngine;

/// <summary>Generic-rig sole clearance during blends. Moves only the runtime visual, never the actor/collider or source rig.</summary>
[DefaultExecutionOrder(50), DisallowMultipleComponent]
public sealed class Scene2MotherFootClearance : MonoBehaviour
{
    private struct Probe { public Vector3 position; public BoneWeight weights; }
    private sealed class Skin
    {
        public Transform[] bones;
        public Matrix4x4[] bind, matrices;
        public readonly List<Probe> probes = new List<Probe>();
    }
    private readonly List<Skin> skins = new List<Skin>();
    private Transform model;
    private Vector3 modelRest;
    private float soleOffset;
    public float Lift { get; private set; }
    public int ProbeCount { get; private set; }

    public void Configure(Animator animator)
    {
        skins.Clear(); ProbeCount = 0;
        model = animator.transform; modelRest = model.localPosition;
        var renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
        float floor = float.PositiveInfinity;
        foreach (var renderer in renderers)
        {
            var baked = new Mesh(); renderer.BakeMesh(baked);
            foreach (var vertex in baked.vertices) floor = Mathf.Min(floor, renderer.transform.TransformPoint(vertex).y);
            Destroy(baked);
        }
        if (float.IsInfinity(floor)) return;
        soleOffset = floor - transform.position.y;
        foreach (var renderer in renderers)
        {
            var mesh = renderer.sharedMesh;
            if (!mesh || !mesh.isReadable) continue; // Source MomUser is already readable; never reimport other assets.
            var skin = new Skin { bones = renderer.bones, bind = mesh.bindposes, matrices = new Matrix4x4[renderer.bones.Length] };
            var vertices = mesh.vertices; var weights = mesh.boneWeights;
            if (weights.Length != vertices.Length) continue;
            for (int i = 0; i < skin.bones.Length; i++) skin.matrices[i] = skin.bones[i].localToWorldMatrix * skin.bind[i];
            for (int i = 0; i < vertices.Length; i++)
            {
                var probe = new Probe { position = vertices[i], weights = weights[i] };
                // Some sandal vertices blend ankle and lower-leg weights. Select by
                // sole geometry, not an arbitrary >50% Foot bone-weight cutoff.
                if (WorldPoint(skin, probe).y < floor + .012f) skin.probes.Add(probe);
            }
            if (skin.probes.Count > 0) { skins.Add(skin); ProbeCount += skin.probes.Count; }
        }
    }
    private static Vector3 WorldPoint(Skin skin, Probe probe)
    {
        var w = probe.weights; var p = probe.position;
        return skin.matrices[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + skin.matrices[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
            skin.matrices[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + skin.matrices[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3;
    }
    public void CorrectNow()
    {
        if (!model || skins.Count == 0) return;
        model.localPosition = modelRest;
        float floor = float.PositiveInfinity;
        foreach (var skin in skins)
        {
            for (int i = 0; i < skin.bones.Length; i++) skin.matrices[i] = skin.bones[i].localToWorldMatrix * skin.bind[i];
            foreach (var probe in skin.probes) floor = Mathf.Min(floor, WorldPoint(skin, probe).y);
        }
        Lift = Mathf.Clamp(transform.position.y + soleOffset - floor, 0, .025f);
        model.position += Vector3.up * Lift;
    }
    private void LateUpdate() => CorrectNow();
    private void OnDisable() { if (model) model.localPosition = modelRest; Lift = 0; }
}
