using UnityEngine;

/// <summary>Local per-renderer ripple feedback, with no material instancing or fluid simulation.</summary>
public sealed class HouseholdWaterSurface : MonoBehaviour
{
    private Renderer surface;
    private MaterialPropertyBlock properties;
    private Vector4 ripple = new Vector4(0, 0, -100, 0);
    private float nextRipple;
    public int RippleCount { get; private set; }
    private void Awake() { surface = GetComponent<Renderer>(); properties = new MaterialPropertyBlock(); }
    public void Disturb(Vector3 position, float strength = 1)
    {
        if (Time.time < nextRipple) return;
        nextRipple = Time.time + .35f;
        ripple = new Vector4(position.x, position.z, Time.time, Mathf.Clamp01(strength)); RippleCount++;
        surface.GetPropertyBlock(properties); properties.SetVector("_Ripple", ripple); surface.SetPropertyBlock(properties);
    }
}
