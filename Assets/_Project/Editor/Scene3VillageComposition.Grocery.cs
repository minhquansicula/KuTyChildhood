using System;
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
    private const string GroceryOutput=OutputFolder+"/Grocery";
    private const string GroceryAssets="Assets/_Project/Art/Models/Scene3/Grocery";
    private const string GroceryTextures="Assets/_Project/Art/Textures/Scene3/Grocery";
    private static readonly string[] GrocerySources={"Assets/_Project/Art/Materials/Scene3/tiemtaphoa.fbx","Assets/_Project/Art/Materials/Scene3/Babantaphoa.fbx"};

    private static Material GroceryMaterial(int index,StringBuilder report)
    {
        EnsureFolder(GroceryAssets);EnsureFolder(GroceryTextures);
        string name=Path.GetFileNameWithoutExtension(GrocerySources[index]),folder=GroceryTextures+"/"+name;EnsureFolder(folder);
        var importer=(ModelImporter)AssetImporter.GetAtPath(GrocerySources[index]);
        if(!Directory.GetFiles(folder).Any(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)))
        {importer.ExtractTextures(folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
        var images=Directory.GetFiles(folder).Where(p=>new[]{".png",".jpg",".jpeg",".tga"}.Contains(Path.GetExtension(p).ToLowerInvariant())).Select(p=>p.Replace('\\','/')).ToArray();
        var albedo=images.FirstOrDefault(p=>!p.ToLowerInvariant().Contains("normal") && !p.ToLowerInvariant().Contains("rough") && !p.ToLowerInvariant().Contains("metal") && !p.ToLowerInvariant().Contains("ao."));
        var normal=images.FirstOrDefault(p=>p.ToLowerInvariant().Contains("normal"));
        if(albedo==null)throw new Exception("Missing grocery albedo: "+name);
        foreach(string image in images)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(image);bool n=image==normal;
            bool changed=ti.maxTextureSize!=2048 || ti.isReadable || !ti.mipmapEnabled || ti.anisoLevel!=2 || ti.textureCompression!=TextureImporterCompression.Compressed || ti.textureType!=(n?TextureImporterType.NormalMap:TextureImporterType.Default) || ti.sRGBTexture!=(image==albedo);
            ti.maxTextureSize=2048;ti.isReadable=false;ti.mipmapEnabled=true;ti.anisoLevel=2;ti.textureCompression=TextureImporterCompression.Compressed;ti.textureType=n?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=image==albedo;
            if(changed)ti.SaveAndReimport();
        }
        string path=GroceryAssets+"/"+name+"_Mat.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name+"_Mat"};AssetDatabase.CreateAsset(mat,path);}
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));mat.SetColor("_BaseColor",new Color(.91f,.9f,.86f));mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.12f);mat.enableInstancing=true;
        if(normal!=null){mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));mat.SetFloat("_BumpScale",.35f);mat.EnableKeyword("_NORMALMAP");}
        EditorUtility.SetDirty(mat);report.AppendLine(name+" material: "+albedo+", 2K compressed mipmaps, matte URP Lit");return mat;
    }

    public static void InspectGroceryModels()
    {
        Directory.CreateDirectory(GroceryOutput);var report=new StringBuilder();
        for(int i=0;i<GrocerySources.Length;i++)
        {
            var mat=GroceryMaterial(i,report);var temp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GrocerySources[i]));temp.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                temp.transform.position=new Vector3(0,100,0);var b=RendererBounds(temp.transform);float s=(i==0?4.5f:1.58f)/b.size.y;temp.transform.localScale*=s;b=RendererBounds(temp.transform);temp.transform.position+=new Vector3(-b.center.x,100-b.min.y,-b.center.z);
                foreach(var t in temp.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                foreach(var r in temp.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
                report.AppendLine("MODEL "+i+" "+RendererBounds(temp.transform));
                foreach(var f in temp.GetComponentsInChildren<MeshFilter>())report.AppendLine("mesh "+f.name+" tris "+Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(k=>(long)f.sharedMesh.GetIndexCount(k)/3)+" submeshes "+f.sharedMesh.subMeshCount+" local "+f.transform.localPosition+" scale "+f.transform.lossyScale);
                var center=RendererBounds(temp.transform).center;float d=i==0?10:3;
                foreach(int side in new[]{0,1,2,3}){var direction=Quaternion.Euler(0,side*90,0)*Vector3.forward;GroceryShot(center+direction*d+Vector3.up*(i==0?1:0),center,GroceryOutput+"/source_"+i+"_"+side+".png",1<<31,true);}
            }
            finally{UnityEngine.Object.DestroyImmediate(temp);}
        }
        var shop=Find("ShopCounter");report.AppendLine("CURRENT SHOP "+HierarchyPath(shop.transform)+" "+RendererBounds(shop.transform));
        report.AppendLine(EditorJsonUtility.ToJson(shop.GetComponent<ShopInteractable>(),true));
        foreach(var c in shop.GetComponentsInChildren<Collider>())report.AppendLine("Collider "+HierarchyPath(c.transform)+" "+c.bounds+" trigger "+c.isTrigger);
        foreach(var component in AllTransforms().SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c is ShopUI || c is ShopManager || c is QuestManager || c is PlayerInteraction))report.AppendLine(component.GetType().Name+" "+EditorJsonUtility.ToJson(component,true));
        File.WriteAllText(GroceryOutput+"/inspection.txt",report.ToString());AssetDatabase.SaveAssets();
    }

    private static void GroceryShot(Vector3 eye,Vector3 aim,string path,int mask=~0,bool isolated=false)
    {
        var obj=new GameObject("GroceryPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=50;camera.nearClipPlane=.03f;camera.farClipPlane=250;camera.cullingMask=mask;
        if(isolated){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.2f,.2f);}
        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(aim-eye));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(1300,900,24);rt.Create();var old=RenderTexture.active;var image=new Texture2D(1300,900,TextureFormat.RGB24,false);
        try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1300,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(obj);}
    }

    [MenuItem("KuTy/Scene 3/Replace grocery store and shopkeeper")]
    public static void InstallGroceryModels()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open outside memory scene in Edit Mode.");
        Directory.CreateDirectory(GroceryOutput);string backup=GroceryOutput+"/before-new-grocery.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up grocery scene.");
        var before=GameplaySnapshot();var report=new StringBuilder();var shop=Find("ShopCounter");Undo.RegisterFullObjectHierarchyUndo(shop,"Replace grocery visuals, retain buying interaction");
        foreach(Transform child in shop.transform)if(child.name!="TapHoa_ModelMoi")child.gameObject.SetActive(false);
        var oldRenderer=shop.GetComponent<Renderer>();if(oldRenderer)oldRenderer.enabled=false;
        // The old counter root was scaled non-uniformly. Normalize its container before
        // fitting source bounds, so the FBX's proportions and its embedded axis conversion survive.
        shop.transform.SetPositionAndRotation(new Vector3(1,.2f,-3),Quaternion.identity);shop.transform.localScale=Vector3.one;
        var visual=shop.transform.Find("TapHoa_ModelMoi");if(!visual)visual=Group("TapHoa_ModelMoi",shop.transform).transform;visual.gameObject.SetActive(true);
        var store=GroceryVisual(visual,"TiemTapHoa_Moi",0,GroceryMaterial(0,report));FitGroceryVisual(store,4.5f,new Vector3(1,.15f,-3),180);
        // Stand beside the wares on the front apron, clear of the hanging calendar
        // and boxes baked into the shop mesh. A seller behind them was hidden.
        var seller=GroceryVisual(visual,"BaBanTapHoa",1,GroceryMaterial(1,report));FitGroceryVisual(seller,1.55f,new Vector3(-1.2f,.2f,-6.5f),180);
        // A few simple solid colliders keep the porch accessible. Do not wrap the
        // entire model in a solid bounding box, which would block the shopkeeper.
        var collision=visual.Find("VaChamTiem");if(!collision)collision=Group("VaChamTiem",visual).transform;
        GroceryBox(collision,"NenHien",new Vector3(1,.19f,-3),new Vector3(5.9f,.12f,5.6f));
        GroceryBox(collision,"KhoHang",new Vector3(1,1.6f,-1.85f),new Vector3(5.25f,2.7f,3.65f));
        GroceryBox(collision,"QuayHang",new Vector3(1.45f,.8f,-5.08f),new Vector3(2.65f,1.1f,.68f));
        GroceryBox(collision,"CotTrai",new Vector3(-1.46f,1.55f,-5.32f),new Vector3(.18f,2.6f,.18f));
        GroceryBox(collision,"CotPhai",new Vector3(3.42f,1.55f,-5.32f),new Vector3(.18f,2.6f,.18f));
        var body=seller.GetComponent<CapsuleCollider>();if(!body)body=Undo.AddComponent<CapsuleCollider>(seller.gameObject);
        float scale=seller.lossyScale.x;body.center=seller.InverseTransformPoint(new Vector3(-1.2f,.95f,-6.5f));body.height=1.45f/scale;body.radius=.2f/scale;seller.gameObject.layer=LayerMask.NameToLayer("Interactable");
        var trigger=shop.GetComponent<BoxCollider>();if(!trigger)trigger=Undo.AddComponent<BoxCollider>(shop);trigger.enabled=true;trigger.isTrigger=true;
        trigger.center=new Vector3(0,.95f,-3.2f);trigger.size=new Vector3(5.1f,1.9f,1.6f);shop.layer=LayerMask.NameToLayer("Interactable");
        VerifyGameplay(before);Physics.SyncTransforms();ValidateGrocery(report);VerifyReferenceWalking();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save grocery integration.");
        File.WriteAllText(GroceryOutput+"/report.txt",report.ToString());CaptureGroceryScene();Inspect(true);SceneView.RepaintAll();
        File.WriteAllText(GroceryOutput+"/result.json","{\"passed\":true,\"storeHeight\":4.5,\"sellerHeight\":1.55}");
    }

    private static Transform GroceryVisual(Transform parent,string name,int source,Material material)
    {
        var holder=parent.Find(name);if(!holder)holder=Group(name,parent).transform;
        if(holder.childCount==0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GrocerySources[source]);if(!prefab)throw new Exception("Missing "+GrocerySources[source]);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,holder);Undo.RegisterCreatedObjectUndo(model,"Place imported grocery model");
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=prefab.transform.localScale;
        }
        foreach(var r in holder.GetComponentsInChildren<Renderer>()){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
        foreach(var a in holder.GetComponentsInChildren<Animator>())if(!a.runtimeAnimatorController)a.enabled=false;
        foreach(var c in holder.GetComponentsInChildren<Camera>())c.gameObject.SetActive(false);
        foreach(var l in holder.GetComponentsInChildren<Light>())l.gameObject.SetActive(false);
        return holder;
    }

    private static void FitGroceryVisual(Transform holder,float height,Vector3 bottom,float yaw)
    {
        holder.localScale=Vector3.one;holder.SetPositionAndRotation(bottom,Quaternion.Euler(0,yaw,0));var b=RendererBounds(holder);
        holder.localScale*=height/b.size.y;b=RendererBounds(holder);holder.position+=new Vector3(bottom.x-b.center.x,bottom.y-b.min.y,bottom.z-b.center.z);
    }
    private static void GroceryBox(Transform parent,string name,Vector3 center,Vector3 size)
    {
        var t=parent.Find(name);if(!t)t=Group(name,parent).transform;t.SetPositionAndRotation(center,Quaternion.identity);t.localScale=Vector3.one;
        var c=t.GetComponent<BoxCollider>();if(!c)c=Undo.AddComponent<BoxCollider>(t.gameObject);c.center=Vector3.zero;c.size=size;c.isTrigger=false;
    }
    private static T GroceryComponent<T>() where T:Component
    {return AllTransforms().Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.GetComponent<T>()).Single(c=>c);}
    private static void ValidateGrocery(StringBuilder report)
    {
        var shop=Find("ShopCounter");var store=shop.transform.Find("TapHoa_ModelMoi/TiemTapHoa_Moi");var seller=shop.transform.Find("TapHoa_ModelMoi/BaBanTapHoa");
        if(!shop.GetComponent<ShopInteractable>().enabled || !store || !seller || shop.transform.Find("TiemTapHoa_Visual").gameObject.activeSelf)throw new Exception("Missing new store or legacy visuals still active.");
        if(Mathf.Abs(RendererBounds(store).size.y-4.5f)>.01f || Mathf.Abs(RendererBounds(seller).size.y-1.55f)>.01f)throw new Exception("Grocery proportions failed.");
        var sellerBounds=RendererBounds(seller);if(Mathf.Abs(sellerBounds.min.y-ReferenceHeight(sellerBounds.center.x,sellerBounds.center.z))>.01f)throw new Exception("Shopkeeper does not stand on the ground.");
        var ui=new SerializedObject(GroceryComponent<ShopUI>());foreach(string key in new[]{"shopPanel","itemListParent","itemSlotPrefab","playerMoneyText","closeButton","messageText"})if(!ui.FindProperty(key).objectReferenceValue)throw new Exception("Missing shop UI reference: "+key);
        var quest=new SerializedObject(GroceryComponent<QuestManager>());foreach(string key in new[]{"candy","marbleJar","shop","inventory","memories"})if(!quest.FindProperty(key).objectReferenceValue)throw new Exception("Missing purchase quest reference: "+key);
        var manager=GroceryComponent<ShopManager>();var jar=(ItemData)quest.FindProperty("marbleJar").objectReferenceValue;if(!manager.ShopItems.Contains(jar))throw new Exception("Marble jar no longer sold.");
        var trigger=shop.GetComponent<BoxCollider>();
        foreach(float x in new[]{-.6f,1f,2.8f})
        {
            var eye=new Vector3(x,1.65f,-8);var aim=new Vector3(x,1.1f,-5.6f);
            if(!Physics.Raycast(eye,(aim-eye).normalized,out var hit,3f,~0,QueryTriggerInteraction.Collide) || hit.collider!=trigger)throw new Exception("Shop interaction ray blocked at x="+x+": "+(hit.collider?hit.collider.name:"no hit"));
        }
        report.AppendLine("PASS: original ShopInteractable, all six UI references, item list and quest references retained; interaction rays reach new counter from left/centre/right within 3m.");
        var probe=new GameObject("GroceryWalkProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=probe.AddComponent<CharacterController>();cc.height=1.6f;cc.center=Vector3.up*.8f;cc.radius=.24f;cc.stepOffset=.3f;
        try
        {
            cc.enabled=false;probe.transform.position=new Vector3(-5,.3f,-7.6f);cc.enabled=true;
            foreach(var target in new[]{new Vector3(-.5f,.3f,-7.6f),new Vector3(-.5f,.3f,-7.05f)})
            {
                for(int k=0;k<180;k++){var delta=target-probe.transform.position;delta.y=0;if(delta.magnitude<.08f)break;cc.Move(Vector3.ClampMagnitude(delta,.05f)+Vector3.down*.025f);}
                var remaining=target-probe.transform.position;remaining.y=0;if(remaining.magnitude>.2f || probe.transform.position.y<.1f)throw new Exception("Cannot walk from the road to new grocery counter: "+probe.transform.position);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(probe);}
        report.AppendLine("PASS: player-sized CharacterController can walk from village road to buying position; shopkeeper grounded and clear of the approach.");
        report.AppendLine("Store height 4.5m, shopkeeper 1.55m; uniform scale only, source geometry unchanged. Two renderers, shared matte materials; simple collision shapes.");
    }
    public static void CaptureGroceryScene()
    {
        Directory.CreateDirectory(GroceryOutput);GroceryShot(new Vector3(-5,3.1f,-12),new Vector3(1,1.9f,-3.7f),GroceryOutput+"/01_TiemTapHoa.png");
        GroceryShot(new Vector3(-.3f,1.65f,-9.8f),new Vector3(.15f,1.35f,-5.5f),GroceryOutput+"/02_QuayMuaBi.png");
    }
    public static void BeginGroceryPlayCheck()
    {
        var report=new StringBuilder();ValidateGrocery(report);if(SceneManager.GetActiveScene().isDirty)EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        SessionState.SetBool("GroceryPlayCheck",true);EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void RegisterGroceryPlayCheck()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("GroceryPlayCheck",false))return;
            double ready=EditorApplication.timeSinceStartup+2;EditorApplication.CallbackFunction check=null;
            check=()=>{if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=check;RunGroceryPlayCheck();};EditorApplication.update+=check;
        };
    }
    private static void RunGroceryPlayCheck()
    {
        var report=new StringBuilder();bool passed=false;
        try
        {
            var quest=QuestManager.Instance;var shop=ShopManager.Instance;var ui=ShopUI.Instance;var money=CurrencyManager.Instance;var inventory=InventoryManager.Instance;
            if(!quest || !shop || !ui || !money || !inventory)throw new Exception("Purchase singletons did not initialize.");
            var trigger=Find("ShopCounter").GetComponent<BoxCollider>();var eye=new Vector3(-.6f,1.65f,-8);var aim=new Vector3(-.6f,1.1f,-5.6f);Physics.SyncTransforms();
            if(!Physics.Raycast(eye,(aim-eye).normalized,out var hit,3f,~0,QueryTriggerInteraction.Collide) || hit.collider!=trigger || hit.collider.gameObject.layer!=LayerMask.NameToLayer("Interactable"))throw new Exception("Live interaction ray does not hit the store.");
            var interaction=hit.collider.GetComponentInParent<ShopInteractable>();
            if(!quest.CanShop){interaction.Interact();if(ui.IsOpen)throw new Exception("Quest lock was bypassed.");report.AppendLine("PASS: shopping remains locked before the purchase quest.");}
            // Temporary Play Mode fixture only. Unity restores the saved quest, funds
            // and inventory when leaving Play Mode; never write this test state to disk.
            var data=new SerializedObject(quest);data.FindProperty("currentStep").enumValueIndex=(int)QuestStep.BuyCandyAndMarbles;data.ApplyModifiedPropertiesWithoutUndo();
            int total=quest.Candy.price+quest.MarbleJar.price;money.AddMoney(total);int starting=money.CurrentMoney;
            if(string.IsNullOrEmpty(interaction.GetPromptText()))throw new Exception("Purchase prompt is hidden.");interaction.Interact();
            if(!ui.IsOpen)throw new Exception("Interaction failed to open shop.");
            var uiData=new SerializedObject(ui);var list=(Transform)uiData.FindProperty("itemListParent").objectReferenceValue;
            foreach(var item in new[]{quest.MarbleJar,quest.Candy})
            {
                var slot=list.GetComponentsInChildren<ShopItemSlotUI>().Single(s=>((TMPro.TMP_Text)new SerializedObject(s).FindProperty("itemNameText").objectReferenceValue).text==item.itemName);
                var button=(UnityEngine.UI.Button)new SerializedObject(slot).FindProperty("buyButton").objectReferenceValue;if(!button.interactable)throw new Exception("Purchase button disabled: "+item.itemName);
                int previous=money.CurrentMoney;button.onClick.Invoke();
                if(!inventory.HasItem(item) || money.CurrentMoney!=previous-item.price)throw new Exception("UI purchase did not deliver item or deduct correct amount: "+item.itemName);
                report.AppendLine("PASS: clicked actual UI button for "+item.itemName+", charged "+item.price+", item entered inventory.");
            }
            if(money.CurrentMoney!=starting-total || !quest.HasRequiredPurchases || !quest.CanPlayMarbles || quest.CurrentStep!=QuestStep.PlayMarbles || ui.IsOpen)throw new Exception("Purchase quest did not advance to marble game and close shop.");
            if(!MemoryCollectionManager.Instance.HasCollected(MemoryType.SimpleJoy))throw new Exception("Purchase memory was not awarded.");
            report.AppendLine("PASS: correct total charge, both items retained, shop closed, memory awarded, marble minigame unlocked. Saved scene quest state is unchanged.");passed=true;
        }
        catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
        finally
        {
            File.WriteAllText(GroceryOutput+"/play-verification.txt",report.ToString());File.WriteAllText(GroceryOutput+"/play-result.json","{\"passed\":"+(passed?"true":"false")+"}");
            SessionState.SetBool("GroceryPlayCheck",false);EditorApplication.ExitPlaymode();
        }
    }
}
