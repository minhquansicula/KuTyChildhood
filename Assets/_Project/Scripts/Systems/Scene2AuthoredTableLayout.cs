using UnityEngine;

/// <summary>Existing dining table and existing dish meshes; never builds a procedural workstand.</summary>
public sealed class Scene2AuthoredTableLayout : MonoBehaviour
{
    public Renderer table;
    public Transform[] dishes = new Transform[3];
    public Vector3[] sourceLocal = new Vector3[3], targetLocal = new Vector3[3];
    public bool useFoodAssets;
    public string[] labels = new string[3];
    public bool IsReady => table && dishes.Length == 3 && dishes[0] && dishes[1] && dishes[2];
}
