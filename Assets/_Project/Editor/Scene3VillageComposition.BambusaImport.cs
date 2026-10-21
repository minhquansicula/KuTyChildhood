using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string BambusaModels="Assets/_Project/Art/Models/Scene3/BambooImported";
    private const string BambusaTextures="Assets/_Project/Art/Textures/Scene3/BambooImported";
    private const string BambusaWork="Tools/Scene3/BambusaImported";
    private const string BambusaMaterial="Assets/_Project/Art/Materials/Scene3/BambusaImported.mat";
    private const string BambusaMarker="BambusaImported_15m";

    [MenuItem("KuTy/Scene 3/Bamboo imported/1 Prepare source 1 preview")]
    public static void PrepareBambusaFirst()
    {
        RequireArtScene();EnsureFolder(BambusaModels);
        var material=PrepareBambusaMaterial();PrepareBambusaVariant(1,material);
        AssetDatabase.SaveAssets();CaptureBambusaVariants(new[]{1},"Source01");
    }

    private static Material PrepareBambusaMaterial()
    {
        var shader=Shader.Find("KuTy/Village/Imported Bambusa");
        if(!shader || ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new Exception("Imported Bambusa shader missing or failed to compile.");
        if(!SystemInfo.supports2DArrayTextures)throw new Exception("This device does not support texture arrays.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(BambusaMaterial);
        if(!material){material=new Material(shader){name="BambusaImported",enableInstancing=true};AssetDatabase.CreateAsset(material,BambusaMaterial);}
        foreach(string side in new[]{"Front","Back","Normal"})
        {
            string path=BambusaTextures+"/Bambusa_"+side+".asset";
            var array=AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
            if(!array)
            {
                var layers=new Texture2D[6];
                for(int slot=0;slot<6;slot++)
                {
                    string texturePath=BambusaTextures+"/"+slot+"_"+side+".png";
                    var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.textureType=TextureImporterType.Default;importer.sRGBTexture=side!="Normal";
                    importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.mipmapEnabled=true;
                    importer.mipMapsPreserveCoverage=side!="Normal";importer.alphaTestReferenceValue=.4f;
                    importer.isReadable=true;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Repeat;
                    importer.filterMode=FilterMode.Trilinear;
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.DXT5,compressionQuality=100});
                    importer.SaveAndReimport();layers[slot]=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                }
                array=new Texture2DArray(layers[0].width,layers[0].height,6,TextureFormat.DXT5,true,side=="Normal") {
                    name="Bambusa_"+side,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                for(int slot=0;slot<6;slot++)for(int mip=0;mip<layers[slot].mipmapCount;mip++)
                    array.SetPixelData(layers[slot].GetPixelData<byte>(mip),mip,slot);
                array.Apply(false,true);AssetDatabase.CreateAsset(array,path);
                for(int slot=0;slot<6;slot++)
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(BambusaTextures+"/"+slot+"_"+side+".png");
                    importer.isReadable=false;importer.SaveAndReimport();
                }
            }
            material.SetTexture(side=="Normal"?"_Normals":"_"+side,array);
        }
        EditorUtility.SetDirty(material);return material;
    }

    private static Mesh ReadBambusaMesh(int source,int lod)
    {
        string name="Bambusa_"+source.ToString("00")+"_LOD"+lod;
        string destination=BambusaModels+"/"+name+".asset";
        string sourcePath=BambusaWork+"/"+name+".asset";
        string fingerprint;using(var hash=SHA256.Create())fingerprint="BambusaExtract:"+Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(sourcePath)));
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(destination);
        if(existing && AssetImporter.GetAtPath(destination).userData==fingerprint)return existing;
        using(var reader=new BinaryReader(File.OpenRead(BambusaWork+"/"+name+".asset")))
        {
            if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="BAM1" || reader.ReadInt32()!=source || reader.ReadInt32()!=lod)
                throw new Exception("Invalid mesh file header.");
            int count=reader.ReadInt32(),triangles=reader.ReadInt32();
            var min=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            var max=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            var positions=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var colors=new Color[count];
            for(int i=0;i<count;i++)
            {
                positions[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                colors[i]=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            }
            var indices=new int[triangles*3];for(int i=0;i<indices.Length;i++)indices[i]=reader.ReadInt32();
            if(reader.BaseStream.Position!=reader.BaseStream.Length || positions.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)))
                throw new Exception("Invalid geometry payload.");
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.vertices=positions;mesh.normals=normals;mesh.uv=uv;mesh.colors=colors;
            mesh.triangles=indices;mesh.RecalculateTangents();
            var bounds=new Bounds((min+max)*.5f,max-min);bounds.Expand(.3f);mesh.bounds=bounds;
            if(existing)
            {
                existing.Clear();existing.indexFormat=mesh.indexFormat;
                existing.vertices=positions;existing.normals=normals;existing.uv=uv;existing.colors=colors;
                existing.triangles=indices;existing.tangents=mesh.tangents;existing.bounds=bounds;
                UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);mesh=existing;
            }
            else AssetDatabase.CreateAsset(mesh,destination);
            var importer=AssetImporter.GetAtPath(destination);importer.userData=fingerprint;AssetDatabase.WriteImportSettingsIfDirty(destination);
            return mesh;
        }
    }

    private static void PrepareBambusaVariant(int source,Material material)
    {
        var root=new GameObject("Bambusa_Source"+source.ToString("00"));
        try
        {
            var group=root.AddComponent<LODGroup>();var lods=new LOD[3];
            for(int lod=0;lod<3;lod++)
            {
                var child=new GameObject("LOD"+lod);child.transform.SetParent(root.transform,false);
                child.AddComponent<MeshFilter>().sharedMesh=ReadBambusaMesh(source,lod);
                var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                lods[lod]=new LOD(new[]{.35f,.13f,.025f}[lod],new Renderer[]{renderer});
            }
            group.SetLODs(lods);group.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,BambusaModels+"/"+root.name+".prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }

    [MenuItem("KuTy/Scene 3/Bamboo imported/2 Preview candidate variants")]
    public static void PreviewBambusaCandidates()
    {
        RequireArtScene();EnsureFolder(BambusaModels);var material=PrepareBambusaMaterial();
        foreach(int source in new[]{1,2,4})PrepareBambusaVariant(source,material);
        AssetDatabase.SaveAssets();CaptureBambusaVariants(new[]{1,2,4},"Variants");
    }

    [MenuItem("KuTy/Scene 3/Bamboo imported/3 Replace seven sample plants")]
    public static void InstallBambusaSample()
    {
        RequireArtScene();if(Find(BambusaMarker))throw new Exception("Imported bamboo sample already installed.");
        int[] sources={1,2,4};EnsureFolder(BambusaModels);
        var material=PrepareBambusaMaterial();foreach(int source in sources)PrepareBambusaVariant(source,material);
        var cover=BambooCover();var targetIds=cover.prototypes.Select((p,i)=>new{p,i}).Where(x=>x.p.name!=null && x.p.name.StartsWith("TreLaMem_")).Select(x=>x.i).ToArray();
        if(targetIds.Length!=3 || cover.plants.Count(p=>targetIds.Contains(p.prototype))!=7)throw new Exception("Expected exactly seven procedural sample plants across three named prototypes.");
        string backup=BambusaWork+"/before-imported.unity";
        if(!File.Exists(backup))
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),backup,true))throw new IOException("Cannot save current scene backup.");
            File.WriteAllText(BambusaWork+"/cover-before-imported.json",EditorJsonUtility.ToJson(cover,true));
            File.WriteAllText(BambusaWork+"/checkpoint-before-imported.txt",ReadArtCheckpoint());
            CaptureBambooViews("BeforeImported");
        }
        var before=GameplaySnapshot();before.Remove(cover);string checkpoint=ReadArtCheckpoint();
        var oldPlants=cover.plants.ToArray();var prototypes=cover.prototypes.ToArray();
        var sky=RenderSettings.skybox;var sun=Find("Sun").transform.rotation;
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.RecordObject(cover,"Replace seven sample bamboo plants with imported Bambusa");
        try
        {
            var next=prototypes.ToArray();var plants=oldPlants.ToArray();
            for(int variant=0;variant<3;variant++)
            {
                int id=targetIds[variant],source=sources[variant];
                next[id]=new VillageGroundCoverRenderer.Prototype{name="Bambusa_Source"+source.ToString("00"),near=ReadBambusaMesh(source,0),middle=ReadBambusaMesh(source,1),far=ReadBambusaMesh(source,2),materials=new[]{material},maxDistance=prototypes[id].maxDistance};
                foreach(int i in Enumerable.Range(0,plants.Length).Where(i=>plants[i].prototype==id))
                {
                    // Bound expansion is for wind culling, not height fitting or grounding.
                    float oldHeight=prototypes[id].near.bounds.size.y*oldPlants[i].scale;
                    float height=source==1?Mathf.Clamp(oldHeight*.65f,2.8f,3.5f):source==2?Mathf.Clamp(oldHeight*.85f,3.8f,4.8f):oldHeight;
                    var vertices=next[id].near.vertices;float bottom=vertices.Min(v=>v.y),top=vertices.Max(v=>v.y);
                    plants[i].scale=height/(top-bottom);
                    var p=plants[i].position;p.y=ReferenceHeight(p.x,p.z)-.025f-bottom*plants[i].scale;plants[i].position=p;
                }
            }
            cover.prototypes=next;cover.plants=plants;cover.Rebuild();EditorUtility.SetDirty(cover);
            Group(BambusaMarker,null);VerifyGameplay(before);
            for(int i=0;i<plants.Length;i++)
                if(!targetIds.Contains(oldPlants[i].prototype) && !oldPlants[i].Equals(plants[i]))throw new Exception("Non-sample vegetation changed.");
            if(ReadArtCheckpoint()!=checkpoint || RenderSettings.skybox!=sky || Find("Sun").transform.rotation!=sun)throw new Exception("Checkpoint/sky/light changed.");
            VerifyReferenceWalking();VerifyBambooSampleWalk();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Cannot save sample.");
            Undo.CollapseUndoOperations(undo);
        }
        catch{Undo.RevertAllDownToGroup(undo);cover.Rebuild();throw;}
        File.WriteAllText(BambusaWork+"/installation.txt","PASS: seven sample plants replaced by imported source groups 1, 2, 4.\n1,523 other plants unchanged; original grass/legacy bamboo prototypes unchanged.\nGameplay payloads verified: "+before.Count+". Checkpoint/sky/sun unchanged.\nReference crossings and sample lane passed in both directions.\nOne material, one submesh per LOD, shared compressed front/back/normal texture arrays.\n");
    }

    [MenuItem("KuTy/Scene 3/Bamboo imported/4 Capture installed sample")]
    public static void CaptureInstalledBambusa()
    {
        RequireArtScene();if(!Find(BambusaMarker))throw new Exception("Imported sample not installed.");
        ValidateInstalledBambusa();
        VerifyReferenceWalking();VerifyBambooSampleWalk();CaptureBambooViews("AfterImported");
        CaptureBambusaVariants(new[]{1,2,4},"Variants");
    }

    private static void ValidateInstalledBambusa()
    {
        if(Find(BambusaVillageMarker)){ValidateWholeVillageBamboo();return;}
        var cover=BambooCover();
        var baseline=JsonUtility.FromJson<BambooBaselineEnvelope>(File.ReadAllText(BambusaWork+"/cover-before-imported.json")).MonoBehaviour;
        if(cover.plants.Length!=baseline.plants.Length || cover.nearDistance!=baseline.nearDistance || cover.middleDistance!=baseline.middleDistance || cover.farDistance!=baseline.farDistance)
            throw new Exception("Plant count or shared LOD distances changed.");
        var report=new StringBuilder();int replaced=0;
        for(int i=0;i<cover.plants.Length;i++)
        {
            var plant=cover.plants[i];var prototype=cover.prototypes[plant.prototype];
            if(prototype.name==null || !prototype.name.StartsWith("Bambusa_Source"))
            {if(!plant.Equals(baseline.plants[i]))throw new Exception("Non-sample plant changed at "+i);continue;}
            replaced++;
            if(plant.position.x!=baseline.plants[i].position.x || plant.position.z!=baseline.plants[i].position.z || plant.yaw!=baseline.plants[i].yaw)
                throw new Exception("Sample horizontal placement/yaw changed.");
            float bottom=prototype.near.vertices.Min(v=>v.y)*plant.scale+plant.position.y;
            if(Mathf.Abs(bottom-(ReferenceHeight(plant.position.x,plant.position.z)-.025f))>.001f)throw new Exception("Bamboo foot is not grounded.");
        }
        if(replaced!=7)throw new Exception("Expected seven imported plants.");
        foreach(var prototype in cover.prototypes.Where(p=>p.name!=null && p.name.StartsWith("Bambusa_Source")))
        {
            if(prototype.materials.Length!=1 || !prototype.materials[0].enableInstancing)throw new Exception("Instancing/material regression.");
            var meshes=new[]{prototype.near,prototype.middle,prototype.far};
            if(meshes[0].triangles.Length<=meshes[1].triangles.Length || meshes[1].triangles.Length<=meshes[2].triangles.Length)
                throw new Exception("LOD triangles do not decrease.");
            foreach(var mesh in meshes)
            {
                if(mesh.subMeshCount!=1 || mesh.vertices.Any(v=>!Finite(v.x)||!Finite(v.y)||!Finite(v.z)) || mesh.normals.Any(n=>!Finite(n.x)||!Finite(n.y)||!Finite(n.z)))
                    throw new Exception("Invalid mesh data: "+mesh.name);
                if(mesh.vertices.Any(v=>!mesh.bounds.Contains(v)))throw new Exception("Mesh bounds exclude geometry.");
            }
            report.AppendLine(prototype.name+" triangles "+string.Join(" / ",meshes.Select(m=>(m.triangles.Length/3).ToString()))+"; one submesh per LOD.");
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>(BambusaMaterial);
        foreach(string slot in new[]{"_Front","_Back","_Normals"})
        {
            var texture=(Texture2DArray)material.GetTexture(slot);
            if(!texture || texture.depth!=6 || texture.isReadable || texture.format!=TextureFormat.DXT5)throw new Exception("Texture array validation failed.");
            report.AppendLine(slot+": "+texture.width+"x"+texture.height+", 6 DXT5 layers, "+texture.mipmapCount+" mips, CPU read/write off.");
        }
        var shader=material.shader;
        if(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Imported bamboo shader compile failure.");
        if(ReadArtCheckpoint()!=File.ReadAllText(BambusaWork+"/checkpoint-before-imported.txt"))throw new Exception("Checkpoint differs from pre-installation snapshot.");
        report.AppendLine("PASS: 7 grounded imported plants, unchanged XZ/yaw; 1,523 other plants and shared LOD distances match disk baseline; checkpoint unchanged.");
        File.WriteAllText(BambusaWork+"/validation.txt",report.ToString());
    }

    private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);

    private static void CaptureBambusaVariants(int[] sources,string folderName)
    {
        string folder=BambusaWork+"/"+folderName;Directory.CreateDirectory(folder);
        foreach(int source in sources)
        for(int capture=0;capture<4;capture++)
        {
            int lod=capture==3?0:capture;
            var preview=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
            var rt=new RenderTexture(1000,1000,24);var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try
            {
                var tree=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BambusaModels+"/Bambusa_Source"+source.ToString("00")+".prefab"));
                SceneManager.MoveGameObjectToScene(tree,preview);tree.GetComponent<LODGroup>().ForceLOD(lod);
                foreach(var transform in tree.GetComponentsInChildren<Transform>())transform.gameObject.layer=31;
                var bounds=tree.GetComponentInChildren<Renderer>().bounds;
                var lightObj=new GameObject("PreviewSun");SceneManager.MoveGameObjectToScene(lightObj,preview);
                var light=lightObj.AddComponent<Light>();var sun=Find("Sun").GetComponent<Light>();light.type=LightType.Directional;light.color=sun.color;light.intensity=sun.intensity;light.transform.rotation=sun.transform.rotation;light.cullingMask=1<<31;
                var obj=new GameObject("PreviewCamera");SceneManager.MoveGameObjectToScene(obj,preview);var camera=obj.AddComponent<Camera>();camera.enabled=false;
                camera.scene=preview;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.38f,.40f,.37f);camera.fieldOfView=36;
                Vector3 center=bounds.center;float distance=Mathf.Max(bounds.size.y,bounds.size.x)*1.85f;
                var view=new Vector3(.45f,.10f,capture==3?1:-1).normalized;
                camera.transform.SetPositionAndRotation(center+view*distance,Quaternion.LookRotation(-view));
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();File.WriteAllBytes(folder+"/Source"+source.ToString("00")+(capture==3?"_Back":lod==0?"":"_LOD"+lod)+".png",image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
}
