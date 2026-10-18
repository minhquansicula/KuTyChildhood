using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string WaterFolder="Assets/_Project/Art/Materials/Scene3/Water";
    [MenuItem("KuTy/Scene 3/Install reflective water and footstep splashes")]
    public static void InstallVillageWater()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Expected village in Edit Mode.");
        var village=Find(VillageName);if(!village) throw new Exception("Village missing.");
        if(Find(MemoryRoot)) throw new Exception("Curved village water is already installed. Use memory_validate; rectangular water installation would flatten its channels.");
        var water=AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name=="KenhDanNuoc" || (t.name=="MatNuoc" && t.IsChildOf(village.transform))))
            .Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r).ToArray();
        if(water.Length!=18 || water.Count(r=>r.name=="KenhDanNuoc")!=2) throw new Exception("Expected two canals and sixteen flooded paddies.");
        var player=AllTransforms().Select(t=>t.GetComponent<FirstPersonController>()).FirstOrDefault(c=>c && c.gameObject.activeInHierarchy);
        if(!player) throw new Exception("Active player missing.");
        Directory.CreateDirectory(OutputFolder);
        var backup=OutputFolder+"/before-interactive-water.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up water scene.");
        var gameplay=GameplaySnapshot();gameplay.Remove(player);
        var shader=Shader.Find("KuTy/VillageWater");var dropsShader=Shader.Find("KuTy/VillageWaterDroplets");
        if(!shader || !dropsShader) throw new Exception("New water shaders not imported.");
        foreach(var s in new[]{shader,dropsShader})
        {
            var errors=ShaderUtil.GetShaderMessages(s).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if(errors.Length>0) throw new Exception(string.Join("; ",errors.Select(m=>m.message)));
        }
        EnsureFolder(WaterFolder);
        var canal=MakeVillageWaterMaterial("NuocMuong_PhanChieu",shader,true);
        var paddy=MakeVillageWaterMaterial("NuocRuong_GonSong",shader,false);
        var meshPath=WaterFolder+"/MatNuoc_Quad.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!mesh)
        {
            mesh=new Mesh{name="MatNuoc_Quad"};
            mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
        }
        var root=village.transform.Find("Nuoc_TuongTac");
        if(!root) {var obj=new GameObject("Nuoc_TuongTac");obj.transform.SetParent(village.transform,false);root=obj.transform;}
        var system=root.GetComponent<VillageWaterSystem>();
        if(!system) system=root.gameObject.AddComponent<VillageWaterSystem>();
        gameplay.Remove(system);
        var surfaces=new VillageWaterSystem.Surface[water.Length];
        for(var i=0;i<water.Length;i++)
        {
            var renderer=water[i];var bounds=renderer.bounds;var isCanal=renderer.name=="KenhDanNuoc";
            // Already-flat surfaces retain their height when this command is run again.
            float y=bounds.max.y+(bounds.size.y>.001f?.003f:0);
            Undo.RecordObjects(new UnityEngine.Object[]{renderer.transform,renderer,renderer.GetComponent<MeshFilter>()},"Interactive village water");
            renderer.transform.SetPositionAndRotation(new Vector3(bounds.center.x,y,bounds.center.z),Quaternion.identity);
            renderer.transform.localScale=new Vector3(bounds.size.x,1,bounds.size.z);
            renderer.GetComponent<MeshFilter>().sharedMesh=mesh;renderer.sharedMaterial=isCanal?canal:paddy;
            renderer.gameObject.layer=4;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
            var rectangle=new Vector4(bounds.min.x,bounds.min.z,bounds.max.x,bounds.max.z);
            surfaces[i]=new VillageWaterSystem.Surface{renderer=renderer,rectangle=rectangle,height=y,canal=isCanal};
        }
        system.surfaces=surfaces;system.player=player;
        var sprayTransform=root.Find("NuocBan_Pool");
        if(!sprayTransform) {var obj=new GameObject("NuocBan_Pool");obj.transform.SetParent(root,false);sprayTransform=obj.transform;}
        var spray=sprayTransform.GetComponent<ParticleSystem>();
        if(!spray) spray=sprayTransform.gameObject.AddComponent<ParticleSystem>();
        spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=spray.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.duration=1;main.startLifetime=.7f;main.startSpeed=0;main.startSize=.03f;main.gravityModifier=.7f;main.maxParticles=128;
        var emission=spray.emission;emission.enabled=false;var shape=spray.shape;shape.enabled=false;
        var colors=spray.colorOverLifetime;colors.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.45f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var sprayRenderer=spray.GetComponent<ParticleSystemRenderer>();
        var dropsPath=WaterFolder+"/GiotNuoc.mat";var drops=AssetDatabase.LoadAssetAtPath<Material>(dropsPath);
        if(!drops) {drops=new Material(dropsShader){name="GiotNuoc"};AssetDatabase.CreateAsset(drops,dropsPath);}
        sprayRenderer.sharedMaterial=drops;sprayRenderer.shadowCastingMode=ShadowCastingMode.Off;sprayRenderer.receiveShadows=false;
        sprayRenderer.renderMode=ParticleSystemRenderMode.Billboard;sprayRenderer.maxParticleSize=.06f;sprayTransform.gameObject.layer=4;
        system.spray=spray;
        var probeTransform=root.Find("PhanChieu_Tinh");
        if(!probeTransform) {var obj=new GameObject("PhanChieu_Tinh");obj.transform.SetParent(root,false);probeTransform=obj.transform;}
        var probe=probeTransform.GetComponent<ReflectionProbe>();
        if(!probe) probe=probeTransform.gameObject.AddComponent<ReflectionProbe>();
        probe.transform.position=new Vector3(0,2,35);probe.size=new Vector3(92,12,104);
        probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.hdr=true;
        probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.cullingMask=~(1<<4);probe.shadowDistance=0;probe.importance=10;probe.blendDistance=4;
        system.reflectionProbe=probe;
        var settings=new SerializedObject(player);settings.FindProperty("enableJump").boolValue=true;settings.FindProperty("jumpHeight").floatValue=.85f;settings.ApplyModifiedProperties();
        ApplyVillageWaterBlocks(system);
        var interactionReport=VerifyVillageWaterInteraction(system);
        system.ClearInteraction();
        VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateScene();AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(system);EditorUtility.SetDirty(probe);EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save interactive water.");
        probe.RenderProbe();Inspect(true);CaptureVillageWater();
        File.WriteAllText(OutputFolder+"/interactive_water_report.txt","PASS\n2 irrigation canals and 16 paddies use reflective animated water.\n18 four-vertex surfaces, 36 water triangles total.\n8 GPU ripple slots, 1 spray emitter capped at 128 particles.\nReflection probe captured once; no per-frame planar reflection or opaque/depth texture copies.\nSpace jump enabled only for village player; wet movement slowed.\n"+
            interactionReport+"\nOther original gameplay components unchanged: "+gameplay.Count+"\nWalking/layout validation passed; scene saved.\n");
    }
    private static Material MakeVillageWaterMaterial(string name,Shader shader,bool canal)
    {
        var path=WaterFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_ShallowColor",canal?new Color(.15f,.4f,.56f):new Color(.19f,.36f,.37f));
        material.SetColor("_DeepColor",canal?new Color(.06f,.24f,.36f):new Color(.08f,.19f,.22f));
        material.SetFloat("_Opacity",canal?.62f:.34f);material.SetFloat("_DepthTone",canal?.4f:.1f);
        material.SetFloat("_WaveStrength",canal?.11f:.035f);material.SetFloat("_FlowSpeed",canal?1:.6f);
        material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    private static void ApplyVillageWaterBlocks(VillageWaterSystem system)
    {
        for(var i=0;i<system.surfaces.Length;i++)
        {
            var surface=system.surfaces[i];var block=new MaterialPropertyBlock();
            block.SetFloat("_SurfaceIndex",i);block.SetVector("_SurfaceBounds",surface.rectangle);surface.renderer.SetPropertyBlock(block);
            block.SetFloat("_SurfaceMeander",surface.meandering?1:0);surface.renderer.SetPropertyBlock(block);
            block.SetFloat("_UseShoreMask",surface.shoreMask?1:0);surface.renderer.SetPropertyBlock(block);
        }
    }
    private static string VerifyVillageWaterInteraction(VillageWaterSystem system)
    {
        system.ClearInteraction();
        var field=Array.FindIndex(system.surfaces,s=>!s.canal);
        var data=system.surfaces[field];var center=new Vector3((data.rectangle.x+data.rectangle.z)/2,data.height-.015f,(data.rectangle.y+data.rectangle.w)/2);
        if(data.meandering) center=VillageLandscapeShape.Warp(center);
        system.ProcessContact(center,Vector3.right,-2,0);
        var before=system.RippleEvents;system.ProcessContact(center+Vector3.forward*.65f,Vector3.right,-2,.4f);
        if(system.RippleEvents<=before) throw new Exception("Paddy footsteps did not create ripples.");
        before=system.RippleEvents;system.ProcessContact(center+Vector3.forward*.65f,Vector3.right,-2,.8f);
        if(system.RippleEvents!=before) throw new Exception("Stationary player produces endless ripples.");
        foreach(var index in Enumerable.Range(0,system.surfaces.Length).Where(i=>system.surfaces[i].canal))
        {
            data=system.surfaces[index];center=new Vector3((data.rectangle.x+data.rectangle.z)/2,data.height-.015f,(data.rectangle.y+data.rectangle.w)/2);
            if(data.meandering) center=VillageLandscapeShape.Warp(center);
            system.ClearInteraction();system.ProcessContact(center+Vector3.up*.6f,Vector3.right,-4,1);
            system.ProcessContact(center,Vector3.right,-7,1.15f);
            if(system.SplashEvents!=1 || system.RippleEvents!=1 || system.spray.particleCount==0) throw new Exception("Canal landing splash failed: "+index);
            system.ClearInteraction();system.ProcessContact(center+Vector3.up*.3f,Vector3.right,0,2);
            if(system.ActiveSurface>=0 || system.RippleEvents!=0) throw new Exception("Bridge height causes false water contact.");
        }
        data=system.surfaces[field];center=new Vector3((data.rectangle.x+data.rectangle.z)/2,data.height,(data.rectangle.y+data.rectangle.w)/2);
        if(data.meandering) center=VillageLandscapeShape.Warp(center);
        system.ClearInteraction();system.ProcessContact(center,Vector3.right,-2,3,false);
        if(system.RippleEvents!=0) throw new Exception("Locked controls trigger water effects.");
        for(var i=0;i<30;i++) system.AddRipple(center,field,.5f,3+i*.3f);
        if(Shader.GetGlobalInt("_VillageWaterRippleCount")>VillageWaterSystem.RippleCapacity) throw new Exception("Unbounded ripple allocation.");
        system.ExpireRipples(20);
        if(Shader.GetGlobalInt("_VillageWaterRippleCount")!=0) throw new Exception("Expired ripples still run on the GPU.");
        system.AddRipple(center,field,.5f,21);
        if(Shader.GetGlobalInt("_VillageWaterRippleCount")!=1) throw new Exception("Ripple pool failed to restart after idle.");
        return "Interaction PASS: walking ripples, stationary/bridge/locked exclusions, landing spray on both canal sides, bounded ripple pool, idle expiry/restart.";
    }
    private static void CaptureVillageWater()
    {
        var folder="Tools/Scene3/Water";Directory.CreateDirectory(folder);
        var obj=new GameObject("WaterPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
        camera.fieldOfView=60;camera.nearClipPlane=.05f;camera.farClipPlane=130;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var target=new RenderTexture(1200,850,24,RenderTextureFormat.ARGB32);target.Create();
        var previous=RenderTexture.active;var image=new Texture2D(1200,850,TextureFormat.RGB24,false);
        try
        {
            for(var side=0;side<2;side++)
            {
                float x=side==0?-5:5;camera.transform.position=new Vector3(x,1.45f,30);camera.transform.LookAt(new Vector3(x,.04f,34));
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1200,850),0,0);image.Apply();
                File.WriteAllBytes(folder+(side==0?"/Muong_Trai.png":"/Muong_Phai.png"),image.EncodeToPNG());
            }
            var system=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();
            var index=Array.FindIndex(system.surfaces,s=>s.canal && s.rectangle.x<0);
            var surface=system.surfaces[index];var point=new Vector3(-5,surface.height,31.2f);
            system.ClearInteraction();var now=Time.realtimeSinceStartup;
            system.ProcessContact(point+Vector3.up*.6f,Vector3.right,-4,now-.4f);
            system.ProcessContact(point,Vector3.right,-7,now-.25f);
            system.spray.Simulate(.16f,false,false);
            camera.transform.position=new Vector3(-5,1.65f,30);camera.transform.LookAt(point);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1200,850),0,0);image.Apply();
            File.WriteAllBytes(folder+"/GonSong_NuocBan.png",image.EncodeToPNG());system.ClearInteraction();
            var field=system.surfaces.First(s=>!s.canal && s.rectangle.x>0 && s.rectangle.y<15);
            point=new Vector3((field.rectangle.x+field.rectangle.z)*.5f,field.height,field.rectangle.y+2);
            index=Array.FindIndex(system.surfaces,s=>s.renderer==field.renderer);
            system.AddRipple(point,index,.65f,Time.realtimeSinceStartup-.35f);
            camera.transform.position=point+new Vector3(0,2,-2);camera.transform.LookAt(point);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1200,850),0,0);image.Apply();
            File.WriteAllBytes(folder+"/NuocRuong.png",image.EncodeToPNG());system.ClearInteraction();
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
