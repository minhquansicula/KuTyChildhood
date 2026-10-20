using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string HdriPath="Assets/_Project/Art/Textures/Scene3/HDRI/golf_course_sunrise_4k.exr";
    private const string RoughAssets=DeepAssets+"/RoughBanks";

    // Render directly across atan2's longitude seam without touching the user's scene.
    public static void CaptureHdriSeam()
    {
        var sky=RenderSettings.skybox;
        if(!sky || sky.shader.name!="KuTy/HDRI/Sky With Distant Horizon")throw new Exception("Current sky is not the distant HDR panorama.");
        var errors=ShaderUtil.GetShaderMessages(sky.shader).Where(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0)throw new Exception(string.Join("\n",errors.Select(e=>e.message)));
        string folder=OutputFolder+"/HdriSeam";Directory.CreateDirectory(folder);
        var preview=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var rt=new RenderTexture(1024,768,24);var image=new Texture2D(1024,768,TextureFormat.RGB24,false);
        try
        {
            var obj=new GameObject("HDRI seam preview"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(obj,preview);
            var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=0;camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=60;camera.aspect=1024f/768;
            obj.AddComponent<Skybox>().material=sky;
            // Offset the seam from the exact viewport center so a pixel quad straddles it.
            float angle=(180-sky.GetFloat("_Rotation")+.1f)*Mathf.Deg2Rad;
            camera.transform.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(angle),.22f,Mathf.Sin(angle)));
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data=obj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=false;
            rt.Create();UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1024,768),0,0);image.Apply();File.WriteAllBytes(folder+"/capture.png",image.EncodeToPNG());
            var pixels=image.GetPixels();float peak=0,neighbours=0;
            for(int y=430;y<710;y++)
            {
                var left=pixels[y*1024+490];var right=pixels[y*1024+534];
                for(int x=500;x<524;x++)peak=Mathf.Max(peak,Mathf.Abs(pixels[y*1024+x].grayscale-(pixels[y*1024+x-3].grayscale+pixels[y*1024+x+3].grayscale)*.5f));
                neighbours=Mathf.Max(neighbours,Mathf.Abs(left.grayscale-right.grayscale));
            }
            File.WriteAllText(folder+"/report.txt","Shader compile errors: 0\nCenter seam contrast: "+peak.ToString("F5")+"\nSurrounding sky variation: "+neighbours.ToString("F5")+"\nScene untouched; sky-only render across longitude seam.\n");
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(preview);}
    }

    [MenuItem("KuTy/Scene 3/Use warm HDRI, rough banks and rooted rice")]
    public static void InstallWarmVillageHdri()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || !Find(DeepRoot)) throw new Exception("Open the current deep-canal village in Edit Mode.");
        if(!File.Exists(HdriPath)) throw new Exception("User's HDRI has not been copied into the project.");
        if(Find("HDRI_LangQue_Am")) {ValidateMemoryVillage();ValidateDeepCanals();CaptureDeepCanals();CaptureMemoryVillage();return;}
        string backup=OutputFolder+"/before-warm-hdri-and-rough-banks.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up current scene.");
        var village=Find(VillageName);var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        var before=GameplaySnapshot();foreach(var field in fields)before.Remove(field);
        var counts=fields.Select(f=>f.PlantCount).ToArray();
        AssetDatabase.ImportAsset(HdriPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(HdriPath);
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
        importer.sRGBTexture=false;importer.isReadable=false;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
        importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.alphaSource=TextureImporterAlphaSource.None;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.BC6H,compressionQuality=100});
        importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
        if(!texture || !Shader.Find("Skybox/Panoramic")) throw new Exception("Panoramic HDR sky could not be imported.");
        Vector3 sourceSun;float skyLuminance;MeasureHdrSky(texture,out sourceSun,out skyLuminance);
        float rotation=Mathf.DeltaAngle(Mathf.Atan2(sourceSun.x,sourceSun.z)*Mathf.Rad2Deg,-18.8f);
        float exposure=Mathf.Clamp(.55f/Mathf.Max(.05f,skyLuminance),.5f,1.5f);
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Warm HDRI and rough rooted rice landscape");
        Undo.RegisterFullObjectHierarchyUndo(village,"Before HDRI atmosphere and bank sculpting");
        try
        {
            Group("HDRI_LangQue_Am",village.transform);EnsureFolder(RoughAssets);
            var bankMaterial=CreateRoughBankMaterial();long triangles=0;
            foreach(var filter in Find(DeepRoot).GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("BoRuong_LienGoc_")).ToArray())
            {
                int seed=int.Parse(filter.name.Substring(filter.name.Length-2));var mesh=SculptRoughBank(filter,seed);
                filter.sharedMesh=mesh;filter.GetComponent<MeshCollider>().sharedMesh=mesh;filter.GetComponent<Renderer>().sharedMaterial=bankMaterial;
                triangles+=mesh.triangles.Length/3;
            }
            foreach(var field in fields) {field.plantingDepth=.085f;field.Regenerate();EditorUtility.SetDirty(field);}
            string materialPath="Assets/_Project/Art/Materials/Scene3/HDRI_LangQue_Am.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!sky) {sky=new Material(Shader.Find("Skybox/Panoramic")){name="HDRI_LangQue_Am"};AssetDatabase.CreateAsset(sky,materialPath);}
            sky.SetTexture("_MainTex",texture);sky.SetColor("_Tint",new Color(.60f,.53f,.44f));
            sky.SetFloat("_Exposure",exposure);sky.SetFloat("_Rotation",rotation);sky.SetFloat("_Mapping",1);sky.SetFloat("_ImageType",0);EditorUtility.SetDirty(sky);
            var sun=Find("Sun").GetComponent<Light>();Undo.RecordObjects(new UnityEngine.Object[]{sun,sun.transform},"Match HDR sunrise direction");
            Vector3 direction=Quaternion.Euler(0,rotation,0)*sourceSun;
            sun.transform.rotation=Quaternion.LookRotation(-direction,Vector3.up);sun.color=new Color(1,.79f,.58f);sun.intensity=1.02f;
            RenderSettings.skybox=sky;RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Skybox;RenderSettings.ambientIntensity=.8f;
            RenderSettings.fogColor=new Color(.69f,.57f,.44f);RenderSettings.fogDensity=.0028f;DynamicGI.UpdateEnvironment();
            var water=village.GetComponentInChildren<VillageWaterSystem>();
            if(water.reflectionProbe)water.reflectionProbe.RenderProbe();
            VerifyGameplay(before);
            if(!counts.SequenceEqual(fields.Select(f=>f.PlantCount))) throw new Exception("Rice population changed while planting roots.");
            ValidateMemoryVillage();ValidateDeepCanals();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save warm HDRI scene.");
            Undo.CollapseUndoOperations(undo);CaptureDeepCanals();CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
            File.WriteAllText(OutputFolder+"/warm_hdri_report.txt","PASS\nUser HDRI: "+HdriPath+"\nImported "+texture.width+" x "+texture.height+", "+texture.format+", mipmaps, no runtime Read/Write.\nSkybox/Panoramic; warm tint (0.60, 0.53, 0.44), exposure "+exposure.ToString("F3")+", rotation "+rotation.ToString("F2")+" degrees.\n16 banks sculpted with irregular crests, asymmetric slopes and merged earth clods; triangles "+triangles+".\nAll "+counts.Sum()+" rice plants retained, mesh-base planting depth 0.085 m (base approximately 0.073 m below ground).\nOriginal LOD meshes, density, seeds, wind and distance settings retained.\nGameplay payloads preserved: "+before.Count+". Walking, water contacts and canal exit validations passed.\nScene saved; HDR reflection captures once, not every frame.\n");
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private static void MeasureHdrSky(Texture2D texture,out Vector3 direction,out float brightness)
    {
        var rt=new RenderTexture(256,128,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);rt.Create();
        var previous=RenderTexture.active;var sample=new Texture2D(256,128,TextureFormat.RGBAFloat,false,true);
        try
        {
            Graphics.Blit(texture,rt);RenderTexture.active=rt;sample.ReadPixels(new Rect(0,0,256,128),0,0);sample.Apply();
            var pixels=sample.GetPixels();var luminance=new List<float>();float maximum=-1;int peak=0;
            for(int y=66;y<123;y++) for(int x=0;x<256;x++)
            {
                Color color=pixels[y*256+x];float l=.2126f*color.r+.7152f*color.g+.0722f*color.b;
                if(float.IsNaN(l) || float.IsInfinity(l))continue;luminance.Add(l);
                if(l>maximum){maximum=l;peak=y*256+x;}
            }
            luminance.Sort();brightness=luminance[luminance.Count/2];
            float u=(peak%256+.5f)/256f,v=(peak/256+.5f)/128f;
            float longitude=(.5f-u)*Mathf.PI*2,latitude=(1-v)*Mathf.PI;
            direction=new Vector3(Mathf.Cos(longitude)*Mathf.Sin(latitude),Mathf.Cos(latitude),Mathf.Sin(longitude)*Mathf.Sin(latitude));
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(sample);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    public static void InstallCompactHdriSky()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || !Find("HDRI_LangQue_Am"))throw new Exception("Warm HDRI scene missing.");
        var before=GameplaySnapshot();var shader=Shader.Find("KuTy/HDRI/Sky With Distant Horizon");if(!shader)throw new Exception("Compact HDR sky shader not imported.");
        if(ShaderUtil.GetShaderMessages(shader).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Compact HDR sky shader compilation failed.");
        var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Scene3/HDRI_LangQue_Am.mat");
        string path="Assets/_Project/Art/Materials/Scene3/HDRI_LangQue_ChanTroiXa.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader){name="HDRI_LangQue_ChanTroiXa"};AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath));material.SetColor("_Tint",original.GetColor("_Tint"));
        material.SetFloat("_Exposure",original.GetFloat("_Exposure"));material.SetFloat("_Rotation",original.GetFloat("_Rotation"));
        material.SetFloat("_LandscapeScale",5);material.SetFloat("_ClearSkyAngle",80);
        Vector3 originalSun=Quaternion.Euler(0,-original.GetFloat("_Rotation"),0)*(-Find("Sun").transform.forward);
        material.SetVector("_SourceSunUV",new Vector4(.5f-Mathf.Atan2(originalSun.z,originalSun.x)/(Mathf.PI*2),.5f+Mathf.Asin(originalSun.y)/Mathf.PI,0,0));
        material.SetVector("_SunDirection",-Find("Sun").transform.forward);material.SetColor("_SunColor",new Color(3,2,.9f,1));EditorUtility.SetDirty(material);
        RenderSettings.skybox=material;DynamicGI.UpdateEnvironment();
        var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();if(water.reflectionProbe)water.reflectionProbe.RenderProbe();
        VerifyGameplay(before);AssetDatabase.SaveAssets();CaptureMemoryVillage();CaptureDeepCanals();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save compact HDR background.");
        Inspect(true);SceneView.RepaintAll();File.AppendAllText(OutputFolder+"/warm_hdri_report.txt","Full HDRI landscape restored to a distant ring: angular width/height reduced 5x; original trees and scenery retained. Panorama repeats around the horizon; photographed sun duplicates masked, one low sun rendered.\nAll scene tree models and gameplay payloads unchanged.\n");
    }
    private static Material CreateRoughBankMaterial()
    {
        string path=NewMaterials+"/BoRuong_DatLomNhom.mat";
        var source=AssetDatabase.LoadAssetAtPath<Material>(NewMaterials+"/BoRuong_DatCo.mat");var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(source){name="BoRuong_DatLomNhom"};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_GrassCoverage",.68f);material.SetColor("_GrassColor",new Color(.24f,.31f,.09f));material.SetColor("_SoilColor",new Color(.61f,.52f,.37f));
        EditorUtility.SetDirty(material);return material;
    }
    private static Mesh SculptRoughBank(MeshFilter filter,int seed)
    {
        var old=filter.sharedMesh.vertices;if(old.Length%7!=0) throw new Exception("Expected original closed bank cross sections.");
        int rows=old.Length/7;var centers=new Vector3[rows];
        for(int i=0;i<rows;i++){centers[i]=VillageLandscapeShape.Unwarp(filter.transform.TransformPoint(old[i*7+3]));centers[i].y=0;}
        var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
        float[] cross={-.34f,-.25f,-.16f,-.08f,0,.08f,.16f,.25f,.34f};
        float[] profile={0,.36f,.65f,.88f,1,.87f,.62f,.33f,0};
        for(int i=0;i<rows;i++)
        {
            var p=centers[i];var inward=Vector3.Cross((centers[(i+1)%rows]-centers[(i+rows-1)%rows]).normalized,Vector3.up);
            float macro=Mathf.PerlinNoise(p.x*.85f+seed*3,p.z*1.5f+37),micro=Mathf.PerlinNoise(p.x*3.2f+37,p.z*4.1f+seed);
            float height=.047f+.062f*macro+.037f*micro;
            float width=.81f+.37f*Mathf.PerlinNoise(p.x*1.6f+seed,p.z*.8f+23),skew=(micro-.5f)*.07f;
            for(int c=0;c<cross.Length;c++)
            {
                var v=p+inward*(cross[c]*width+skew*profile[c]);
                float erosion=.86f+.22f*Mathf.PerlinNoise(p.x*4+c*.77f,p.z*3.4f+seed);
                v.y=Mathf.Min(.123f,height*profile[c]*erosion);vertices.Add(filter.transform.InverseTransformPoint(VillageLandscapeShape.Warp(v)));
                colors.Add(new Color(.08f+.75f*profile[c]*(.65f+.35f*macro),0,0,1));
                if(c<8){int a=i*9+c,b=((i+1)%rows)*9+c;indices.AddRange(new[]{a,b+1,b,a,a+1,b+1});}
            }
        }
        // Clods are part of the same bank mesh and collider, not a GameObject per stone.
        for(int i=0;i<rows;i+=5)
        {
            var p=centers[i];float n=Mathf.PerlinNoise(p.x*3+seed,p.z*2+31);if(n<.48f)continue;
            var center=vertices[i*9+4];float r=.035f+n*.035f;int top=vertices.Count;
            var tip=center+Vector3.up*(.018f+n*.02f);tip.y=Mathf.Min(.143f,tip.y);
            vertices.Add(tip);colors.Add(new Color(.05f,0,0,1));
            for(int side=0;side<7;side++) {float a=side*Mathf.PI*2/7;vertices.Add(center+new Vector3(Mathf.Cos(a)*r,-.007f,Mathf.Sin(a)*r));colors.Add(new Color(.1f,0,0,1));}
            for(int side=0;side<7;side++) indices.AddRange(new[]{top,top+(side+1)%7+1,top+side+1});
        }
        var mesh=new Mesh{name="BoRuong_DatLomNhom_"+seed.ToString("00"),indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();string path=RoughAssets+"/"+mesh.name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
