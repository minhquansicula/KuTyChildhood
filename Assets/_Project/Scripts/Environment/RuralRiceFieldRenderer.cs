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
    public bool meanderingField;
    [Range(0,.2f)] public float plantingDepth;
    public float nearDistance = 6f, middleDistance = 25f, farDistance = 180f;
    [Header("Organic field placement (canonical world XZ)")]
    public Vector2[] fieldPolygon=Array.Empty<Vector2>();
    public int targetInstances;
    public float normalizedHeight=.8f, soilHeight=-.22f, rootBurial=.05f, randomTilt=.1f;
    public bool regionalGpuBatch;
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
        if(fieldPolygon!=null && fieldPolygon.Length>=3 && targetInstances>0){GeneratePolygon();return;}
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
            if (meanderingField) position = VillageLandscapeShape.Warp(position);
            var scale = plantScale * (.92f + (float)random.NextDouble() * .16f);
            // Imported pivots need not sit at the lowest root. Anchor the actual mesh base in soil.
            if(plantingDepth>0) position.y-=plantingDepth+(nearMesh?nearMesh.bounds.min.y*scale:0);
            var rotation = transform.rotation * Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
            tile.positions.Add(position);
            tile.matrices.Add(Matrix4x4.TRS(position, rotation, Vector3.one * scale));
            // Include the strongest gust plus player bending so moving tips do not get culled.
            var plantBounds = new Bounds(position + Vector3.up * .7f, new Vector3(3.4f, 2, 3.4f));
            if (tile.positions.Count == 1) tile.bounds = plantBounds; else tile.bounds.Encapsulate(plantBounds);
            PlantCount++;
        }
    }

    public void CopyInstances(List<Matrix4x4> output) {foreach(var tile in tiles)output.AddRange(tile.matrices);}
    public Bounds PlacementBounds
    {
        get {if(tiles.Count==0)return new Bounds(transform.position,Vector3.zero);var b=tiles[0].bounds;foreach(var tile in tiles)b.Encapsulate(tile.bounds);return b;}
    }
    private static float Halton(int index,int basis)
    {float fraction=1,result=0;while(index>0){fraction/=basis;result+=fraction*(index%basis);index/=basis;}return result;}
    public static bool InsidePolygon(Vector2 point,Vector2[] polygon,float margin=0)
    {
        bool inside=false;float closest=float.MaxValue;
        for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        {
            var a=polygon[j];var b=polygon[i];var edge=b-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,edge)/Mathf.Max(edge.sqrMagnitude,.00001f));
            closest=Mathf.Min(closest,(point-a-edge*t).sqrMagnitude);
            if((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
        }
        return inside && closest>=margin*margin;
    }
    private void GeneratePolygon()
    {
        if(!nearMesh)return;float xmin=float.MaxValue,xmax=float.MinValue,zmin=float.MaxValue,zmax=float.MinValue;
        foreach(var p in fieldPolygon){xmin=Mathf.Min(xmin,p.x);xmax=Mathf.Max(xmax,p.x);zmin=Mathf.Min(zmin,p.y);zmax=Mathf.Max(zmax,p.y);}
        var random=new System.Random(seed);var cells=new Dictionary<Vector2Int,Tile>();var source=nearMesh.bounds;
        for(int attempt=1;PlantCount<targetInstances && attempt<=targetInstances*15;attempt++)
        {
            var p=new Vector2(Mathf.Lerp(xmin,xmax,Halton(attempt+seed%997,2)),Mathf.Lerp(zmin,zmax,Halton(attempt+seed%997,3)));
            p+=new Vector2((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*.12f;
            if(!InsidePolygon(p,fieldPolygon,.6f))continue;
            float yaw=(float)random.NextDouble()*360,rx=((float)random.NextDouble()*2-1)*randomTilt*Mathf.Rad2Deg,rz=((float)random.NextDouble()*2-1)*randomTilt*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(rx,yaw,rz);var basis=Matrix4x4.Rotate(rotation);
            var extents=source.extents;var rotatedExtents=new Vector3(
                Mathf.Abs(basis.m00)*extents.x+Mathf.Abs(basis.m01)*extents.y+Mathf.Abs(basis.m02)*extents.z,
                Mathf.Abs(basis.m10)*extents.x+Mathf.Abs(basis.m11)*extents.y+Mathf.Abs(basis.m12)*extents.z,
                Mathf.Abs(basis.m20)*extents.x+Mathf.Abs(basis.m21)*extents.y+Mathf.Abs(basis.m22)*extents.z);
            float height=Mathf.Clamp(normalizedHeight*(.85f+(float)random.NextDouble()*.30f),.7f,.9f);
            float scale=height/Mathf.Max(.001f,rotatedExtents.y*2);
            var transformedCenter=rotation*(source.center*scale);var position=new Vector3(p.x,0,p.y);
            if(meanderingField)position=VillageLandscapeShape.Warp(position);
            float ground=soilHeight+.007f*Mathf.Sin(p.x*3.1f+p.y*1.7f);
            position.y=ground-rootBurial-(transformedCenter.y-rotatedExtents.y*scale);
            var key=new Vector2Int(Mathf.FloorToInt(position.x/12),Mathf.FloorToInt(position.z/12));
            var bounds=new Bounds(position+transformedCenter,rotatedExtents*scale*2+new Vector3(.9f,.25f,.9f));
            if(!cells.TryGetValue(key,out var tile)){tile=new Tile{bounds=bounds};cells.Add(key,tile);tiles.Add(tile);}else tile.bounds.Encapsulate(bounds);
            tile.positions.Add(position);tile.matrices.Add(Matrix4x4.TRS(position,rotation,Vector3.one*scale));PlantCount++;
        }
    }

    private void Render(ScriptableRenderContext context, Camera camera)
    {
        if (regionalGpuBatch || !isActiveAndEnabled || !sharedMaterial || !nearMesh || !middleMesh || !farMesh ||
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
