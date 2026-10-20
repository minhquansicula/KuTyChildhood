using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Authored grass positions in the playable village, culled in tiles and drawn in shared GPU batches.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class VillageGroundCoverRenderer : MonoBehaviour
{
    [Serializable] public struct Prototype { public string name; public Mesh near, middle, far; public Material[] materials; public float maxDistance; }
    [Serializable] public struct Plant { public Vector3 position; public float yaw, scale; public int prototype; public bool paddy; }
    public Prototype[] prototypes=Array.Empty<Prototype>();
    public Plant[] plants=Array.Empty<Plant>();
    public float nearDistance=10, middleDistance=24, farDistance=48;
    public int LastVisiblePlants {get; private set;}
    public int LastDrawCalls {get; private set;}
    private sealed class Tile { public Bounds bounds; public readonly List<int> indices=new List<int>(); }
    private readonly List<Tile> tiles=new List<Tile>();
    private readonly Plane[] planes=new Plane[6];
    private Matrix4x4[] transforms;
    private Bounds[] plantBounds;
    private Matrix4x4[][] buffers;
    private int[] counts;
    private Bounds[] batchBounds;
    private void OnEnable() {Rebuild();RenderPipelineManager.beginCameraRendering+=Render;}
    private void OnDisable() {RenderPipelineManager.beginCameraRendering-=Render;}
    private void OnValidate() {Rebuild();}
    public void Rebuild()
    {
        tiles.Clear();transforms=new Matrix4x4[plants.Length];plantBounds=new Bounds[plants.Length];buffers=new Matrix4x4[prototypes.Length*3][];
        counts=new int[buffers.Length];batchBounds=new Bounds[buffers.Length];
        for(int i=0;i<buffers.Length;i++)buffers[i]=new Matrix4x4[1023];
        var cells=new Dictionary<Vector2Int,Tile>();
        for(int i=0;i<plants.Length;i++)
        {
            var p=plants[i];transforms[i]=Matrix4x4.TRS(p.position,Quaternion.Euler(0,p.yaw,0),Vector3.one*p.scale);
            var local=p.prototype>=0 && p.prototype<prototypes.Length && prototypes[p.prototype].near?prototypes[p.prototype].near.bounds:new Bounds(Vector3.up*.3f,Vector3.one);
            float radius=local.extents.magnitude*p.scale+.25f;plantBounds[i]=new Bounds(transforms[i].MultiplyPoint3x4(local.center),Vector3.one*radius*2);
            var key=new Vector2Int(Mathf.FloorToInt(p.position.x/12),Mathf.FloorToInt(p.position.z/12));
            if(!cells.TryGetValue(key,out var tile)) {tile=new Tile{bounds=plantBounds[i]};cells.Add(key,tile);tiles.Add(tile);}
            else tile.bounds.Encapsulate(plantBounds[i]);
            tile.indices.Add(i);
        }
    }
    private void Render(ScriptableRenderContext context,Camera camera)
    {
        if(!isActiveAndEnabled || !SystemInfo.supportsInstancing || prototypes.Length==0 ||
            (camera.cameraType!=CameraType.Game && camera.cameraType!=CameraType.SceneView))return;
        if(transforms==null || transforms.Length!=plants.Length || buffers.Length!=prototypes.Length*3)Rebuild();
        GeometryUtility.CalculateFrustumPlanes(camera,planes);Array.Clear(counts,0,counts.Length);
        LastVisiblePlants=LastDrawCalls=0;var eye=camera.transform.position;
        float near=nearDistance*nearDistance,middle=middleDistance*middleDistance,maxDistance=farDistance;
        foreach(var prototype in prototypes)maxDistance=Mathf.Max(maxDistance,prototype.maxDistance);
        float far=maxDistance*maxDistance;
        foreach(var tile in tiles)
        {
            if(!GeometryUtility.TestPlanesAABB(planes,tile.bounds) || tile.bounds.SqrDistance(new Vector3(eye.x,tile.bounds.center.y,eye.z))>far)continue;
            foreach(int index in tile.indices)
            {
                var p=plants[index];if(p.prototype<0 || p.prototype>=prototypes.Length)continue;
                float limit=prototypes[p.prototype].maxDistance>0?prototypes[p.prototype].maxDistance:farDistance;
                var delta=p.position-eye;delta.y=0;float d=delta.sqrMagnitude;if(d>limit*limit)continue;
                int batch=p.prototype*3+(d<near?0:d<middle?1:2);
                if(counts[batch]==1023) {Draw(camera,batch);counts[batch]=0;}
                var bounds=plantBounds[index];
                if(counts[batch]==0)batchBounds[batch]=bounds;else batchBounds[batch].Encapsulate(bounds);
                buffers[batch][counts[batch]++]=transforms[index];LastVisiblePlants++;
            }
        }
        for(int batch=0;batch<counts.Length;batch++)if(counts[batch]>0)Draw(camera,batch);
    }
    private void Draw(Camera camera,int batch)
    {
        var prototype=prototypes[batch/3];int lod=batch%3;var mesh=lod==0?prototype.near:lod==1?prototype.middle:prototype.far;
        if(!mesh || prototype.materials==null)return;
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            var material=prototype.materials[Mathf.Min(sub,prototype.materials.Length-1)];if(!material)continue;
            var parameters=new RenderParams(material){camera=camera,layer=gameObject.layer,worldBounds=batchBounds[batch],
                shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,lightProbeUsage=LightProbeUsage.Off};
            Graphics.RenderMeshInstanced(parameters,mesh,sub,buffers[batch],counts[batch]);LastDrawCalls++;
        }
    }
}
