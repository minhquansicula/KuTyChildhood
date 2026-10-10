using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class Scene3VillageComposition
{
    public static void UpdateRiceLodDistances()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Update rice LOD distances in Edit Mode.");
        var village = Find(VillageName);
        if(!village) throw new Exception("Compact village missing.");
        var fields = village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        if(fields.Length != FieldRects.Length) throw new Exception("Expected all sixteen rice fields.");
        foreach(var field in fields)
        {
            Undo.RecordObject(field,"Increase rice LOD distances");
            field.nearDistance = 6; field.middleDistance = 25;
            EditorUtility.SetDirty(field);
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save rice LOD distances.");
        Debug.Log("[VillageComposition] Saved rice LOD distances: near 6m, middle 25m, fields " + fields.Length);
    }
    public static void Refine()
    {
        var village = Find(VillageName);
        RefineHomes(village);
        foreach(var field in village.GetComponentsInChildren<RuralRiceFieldRenderer>())
        {
            Undo.RecordObject(field,"Rice visibility throughout compact village");
            field.farDistance = 180; field.Regenerate(); EditorUtility.SetDirty(field);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    private static void RefineHomes(GameObject village)
    {
        for(var index=0;index<HomeNames.Length;index++)
        {
            var site = village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(index+1));
            var home = site.Find(HomeNames[index]);
            if(home.Find("Bottom_band"))
            {
                Undo.RecordObject(home,"Keep imported house upright");
                home.rotation=Quaternion.Euler(0,90,0)*Quaternion.Euler(-90,0,0);
                if(!site.Find("VaChamNha"))
                {
                    var bounds = RendererBounds(home);
                    var collision = Group("VaChamNha",site);
                    collision.transform.position=bounds.center;
                    collision.AddComponent<BoxCollider>().size=new Vector3(bounds.size.x*.72f,bounds.size.y*.8f,bounds.size.z*.8f);
                }
            }
            var center = HomePositions[index];
            foreach(var propName in new[]{"CayOi_SanNha","BoBanGheGo"})
            {
                var prop = site.Find(propName); if(!prop) continue;
                var bounds = RendererBounds(prop);
                var target = propName=="CayOi_SanNha" ? new Vector3(center.x+Mathf.Sign(center.x)*5.55f,bounds.center.y,center.z+5.2f)
                    : new Vector3(center.x-Mathf.Sign(center.x)*5.2f,bounds.center.y,center.z-5.3f);
                Undo.RecordObject(prop,"Place garden props inside compact yard"); prop.position += target-bounds.center;
            }
        }
    }
    private static Bounds RendererBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    public static void BatchComposeAndVerify()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            if(!Find(VillageName)) { Capture(); Compose(); }
            Refine(); Capture(); ValidateScene();
            EditorApplication.Exit(0);
        }
        catch(Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
    public static void BatchVerifyOnly()
    {
        try { EditorSceneManager.OpenScene(ScenePath); Capture(); ValidateScene(); EditorApplication.Exit(0); }
        catch(Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
    public static void Capture()
    {
        var folder = OutputFolder + (Find(VillageName) ? "/After" : "/Before");
        Directory.CreateDirectory(folder);
        var positions = new[] { new Vector3(61,77,-27), new Vector3(0,1.65f,-5), new Vector3(-4,3.2f,34) };
        var targets = new[] { new Vector3(0,0,36), new Vector3(0,1.5f,30), new Vector3(-17,1,45) };
        var names = new[] { "01_ToanCanh", "02_DuongLang", "03_SanBi" };
        for (var index = 0; index < positions.Length; index++)
        {
            var obj = new GameObject("VillagePreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = obj.AddComponent<Camera>(); camera.enabled = false;
            camera.transform.SetPositionAndRotation(positions[index], Quaternion.LookRotation(targets[index] - positions[index]));
            camera.fieldOfView = index == 0 ? 57 : 70; camera.nearClipPlane = .1f; camera.farClipPlane = 250;
            camera.clearFlags = CameraClearFlags.Skybox; camera.allowHDR = true;
            var extra = camera.GetUniversalAdditionalCameraData(); extra.renderPostProcessing = false;
            var destination = new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);
            destination.Create();
            var previous = RenderTexture.active;
            var texture = new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                RenderTexture.active = destination;
                texture.ReadPixels(new Rect(0,0,1600,1000),0,0); texture.Apply();
                File.WriteAllBytes(folder + "/" + names[index] + ".png",texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture); destination.Release(); UnityEngine.Object.DestroyImmediate(destination);
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        Debug.Log("[VillageComposition] Captured three views to " + folder);
    }

    public static void ValidateScene()
    {
        var village = Find(VillageName); if(!village) throw new Exception("Compact village missing.");
        var report = new LayoutReport { scene = ScenePath, fields = FieldRects.Length, groundWidth = 88, groundLength = 102 };
        ValidateLayout(report);
        var fields = village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        report.plants = fields.Sum(field=>field.PlantCount); report.tiles = fields.Sum(field=>field.TileCount);
        var material = fields[0].sharedMaterial;
        var errors = ShaderUtil.GetShaderMessages(material.shader).Where(message=>message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0) throw new Exception("Rice shader errors: " + string.Join("; ",errors.Select(e=>e.message)));
        for(var index=0;index<HomeNames.Length;index++)
        {
            var site = village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(index+1));
            var bounds = RendererBounds(site.Find(HomeNames[index]));
            if(bounds.min.y<.07f || bounds.size.y<3 || bounds.size.y>6) throw new Exception("House not upright/grounded: "+HomeNames[index]);
            foreach(var name in new[]{"CayOi_SanNha","BoBanGheGo"})
            {
                var prop = site.Find(name); if(!prop) continue;
                var propBounds = RendererBounds(prop);
                if(propBounds.min.x < -43 || propBounds.max.x > 43 || propBounds.min.z < -12 || propBounds.max.z>85)
                    throw new Exception("Garden prop outside compact map: "+site.name+"/"+name);
            }
        }
        report.checks.Add("All five homes stand upright; their trees and furniture lie within the village boundary");
        if(!Find("SanBi_DatNen").activeInHierarchy) throw new Exception("Marble clearing ground is inactive.");
        var limits = village.GetComponentsInChildren<BoxCollider>().Count(c=>c.name.StartsWith("RanhGioi_"));
        if(limits != 4) throw new Exception("Expected four map boundary colliders; got " + limits);
        Physics.SyncTransforms();
        foreach(var point in new[] { new Vector3(0,0,-5),new Vector3(0,0,44),new Vector3(-15,0,44),new Vector3(20,0,18.5f),new Vector3(-5,0,44),new Vector3(5,0,18.5f) })
        {
            var hits = Physics.RaycastAll(point+Vector3.up*3,Vector3.down,4,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.collider.GetComponent<CharacterController>() && !h.rigidbody).OrderBy(h=>h.distance).ToArray();
            if(hits.Length==0) throw new Exception("No walkable surface at "+point);
            var hit = hits[0];
            if(hit.point.y> .5f || hit.point.y < -.1f) throw new Exception("Unexpected path elevation at "+point+": "+hit.point);
        }
        report.checks.Add("Player start, main road, shop entry, marble clearing and canal bridges have walkable surfaces");
        report.checks.Add("Rice shader has no compiler errors and four boundary colliders are active");
        VerifyWalking();
        report.checks.Add("CharacterController traverses canal bridges to shop and marble clearing, and stays inside all four boundaries");
        report.passed = true;
        File.WriteAllText(OutputFolder+"/validation_report.json",JsonUtility.ToJson(report,true));
        Debug.Log("[VillageComposition] Validation passed.");
    }

    private static void VerifyWalking()
    {
        var obj = new GameObject("VillageWalkProbe") { hideFlags=HideFlags.HideAndDontSave };
        var controller = obj.AddComponent<CharacterController>();
        controller.height=1.6f; controller.center=Vector3.up*.8f; controller.radius=.25f; controller.stepOffset=.3f;
        try
        {
            foreach(var route in new[] {
                new[]{new Vector3(0,.12f,18.5f),new Vector3(10,.12f,18.5f),new Vector3(17,.12f,18.5f)},
                new[]{new Vector3(0,.12f,44),new Vector3(-8,.12f,44),new Vector3(-11,.12f,44)} })
            {
                controller.enabled=false; obj.transform.position=route[0]; controller.enabled=true; Physics.SyncTransforms();
                foreach(var goal in route.Skip(1))
                {
                    for(var step=0;step<200;step++)
                    {
                        var delta=goal-obj.transform.position; delta.y=0;
                        if(delta.magnitude<.15f) break;
                        controller.Move(Vector3.ClampMagnitude(delta,.12f)+Vector3.down*.05f);
                    }
                    var remaining=goal-obj.transform.position; remaining.y=0;
                    if(remaining.magnitude>.35f || obj.transform.position.y < -.15f) throw new Exception("Walking route blocked at "+obj.transform.position+" towards "+goal);
                }
            }
            foreach(var edge in new[]{new Vector3(40,.12f,42),new Vector3(-40,.12f,42),new Vector3(0,.12f,82),new Vector3(0,.12f,-9)})
            {
                controller.enabled=false; obj.transform.position=edge; controller.enabled=true; Physics.SyncTransforms();
                var direction = Mathf.Abs(edge.x)>1 ? new Vector3(Mathf.Sign(edge.x),0,0) : new Vector3(0,0,edge.z>0?1:-1);
                for(var step=0;step<80;step++) controller.Move(direction*.12f+Vector3.down*.04f);
                var p=obj.transform.position;
                if(p.x>43 || p.x< -43 || p.z>85 || p.z< -12) throw new Exception("Map boundary failed at "+p);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
}
