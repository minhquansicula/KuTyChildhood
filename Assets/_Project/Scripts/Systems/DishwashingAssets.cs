using UnityEngine;

/// <summary>Engine-verified art and surface contacts, independent of story/save data.</summary>
[CreateAssetMenu(menuName = "KuTy/Dishwashing assets")]
public sealed class DishwashingAssets : ScriptableObject
{
    public GameObject bowl, plate, basin, sponge, soap, rack;
    public Vector3[] stainPoints = new Vector3[6];
    public Vector3[] stainNormals = new Vector3[6];
    public Vector3 bowlSize;
    public bool IsReady => bowl && basin && sponge && soap && rack &&
        stainPoints != null && stainPoints.Length == 6 && stainNormals != null && stainNormals.Length == 6;
}
