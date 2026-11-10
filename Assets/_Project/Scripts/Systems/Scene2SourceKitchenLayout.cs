using UnityEngine;

/// <summary>Provenance for the articulated copies of the scene's original kitchen models.</summary>
public sealed class Scene2SourceKitchenLayout : MonoBehaviour
{
    public Mesh originalStove, originalRiceStove;
    public Material stoveMaterial, riceMaterial;
    public Transform formerFireRoot, formerCookRoot;
    public Renderer fireVisual, cookingBody, cookingRice;
    public bool IsReady => originalStove && originalRiceStove && fireVisual && cookingBody && cookingRice;
}
