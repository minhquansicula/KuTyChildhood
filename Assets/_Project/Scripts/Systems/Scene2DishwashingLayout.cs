using System;
using UnityEngine;

/// <summary>Scene references only. The user's authored transforms are never moved by the game.</summary>
public sealed class Scene2DishwashingLayout : MonoBehaviour
{
    [Serializable]
    public sealed class Dish
    {
        public Transform source;
        public string label;
        // Surface probes in source-local coordinates, measured against the actual mesh in Editor.
        public Vector3[] points = new Vector3[6];
        public Vector3[] normals = new Vector3[6];
    }

    public Transform fixture, basin, sponge, soap, rack, faucet, outlet;
    public Dish[] dishes = new Dish[0];
    public bool IsReady => fixture && basin && sponge && soap && rack && faucet && outlet &&
        dishes != null && dishes.Length == 3 && Array.TrueForAll(dishes,
            d => d != null && d.source && d.points != null && d.points.Length == 6 && d.normals != null && d.normals.Length == 6);

    public static Bounds VisualBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.one * .01f);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
