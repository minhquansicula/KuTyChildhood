using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Dense rice in small culled tiles, without a GameObject for each plant.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RuralRiceFieldRenderer : MonoBehaviour
{
    public Mesh nearMesh, middleMesh, farMesh;
    public Material sharedMaterial;
    public Transform player;
    public Vector2 fieldSize = new Vector2(12, 12);
    [Min(.35f)] public float spacing = .78f;
    [Range(.3f, 1f)] public float plantScale = .62f;
    public int seed = 3109;
    public float nearDistance = 6f, middleDistance = 25f, farDistance = 180f;
    public int PlantCount { get; private set; }
    public int TileCount => tiles.Count;
    public int LastDrawCalls { get; private set; }
    public int LastVisiblePlants { get; private set; }
    private readonly List<Tile> tiles = new List<Tile>();
    private readonly Plane[] planes = new Plane[6];
    private readonly Matrix4x4[][] lodBuffers = { new Matrix4x4[256], new Matrix4x4[256], new Matrix4x4[256] };
    private readonly int[] counts = new int[3];
    private Matrix4x4 generatedTransform;
    private sealed class Tile
    {
        public Bounds bounds;
        public readonly List<Vector3> positions = new List<Vector3>();
        public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
    }

    private void OnEnable()
    {
        Regenerate();
        RenderPipelineManager.beginCameraRendering += Render;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= Render;
    }
    private void OnValidate() { Regenerate(); }

    public void Regenerate()
    {
        tiles.Clear(); PlantCount = 0;
        generatedTransform = transform.localToWorldMatrix;
        if (fieldSize.x <= 0 || fieldSize.y <= 0 || spacing < .35f) return;
        var random = new System.Random(seed);
        var byCell = new Dictionary<Vector2Int, Tile>();
        var columns = Mathf.Max(1, Mathf.FloorToInt((fieldSize.x - 1.1f) / spacing));
        var rows = Mathf.Max(1, Mathf.FloorToInt((fieldSize.y - 1.1f) / spacing));
        for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
        {
            var key = new Vector2Int(column / 8, row / 8);
            if (!byCell.TryGetValue(key, out var tile))
            {
                tile = new Tile(); byCell.Add(key, tile); tiles.Add(tile);
            }
            var local = new Vector3((column - (columns - 1) * .5f) * spacing, .012f,
                (row - (rows - 1) * .5f) * spacing);
            local.x += ((float)random.NextDouble() - .5f) * .14f;
            local.z += ((float)random.NextDouble() - .5f) * .14f;
            var position = transform.TransformPoint(local);
            var scale = plantScale * (.92f + (float)random.NextDouble() * .16f);
            var rotation = transform.rotation * Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
            tile.positions.Add(position);
            tile.matrices.Add(Matrix4x4.TRS(position, rotation, Vector3.one * scale));
            // Include the strongest gust plus player bending so moving tips do not get culled.
            var plantBounds = new Bounds(position + Vector3.up * .7f, new Vector3(3.4f, 2, 3.4f));
            if (tile.positions.Count == 1) tile.bounds = plantBounds; else tile.bounds.Encapsulate(plantBounds);
            PlantCount++;
        }
    }

    private void Render(ScriptableRenderContext context, Camera camera)
    {
        if (!isActiveAndEnabled || !sharedMaterial || !nearMesh || !middleMesh || !farMesh ||
            (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)) return;
        if (generatedTransform != transform.localToWorldMatrix) Regenerate();
        if (!SystemInfo.supportsInstancing) return;
        GeometryUtility.CalculateFrustumPlanes(camera, planes);
        LastDrawCalls = 0; LastVisiblePlants = 0;
        Shader.SetGlobalVector("_VillageRiceInteractor", player ? new Vector4(player.position.x, player.position.y,
            player.position.z, 1.15f) : new Vector4(0, 0, 0, 0));
        var eye = camera.transform.position;
        var nearSquared = nearDistance * nearDistance;
        var middleSquared = middleDistance * middleDistance;
        var farSquared = farDistance * farDistance;
        foreach (var tile in tiles)
        {
            var groundEye = new Vector3(eye.x, tile.bounds.center.y, eye.z);
            if (!GeometryUtility.TestPlanesAABB(planes, tile.bounds) || tile.bounds.SqrDistance(groundEye) > farSquared) continue;
            counts[0] = counts[1] = counts[2] = 0;
            for (var index = 0; index < tile.positions.Count; index++)
            {
                // Ground-plane distance keeps aerial and first-person LOD decisions consistent.
                var delta = tile.positions[index] - eye; delta.y = 0;
                var distanceSquared = delta.sqrMagnitude;
                if (distanceSquared > farSquared) continue;
                var lod = distanceSquared < nearSquared ? 0 : distanceSquared < middleSquared ? 1 : 2;
                lodBuffers[lod][counts[lod]++] = tile.matrices[index];
            }
            for (var lod = 0; lod < 3; lod++)
            {
                if (counts[lod] == 0) continue;
                var parameters = new RenderParams(sharedMaterial)
                {
                    camera = camera, layer = gameObject.layer, worldBounds = tile.bounds,
                    shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true,
                    lightProbeUsage = LightProbeUsage.Off
                };
                Graphics.RenderMeshInstanced(parameters, lod == 0 ? nearMesh : lod == 1 ? middleMesh : farMesh,
                    0, lodBuffers[lod], counts[lod]);
                LastDrawCalls++; LastVisiblePlants += counts[lod];
            }
        }
    }
}
