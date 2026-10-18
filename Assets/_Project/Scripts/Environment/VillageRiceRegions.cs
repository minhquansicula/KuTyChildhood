using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Four spatial rice regions; compute culls and selects LOD without per-frame CPU instance loops.</summary>
[ExecuteAlways,DisallowMultipleComponent]
public sealed class VillageRiceRegions : MonoBehaviour
{
    public RuralRiceFieldRenderer[] regions=Array.Empty<RuralRiceFieldRenderer>();
    public ComputeShader culling;
    public Transform player;
    public int LastDrawCalls {get;private set;}
    public int LastVisibleRegions {get;private set;}
    public int TotalInstances {get;private set;}
    [StructLayout(LayoutKind.Sequential)] private struct Instance {public Matrix4x4 objectToWorld,worldToObject;public Vector4 sphere;}
    private sealed class Region
    {
        public Bounds bounds;public int count;public ComputeBuffer instances;
        public bool visibleThisCamera;
        public readonly ComputeBuffer[] visible=new ComputeBuffer[3],arguments=new ComputeBuffer[3];
        public readonly MaterialPropertyBlock[] blocks=new MaterialPropertyBlock[3];
        public RuralRiceFieldRenderer source;
        public void Release(){instances?.Release();for(int i=0;i<3;i++){visible[i]?.Release();arguments[i]?.Release();}}
    }
    private readonly List<Region> batches=new List<Region>();
    private readonly Plane[] planes=new Plane[6];private readonly Vector4[] planeVectors=new Vector4[6];
    private int kernel;
    private void OnEnable(){Rebuild();RenderPipelineManager.beginCameraRendering+=Render;}
    private void OnDisable(){RenderPipelineManager.beginCameraRendering-=Render;Release();}
    private void OnValidate(){Release();}
    private void Release(){foreach(var b in batches)b.Release();batches.Clear();}
    public void Rebuild()
    {
        Release();TotalInstances=0;if(!culling || !SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing)return;
        kernel=culling.FindKernel("CullRice");
        foreach(var source in regions)
        {
            if(!source || !source.isActiveAndEnabled)continue;source.Regenerate();var matrices=new List<Matrix4x4>(source.PlantCount);source.CopyInstances(matrices);
            if(matrices.Count==0)continue;var data=new Instance[matrices.Count];var bounds=source.nearMesh.bounds;
            for(int i=0;i<data.Length;i++)
            {
                var m=matrices[i];var center=m.MultiplyPoint3x4(bounds.center);float radius=bounds.extents.magnitude*m.GetColumn(0).magnitude+.45f;
                data[i]=new Instance{objectToWorld=m,worldToObject=m.inverse,sphere=new Vector4(center.x,center.y,center.z,radius)};
            }
            var b=new Region{source=source,count=data.Length,bounds=source.PlacementBounds,instances=new ComputeBuffer(data.Length,144,ComputeBufferType.Structured)};b.instances.SetData(data);
            for(int lod=0;lod<3;lod++)
            {
                var mesh=lod==0?source.nearMesh:lod==1?source.middleMesh:source.farMesh;
                b.visible[lod]=new ComputeBuffer(data.Length,4,ComputeBufferType.Append);b.visible[lod].SetCounterValue(0);
                b.arguments[lod]=new ComputeBuffer(1,20,ComputeBufferType.IndirectArguments);
                b.arguments[lod].SetData(new uint[]{mesh.GetIndexCount(0),0,mesh.GetIndexStart(0),(uint)mesh.GetBaseVertex(0),0});
                b.blocks[lod]=new MaterialPropertyBlock();b.blocks[lod].SetBuffer("_RiceInstances",b.instances);b.blocks[lod].SetBuffer("_RiceVisible",b.visible[lod]);
            }
            batches.Add(b);TotalInstances+=data.Length;
        }
    }
    private void Render(ScriptableRenderContext context,Camera camera)
    {
        if(!isActiveAndEnabled || !culling || (camera.cameraType!=CameraType.Game && camera.cameraType!=CameraType.SceneView))return;
        if(batches.Count==0)Rebuild();if(batches.Count==0)return;
        GeometryUtility.CalculateFrustumPlanes(camera,planes);for(int i=0;i<6;i++)planeVectors[i]=new Vector4(planes[i].normal.x,planes[i].normal.y,planes[i].normal.z,planes[i].distance);
        culling.SetVectorArray("_RicePlanes",planeVectors);culling.SetVector("_RiceEye",camera.transform.position);
        Shader.SetGlobalVector("_VillageRiceInteractor",player?new Vector4(player.position.x,player.position.y,player.position.z,1.15f):Vector4.zero);
        LastDrawCalls=LastVisibleRegions=0;
        foreach(var b in batches)
        {
            b.visibleThisCamera=GeometryUtility.TestPlanesAABB(planes,b.bounds);if(!b.visibleThisCamera)continue;LastVisibleRegions++;
            var source=b.source;culling.SetInt("_RiceCount",b.count);culling.SetVector("_RiceDistances",new Vector4(source.nearDistance*source.nearDistance,source.middleDistance*source.middleDistance,source.farDistance*source.farDistance,0));
            for(int lod=0;lod<3;lod++)b.visible[lod].SetCounterValue(0);
            culling.SetBuffer(kernel,"_RiceInstances",b.instances);culling.SetBuffer(kernel,"_VisibleNear",b.visible[0]);culling.SetBuffer(kernel,"_VisibleMiddle",b.visible[1]);culling.SetBuffer(kernel,"_VisibleFar",b.visible[2]);
            culling.Dispatch(kernel,(b.count+63)/64,1,1);
            for(int lod=0;lod<3;lod++)
            {
                ComputeBuffer.CopyCount(b.visible[lod],b.arguments[lod],4);var mesh=lod==0?source.nearMesh:lod==1?source.middleMesh:source.farMesh;
                Graphics.DrawMeshInstancedIndirect(mesh,0,source.sharedMaterial,b.bounds,b.arguments[lod],0,b.blocks[lod],ShadowCastingMode.Off,true,gameObject.layer,camera,LightProbeUsage.Off);LastDrawCalls++;
            }
        }
    }
    // Editor verification only: never called from the rendering loop.
    public int ReadVisibleInstanceCount()
    {
        int total=0;var args=new uint[5];
        foreach(var b in batches)if(b.visibleThisCamera)for(int i=0;i<3;i++){b.arguments[i].GetData(args);total+=(int)args[1];}
        return total;
    }
}
