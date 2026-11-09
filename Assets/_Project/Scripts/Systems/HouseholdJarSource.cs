using UnityEngine;

/// <summary>Non-destructive opening of an existing, fused closed jar mesh for its chore.</summary>
public sealed class HouseholdJarSource : MonoBehaviour
{
    public MeshRenderer original;
    public Mesh openBody, openedLid;
    public Vector3 mouthLocal;
    public float openingRadius;
    public bool IsReady => original && openBody && openingRadius > .03f;
    public Vector3 Mouth => original.transform.TransformPoint(mouthLocal);
}
