using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string ActorOutput=OutputFolder+"/Actors";
    private const string ActorAssets="Assets/_Project/Art/Models/Scene3/VillageActors";
    private const string ActorTextures="Assets/_Project/Art/Textures/Scene3/VillageActors";
    private const string ActorsMarker="DanLang_ModelDungTiLe";
    private static readonly string[] ActorModels={"Assets/_Project/Art/Materials/Scene3/contrau.fbx","Assets/_Project/Art/Materials/Scene3/nguoidatrau.fbx","Assets/_Project/Art/Materials/Scene3/trecon.fbx"};
    public static void BatchInspectVillageActors()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);Directory.CreateDirectory(ActorOutput);var report=new StringBuilder();
            report.AppendLine("SCENE "+ScenePath);
            foreach(string path in ActorModels)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab){report.AppendLine("MISSING "+path);continue;}
                var temp=(GameObject)PrefabUtility.InstantiatePrefab(prefab);temp.hideFlags=HideFlags.HideAndDontSave;
                report.AppendLine("MODEL "+path+" bounds "+RendererBounds(temp.transform));
                foreach(var f in temp.GetComponentsInChildren<MeshFilter>(true))report.AppendLine(" mesh "+f.name+" bounds "+f.sharedMesh.bounds+" tris "+Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3));
                foreach(var r in temp.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m)report.AppendLine(" material "+m.name+" shader "+m.shader.name+" tex "+AssetDatabase.GetAssetPath(m.mainTexture));
                UnityEngine.Object.DestroyImmediate(temp);
            }
            foreach(var tree in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name.StartsWith("CayMaxtree_")))
            {
                report.AppendLine("TREE "+HierarchyPath(tree)+" pos "+tree.position+" scale "+tree.lossyScale+" rot "+tree.eulerAngles);
                foreach(var f in tree.GetComponentsInChildren<MeshFilter>())report.AppendLine(" "+f.name+" local "+f.transform.localPosition+" rot "+f.transform.localEulerAngles+" scale "+f.transform.localScale+" mesh "+f.sharedMesh.bounds+" world "+f.GetComponent<Renderer>().bounds);
            }
            foreach(var m in AllTransforms().Select(t=>t.GetComponent<MarbleInteractable>()).Where(m=>m))
            {
                report.AppendLine("MARBLE "+HierarchyPath(m.transform)+" pos "+m.transform.position+" scale "+m.transform.lossyScale);
                foreach(var c in m.GetComponentsInChildren<Collider>())report.AppendLine(" hitbox "+c.GetType().Name+" "+c.bounds+" trigger "+c.isTrigger+" layer "+c.gameObject.layer);
            }
            File.WriteAllText(ActorOutput+"/inspection.txt",report.ToString());Inspect(true);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory(ActorOutput);File.WriteAllText(ActorOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
    }

    public static void BatchInstallVillageActors()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);InstallVillageActors();
            // The sky's ambient probe completes asynchronously after opening a scene.
            // Let the editor finish it before making the final visual inspection captures.
            DynamicGI.UpdateEnvironment();double readyAt=EditorApplication.timeSinceStartup+6;
            EditorApplication.CallbackFunction capture=null;
            capture=()=>
            {
                if(EditorApplication.timeSinceStartup<readyAt)return;EditorApplication.update-=capture;
                try{CaptureVillageActors();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Inspect(true);EditorApplication.Exit(0);}
                catch(Exception e){Debug.LogException(e);File.WriteAllText(ActorOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
            };
            EditorApplication.update+=capture;
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory(ActorOutput);File.WriteAllText(ActorOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
    }

    public static void BatchVerifyActorMotion()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);var report=new StringBuilder();
            foreach(var group in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name.StartsWith("NhomDanDatGiaSuc_")))
            {
                var clip=group.GetComponent<Animation>().clip;var original=new Pose(group.position,group.rotation);
                try
                {
                    for(int step=0;step<=4;step++)
                    {
                        float time=clip.length*(step+.1f)/4.2f;clip.SampleAnimation(group.gameObject,time);var at=group.position;
                        clip.SampleAnimation(group.gameObject,time+.02f);var travel=group.position-at;travel.y=0;
                        clip.SampleAnimation(group.gameObject,time);Physics.SyncTransforms();
                        if(travel.sqrMagnitude<.000001f || Vector3.Dot(group.forward,travel.normalized)<.98f)throw new Exception("Animation heading disagrees with travel at t="+time+", facing="+group.forward+", travel="+travel);
                        foreach(string name in new[]{"NguoiDat_ModelMoi","Trau_ModelMoi"})
                        {
                            var bounds=RendererBounds(group.Find(name));float ground=ReferenceHeight(bounds.center.x,bounds.center.z);
                            if(Mathf.Abs(bounds.min.y-ground)>.02f)throw new Exception("Moving actor floats or sinks at t="+time+": "+name);
                        }
                    }
                }
                finally{group.SetPositionAndRotation(original.position,original.rotation);Physics.SyncTransforms();}
                report.AppendLine(group.name+": sampled full animation at five times; heading and ground contact passed.");
            }
            ValidateVillageActors(report);File.WriteAllText(ActorOutput+"/motion-verification.txt",report.ToString());if(File.Exists(ActorOutput+"/motion-error.txt"))File.Delete(ActorOutput+"/motion-error.txt");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText(ActorOutput+"/motion-error.txt",e.ToString());EditorApplication.Exit(1);}
    }

    [MenuItem("KuTy/Scene 3/Fit villagers, marble interaction and joined tree canopies")]
    public static void InstallVillageActors()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open the outside memory scene in Edit Mode.");
        Directory.CreateDirectory(ActorOutput);EnsureFolder(ActorAssets);EnsureFolder(ActorTextures);
        string backup=OutputFolder+"/before-actors-scale-hitbox-trees-20261018.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up village.");
        var report=new StringBuilder();
        var marbleGame=Find("BaiBanBi_VaNhomTre").GetComponentInChildren<MarbleShootingGame>();var marbleData=new SerializedObject(marbleGame);
        if(!marbleData.FindProperty("marbleGround").objectReferenceValue)
        {
            var floor=Find("SanBi_DatNen").GetComponent<Collider>();if(!floor)throw new Exception("Marble ground collider missing.");
            Undo.RecordObject(marbleGame,"Restore missing marble floor reference");marbleData.FindProperty("marbleGround").objectReferenceValue=floor;marbleData.ApplyModifiedProperties();report.AppendLine("Restored missing marbleGround reference to the existing SanBi_DatNen collider.");
        }
        var before=GameplaySnapshot();
        var village=Find(VillageName);Undo.RegisterFullObjectHierarchyUndo(village,"Fit villagers and attach tree crowns");
        var marker=Find(ActorsMarker);if(!marker)marker=Group(ActorsMarker,village.transform);
        var materials=new Material[3];for(int i=0;i<3;i++)materials[i]=ActorMaterial(i,report);
        ReplaceHerdActors(materials,report);PlaceMarbleChildren(materials[2],report);EnlargeMarbleInteraction(report);JoinVillageTreeCanopies(report);
        Physics.SyncTransforms();VerifyGameplay(before);ValidateVillageActors(report);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save actor integration.");
        File.WriteAllText(ActorOutput+"/report.txt",report.ToString());CaptureVillageActors();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        if(File.Exists(ActorOutput+"/error.txt"))File.Delete(ActorOutput+"/error.txt");
        File.WriteAllText(ActorOutput+"/result.json","{\"passed\":true,\"message\":\"Actor proportions, circular child placement, interaction hitbox and joined trees saved\"}");
    }

    private static Material ActorMaterial(int index,StringBuilder report)
    {
        string name=Path.GetFileNameWithoutExtension(ActorModels[index]),folder=ActorTextures+"/"+name;EnsureFolder(folder);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ActorModels[index]);
        if(!Directory.GetFiles(folder).Any(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase) || p.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)))
        {bool extracted=importer.ExtractTextures(folder);report.AppendLine(name+" embedded texture extraction: "+extracted);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
        var images=Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(p=>new[]{".png",".jpg",".jpeg",".tga"}.Contains(Path.GetExtension(p).ToLowerInvariant())).Select(p=>p.Replace('\\','/')).ToArray();
        string albedo=images.FirstOrDefault(p=>!p.ToLowerInvariant().Contains("normal") && !p.ToLowerInvariant().Contains("rough") && !p.ToLowerInvariant().Contains("metal") && !p.ToLowerInvariant().Contains("ao."));
        string normal=images.FirstOrDefault(p=>p.ToLowerInvariant().Contains("normal"));
        foreach(string image in images)
        {
            var texture=(TextureImporter)AssetImporter.GetAtPath(image);if(!texture)continue;bool isNormal=image==normal;
            bool changed=texture.maxTextureSize!=2048 || texture.textureType!=(isNormal?TextureImporterType.NormalMap:TextureImporterType.Default) || !texture.mipmapEnabled || texture.isReadable || texture.textureCompression!=TextureImporterCompression.Compressed || texture.anisoLevel!=2 || texture.sRGBTexture!=(!isNormal && image==albedo);
            texture.maxTextureSize=2048;texture.mipmapEnabled=true;texture.isReadable=false;texture.textureCompression=TextureImporterCompression.Compressed;texture.anisoLevel=2;
            texture.textureType=isNormal?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=!isNormal && image==albedo;
            if(changed)texture.SaveAndReimport();
        }
        string path=ActorAssets+"/"+name+"_Mat.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name+"_Mat"};AssetDatabase.CreateAsset(material,path);}
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(ActorModels[index]).GetComponentInChildren<Renderer>().sharedMaterial;
        var baseTexture=albedo!=null?AssetDatabase.LoadAssetAtPath<Texture2D>(albedo):source.mainTexture;
        if(baseTexture)material.SetTexture("_BaseMap",baseTexture);
        material.SetColor("_BaseColor",baseTexture?new Color(.91f,.9f,.86f):source.GetColor("_BaseColor"));
        material.SetFloat("_Smoothness",.12f);material.SetFloat("_Metallic",0);material.enableInstancing=true;
        if(normal!=null){material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetFloat("_BumpScale",.4f);material.EnableKeyword("_NORMALMAP");}
        EditorUtility.SetDirty(material);report.AppendLine(name+" shared material, albedo "+AssetDatabase.GetAssetPath(baseTexture));return material;
    }

    private static Transform ActorVisual(Transform parent,string name,int source,Material material)
    {
        var holder=parent.Find(name);if(!holder)holder=Group(name,parent).transform;
        if(holder.childCount==0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ActorModels[source]);var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,holder);Undo.RegisterCreatedObjectUndo(model,"Use imported villager model");
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=prefab.transform.localScale;
        }
        foreach(var renderer in holder.GetComponentsInChildren<Renderer>()){renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
        foreach(var animator in holder.GetComponentsInChildren<Animator>())if(!animator.runtimeAnimatorController)animator.enabled=false;
        return holder;
    }

    private static void FitActor(Transform actor,float height,Vector3 feet,Quaternion rotation,bool animal)
    {
        actor.localScale=Vector3.one;actor.SetPositionAndRotation(feet,rotation);var bounds=RendererBounds(actor);actor.localScale*=height/bounds.size.y;bounds=RendererBounds(actor);
        actor.position+=new Vector3(feet.x-bounds.center.x,feet.y-bounds.min.y,feet.z-bounds.center.z);
        float scale=actor.lossyScale.x;
        if(animal)
        {
            var box=actor.GetComponent<BoxCollider>();if(!box)box=Undo.AddComponent<BoxCollider>(actor.gameObject);
            box.center=actor.InverseTransformPoint(feet+Vector3.up*height*.48f);box.size=new Vector3(.66f,height*.85f,1.7f)/scale;
        }
        else
        {
            var capsule=actor.GetComponent<CapsuleCollider>();if(!capsule)capsule=Undo.AddComponent<CapsuleCollider>(actor.gameObject);
            capsule.center=actor.InverseTransformPoint(feet+Vector3.up*height*.48f);capsule.height=height*.93f/scale;capsule.radius=(height>1.4f?.21f:.18f)/scale;
        }
    }

    private static void ReplaceHerdActors(Material[] materials,StringBuilder report)
    {
        var groups=AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name.StartsWith("NhomDanDatGiaSuc_")).OrderBy(t=>t.name).ToArray();
        for(int i=0;i<groups.Length;i++)
        {
            var group=groups[i];Undo.RegisterFullObjectHierarchyUndo(group.gameObject,"Fit buffalo and herder");
            foreach(var t in group.Cast<Transform>().Where(t=>t.name!="Trau_ModelMoi" && t.name!="NguoiDat_ModelMoi" && t.name!="DayDat_ModelMoi").ToArray())t.gameObject.SetActive(false);
            var route=i==0?new[]{new Vector3(-5,0,34),new Vector3(-4.2f,0,27),new Vector3(-5.2f,0,20),new Vector3(-7,0,14)}:new[]{new Vector3(-3,0,49),new Vector3(-4.2f,0,43),new Vector3(-5,0,36)};
            var path=ReferenceSpline(route);int middle=path.Count/2;var direction=(path[Mathf.Min(middle+1,path.Count-1)]-path[Mathf.Max(0,middle-1)]).normalized;
            var p=path[middle];p.y=ReferenceHeight(p.x,p.z);group.SetPositionAndRotation(p,Quaternion.LookRotation(direction));
            var human=ActorVisual(group,"NguoiDat_ModelMoi",1,materials[1]);var buffalo=ActorVisual(group,"Trau_ModelMoi",0,materials[0]);
            FitActor(human,1.65f,group.TransformPoint(new Vector3(-.4f,0,1.15f)),group.rotation,false);
            FitActor(buffalo,1.45f,group.TransformPoint(new Vector3(.4f,0,-1.1f)),group.rotation,true);
            BuildHerdRope(group,human,buffalo);
            var animation=group.GetComponent<Animation>();if(animation)
            {
                string clipPath=ActorAssets+"/DanTrau_DuongCong_"+(i+1)+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(!clip){clip=new AnimationClip{name="DanTrau_DuongCong_"+(i+1),legacy=true};AssetDatabase.CreateAsset(clip,clipPath);}clip.ClearCurves();clip.wrapMode=WrapMode.Loop;
                var x=new AnimationCurve();var y=new AnimationCurve();var z=new AnimationCurve();var qx=new AnimationCurve();var qy=new AnimationCurve();var qz=new AnimationCurve();var qw=new AnimationCurve();float lastYaw=0;
                for(int k=0;k<path.Count;k++)
                {
                    float time=k*48f/(path.Count-1);var world=path[k];world.y=ReferenceHeight(world.x,world.z);var local=group.parent.InverseTransformPoint(world);
                    var forward=path[Mathf.Min(k+1,path.Count-1)]-path[Mathf.Max(0,k-1)];float angle=Quaternion.LookRotation(forward).eulerAngles.y-group.parent.eulerAngles.y;if(k>0)angle=lastYaw+Mathf.DeltaAngle(lastYaw,angle);lastYaw=angle;
                    x.AddKey(time,local.x);y.AddKey(time,local.y);z.AddKey(time,local.z);var q=Quaternion.Euler(0,angle,0);qx.AddKey(time,q.x);qy.AddKey(time,q.y);qz.AddKey(time,q.z);qw.AddKey(time,q.w);
                }
                clip.SetCurve("",typeof(Transform),"localPosition.x",x);clip.SetCurve("",typeof(Transform),"localPosition.y",y);clip.SetCurve("",typeof(Transform),"localPosition.z",z);
                clip.SetCurve("",typeof(Transform),"localRotation.x",qx);clip.SetCurve("",typeof(Transform),"localRotation.y",qy);clip.SetCurve("",typeof(Transform),"localRotation.z",qz);clip.SetCurve("",typeof(Transform),"localRotation.w",qw);clip.EnsureQuaternionContinuity();
                animation.AddClip(clip,clip.name);animation.clip=clip;animation.playAutomatically=true;animation.wrapMode=WrapMode.Loop;EditorUtility.SetDirty(clip);EditorUtility.SetDirty(animation);
            }
            report.AppendLine(group.name+": human 1.65 m, buffalo 1.45 m; original model proportions retained; road route and rope move with the pair.");
        }
    }
    private static void BuildHerdRope(Transform group,Transform human,Transform buffalo)
    {
        var rope=group.Find("DayDat_ModelMoi");if(!rope)rope=Group("DayDat_ModelMoi",group).transform;
        var line=rope.GetComponent<LineRenderer>();if(!line)line=Undo.AddComponent<LineRenderer>(rope.gameObject);
        line.useWorldSpace=false;line.positionCount=3;line.startWidth=line.endWidth=.012f;line.numCapVertices=2;
        var a=RendererBounds(human).center-group.forward*.1f+group.right*.3f;var b=RendererBounds(buffalo).center+group.forward*.85f+Vector3.up*.25f;
        line.SetPositions(new[]{rope.InverseTransformPoint(a),rope.InverseTransformPoint((a+b)*.5f-Vector3.up*.18f),rope.InverseTransformPoint(b)});
        string path=ActorAssets+"/DayDat.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="DayDat"};material.SetColor("_BaseColor",new Color(.28f,.2f,.11f));AssetDatabase.CreateAsset(material,path);}line.sharedMaterial=material;line.shadowCastingMode=ShadowCastingMode.Off;
    }

    private static void PlaceMarbleChildren(Material material,StringBuilder report)
    {
        if(Find(CrowdMarker)){PlaceShoulderCrowd(material,report);return;}
        var area=Find("BaiBanBi_VaNhomTre").transform;var game=area.GetComponentInChildren<MarbleShootingGame>();var serialized=new SerializedObject(game);
        var ring=(Collider)serialized.FindProperty("ringCollider").objectReferenceValue;var floor=(Collider)serialized.FindProperty("marbleGround").objectReferenceValue;
        var center=ring.transform.position;center.y=floor.bounds.max.y;
        foreach(var t in area.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("BanBe_PhacThao_") || t.name=="NhomTreChoibi").ToArray())t.gameObject.SetActive(false);
        var kids=area.Find("TreMoi_QuanhSanBi");if(!kids)kids=Group("TreMoi_QuanhSanBi",area).transform;
        float[] angles={40,110,155,225,310},radii={2.55f,2.5f,2.55f,2.45f,2.65f},heights={1.18f,1.23f,1.27f,1.2f,1.24f};
        for(int i=0;i<5;i++)
        {
            float a=angles[i]*Mathf.Deg2Rad;var p=center+new Vector3(Mathf.Cos(a)*radii[i],0,Mathf.Sin(a)*radii[i]);
            var kid=ActorVisual(kids,"TreChoibi_"+(i+1),2,material);FitActor(kid,heights[i],p,Quaternion.LookRotation(new Vector3(center.x-p.x,0,center.z-p.z)),false);
            report.AppendLine(kid.name+" height "+RendererBounds(kid).size.y+" m, feet "+p+", faces ring.");
        }
        report.AppendLine("Five children share one source mesh/material; east approach and close gameplay camera remain open.");
    }
    private static void EnlargeMarbleInteraction(StringBuilder report)
    {
        var interactable=AllTransforms().Select(t=>t.GetComponent<MarbleInteractable>()).First(m=>m && m.gameObject.activeInHierarchy);
        var game=(MarbleShootingGame)new SerializedObject(interactable).FindProperty("marbleGame").objectReferenceValue;
        var data=new SerializedObject(game);var ring=(Collider)data.FindProperty("ringCollider").objectReferenceValue;var floor=(Collider)data.FindProperty("marbleGround").objectReferenceValue;
        Undo.RecordObject(interactable.transform,"Expand marble interaction target");var p=ring.transform.position;p.y=floor.bounds.max.y;interactable.transform.SetPositionAndRotation(p,Quaternion.identity);interactable.transform.localScale=Vector3.one;
        var box=interactable.GetComponent<BoxCollider>();if(!box)box=Undo.AddComponent<BoxCollider>(interactable.gameObject);Undo.RecordObject(box,"Larger interaction trigger");
        box.center=new Vector3(0,.78f,0);box.size=new Vector3(3.2f,1.56f,3.2f);box.isTrigger=true;box.enabled=true;interactable.gameObject.layer=LayerMask.NameToLayer("Interactable");
        report.AppendLine("Marble interaction trigger: 3.2 x 1.56 x 3.2 m over the actual scoring ring. Scoring collider and game references unchanged.");
    }

    private static void JoinVillageTreeCanopies(StringBuilder report)
    {
        foreach(var group in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())
        {
            var lods=group.GetLODs();bool joined=lods.All(l=>l.renderers.Length==1 && l.renderers[0] && l.renderers[0].name.StartsWith("ThanTan_LienKhoi_"));if(joined)continue;
            for(int level=0;level<lods.Length;level++)
            {
                var renderers=lods[level].renderers.Where(r=>r).ToArray();var wood=renderers.First(r=>!r.name.StartsWith("TanLaXanh"));var canopy=renderers.FirstOrDefault(r=>r.name.StartsWith("TanLaXanh"));
                if(canopy)AlignCrownToBranches(group.transform,wood,canopy);
                var materials=new List<Material>();var batches=new List<List<CombineInstance>>();var signature=new StringBuilder();
                foreach(var renderer in renderers)
                {
                    var filter=renderer.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var matrix=group.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    signature.Append(AssetDatabase.GetAssetPath(mesh)).Append(matrix.ToString("F4"));
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var material=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)];int slot=materials.IndexOf(material);if(slot<0){slot=materials.Count;materials.Add(material);batches.Add(new List<CombineInstance>());}
                        batches[slot].Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=matrix});
                    }
                }
                string key=Hash128.Compute(signature.ToString()).ToString().Substring(0,10),path=ActorAssets+"/ThanTan_LOD"+level+"_"+key+".asset";
                var merged=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!merged)
                {
                    var temporary=new List<Mesh>();
                    try
                    {
                        for(int m=0;m<materials.Count;m++){var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batches[m].ToArray(),true,true);temporary.Add(mesh);}
                        merged=new Mesh{name="ThanTan_LOD"+level+"_"+key,indexFormat=IndexFormat.UInt32};merged.CombineMeshes(temporary.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);merged.RecalculateBounds();AssetDatabase.CreateAsset(merged,path);
                    }
                    finally{foreach(var temp in temporary)UnityEngine.Object.DestroyImmediate(temp);}
                }
                var obj=MeshProp(group.transform,"ThanTan_LienKhoi_LOD"+level,group.transform.position,merged,materials.ToArray());obj.transform.localPosition=Vector3.zero;obj.transform.localRotation=Quaternion.identity;obj.transform.localScale=Vector3.one;
                var combinedRenderer=obj.GetComponent<Renderer>();combinedRenderer.shadowCastingMode=ShadowCastingMode.On;combinedRenderer.receiveShadows=true;
                foreach(var old in renderers)old.gameObject.SetActive(false);
                lods[level].renderers=new[]{combinedRenderer};
            }
            group.SetLODs(lods);group.RecalculateBounds();EditorUtility.SetDirty(group);report.AppendLine(group.name+": trunk and crown aligned then combined in tree-local coordinates, one renderer per LOD.");
        }
    }
    private static void AlignCrownToBranches(Transform root,Renderer original,Renderer crown)
    {
        var filter=original.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var positions=mesh.vertices;var toRoot=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;var bounds=new Bounds();bool any=false;
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            var material=original.sharedMaterials[Mathf.Min(sub,original.sharedMaterials.Length-1)];if(!material || material.name.IndexOf("leaf",StringComparison.OrdinalIgnoreCase)<0)continue;
            foreach(int i in mesh.GetIndices(sub)){var p=toRoot.MultiplyPoint3x4(positions[i]);if(!any){bounds=new Bounds(p,Vector3.zero);any=true;}else bounds.Encapsulate(p);}
        }
        if(!any)throw new Exception("Cannot find original tree leaf attachment bounds: "+root.name);
        var canopyFilter=crown.GetComponent<MeshFilter>();var center=(root.worldToLocalMatrix*canopyFilter.transform.localToWorldMatrix).MultiplyPoint3x4(canopyFilter.sharedMesh.bounds.center);
        // Match the original leaf attachment, rather than assuming the FBX pivot is the crown centre.
        var shift=bounds.center-center;crown.transform.position+=root.TransformVector(shift);
    }

    private static void ValidateVillageActors(StringBuilder report)
    {
        var kids=Find("TreMoi_QuanhSanBi").transform;var area=Find("BaiBanBi_VaNhomTre").transform;var game=area.GetComponentInChildren<MarbleShootingGame>();var data=new SerializedObject(game);
        var ring=(Collider)data.FindProperty("ringCollider").objectReferenceValue;var floor=(Collider)data.FindProperty("marbleGround").objectReferenceValue;var center=ring.transform.position;
        int expected=Find(CrowdMarker)?12:5;if(kids.childCount!=expected)throw new Exception("Unexpected number of children around the marble ring: "+kids.childCount);
        foreach(Transform kid in kids)
        {
            var b=RendererBounds(kid);if(b.size.y<1.15f || b.size.y>1.3f || Mathf.Abs(b.min.y-floor.bounds.max.y)>.005f)throw new Exception("Child scale or foot contact failed.");
            var facing=center-b.center;facing.y=0;if(Vector3.Dot(kid.forward,facing.normalized)<.95f)throw new Exception("Child does not face the scoring ring.");
            if(Vector2.Distance(new Vector2(b.center.x,b.center.z),new Vector2(center.x,center.z))<2.3f)throw new Exception("Child enters marble play area.");
        }
        foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name=="NguoiDat_ModelMoi" || t.name=="Trau_ModelMoi")))
        {float target=t.name=="Trau_ModelMoi"?1.45f:1.65f;if(Mathf.Abs(RendererBounds(t).size.y-target)>.005f)throw new Exception("Herder/buffalo proportions failed.");}
        foreach(var group in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())foreach(var lod in group.GetLODs())if(lod.renderers.Length!=1 || !lod.renderers[0].name.StartsWith("ThanTan_LienKhoi_") || lod.renderers[0].transform.localPosition.sqrMagnitude>.00001f)throw new Exception("Tree still has separate crown/trunk transforms.");
        var interactable=area.GetComponentInChildren<MarbleInteractable>();var trigger=interactable.GetComponent<BoxCollider>();if(!trigger.isTrigger || trigger.bounds.size.x<3.19f || trigger.bounds.size.y<1.55f)throw new Exception("Interaction volume failed.");
        for(int i=0;i<4;i++)
        {
            var offset=Quaternion.Euler(0,90*i,0)*Vector3.forward;var eye=new Vector3(center.x,floor.bounds.max.y+1.45f,center.z)+offset*3.5f;var aim=new Vector3(center.x,floor.bounds.max.y+.7f,center.z);
            if(!Physics.Raycast(eye,(aim-eye).normalized,out var hit,3f,~0,QueryTriggerInteraction.Collide) || hit.collider!=trigger)throw new Exception("Marble interaction ray is blocked from side "+i+": "+(hit.collider?hit.collider.name:"no collider"));
        }
        VerifyReferenceWalking();report.AppendLine("PASS: actor heights/feet/facing, tree LOD unification, interaction rays from four directions, gameplay references and walking routes.");
    }

    private static void CaptureVillageActors()
    {
        var center=Find("ScoringRing").transform.position;var herd=Find("NhomDanDatGiaSuc_1").transform;var tree=Find("CayMaxtree_08_SanBi").transform;
        var views=new[]{new[]{center+new Vector3(6,3.8f,-7),center+Vector3.up*.7f},new[]{herd.position+new Vector3(6,2.7f,-5),herd.position+Vector3.up*.8f},new[]{tree.position+new Vector3(9,3,-8),tree.position+Vector3.up*3.2f}};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("ActorPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.nearClipPlane=.03f;camera.farClipPlane=250;camera.allowHDR=true;
            camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rt=new RenderTexture(1400,900,24);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(ActorOutput+"/"+(i==0?"01_TreChoiBi":i==1?"02_NguoiDatTrau":"03_CayLienKhoi")+".png",image.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
    }
}
