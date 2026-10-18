using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class KuTyOfficeBlockoutBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    const string Art = "Assets/_Project/Generated/OfficeBlockout";
    static Material wall, floor, wood, dark, metal, blue, white, accent, glass, screen, green;
    static Transform root;
    static Transform Group(string name, Transform parent, Vector3 p = default)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = p; return t;
    }
    static Transform Slot(string name, Transform parent, Vector3 p, float yaw = 0)
    {
        var slot = Group(name + "_SLOT", parent, p); slot.localRotation = Quaternion.Euler(0, yaw, 0);
        Group("Collision", slot); return Group("Visual_REPLACE", slot);
    }
    static GameObject Box(string name, Transform parent, Vector3 p, Vector3 size, Material mat, bool solid = true)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
        o.transform.SetParent(parent, false); o.transform.localPosition = p; o.transform.localScale = size;
        o.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        if (solid)
        {
            var collision = parent.name == "Visual_REPLACE" ? parent.parent.Find("Collision") : parent;
            var c = Group(name + "_Collider", collision, p); c.gameObject.AddComponent<BoxCollider>().size = size;
        }
        return o;
    }
    static void Shape(string name, Transform parent, PrimitiveType type, Vector3 p, Vector3 s, Material mat)
    {
        var o = GameObject.CreatePrimitive(type); o.name = name; o.transform.SetParent(parent,false);
        o.transform.localPosition=p; o.transform.localScale=s; o.GetComponent<Renderer>().sharedMaterial=mat;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    static Material Mat(string name, Color color, float emission = 0)
    {
        string path = Art + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.25f);
        if(emission>0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*emission); }
        EditorUtility.SetDirty(m); return m;
    }
    static void Label(string text, Transform parent, Vector3 p, float yaw, float scale = .07f)
    {
        var t=Group("Label_"+text,parent,p); t.localRotation=Quaternion.Euler(0,yaw,0);
        var tm=t.gameObject.AddComponent<TextMesh>(); tm.text=text; tm.fontSize=64; tm.characterSize=scale;
        tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=new Color(.86f,.92f,.94f);
    }
    static Transform Chair(string name, Transform parent, Vector3 p, float yaw=0)
    {
        var v=Slot(name,parent,p,yaw);
        Box("Seat",v,new Vector3(0,.48f,0),new Vector3(.58f,.12f,.56f),blue);
        Box("Back",v,new Vector3(0,.85f,-.25f),new Vector3(.58f,.68f,.1f),blue);
        Box("Stem",v,new Vector3(0,.25f,0),new Vector3(.1f,.4f,.1f),metal);
        Box("BaseA",v,new Vector3(0,.08f,0),new Vector3(.65f,.08f,.12f),dark);
        Box("BaseB",v,new Vector3(0,.08f,0),new Vector3(.12f,.08f,.65f),dark);
        return v.parent;
    }
    static void Desk(string name, Transform parent, Vector3 p, float yaw=0, bool hero=false)
    {
        var v=Slot(name,parent,p,yaw);
        Box("Desktop",v,new Vector3(0,.76f,0),new Vector3(1.8f,.08f,.85f),wood);
        foreach(float x in new[]{-.77f,.77f}) Box("Leg",v,new Vector3(x,.36f,0),new Vector3(.07f,.72f,.65f),metal);
        Box("Drawer",v,new Vector3(.55f,.43f,.06f),new Vector3(.42f,.55f,.6f),white);
        for(int i=0;i<3;i++) Box("DrawerHandle",v,new Vector3(.55f,.25f+i*.16f,-.247f),new Vector3(.18f,.025f,.025f),metal,false);
        Box("MonitorFoot",v,new Vector3(0,.83f,.2f),new Vector3(.34f,.035f,.22f),dark,false);
        Box("MonitorStand",v,new Vector3(0,1.02f,.25f),new Vector3(.055f,.35f,.055f),metal,false);
        Box("Monitor",v,new Vector3(0,1.21f,.25f),new Vector3(.7f,.43f,.05f),dark,false);
        Box("Display",v,new Vector3(0,1.21f,.219f),new Vector3(.64f,.37f,.008f),screen,false);
        Box("Keyboard",v,new Vector3(0,.82f,-.18f),new Vector3(.5f,.03f,.16f),dark,false);
        Box("Mouse",v,new Vector3(.42f,.825f,-.18f),new Vector3(.08f,.045f,.12f),dark,false);
        Box("PaperStack",v,new Vector3(-.6f,.83f,.12f),new Vector3(.32f,.06f,.24f),white,false);
        if(hero)
        {
            Shape("CoffeeCup",v,PrimitiveType.Cylinder,new Vector3(-.56f,.88f,-.23f),new Vector3(.11f,.07f,.11f),accent);
            Box("StickyNote",v,new Vector3(.29f,1.03f,.21f),new Vector3(.09f,.09f,.01f),accent,false);
            Shape("WaterBottle",v,PrimitiveType.Cylinder,new Vector3(.7f,.94f,.25f),new Vector3(.09f,.14f,.09f),blue);
        }
    }
    static void Cabinet(string name, Transform parent, Vector3 p, float yaw=0)
    {
        var v=Slot(name,parent,p,yaw);
        Box("Body",v,new Vector3(0,.95f,0),new Vector3(1.2f,1.9f,.45f),white);
        Box("DoorSeam",v,new Vector3(0,.95f,-.23f),new Vector3(.015f,1.82f,.015f),metal,false);
        foreach(float x in new[]{-.09f,.09f}) Box("Handle",v,new Vector3(x,1,-.26f),new Vector3(.025f,.18f,.035f),dark,false);
    }
    static void Lamp(Transform parent, Vector3 p)
    {
        var v=Slot("CeilingLight",parent,p);
        Box("Housing",v,Vector3.zero,new Vector3(1.3f,.07f,.33f),metal,false);
        Box("Diffuser",v,new Vector3(0,-.043f,0),new Vector3(1.19f,.015f,.25f),screen,false);
        var l=Group("Light",v,new Vector3(0,-.15f,0)).gameObject.AddComponent<Light>();
        l.type=LightType.Point; l.color=new Color(.82f,.9f,1); l.intensity=1.8f; l.range=5; l.shadows=LightShadows.None;
    }

    [MenuItem("KuTy/Office Blockout/Build New Map")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if(File.Exists(ScenePath)) throw new InvalidOperationException("Blockout already exists; edit the scene instead of overwriting it.");
        if(!EditorSceneManager.SaveOpenScenes()) throw new InvalidOperationException("Could not save open scenes.");
        Directory.CreateDirectory(Art); AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        wall=Mat("Wall",new Color(.62f,.68f,.68f)); floor=Mat("Carpet",new Color(.19f,.25f,.29f));
        wood=Mat("Oak",new Color(.56f,.39f,.24f)); dark=Mat("Charcoal",new Color(.075f,.11f,.14f));
        metal=Mat("Metal",new Color(.28f,.34f,.37f)); blue=Mat("Fabric",new Color(.16f,.29f,.35f));
        white=Mat("Paper",new Color(.83f,.85f,.8f)); accent=Mat("Ochre",new Color(.91f,.58f,.2f));
        glass=Mat("FrostedPanel",new Color(.4f,.57f,.62f)); screen=Mat("Screen",new Color(.56f,.78f,.87f),.6f);
        green=Mat("Leaves",new Color(.24f,.37f,.26f));
        root=Group("OFFICE_BLOCKOUT",null);
        var architecture=Group("01_Architecture",root);
        var shell=Slot("RoomShell",architecture,Vector3.zero);
        Box("Floor",shell,new Vector3(0,-.12f,0),new Vector3(14,.24f,10),floor);
        Box("NorthWall",shell,new Vector3(0,1.5f,5),new Vector3(14.2f,3,.18f),wall);
        Box("WestWall",shell,new Vector3(-7,1.5f,0),new Vector3(.18f,3,10),wall);
        Box("EastSill",shell,new Vector3(7,.45f,0),new Vector3(.18f,.9f,10),wall);
        Box("EastHeader",shell,new Vector3(7,2.85f,0),new Vector3(.18f,.3f,10),wall);
        Box("SouthWall",shell,new Vector3(0,1.5f,-5),new Vector3(14.2f,3,.18f),wall);
        for(int i=0;i<=5;i++) Box("WindowMullion",shell,new Vector3(7,1.8f,-5+2*i),new Vector3(.16f,1.8f,.065f),dark);
        // Invisible barrier keeps the open-window blockout safe to walk through.
        var windowBarrier=Group("WindowCollision",architecture,new Vector3(7,1.8f,0)).gameObject.AddComponent<BoxCollider>(); windowBarrier.size=new Vector3(.1f,1.8f,10);
        var ceiling=Slot("Ceiling_HIDE_FOR_TOP_VIEW",architecture,new Vector3(0,3.08f,0));
        Box("Ceiling",ceiling,Vector3.zero,new Vector3(14,.16f,10),white);
        for(int x=-6;x<=6;x+=2) Box("CeilingSeam",ceiling,new Vector3(x,-.086f,0),new Vector3(.015f,.005f,10),metal,false);
        for(int z=-4;z<=4;z+=2) Box("CeilingSeam",ceiling,new Vector3(0,-.086f,z),new Vector3(14,.005f,.015f),metal,false);
        foreach(float z in new[]{-4.88f,4.88f}) Box("Skirting",shell,new Vector3(0,.07f,z),new Vector3(13.8f,.14f,.04f),dark,false);

        var boss=Group("02_BossOffice",root);
        var partition=Slot("BossPartitions",boss,Vector3.zero);
        Box("FrontLower",partition,new Vector3(-4.5f,.55f,1),new Vector3(5,1.1f,.12f),wall);
        Box("FrontGlass",partition,new Vector3(-4.5f,1.9f,1),new Vector3(5,1.6f,.055f),glass);
        for(int i=0;i<=4;i++) Box("GlassFrame",partition,new Vector3(-7+i*1.25f,1.9f,.96f),new Vector3(.045f,1.8f,.07f),dark,false);
        Box("EastUpperWall",partition,new Vector3(-2,1.5f,3.9f),new Vector3(.14f,3,2.2f),wall);
        Box("EastSmallWall",partition,new Vector3(-2,1.5f,1.2f),new Vector3(.14f,3,.4f),wall);
        Box("DoorHeader",partition,new Vector3(-2,2.68f,2.1f),new Vector3(.2f,.64f,1.4f),wall);
        var pivot=Group("BossDoor_Hinge",boss,new Vector3(-2,0,2.8f));
        var door=Slot("DoorLeaf",pivot,Vector3.zero);
        Box("Leaf",door,new Vector3(0,1.16f,-.7f),new Vector3(.085f,2.32f,1.36f),wood);
        Box("Handle",door,new Vector3(.075f,1.03f,-1.21f),new Vector3(.08f,.04f,.19f),metal,false);
        Label("DIRECTOR",partition,new Vector3(-1.9f,2.65f,2.1f),-90,.055f);
        Desk("BossDesk",boss,new Vector3(-4.8f,0,3.5f),180);
        Chair("BossChair",boss,new Vector3(-4.8f,0,4.35f),180);
        Chair("GuestChairA",boss,new Vector3(-5.9f,0,2.25f));
        Chair("GuestChairB",boss,new Vector3(-6.55f,0,2.25f));
        Cabinet("BossCabinet",boss,new Vector3(-6.65f,0,4.05f),90);
        var mannequin=Slot("BossPlaceholder",boss,new Vector3(-4.8f,0,4.35f),180);
        Shape("Torso",mannequin,PrimitiveType.Capsule,new Vector3(0,.92f,0),new Vector3(.4f,.35f,.26f),dark);
        Shape("Head",mannequin,PrimitiveType.Sphere,new Vector3(0,1.47f,0),new Vector3(.23f,.28f,.23f),wood);

        var work=Group("03_Workstations",root);
        for(int i=0;i<3;i++)
        {
            var p=new Vector3(-5.5f+i*2.7f,0,-2);
            Desk("StaffDesk_"+(i+1),work,p); Chair("StaffChair_"+(i+1),work,p+new Vector3(0,0,-1));
            var divider=Slot("Cubicle_"+(i+1),work,p+new Vector3(0,0,.52f));
            Box("Panel",divider,new Vector3(0,.7f,0),new Vector3(1.95f,1.4f,.055f),blue);
            Box("TopRail",divider,new Vector3(0,1.42f,0),new Vector3(2,.035f,.07f),metal,false);
        }
        Desk("TyDesk",work,new Vector3(5.5f,0,-2.4f),90,true);
        var tyChair=Chair("TyChair",work,new Vector3(4.4f,0,-2.4f),90);
        var coat=tyChair.Find("Visual_REPLACE"); Box("Coat",coat,new Vector3(0,.85f,-.32f),new Vector3(.44f,.65f,.035f),accent,false);
        var seat=Group("TySeatAnchor",work,new Vector3(4.55f,0,-2.4f)); seat.localRotation=Quaternion.Euler(0,90,0);

        var utility=Group("04_UtilityArea",root);
        Cabinet("ArchiveCabinetA",utility,new Vector3(.3f,0,4.6f)); Cabinet("ArchiveCabinetB",utility,new Vector3(1.65f,0,4.6f));
        var printer=Slot("Printer",utility,new Vector3(3.2f,0,4.4f));
        Box("Stand",printer,new Vector3(0,.42f,0),new Vector3(1,.84f,.65f),white);
        Box("PrinterBody",printer,new Vector3(0,1,0),new Vector3(.8f,.32f,.55f),metal);
        Box("ScannerLid",printer,new Vector3(0,1.18f,0),new Vector3(.84f,.06f,.57f),dark,false);
        Box("PaperTray",printer,new Vector3(0,.94f,-.36f),new Vector3(.46f,.04f,.28f),dark,false);
        var water=Slot("WaterCooler",utility,new Vector3(5.5f,0,4.35f));
        Box("Body",water,new Vector3(0,.55f,0),new Vector3(.55f,1.1f,.5f),white);
        Shape("Bottle",water,PrimitiveType.Cylinder,new Vector3(0,1.35f,0),new Vector3(.37f,.25f,.37f),blue);
        Box("Tap",water,new Vector3(0,.85f,-.28f),new Vector3(.25f,.13f,.09f),dark,false);
        var bin=Slot("WasteBin",utility,new Vector3(2.4f,0,3.8f));
        Box("Body",bin,new Vector3(0,.23f,0),new Vector3(.35f,.46f,.35f),dark);
        var clock=Slot("Clock",utility,new Vector3(3.6f,2.35f,4.88f));
        Box("Case",clock,Vector3.zero,new Vector3(.7f,.28f,.06f),dark,false); Label("17:42",clock,new Vector3(0,0,-.04f),0,.065f);
        var plant=Slot("CornerPlant",utility,new Vector3(6.2f,0,-4.2f));
        Shape("Pot",plant,PrimitiveType.Cylinder,new Vector3(0,.25f,0),new Vector3(.4f,.25f,.4f),wood);
        for(int i=0;i<5;i++) Shape("Leaves",plant,PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*2)*.18f,.7f+i*.1f,Mathf.Cos(i*2)*.15f),new Vector3(.3f,.6f,.25f),green);

        var outside=Group("05_Exterior_Backdrop",root);
        var city=Slot("CitySilhouette",outside,Vector3.zero);
        Box("Street",city,new Vector3(14,-3,0),new Vector3(10,.1f,65),dark,false);
        for(int i=0;i<11;i++)
        {
            float h=5+(i*7%8); float z=-24+i*5;
            Box("Building_"+i,city,new Vector3(19+i%2*3,h*.5f-3,z),new Vector3(3,h,3.6f),i%2==0?wall:blue,false);
            for(int j=0;j<4;j++) Box("WindowBand",city,new Vector3(17.48f+i%2*3,-1+j*1.4f,z),new Vector3(.025f,.45f,2.8f),screen,false);
        }
        var lighting=Group("06_Lighting",root);
        foreach(float x in new[]{-4.7f,0,4.4f}) foreach(float z in new[]{-2.6f,3.2f}) Lamp(lighting,new Vector3(x,2.89f,z));
        var sun=Group("AfternoonSun",lighting).gameObject.AddComponent<Light>(); sun.type=LightType.Directional;
        sun.transform.rotation=Quaternion.Euler(32,-90,0); sun.color=new Color(1,.8f,.57f); sun.intensity=1.2f; sun.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.43f,.5f,.56f);
        RenderSettings.ambientEquatorColor=new Color(.33f,.39f,.42f); RenderSettings.ambientGroundColor=new Color(.2f,.22f,.23f);
        RenderSettings.fog=false;
        var player=Group("Player",null,new Vector3(-4.8f,.04f,2.05f));
        var cc=player.gameObject.AddComponent<CharacterController>(); cc.height=1.8f; cc.center=new Vector3(0,.9f,0); cc.radius=.25f; cc.stepOffset=.25f;
        var camera=Group("PlayerCamera",player,new Vector3(0,1.65f,0)).gameObject.AddComponent<Camera>();
        camera.tag="MainCamera"; camera.fieldOfView=68; camera.nearClipPlane=.05f; camera.farClipPlane=100;
        camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        var movement=player.gameObject.AddComponent<FirstPersonController>();
        var so=new SerializedObject(movement); so.FindProperty("playerCamera").objectReferenceValue=camera.transform;
        so.FindProperty("walkSpeed").floatValue=2; so.FindProperty("groundMask").intValue=~0; so.ApplyModifiedPropertiesWithoutUndo();
        var walkthrough=player.gameObject.AddComponent<OfficeBlockoutWalkthrough>();
        walkthrough.view=camera; walkthrough.movement=movement; walkthrough.doorPivot=pivot; walkthrough.seat=seat; walkthrough.chairRoot=tyChair;
        EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(60,-40,0),15);
        Debug.Log("OFFICE_BLOCKOUT_BUILT: "+ScenePath);
        Capture();
    }

    [MenuItem("KuTy/Office Blockout/Capture and Validate")]
    public static void Capture()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath) throw new InvalidOperationException("Open office blockout first.");
        Directory.CreateDirectory("Assets/Screenshots");
        var camera=new GameObject("TemporaryPreviewCamera").AddComponent<Camera>();
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); camera.fieldOfView=65; camera.nearClipPlane=.05f; camera.farClipPlane=100;
        var ceiling=GameObject.Find("Ceiling_HIDE_FOR_TOP_VIEW_SLOT");
        var exterior=GameObject.Find("05_Exterior_Backdrop");
        bool ceilingActive=ceiling && ceiling.activeSelf, exteriorActive=exterior && exterior.activeSelf;
        try
        {
            Render(camera,new Vector3(-4.8f,1.69f,2.05f),new Vector3(-4.8f,1.69f,4.2f),"OfficeBlockout_Boss.png");
            Render(camera,new Vector3(-1.1f,1.65f,2),new Vector3(3,1,-2),"OfficeBlockout_Workspace.png");
            Render(camera,new Vector3(4.55f,1.2f,-2.4f),new Vector3(5.55f,1.06f,-2.4f),"OfficeBlockout_Seated.png");
            if(ceiling) ceiling.SetActive(false);
            if(exterior) exterior.SetActive(false);
            camera.orthographic=true; camera.orthographicSize=8.7f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.1f,.14f,.17f);
            Render(camera,new Vector3(13,18,-16),new Vector3(0,.5f,0),"OfficeBlockout_Overview.png");
        }
        finally { if(ceiling) ceiling.SetActive(ceilingActive); if(exterior) exterior.SetActive(exteriorActive); UnityEngine.Object.DestroyImmediate(camera.gameObject); }
        Physics.SyncTransforms();
        var pivot=GameObject.Find("BossDoor_Hinge").transform; var rotation=pivot.localRotation;
        pivot.localRotation=Quaternion.Euler(0,95,0); Physics.SyncTransforms();
        var points=new[]{new Vector3(-4.8f,0,2.05f),new Vector3(-2.7f,0,2.05f),new Vector3(-1.2f,0,2.05f),new Vector3(2.8f,0,2.05f),new Vector3(2.8f,0,-2.4f),new Vector3(3.55f,0,-2.4f)};
        int blocked=0;
        for(int i=0;i<points.Length-1;i++)
        {
            Vector3 delta=points[i+1]-points[i];
            var hits=Physics.CapsuleCastAll(points[i]+Vector3.up*.35f,points[i]+Vector3.up*1.55f,.24f,delta.normalized,delta.magnitude);
            foreach(var hit in hits) if(!hit.transform.GetComponent<CharacterController>()) { blocked++; Debug.LogError("BLOCKOUT_PATH_BLOCKED: "+hit.collider.name); }
        }
        pivot.localRotation=rotation; Physics.SyncTransforms();

        File.WriteAllText("Assets/Screenshots/OfficeBlockout_Validation.txt","Route obstruction count: "+blocked+"\nSlots: "+UnityEngine.Object.FindObjectsOfType<Transform>().Count(t=>t.name.EndsWith("_SLOT"))+"\n");
        AssetDatabase.Refresh(); Debug.Log("OFFICE_BLOCKOUT_VALIDATED: blocked="+blocked);
    }
    static void Render(Camera camera, Vector3 from, Vector3 to, string filename)
    {
        camera.transform.position=from; camera.transform.LookAt(to);
        var rt=RenderTexture.GetTemporary(1440,900,24); var previous=RenderTexture.active;
        var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1440,900),0,0); image.Apply(); File.WriteAllBytes("Assets/Screenshots/"+filename,image.EncodeToPNG()); }
        finally { camera.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
    }
}
