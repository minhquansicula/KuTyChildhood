using UnityEngine;

/// <summary>Keeps the imported letter resting on the specific table mesh in Scene 2.</summary>
[ExecuteAlways]
public sealed class Scene2TabletopLetter : MonoBehaviour
{
    [SerializeField] private MeshCollider table;
    [SerializeField] private Transform letterModel;
    [SerializeField, Min(0.05f)] private float width = 0.4f;
    private bool placed;
    private Vector3 lastTableCenter;
    private Vector3 lastTableSize;

    private void OnEnable() => placed = false;
    private void OnValidate() => placed = false;
    private void Update()
    {
        if (table == null) return;
        Bounds current = table.bounds;
        if (placed && current.center == lastTableCenter && current.size == lastTableSize) return;
        placed = Place();
        if (!placed) return;
        lastTableCenter = current.center;
        lastTableSize = current.size;
    }

    private bool Place()
    {
        if (table == null || letterModel == null || !table.enabled) return false;

        Bounds surface = table.bounds;
        if (surface.size.sqrMagnitude < 0.001f) return false;

        transform.position = new Vector3(surface.center.x, surface.max.y + 0.01f, surface.center.z);
        transform.rotation = Quaternion.identity;
        letterModel.localPosition = Vector3.zero;
        letterModel.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        letterModel.localScale = Vector3.one;

        Renderer[] renderers = letterModel.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return false;

        Bounds letter = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) letter.Encapsulate(renderers[i].bounds);
        float currentWidth = Mathf.Max(letter.size.x, letter.size.z);
        if (currentWidth < 0.001f) return false;

        float maxWidth = Mathf.Min(width, surface.size.x * 0.4f, surface.size.z * 0.4f);
        letterModel.localScale = Vector3.one * (maxWidth / currentWidth);

        letter = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) letter.Encapsulate(renderers[i].bounds);
        transform.position += Vector3.up * (surface.max.y + 0.01f - letter.min.y);
        return true;
    }
}
