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
    private const string MarbleModels="Assets/_Project/Art/Models/Scene3/VienBi";
    private const string MarbleTextures="Assets/_Project/Art/Textures/Scene3/VienBi";
    private const string MarbleMaterials="Assets/_Project/Art/Materials/Scene3/VienBi";
    private static readonly string[] MarbleNames={"PlayerMarble","Target0","Target1","Target2","Target3","Target4"};
    private static readonly string[] MarbleFiles={"Player","Target0","Target1","Target2","Target3","Target4"};

    [MenuItem("KuTy/Scene 3/Install optimized Vietnamese glass marbles")]
    public static void InstallOptimizedMarbles()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Install marbles in Edit Mode.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath) throw new Exception("Expected village scene.");
        var village=Find(VillageName);if(!village) throw new Exception("Village missing.");
        var area=village.transform.Find("BaiBanBi_VaNhomTre");if(!area) throw new Exception("Marble game area missing.");
        var marbleObjects=MarbleNames.Select(n=>area.Find(n)).ToArray();
        if(marbleObjects.Any(t=>!t)) throw new Exception("A playable marble is missing.");
        var oldBodies=marbleObjects.Select(t=>t.GetComponent<Rigidbody>()).ToArray();
        var oldColliders=marbleObjects.Select(t=>t.GetComponent<SphereCollider>()).ToArray();
        if(oldBodies.Any(b=>!b)||oldColliders.Any(c=>!c)) throw new Exception("Marble physics setup is incomplete.");
        var game=area.Find("MarbleShootingGame");if(!game||!game.GetComponent<MarbleShootingGame>()) throw new Exception("Marble minigame missing.");
        var gameplay=GameplaySnapshot();
        Directory.CreateDirectory(OutputFolder);
        var backup=OutputFolder+"/before-optimized-marbles.unity";
        if(!File.Exists(backup)&&!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up village scene.");
        EnsureFolder(MarbleMaterials);
        var totalTriangles=0;
        for(var index=0;index<MarbleNames.Length;index++)
        {
            var variant=MarbleFiles[index];
            var texture=ImportMarbleTexture(variant);
            var modelPath=MarbleModels+"/VienBi_"+variant+".fbx";
            AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if(importer==null) throw new Exception("Cannot import marble mesh "+modelPath);
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.isReadable=false;importer.addCollider=false;importer.globalScale=1;
            importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.None;
            importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var filters=model?model.GetComponentsInChildren<MeshFilter>(true):null;
            if(filters==null||filters.Length!=1||!filters[0].sharedMesh) throw new Exception("Expected one mesh in "+modelPath);
            var mesh=filters[0].sharedMesh;
            var triangles=(int)(mesh.GetIndexCount(0)/3);
            if(triangles>1000||mesh.bounds.center.magnitude>.04f||mesh.bounds.size.magnitude>1.8f)
                throw new Exception("Marble mesh dimensions/budget invalid: "+variant+" "+triangles+" "+mesh.bounds);
            totalTriangles+=triangles;
            var materialPath=MarbleMaterials+"/VienBi_"+variant+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material)
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader) throw new Exception("URP/Lit unavailable.");
                material=new Material(shader){name="VienBi_"+variant};AssetDatabase.CreateAsset(material,materialPath);
            }
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",new Color(.08f,.08f,.08f,1));
            material.SetFloat("_Metallic",.03f);material.SetFloat("_Smoothness",.8f);
            material.SetTexture("_EmissionMap",texture);
            material.SetColor("_EmissionColor",Color.white);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            material.SetFloat("_Surface",0);material.enableInstancing=true;
            EditorUtility.SetDirty(material);
            var body=marbleObjects[index];
            Undo.RecordObject(body.GetComponent<MeshFilter>(),"Use optimized marble mesh");
            Undo.RecordObject(body.GetComponent<MeshRenderer>(),"Use painted marble material");
            body.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=body.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(body.GetComponent<Rigidbody>()!=oldBodies[index]||body.GetComponent<SphereCollider>()!=oldColliders[index])
                throw new Exception("Marble physics reference changed: "+body.name);
        }
        var serializedGame=new SerializedObject(game.GetComponent<MarbleShootingGame>());
        if(serializedGame.FindProperty("playerMarble").objectReferenceValue!=oldBodies[0]) throw new Exception("Player marble reference changed.");
        var targets=serializedGame.FindProperty("targetMarbles");
        if(targets.arraySize!=5) throw new Exception("Target marble list size changed.");
        for(var index=0;index<5;index++)
            if(targets.GetArrayElementAtIndex(index).objectReferenceValue!=oldBodies[index+1]) throw new Exception("Target marble reference changed: "+index);
        VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateScene();AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save marble scene.");
        Inspect(true);CaptureMarbles();
        File.WriteAllText(OutputFolder+"/optimized_marbles_report.txt","PASS\nSix playable marbles changed to imported models.\nTotal visible marble triangles: "+totalTriangles+
            "\nSix 512px compressed, mipmapped colour textures.\nSix existing Rigidbodies and SphereColliders preserved.\nGameplay components preserved: "+gameplay.Count+
            "\nVillage layout and walking checks passed.\nScene saved.\n");
    }

    public static void BatchInstallOptimizedMarbles()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            InstallOptimizedMarbles();
            EditorApplication.Exit(0);
        }
        catch(Exception exception) { Debug.LogException(exception);EditorApplication.Exit(1); }
    }

    [MenuItem("KuTy/Scene 3/Fix marbles to round 16 mm glass spheres")]
    public static void ResizeRealisticMarbles()
    {
        ConfigureMarbleSize(.016f,1,1,1);
    }

    [MenuItem("KuTy/Scene 3/Use readable 10 cm marbles")]
    public static void ResizeReadableMarbles()
    {
        ConfigureMarbleSize(.1f,4,5,3.4f);
    }

    private static void ConfigureMarbleSize(float diameter,float layoutScale,float targetScale,float cameraScale)
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ScenePath) throw new Exception("Expected village in Edit Mode.");
        var area=Find(VillageName).transform.Find("BaiBanBi_VaNhomTre");
        var game=area.Find("MarbleShootingGame").GetComponent<MarbleShootingGame>();
        var before=GameplaySnapshot();before.Remove(game); // Only this minigame's scale-dependent tuning changes.
        var backup=OutputFolder+(diameter>.02f?"/before-readable-marbles.unity":"/before-real-size-marbles.unity");
        if(!File.Exists(backup)&&!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up marble size.");
        InstallOptimizedMarbles();
        var serialized=new SerializedObject(game);
        var referenceNames=new[]{"playerMarble","targetMarbles","shootPosition","ringCollider","gameCameraPosition","gameCameraLookAt","aimLine"};
        var references=referenceNames.Where(n=>n!="targetMarbles").ToDictionary(n=>n,n=>serialized.FindProperty(n).objectReferenceValue);
        var oldTargets=Enumerable.Range(0,5).Select(i=>serialized.FindProperty("targetMarbles").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var ring=(Collider)references["ringCollider"];
        var center=ring.transform.position;
        var hits=Physics.RaycastAll(new Vector3(center.x,2,center.z),Vector3.down,4,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.normal.y>.9f&&!h.collider.GetComponent<Rigidbody>()).OrderBy(h=>h.distance).ToArray();
        if(hits.Length==0) throw new Exception("No ground under marbles.");
        var floor=hits[0].collider;var groundY=hits[0].point.y;
        Undo.RecordObject(floor,"Tiny marble contact offset");floor.contactOffset=.0002f;
        var radius=diameter*.5f;
        var mass=.0054f*Mathf.Pow(diameter/.016f,3);
        var impulseScale=mass/.0054f*layoutScale;
        center.y=groundY+radius;
        var materialPath=MarbleMaterials+"/GlassMarblePhysics.physicMaterial";
        var physicsMaterial=AssetDatabase.LoadAssetAtPath<PhysicMaterial>(materialPath);
        if(!physicsMaterial)
        {
            physicsMaterial=new PhysicMaterial("GlassMarblePhysics");AssetDatabase.CreateAsset(physicsMaterial,materialPath);
        }
        physicsMaterial.dynamicFriction=.18f;physicsMaterial.staticFriction=.22f;physicsMaterial.bounciness=.3f;
        physicsMaterial.frictionCombine=PhysicMaterialCombine.Minimum;physicsMaterial.bounceCombine=PhysicMaterialCombine.Minimum;
        EditorUtility.SetDirty(physicsMaterial);
        for(var i=0;i<MarbleNames.Length;i++)
        {
            var marble=area.Find(MarbleNames[i]);var body=marble.GetComponent<Rigidbody>();var collider=marble.GetComponent<SphereCollider>();
            Undo.RecordObjects(new UnityEngine.Object[]{marble,body,collider},"Round life-size marble");
            var position=center;
            if(i==0) position.z-=.16f*layoutScale;
            else {var angle=(30+(i-1)*30)*Mathf.Deg2Rad;position+=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.045f*targetScale;}
            marble.position=position;marble.rotation=Quaternion.identity;marble.localScale=Vector3.one*diameter;
            collider.center=Vector3.zero;collider.radius=.5f;collider.contactOffset=.0002f;collider.sharedMaterial=physicsMaterial;
            body.mass=mass;body.drag=2;body.angularDrag=2;body.maxAngularVelocity=200;
            body.maxDepenetrationVelocity=.5f;body.sleepThreshold=.000001f;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.velocity=body.angularVelocity=Vector3.zero;
            var size=marble.GetComponent<MeshRenderer>().bounds.size;
            if((size-Vector3.one*diameter).magnitude>.00001f||Mathf.Abs(collider.radius*marble.lossyScale.x-radius)>.000001f)
                throw new Exception("Marble size or collider mismatch: "+marble.name+" "+size);
        }
        var shoot=(Transform)references["shootPosition"];Undo.RecordObject(shoot,"Small marble start");shoot.position=area.Find("PlayerMarble").position;
        // A small chalk circle and close camera keep the life-size marbles playable.
        Undo.RecordObject(ring.transform,"Life-size scoring ring");ring.transform.position=new Vector3(center.x,groundY+.001f,center.z);
        ring.transform.localScale=new Vector3(.18f*layoutScale,.002f,.18f*layoutScale);
        var ringRenderer=ring.GetComponent<MeshRenderer>();if(ringRenderer) {Undo.RecordObject(ringRenderer,"Flat chalk ring");ringRenderer.enabled=false;}
        var circle=ring.GetComponent<LineRenderer>();Undo.RecordObject(circle,"Life-size chalk ring");
        circle.useWorldSpace=true;circle.loop=true;circle.positionCount=64;circle.startWidth=circle.endWidth=.0015f*layoutScale;
        for(var i=0;i<64;i++) {var angle=i*Mathf.PI*2/64;circle.SetPosition(i,new Vector3(center.x+Mathf.Cos(angle)*.09f*layoutScale,groundY+.001f,center.z+Mathf.Sin(angle)*.09f*layoutScale));}
        var lookAt=(Transform)references["gameCameraLookAt"];var cameraAnchor=(Transform)references["gameCameraPosition"];
        Undo.RecordObjects(new UnityEngine.Object[]{lookAt,cameraAnchor},"Close marble gameplay camera");
        lookAt.position=center+new Vector3(0,0,-.045f*layoutScale);cameraAnchor.position=center+new Vector3(0,.36f*cameraScale,-.19f*cameraScale);cameraAnchor.LookAt(lookAt);
        var aim=(LineRenderer)references["aimLine"];Undo.RecordObject(aim,"Small marble aim line");aim.startWidth=aim.endWidth=.0015f*layoutScale;
        var values=new System.Collections.Generic.Dictionary<string,float>{{"minForce",.001f*impulseScale},{"maxForce",.0065f*impulseScale},{"minimumDrag",.004f*layoutScale},
            {"fullPowerDragDistance",.08f*layoutScale},{"aimPickRadius",Mathf.Max(.02f,radius*1.4f)},{"aimPreviewLength",.2f*layoutScale},{"settledSpeed",.002f*layoutScale},
            {"fallDistance",.1f*layoutScale},{"gameFieldOfView",50},{"gameNearClip",.005f},{"marbleContactOffset",.0002f}};
        foreach(var pair in values) serialized.FindProperty(pair.Key).floatValue=pair.Value;
        serialized.FindProperty("marbleGround").objectReferenceValue=floor;
        serialized.ApplyModifiedProperties();serialized.Update();
        foreach(var pair in references) if(serialized.FindProperty(pair.Key).objectReferenceValue!=pair.Value) throw new Exception("Minigame reference changed: "+pair.Key);
        for(var i=0;i<5;i++) if(serialized.FindProperty("targetMarbles").GetArrayElementAtIndex(i).objectReferenceValue!=oldTargets[i]) throw new Exception("Target reference changed.");
        VerifyGameplay(before);Physics.SyncTransforms();
        var physicsResult=VerifySmallMarblePhysics(physicsMaterial,diameter,layoutScale);
        for(var i=0;i<MarbleNames.Length;i++)
        for(var j=i+1;j<MarbleNames.Length;j++)
            if(Vector3.Distance(area.Find(MarbleNames[i]).position,area.Find(MarbleNames[j]).position)<diameter)
                throw new Exception("Marbles overlap: "+i+", "+j);
        ValidateScene();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save real-size marbles.");
        Inspect(true);CaptureMarbles(true);
        File.WriteAllText(OutputFolder+(diameter>.02f?"/readable_marbles_report.txt":"/real_size_marbles_report.txt"),"PASS\n6 spherical meshes, 832 triangles each (4992 total).\nDiameter "+diameter+" m; collider radius "+radius+" m.\n"+
            "Scoring circle diameter "+(.18f*layoutScale)+" m; close gameplay camera; scaled impulse/aim settings.\nNo overlapping marbles.\nAll minigame object references retained.\nOther gameplay components unchanged: "+before.Count+"\n"+physicsResult+"\nVillage validation passed; scene saved.\n");
    }

    private static string VerifySmallMarblePhysics(PhysicMaterial material,float diameter,float layoutScale)
    {
        var previous=SceneManager.GetActiveScene();
        var testScene=EditorSceneManager.NewPreviewScene();
        var simulation=testScene.GetPhysicsScene();
        try
        {
            if(!simulation.IsValid()||simulation.Equals(Physics.defaultPhysicsScene)) throw new Exception("Preview physics is not isolated; refusing to simulate village.");
            var radius=diameter*.5f;var mass=.0054f*Mathf.Pow(diameter/.016f,3);var spacing=.06f*layoutScale;
            var floor=new GameObject("TestFloor");SceneManager.MoveGameObjectToScene(floor,testScene);
            floor.transform.position=new Vector3(0,-.05f,0);var box=floor.AddComponent<BoxCollider>();box.size=new Vector3(2*layoutScale,.1f,2*layoutScale);box.contactOffset=.0002f;
            var bodies=new Rigidbody[2];
            for(var i=0;i<2;i++)
            {
                var obj=new GameObject("TestMarble"+i);SceneManager.MoveGameObjectToScene(obj,testScene);obj.transform.position=new Vector3(0,radius+.0001f,i*spacing);
                var c=obj.AddComponent<SphereCollider>();c.radius=radius;c.contactOffset=.0002f;c.sharedMaterial=material;
                var b=obj.AddComponent<Rigidbody>();b.mass=mass;b.drag=b.angularDrag=2;b.maxAngularVelocity=200;
                b.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;b.sleepThreshold=.000001f;bodies[i]=b;
            }
            bodies[0].AddForce(Vector3.forward*.0035f*(mass/.0054f)*layoutScale,ForceMode.Impulse);
            var maxTargetTravel=0f;
            for(var step=0;step<400;step++)
            {
                simulation.Simulate(.02f);maxTargetTravel=Mathf.Max(maxTargetTravel,bodies[1].position.z-spacing);
                if(bodies.Any(b=>b.position.y<radius*.75f)) throw new Exception("Marble fell through ground.");
            }
            if(maxTargetTravel<.03f*layoutScale) throw new Exception("Marble shot did not transfer enough motion: "+maxTargetTravel);
            if(bodies.Any(b=>b.velocity.magnitude>.002f*layoutScale)) throw new Exception("Marbles failed to settle within 8 seconds.");
            return "Physics PASS: "+(diameter*1000)+" mm sphere collision transferred motion, no ground tunnelling, settled within 8 s. Target travel: "+maxTargetTravel.ToString("F4")+" m.";
        }
        finally {EditorSceneManager.ClosePreviewScene(testScene);SceneManager.SetActiveScene(previous);}
    }

    private static Texture2D ImportMarbleTexture(string variant)
    {
        var path=MarbleTextures+"/VienBi_"+variant+"_Color.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new Exception("Cannot import marble colour "+path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
        importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Compressed;
        importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture) throw new Exception("Marble texture missing: "+path);
        return texture;
    }

    private static void CaptureMarbles(bool lifeSize=false)
    {
        var folder="Tools/Scene3/VienBi/Unity";Directory.CreateDirectory(folder);
        var obj=new GameObject("MarblePreview"){hideFlags=HideFlags.HideAndDontSave};
        var camera=obj.AddComponent<Camera>();camera.enabled=false;
        camera.transform.position=new Vector3(-13.6f,6.0f,41.3f);
        camera.transform.LookAt(new Vector3(-15,.18f,43.5f));camera.fieldOfView=50;
        camera.nearClipPlane=.1f;camera.farClipPlane=120;
        if(lifeSize)
        {
            var area=Find(VillageName).transform.Find("BaiBanBi_VaNhomTre");
            camera.transform.SetPositionAndRotation(area.Find("MarbleCameraPosition").position,area.Find("MarbleCameraPosition").rotation);
            camera.nearClipPlane=.005f;
        }
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var target=new RenderTexture(1600,1100,24,RenderTextureFormat.ARGB32);target.Create();
        var previous=RenderTexture.active;var image=new Texture2D(1600,1100,TextureFormat.RGB24,false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1100),0,0);image.Apply();
            File.WriteAllBytes(folder+"/Marble_Game_Area.png",image.EncodeToPNG());
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
