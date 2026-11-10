using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2SourceKitchenBinding
{
    const string Output=Scene2SourceKitchenArticulation.Output;
    static Transform Group(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;Undo.RegisterCreatedObjectUndo(t.gameObject,"Articulate original kitchen");
        t.SetParent(parent,false);t.position=position;return t;
    }
    static Renderer Visual(string name,Transform parent,Mesh mesh,Material material,Vector3 position)
    {
        var t=Group(name,parent,position);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        t.gameObject.layer=LayerMask.NameToLayer("Interactable");return r;
    }
    static Mesh Bake(Mesh input,Transform source,Vector3 origin,string filename)
    {
        var mesh=UnityEngine.Object.Instantiate(input);mesh.name=filename;
        mesh.vertices=input.vertices.Select(v=>source.TransformPoint(v)-origin).ToArray();
        var normalMatrix=source.localToWorldMatrix.inverse.transpose;
        mesh.normals=input.normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
        mesh.tangents=input.tangents.Select(t=>
        {
            var d=source.localToWorldMatrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;
            return new Vector4(d.x,d.y,d.z,t.w);
        }).ToArray();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Scene2SourceKitchenArticulation.Art+"/"+filename+".asset");return mesh;
    }
    static BoxCollider Box(Transform t,Vector3 center,Vector3 size,bool trigger)
    {var c=Undo.AddComponent<BoxCollider>(t.gameObject);c.center=center;c.size=size;c.isTrigger=trigger;return c;}
    static Collider MeshSolid(Renderer r)
    {var c=Undo.AddComponent<MeshCollider>(r.gameObject);c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;return c;}
    static Transform View(string name,Transform parent,Vector3 position,Vector3 at)
    {var t=Group(name,parent,position);t.LookAt(at);return t;}
    static Light Glow(Transform parent,Vector3 position)
    {var t=Group("Source ember light",parent,position);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=1.7f;l.color=new Color(1,.38f,.1f);l.intensity=0;l.shadows=LightShadows.None;return l;}
    static Mesh RicePatch(Transform source)
    {
        var mesh=source.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices.Select(source.TransformPoint).ToArray();
        var material=source.GetComponent<Renderer>().sharedMaterial;var texture=material.GetTexture("_BaseMap");
        var rt=RenderTexture.GetTemporary(1024,1024,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
        var sample=new Texture2D(1024,1024,TextureFormat.RGB24,false);var uv=mesh.uv;
        try
        {
            Graphics.Blit(texture,rt);RenderTexture.active=rt;sample.ReadPixels(new Rect(0,0,1024,1024),0,0);sample.Apply();
            return Scene2OriginalGeometry.Triangles(mesh,(a,b,c)=>
            {
                var p=(vertices[a]+vertices[b]+vertices[c])/3;var tex=(uv[a]+uv[b]+uv[c])/3;
                var color=sample.GetPixelBilinear(tex.x-Mathf.Floor(tex.x),tex.y-Mathf.Floor(tex.y));
                return p.y>.86f&&p.y<.955f && new Vector2(p.x+6.126f,p.z+6.428f).sqrMagnitude<.27f*.27f && Mathf.Min(color.r,color.g,color.b)>.24f;
            });
        }
        finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(sample);}
    }
    [MenuItem("KuTy/Existing assets/Bind original source kitchen only")]
    public static void Bind()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Saved Home required; never overwrite pending user edits.");
        var kitchen=UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if(!kitchen||kitchen.GetComponent<Scene2SourceKitchenLayout>()||Directory.Exists(Scene2SourceKitchenArticulation.Art))
            throw new InvalidOperationException("Already bound or candidate assets exist; inspect rather than reinstall.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory("Builds/SourceKitchen-20261109");
        string backup="Builds/SourceKitchen-20261109/Home-before-source-binding.unity";
        if(File.Exists(backup)) throw new InvalidOperationException("One-shot backup exists; do not rerun blindly.");
        File.Copy(scene.path,backup);
        var before=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale,t.parent));
        using(var parts=Scene2SourceKitchenArticulation.BuildClosed())
        {
            Directory.CreateDirectory(Scene2SourceKitchenArticulation.Art);AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();
            try
            {
                var source=parts.source;var fuelSource=parts.fuelSource;
                var stoveMat=source.GetComponent<Renderer>().sharedMaterial;var riceMat=fuelSource.GetComponent<Renderer>().sharedMaterial;
                var original=source.GetComponent<MeshFilter>().sharedMesh;
                var originalWorld=original.vertices.Select(source.TransformPoint).ToArray();
                var sourceBounds=new Bounds(originalWorld[0],Vector3.zero);foreach(var p in originalWorld) sourceBounds.Encapsulate(p);
                Vector3 fireOrigin=new Vector3(sourceBounds.center.x,sourceBounds.min.y,sourceBounds.center.z);
                Vector3 cookOrigin=new Vector3(-6.0745f,.025f,-6.4259f);
                var body=Bake(parts.meshes[0],source,fireOrigin,"OriginalCookingBody");
                var lidBounds=new Bounds(source.TransformPoint(parts.meshes[1].vertices[0]),Vector3.zero);
                foreach(var v in parts.meshes[1].vertices) lidBounds.Encapsulate(source.TransformPoint(v));
                var lidOrigin=new Vector3(lidBounds.center.x,1.000f,lidBounds.center.z);
                var lidMesh=Bake(parts.meshes[1],source,lidOrigin,"OriginalCookingLid");
                var fireMesh=Bake(original,source,fireOrigin,"OriginalFireStove");
                var fuelMeshes=new Mesh[3];
                for(int i=0;i<3;i++)
                {
                    var vv=parts.meshes[i+2].vertices.Select(fuelSource.TransformPoint).ToArray();
                    var bounds=new Bounds(vv[0],Vector3.zero);foreach(var v in vv) bounds.Encapsulate(v);
                    fuelMeshes[i]=Bake(parts.meshes[i+2],fuelSource,bounds.center,"OriginalFuel"+(i+1));
                }
                var fire=Group("Original source fire tending",kitchen.worldRoot,fireOrigin);
                var cook=Group("Original source rice cooking",kitchen.worldRoot,cookOrigin);
                var fireVisual=Visual("Original SM_StovePot",fire,fireMesh,stoveMat,fireOrigin);
                var cookBody=Visual("Original pot and stove body",cook,body,stoveMat,cookOrigin);
                var lid=Visual("Original articulated lid",cook,lidMesh,stoveMat,cookOrigin+lidOrigin-fireOrigin).transform;
                var solids=kitchen.retainedColliders.Where(c=>c&&c.transform.IsChildOf(kitchen.washRoot)).ToList();
                solids.Add(MeshSolid(fireVisual));solids.Add(MeshSolid(cookBody));
                // Keep the visible lid solid for exploration; its own hit collider is
                // explicitly excluded from pointer occlusion, not the entire pot.
                solids.Add(MeshSolid(lid.GetComponent<Renderer>()));
                var lidHitRoot=Group("Original lid grip hit",lid,lid.position+Vector3.up*(lidMesh.bounds.max.y-.04f));
                var lidHit=Box(lidHitRoot,Vector3.zero,new Vector3(.16f,.14f,.16f),true);
                var hearth=Group("Original hearth opening",fire,fireOrigin+new Vector3(.64f,.43f,0));
                var hearthHit=Box(hearth,Vector3.zero,new Vector3(.22f,.30f,.42f),true);
                var logs=new Transform[3];var hits=new Collider[3];var slots=new Transform[3];
                for(int i=0;i<3;i++)
                {
                    var position=fireOrigin+new Vector3(.85f,0,(i-1)*.20f+.1f);
                    position.y=.028f-fuelMeshes[i].bounds.min.y;
                    logs[i]=Visual("Source kindling "+(i+1),fire,fuelMeshes[i],riceMat,position).transform;
                    hits[i]=Box(logs[i],fuelMeshes[i].bounds.center,fuelMeshes[i].bounds.size+Vector3.one*.025f,true);
                    slots[i]=Group("Source kindling slot "+i,fire,fireOrigin+new Vector3(.22f,.43f+(i%2)*.045f,(i-1)*.10f));
                }
                var blow=Group("Blow at the original hearth",fire,hearth.position+new Vector3(.03f,0,0));
                var blowHit=Box(blow,Vector3.zero,new Vector3(.20f,.25f,.35f),true);
                var fuel=Visual("Original fuel control",cook,fuelMeshes[0],riceMat,cookOrigin+new Vector3(.40f,.43f,0)).transform;
                var fuelHit=Box(fuel,fuelMeshes[0].bounds.center,fuelMeshes[0].bounds.size+Vector3.one*.035f,true);
                var fuelAxis=Group("Source fuel feed direction",cook,fuel.position);fuelAxis.rotation=Quaternion.LookRotation(Vector3.left);
                var steam=Group("Original pot steam",cook,lid.position+Vector3.up*.04f);
                var rest=Group("Original lid supported rest",cook,new Vector3(-4.78f,.028f,-6.55f));
                // Floor support uses actual mesh footprint at identity, not the source
                // object's tiny FBX units or its old nudge marker.
                FitRest(kitchen,lid,rest);
                using(var patch=new MeshLifetime(RicePatch(fuelSource)))
                {
                    if(patch.mesh.vertexCount<30) throw new InvalidOperationException("Original rice surface extraction is empty.");
                    var points=patch.mesh.vertices.Select(fuelSource.TransformPoint).ToArray();var rb=new Bounds(points[0],Vector3.zero);foreach(var p in points) rb.Encapsulate(p);
                    var riceMesh=Bake(patch.mesh,fuelSource,rb.center,"OriginalRiceSurface");
                    var rice=Visual("Original rice surface",cook,riceMesh,riceMat,cookOrigin+lidOrigin-fireOrigin);
                    rice.transform.position=new Vector3(lid.position.x,lid.position.y+.003f-riceMesh.bounds.max.y,lid.position.z);
                    var layout=Undo.AddComponent<Scene2SourceKitchenLayout>(kitchen.gameObject);
                    layout.originalStove=original;layout.originalRiceStove=fuelSource.GetComponent<MeshFilter>().sharedMesh;
                    layout.stoveMaterial=stoveMat;layout.riceMaterial=riceMat;layout.fireVisual=fireVisual;layout.cookingBody=cookBody;layout.cookingRice=rice;
                    layout.formerFireRoot=kitchen.fireRoot;layout.formerCookRoot=kitchen.cookRoot;
                    foreach(var old in new[]{kitchen.fireRoot,kitchen.cookRoot}) {Undo.RecordObject(old.gameObject,"Retain former kitchen inactive");old.gameObject.SetActive(false);}
                    foreach(var target in UnityEngine.Object.FindObjectsOfType<HouseholdChoreInteractable>(true).Where(t=>t.Chore==HouseholdChore.LightFire||t.Chore==HouseholdChore.CookRice))
                    {Undo.RecordObject(target,"Retarget source kitchen E interaction");target.enabled=false;}
                    Undo.AddComponent<HouseholdChoreInteractable>(fireVisual.gameObject).Configure(HouseholdChore.LightFire,fireVisual.transform);
                    Undo.AddComponent<HouseholdChoreInteractable>(cookBody.gameObject).Configure(HouseholdChore.CookRice,cookBody.transform);
                    Undo.RecordObject(kitchen,"Bind original source kitchen");
                    kitchen.fireRoot=fire;kitchen.cookRoot=cook;kitchen.hearth=hearth;kitchen.hearthHit=hearthHit;
                    kitchen.kindling=logs;kitchen.kindlingHits=hits;kitchen.kindlingSlots=slots;kitchen.blowTube=blow;kitchen.blowHit=blowHit;
                    kitchen.fireGlow=Glow(fire,hearth.position);kitchen.cookGlow=Glow(cook,cookOrigin+new Vector3(.1f,.5f,0));
                    kitchen.lid=lid;kitchen.lidHit=lidHit;kitchen.lidRestPoint=rest;kitchen.fuelLog=fuel;kitchen.fuelHit=fuelHit;kitchen.fuelAxis=fuelAxis;kitchen.steamAnchor=steam;
                    kitchen.fireView=View("Source fire close view",kitchen.worldRoot,fireOrigin+new Vector3(1.85f,1.25f,.75f),fireOrigin+new Vector3(.36f,.37f,0));
                    kitchen.cookView=View("Source cooking close view",kitchen.worldRoot,cookOrigin+new Vector3(1.85f,1.6f,.70f),cookOrigin+new Vector3(.1f,.74f,0));
                    kitchen.retainedColliders=solids.ToArray();kitchen.emberRenderers=new Renderer[0];kitchen.fireStoveRenderer=null;kitchen.importedKitchen=true;
                    EditorUtility.SetDirty(kitchen);EditorUtility.SetDirty(layout);
                }
                foreach(var entry in before)
                    if(!entry.Key||entry.Key.localPosition!=entry.Value.Item1||entry.Key.localRotation!=entry.Value.Item2||entry.Key.localScale!=entry.Value.Item3||entry.Key.parent!=entry.Value.Item4)
                        throw new InvalidOperationException("Source kitchen changed existing pose: "+entry.Key);
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.WriteAllText(Output+"/binding.txt",parts.report+"\nPASS preserved "+before.Count+" existing local poses/parents. Former fire/cook roots retained inactive. Wash setup and source FBXs untouched.\n");
                Capture(kitchen.fireView,"source-fire-edit.png");Capture(kitchen.cookView,"source-cook-edit.png");
                Debug.Log("[SourceKitchen] Original assets bound; local poses preserved. Inspect source-fire/source-cook evidence before play QA.");
            }
            catch {Undo.RevertAllDownToGroup(undo);throw;}
        }
    }
    sealed class MeshLifetime:IDisposable
    {public readonly Mesh mesh;public MeshLifetime(Mesh mesh){this.mesh=mesh;}public void Dispose()=>UnityEngine.Object.DestroyImmediate(mesh);}
    [MenuItem("KuTy/Existing assets/Refine original rice surface only")]
    public static void RefineRice()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
        var kitchen=UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();var layout=kitchen.GetComponent<Scene2SourceKitchenLayout>();
        var filter=layout.cookingRice.GetComponent<MeshFilter>();var asset=filter.sharedMesh;
        if(AssetDatabase.GetAssetPath(asset)!=Scene2SourceKitchenArticulation.Art+"/OriginalRiceSurface.asset")throw new InvalidOperationException("Unexpected rice mesh; do not overwrite user assets.");
        var source=Scene2SourceKitchenArticulation.Source("beplua_naucom");
        using(var lifetime=new MeshLifetime(RicePatch(source)))
        {
            var mesh=lifetime.mesh;if(mesh.vertexCount<30)throw new InvalidOperationException("No textured source rice triangles.");
            var world=mesh.vertices.Select(source.TransformPoint).ToArray();var bounds=new Bounds(world[0],Vector3.zero);foreach(var p in world)bounds.Encapsulate(p);
            float fit=.64f/Mathf.Max(bounds.size.x,bounds.size.z);
            mesh.vertices=world.Select(p=>Vector3.Scale(p-bounds.center,new Vector3(fit,.07f,fit))).ToArray();
            var nm=source.localToWorldMatrix.inverse.transpose;
            mesh.normals=mesh.normals.Select(n=>{var w=nm.MultiplyVector(n);return new Vector3(w.x/fit,w.y/.07f,w.z/fit).normalized;}).ToArray();
            mesh.tangents=mesh.tangents.Select(t=>{var w=source.localToWorldMatrix.MultiplyVector(new Vector3(t.x,t.y,t.z));w=Vector3.Scale(w,new Vector3(fit,.07f,fit)).normalized;return new Vector4(w.x,w.y,w.z,t.w);}).ToArray();mesh.RecalculateBounds();
            string backup="Builds/SourceKitchen-20261109/OriginalRiceSurface-before-refinement.asset";if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(asset),backup);
            EditorUtility.CopySerialized(mesh,asset);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
            Undo.RecordObject(layout.cookingRice.transform,"Align original source rice above contact repair");
            layout.cookingRice.transform.position=new Vector3(kitchen.lid.position.x,kitchen.lid.position.y+.004f-mesh.bounds.min.y,kitchen.lid.position.z);
            Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Scene2OriginalGeometry.Render(layout.cookingRice.transform,asset,"rice-source-refined-preview.png",new Vector3(0,1,.2f));
            File.Copy(Scene2ExistingAssetReview.Output+"/rice-source-refined-preview.png",Output+"/rice-source-refined-preview.png",true);
            File.WriteAllText(Output+"/rice-refinement.txt","Original white-rice texture islands selected from original triangles; baked metal/lid triangles excluded. Source UVs/material retained; source relief compressed vertically to fit the pot, all rice >=4mm above repaired body contact.\nTriangles="+asset.triangles.Length/3+"; thickness="+asset.bounds.size.y.ToString("F6"));
        }
        Debug.Log("[SourceKitchen] Original rice triangle surface refined; source FBX and non-kitchen layout untouched.");
    }
    static void FitRest(Scene2KitchenHandsOn kitchen,Transform lid,Transform rest)
    {
        Physics.SyncTransforms();var bounds=Scene2DishwashingLayout.VisualBounds(lid);
        float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
        var candidates=new[]{rest.position}.Concat(new[]{1.0f,1.2f,1.45f}.SelectMany(distance=>new[]{Vector3.right,Vector3.back,Vector3.forward,Vector3.left}.Select(direction=>bounds.center+direction*distance)));
        foreach(var candidate in candidates)
        {
            var hits=Physics.RaycastAll(new Vector3(candidate.x,bounds.max.y+.3f,candidate.z),Vector3.down,4,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.normal.y>.95f&&h.point.y<bounds.min.y-.25f&&!h.transform.IsChildOf(kitchen.worldRoot)&&!h.collider.GetComponentInParent<FirstPersonController>()).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                bool supported=true;
                for(int i=0;i<8;i++)
                {
                    var offset=new Vector3(Mathf.Cos(i*Mathf.PI/4),0,Mathf.Sin(i*Mathf.PI/4))*radius*.95f;
                    if(!hit.collider.Raycast(new Ray(hit.point+offset+Vector3.up*.1f,Vector3.down),out var edge,.2f)||Mathf.Abs(edge.point.y-hit.point.y)>.005f){supported=false;break;}
                }
                if(!supported)continue;
                var blocked=Physics.OverlapBox(hit.point+Vector3.up*(bounds.extents.y+.008f),new Vector3(radius+.012f,bounds.extents.y+.004f,radius+.012f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)
                    .Any(c=>c!=hit.collider&&!c.transform.IsChildOf(kitchen.worldRoot));
                if(blocked)continue;
                rest.rotation=lid.rotation;rest.position=hit.point+Vector3.up*.003f-new Vector3(bounds.center.x-lid.position.x,bounds.min.y-lid.position.y,bounds.center.z-lid.position.z);
                return;
            }
        }
        throw new InvalidOperationException("No clear fully supported source lid footprint; do not leave it floating.");
    }
    [MenuItem("KuTy/Existing assets/Fit source lid rest to real support only")]
    public static void FitSourceRest()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
        var kitchen=UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if(!kitchen.GetComponent<Scene2SourceKitchenLayout>()||kitchen.lidRestPoint.name!="Original lid supported rest")throw new InvalidOperationException("Source rest marker missing.");
        Undo.RecordObject(kitchen.lidRestPoint,"Support original lid on real floor");FitRest(kitchen,kitchen.lid,kitchen.lidRestPoint);
        Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output+"/lid-support.txt","Ray/8-edge footprint support and clear volume verified; only the newly authored source rest marker changed.\nRest="+kitchen.lidRestPoint.position.ToString("F6"));
        Debug.Log("[SourceKitchen] Source lid rest fitted to actual support; original props untouched.");
    }
    public static void Capture(Transform view,string file)
    {
        var go=new GameObject("Source kitchen evidence",typeof(Camera));var camera=go.GetComponent<Camera>();go.transform.SetPositionAndRotation(view.position,view.rotation);
        camera.fieldOfView=48;camera.nearClipPlane=.025f;var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Output+"/"+file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
