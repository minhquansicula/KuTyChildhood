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
    private Vector4[][] fadeBuffers;
    private bool[] smoothPrototypes;
    private MaterialPropertyBlock fadeProperties;
    private static readonly int FadeRangeId=Shader.PropertyToID("_BambooLodRange");
    private sealed class CameraLods
    {
        public int[] targets;
        public Vector3[] weights;
        public double time;
    }
    private readonly Dictionary<Camera,CameraLods> cameraLods=new Dictionary<Camera,CameraLods>();
    private const float BambooHysteresis=.75f,BambooFadeSeconds=.4f;
    private void OnEnable() {Rebuild();RenderPipelineManager.beginCameraRendering+=Render;}
    private void OnDisable() {RenderPipelineManager.beginCameraRendering-=Render;cameraLods?.Clear();}
    private void OnValidate() {Rebuild();}
    public void Rebuild()
    {
        tiles.Clear();transforms=new Matrix4x4[plants.Length];plantBounds=new Bounds[plants.Length];buffers=new Matrix4x4[prototypes.Length*3][];
        counts=new int[buffers.Length];batchBounds=new Bounds[buffers.Length];
        cameraLods.Clear();fadeBuffers=new Vector4[buffers.Length][];smoothPrototypes=new bool[prototypes.Length];
        for(int i=0;i<prototypes.Length;i++)
        {
            var materials=prototypes[i].materials;
            smoothPrototypes[i]=materials!=null && materials.Length>0;
            if(materials!=null)foreach(var material in materials)
                smoothPrototypes[i]&=material && material.HasProperty(FadeRangeId);
        }
        for(int i=0;i<buffers.Length;i++){buffers[i]=new Matrix4x4[1023];fadeBuffers[i]=new Vector4[1023];}
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
        // State belongs to each camera, so Scene View cannot interrupt Game View.
        if(!cameraLods.TryGetValue(camera,out var state))
        {
            if(cameraLods.Count>=8)cameraLods.Clear();
            state=new CameraLods{targets=new int[plants.Length],weights=new Vector3[plants.Length],time=Time.realtimeSinceStartupAsDouble};
            for(int i=0;i<state.targets.Length;i++)state.targets[i]=-1;
            cameraLods.Add(camera,state);
        }
        double now=Time.realtimeSinceStartupAsDouble;
        float deltaTime=Mathf.Clamp((float)(now-state.time),0,.1f);state.time=now;
        foreach(var tile in tiles)
        {
            if(!GeometryUtility.TestPlanesAABB(planes,tile.bounds) || tile.bounds.SqrDistance(new Vector3(eye.x,tile.bounds.center.y,eye.z))>far)continue;
            foreach(int index in tile.indices)
            {
                var p=plants[index];if(p.prototype<0 || p.prototype>=prototypes.Length)continue;
                float limit=prototypes[p.prototype].maxDistance>0?prototypes[p.prototype].maxDistance:farDistance;
                var delta=p.position-eye;delta.y=0;float d=delta.sqrMagnitude;if(d>limit*limit)continue;
                if(smoothPrototypes[p.prototype])
                {
                    int target=SelectBambooLod(Mathf.Sqrt(d),state.targets[index],nearDistance,middleDistance);
                    var weights=state.targets[index]<0?LodUnit(target):AdvanceBambooWeights(state.weights[index],target,deltaTime);
                    state.targets[index]=target;state.weights[index]=weights;
                    float start=0;
                    for(int lod=0;lod<3;lod++)
                    {
                        float end=start+weights[lod];
                        if(weights[lod]>0)Queue(camera,index,p.prototype*3+lod,new Vector4(start,lod==2?1:end,0,0));
                        start=end;
                    }
                }
                else Queue(camera,index,p.prototype*3+(d<near?0:d<middle?1:2),new Vector4(0,1,0,0));
                LastVisiblePlants++;
            }
        }
        for(int batch=0;batch<counts.Length;batch++)if(counts[batch]>0)Draw(camera,batch);
    }
    private static Vector3 LodUnit(int lod)=>lod==0?Vector3.right:lod==1?Vector3.up:Vector3.forward;
    private static Vector3 AdvanceBambooWeights(Vector3 weights,int target,float deltaTime)
    {
        var next=Vector3.MoveTowards(weights,LodUnit(target),Mathf.Max(0,deltaTime)*1.414214f/BambooFadeSeconds);
        return next/(next.x+next.y+next.z);
    }
    private static int SelectBambooLod(float distance,int previous,float near,float middle)
    {
        if(previous<0)return distance<near?0:distance<middle?1:2;
        // Clamp the dead band for projects with unusually close thresholds.
        float h=Mathf.Min(BambooHysteresis,Mathf.Max(0,(middle-near)*.25f),Mathf.Max(0,near*.25f));
        int lod=previous;
        while(lod<2 && distance>(lod==0?near:middle)+h)lod++;
        while(lod>0 && distance<(lod==1?near:middle)-h)lod--;
        return lod;
    }
    private void Queue(Camera camera,int index,int batch,Vector4 fade)
    {
        if(counts[batch]==1023){Draw(camera,batch);counts[batch]=0;}
        if(counts[batch]==0)batchBounds[batch]=plantBounds[index];else batchBounds[batch].Encapsulate(plantBounds[index]);
        int slot=counts[batch]++;buffers[batch][slot]=transforms[index];fadeBuffers[batch][slot]=fade;
    }
    private void Draw(Camera camera,int batch)
    {
        var prototype=prototypes[batch/3];int lod=batch%3;var mesh=lod==0?prototype.near:lod==1?prototype.middle:prototype.far;
        if(!mesh || prototype.materials==null || prototype.materials.Length==0)return;
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            var material=prototype.materials[Mathf.Min(sub,prototype.materials.Length-1)];if(!material)continue;
            var parameters=new RenderParams(material){camera=camera,layer=gameObject.layer,worldBounds=batchBounds[batch],
                shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,lightProbeUsage=LightProbeUsage.Off};
            if(smoothPrototypes[batch/3])
            {
                if(fadeProperties==null)fadeProperties=new MaterialPropertyBlock();
                fadeProperties.Clear();fadeProperties.SetVectorArray(FadeRangeId,fadeBuffers[batch]);parameters.matProps=fadeProperties;
            }
            Graphics.RenderMeshInstanced(parameters,mesh,sub,buffers[batch],counts[batch]);LastDrawCalls++;
        }
    }
}
