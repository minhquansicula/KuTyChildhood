using System;
using System.Collections.Generic;
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
    private const string BankModels=DetailModels+"/NaturalBanks";
    [MenuItem("KuTy/Scene 3/Remove all rice bank grass")]
    public static void ClearBankGrass()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Remove grass in Edit Mode.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath) throw new Exception("Expected village scene.");
        var grass=AllTransforms().Where(t=>t.name=="CoBoRuong_Gop" || t.name=="CoBoKenh_Gop").Select(t=>t.gameObject).ToArray();
        var gameplay=GameplaySnapshot();Directory.CreateDirectory(OutputFolder);
        var backup=OutputFolder+"/before-remove-bank-grass.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up scene.");
        var triangles=grass.Sum(g=>(long)g.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3);
        Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Remove all bank grass");
        foreach(var obj in grass) Undo.DestroyObjectImmediate(obj);
        VerifyGameplay(gameplay);Physics.SyncTransforms();VerifyWalking();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save village without grass.");
        Undo.CollapseUndoOperations(undo);CaptureBankDetail();Inspect(true);
        File.WriteAllText(OutputFolder+"/remove_grass_report.txt","PASS\nRemoved grass objects: "+grass.Length+"\nRemoved grass triangles: "+triangles+
            "\nRemaining bank grass objects: "+AllTransforms().Count(t=>t.name=="CoBoRuong_Gop" || t.name=="CoBoKenh_Gop")+
            "\nOriginal gameplay components preserved: "+gameplay.Count+"\nWalking validation passed; scene saved.\n");
        SceneView.RepaintAll();
    }
    [MenuItem("KuTy/Scene 3/Thin and shorten rice bank grass")]
    public static void ThinBankGrass()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Thin grass in Edit Mode.");
        var filters=AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name=="CoBoRuong_Gop" || t.name=="CoBoKenh_Gop"))
            .Select(t=>t.GetComponent<MeshFilter>()).ToArray();
        if(filters.Length!=17) throw new Exception("Expected seventeen combined bank grass meshes.");
        var report=OutputFolder+"/thin_grass_report.txt";
        if(File.Exists(report)) {CaptureBankDetail();return;} // Do not thin the same mesh twice.
        var gameplay=GameplaySnapshot();var before=0;var after=0;
        var backup=OutputFolder+"/GrassBeforeThinning";Directory.CreateDirectory(backup);
        // Validate and back up every source before changing any of the seventeen assets.
        foreach(var filter in filters)
        {
            var mesh=filter.sharedMesh;
            if(!mesh || !mesh.isReadable || mesh.vertexCount%5!=0 || mesh.triangles.Length!=mesh.vertexCount/5*9)
                throw new Exception("Unexpected grass blade topology: "+filter.name);
            var copy=backup+"/"+mesh.name+".asset";
            if(!File.Exists(copy)) File.Copy(AssetDatabase.GetAssetPath(mesh),copy);
        }
        foreach(var filter in filters)
        {
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var colors=mesh.colors;
            var geometry=new BankGeometry();var blades=vertices.Length/5;before+=blades;
            for(var blade=0;blade<blades;blade++)
            {
                if((blade/5)%2!=0 || blade%5>=2) continue;
                var start=geometry.vertices.Count;var rootY=vertices[blade*5].y;
                for(var corner=0;corner<5;corner++)
                {
                    var point=vertices[blade*5+corner];point.y=rootY+(point.y-rootY)*.65f;
                    geometry.Add(point,colors[blade*5+corner]);
                }
                geometry.Quad(start,start+1,start+2,start+3);geometry.triangles.AddRange(new[]{start+3,start+2,start+4});after++;
            }
            Undo.RegisterCompleteObjectUndo(mesh,"Thin rice bank grass");geometry.Save(mesh.name);
        }
        AssetDatabase.SaveAssets();VerifyGameplay(gameplay);Physics.SyncTransforms();VerifyWalking();
        CaptureBankDetail();
        File.WriteAllText(report,"PASS\nCombined meshes: "+filters.Length+"\nGrass blades before / after: "+before+" / "+after+
            "\nGrass triangles before / after: "+before*3+" / "+after*3+"\nRemaining blade height: 65%\nOriginal gameplay components preserved: "+gameplay.Count+
            "\nEarth banks, colliders, rice, houses and village layout unchanged.\nNo FPS improvement asserted; main-thread timing needs profiling at the same camera.\n");
        Inspect(true);SceneView.RepaintAll();
    }
    public static void BatchNaturalBanks()
    {
        try {EditorSceneManager.OpenScene(ScenePath);MakeNaturalBanks();ValidateScene();EditorApplication.Exit(0);}
        catch(Exception exception) {Debug.LogException(exception);EditorApplication.Exit(1);}
    }
    private sealed class BankGeometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Color> colors=new List<Color>();
        public readonly List<int> triangles=new List<int>();
        public int Add(Vector3 point,Color color) {vertices.Add(point);colors.Add(color);return vertices.Count-1;}
        public void Quad(int a,int b,int c,int d) {triangles.AddRange(new[]{a,b,c,a,c,d});}
        public Mesh Save(string name)
        {
            EnsureFolder(BankModels);var path=BankModels+"/"+name+".asset";
            var mesh=new Mesh{name=name};if(vertices.Count>65535) mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing) {EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
    public static void MakeNaturalBanks()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Natural banks require Edit Mode.");
        var scene=SceneManager.GetActiveScene();var village=Find(VillageName);
        if(!village) throw new Exception("Compact village missing.");
        Directory.CreateDirectory(OutputFolder);
        if(!File.Exists(OutputFolder+"/before-natural-banks.unity") && !EditorSceneManager.SaveScene(scene,OutputFolder+"/before-natural-banks.unity",true)) throw new Exception("Cannot back up current scene.");
        var gameplay=GameplaySnapshot();
        var ground=Find("Ground"); var environment=Find("Scene3_DuongLang_RuongLua");
        Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Natural grassy rice banks");
        Undo.RegisterFullObjectHierarchyUndo(environment,"Natural grassy rice banks");
        Undo.RecordObject(ground.GetComponent<Renderer>(),"Matte ground cover");
        var bankMaterial=MakeMatteMaterial("BoRuong_DatCo",.9f,true,new Color(.25f,.34f,.09f));
        var groundMaterial=MakeMatteMaterial("NenCo_Nham",1,false,new Color(.25f,.34f,.11f));
        var grassMaterial=MakeMatteMaterial("CoBoRuong_Nham",1,false,new Color(.27f,.37f,.1f));
        grassMaterial.SetFloat("_GrassBend",.04f);EditorUtility.SetDirty(grassMaterial);
        try
        {
            var oldGrass=AllTransforms().Where(t=>t.name=="CoBoRuong_Gop" || t.name=="CoBoKenh_Gop").Select(t=>t.gameObject).ToArray();
            foreach(var obj in oldGrass) Undo.DestroyObjectImmediate(obj);
            var bridges=AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name=="CauVanQuaMuong")
                .Select(t=>t.GetComponent<Renderer>().bounds).ToArray();
            var banks=AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name.StartsWith("BoRuong_") || t.name=="BoKenh")).ToArray();
            var chunks=new Dictionary<Transform,BankGeometry>();var index=0;var bladeCount=0;
            foreach(var bank in banks)
            {
                var renderer=bank.GetComponent<MeshRenderer>();if(!renderer) continue;
                var bounds=renderer.bounds;var alongZ=bounds.size.z>bounds.size.x;
                var length=alongZ?bounds.size.z:bounds.size.x;
                var center=new Vector3(bounds.center.x,0,bounds.center.z);
                var rotation=Quaternion.Euler(0,alongZ?0:90,0);
                var canal=bank.name=="BoKenh";
                var width=canal?.82f:.72f;var height=canal?.17f:.145f;
                var geometry=BuildEarthBank(length,width,height,index);
                var mesh=geometry.Save("BoDat_"+index.ToString("00"));
                Undo.RecordObject(bank,"Sloping earth bank");bank.SetPositionAndRotation(center,rotation);bank.localScale=Vector3.one;
                Undo.RecordObject(bank.GetComponent<MeshFilter>(),"Sloping earth bank mesh");bank.GetComponent<MeshFilter>().sharedMesh=mesh;
                Undo.RecordObject(renderer,"Matte grassy bank material");renderer.sharedMaterial=bankMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                foreach(var collider in bank.GetComponents<Collider>()) Undo.DestroyObjectImmediate(collider);
                var collision=Undo.AddComponent<MeshCollider>(bank.gameObject);collision.sharedMesh=mesh;
                // Keep the earth banks without generating grass again.
                index++;
            }
            foreach(var chunk in chunks)
            {
                var canal=chunk.Key==village.transform;
                var mesh=chunk.Value.Save(canal?"CoKenh_Gop":chunk.Key.name+"_CoBoGop");
                var obj=Group(canal?"CoBoKenh_Gop":"CoBoRuong_Gop",chunk.Key);obj.transform.position=Vector3.zero;
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=grassMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            foreach(var renderer in village.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("GoDat")))
            {Undo.RecordObject(renderer,"Matte grassy village perimeter");renderer.sharedMaterial=groundMaterial;}
            VerifyGameplay(gameplay);Physics.SyncTransforms();VerifyWalking();
            if(index!=68) throw new Exception("Expected 64 field banks plus 4 canal banks, got "+index);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save natural banks.");
            Undo.CollapseUndoOperations(undo);Inspect(true);Capture();CaptureBankDetail();
            foreach(var material in new[]{bankMaterial,groundMaterial,grassMaterial})
            {
                var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if(errors.Length>0) throw new Exception(string.Join("; ",errors.Select(e=>e.message)));
            }
            File.WriteAllText(OutputFolder+"/natural_banks_report.txt","PASS\nEarth banks: "+index+"\nCombined grass meshes: "+chunks.Count+"\nGrass blades: "+bladeCount+"\nOriginal gameplay components unchanged: "+gameplay.Count+"\nCharacterController bridge/shop/marble routes and map boundaries passed.\nMatte diffuse shading: no specular highlight or environment reflection term.\n");
            SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }
    private static Material MakeMatteMaterial(string name,float coverage,bool vertexMask,Color tint)
    {
        EnsureFolder(NewMaterials);var path=NewMaterials+"/"+name+".mat";
        var shader=Shader.Find("KuTy/Village/Matte Grass Earth");if(!shader) throw new Exception("Matte grass shader has not imported.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path);}
        Undo.RecordObject(material,"Matte grass settings");material.shader=shader;
        material.SetTexture("_SoilMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Scene3/brown_mud_dry_diff_4k.png"));
        material.SetColor("_GrassColor",tint);material.SetColor("_SoilColor",new Color(.7f,.62f,.48f));
        material.SetFloat("_GrassCoverage",coverage);material.SetFloat("_UseVertexCoverage",vertexMask?1:0);
        material.SetFloat("_SoilScale",.65f);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    private static BankGeometry BuildEarthBank(float length,float width,float height,int seed)
    {
        var geometry=new BankGeometry();var segments=Mathf.Max(2,Mathf.CeilToInt(length/.65f));
        var x=new[]{-.5f,-.24f,0,.24f,.5f};var y=new[]{-.01f,height*.8f,height,height*.8f,-.01f};
        var mask=new[]{.12f,.72f,.96f,.72f,.12f};
        for(var segment=0;segment<=segments;segment++)
        {
            var z=-length*.5f+length*segment/segments;
            var noise=Mathf.PerlinNoise(seed*.71f+1,z*.6f+170);
            var offset=(Mathf.PerlinNoise(seed*.37f+11,z*.32f+170)-.5f)*.05f;
            for(var cross=0;cross<5;cross++)
                geometry.Add(new Vector3(x[cross]*width+offset,y[cross]>0?y[cross]*(.91f+.18f*noise):y[cross],z),new Color(mask[cross],0,0,1));
            if(segment==0) continue;
            for(var cross=0;cross<4;cross++) {var a=(segment-1)*5+cross;geometry.Quad(a,a+5,a+6,a+1);}
        }
        for(var cross=1;cross<4;cross++)
        {
            geometry.triangles.AddRange(new[]{0,cross,cross+1});var last=segments*5;geometry.triangles.AddRange(new[]{last,last+cross+1,last+cross});
        }
        return geometry;
    }
    private static int AddBankGrass(BankGeometry geometry,Matrix4x4 transform,float length,float width,float height,int seed,Bounds[] bridges,bool canal)
    {
        var random=new System.Random(941+seed*31);var count=0;
        for(var z=-length*.5f+.18f;z<length*.5f-.1f;z+=.28f)
        for(var side=-1;side<=1;side+=2)
        {
            var local=new Vector3(side*width*(.19f+(float)random.NextDouble()*.10f),height*.81f,z+((float)random.NextDouble()-.5f)*.18f);
            var world=transform.MultiplyPoint3x4(local);
            if(bridges.Any(b=>Mathf.Abs(world.x-b.center.x)<b.extents.x+.15f && Mathf.Abs(world.z-b.center.z)<b.extents.z+.15f)) continue;
            for(var blade=0;blade<5;blade++)
            {
                var angle=(float)random.NextDouble()*Mathf.PI*2;
                var right=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var h=(canal?.14f:.09f)+(float)random.NextDouble()*(canal?.22f:.15f);
                var root=world+right*((float)random.NextDouble()-.5f)*.11f;
                var lean=right*((float)random.NextDouble()-.5f)*.24f;
                var w=.007f+(float)random.NextDouble()*.010f;
                var tint=(float)random.NextDouble();
                var a=geometry.Add(root-right*w,new Color(1,0,tint,1));var b=geometry.Add(root+right*w,new Color(1,0,tint,1));
                var c=geometry.Add(root+Vector3.up*h*.58f+lean*.25f+right*w*.65f,new Color(1,.58f,tint,1));
                var d=geometry.Add(root+Vector3.up*h*.58f+lean*.25f-right*w*.65f,new Color(1,.58f,tint,1));
                var tip=geometry.Add(root+Vector3.up*h+lean,new Color(1,1,tint,1));
                geometry.Quad(a,b,c,d);geometry.triangles.AddRange(new[]{d,c,tip});count++;
            }
        }
        return count;
    }
    private static void CaptureBankDetail()
    {
        var obj=new GameObject("NaturalBankPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
        camera.transform.SetPositionAndRotation(new Vector3(-5,2.2f,15),Quaternion.LookRotation(new Vector3(-5,.3f,37)-new Vector3(-5,2.2f,15)));
        camera.fieldOfView=65;camera.farClipPlane=180;camera.nearClipPlane=.1f;camera.allowHDR=true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(1400,900,24,RenderTextureFormat.ARGB32);rt.Create();var previous=RenderTexture.active;var texture=new Texture2D(1400,900,TextureFormat.RGB24,false);
        try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1400,900),0,0);texture.Apply();File.WriteAllBytes(OutputFolder+"/After/04_BoRuong_Co.png",texture.EncodeToPNG());}
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
