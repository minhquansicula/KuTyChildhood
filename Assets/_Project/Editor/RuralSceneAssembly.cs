using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

internal static class RuralSceneAssembly
{
    const string Folder="Assets/_Project/Art/Countryside/";
    [MenuItem("KuTy/Development/Assemble Rural Landscape")]
    static void Assemble()
    {
        const string path="Assets/_Project/Scenes/Act1_CutScene2_Act2.unity";
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.GetComponent<Act2CountrysideShot>());
            var old=root.transform.Find("Rural landscape");
            if(old)Object.DestroyImmediate(old.gameObject);
            var world=new GameObject("Rural landscape").transform;
            world.SetParent(root.transform,false);
            foreach(Transform child in root.transform)
                if(child.name.Contains("Background image")||child.name.Contains("shadow")||child.name.Contains("Bamboo |"))child.gameObject.SetActive(false);
            var riceMat=ModelMaterial("RiceClump");
            var bananaMat=ModelMaterial("BananaClump");
            var cottageMat=ModelMaterial("TimberCottage");
            var dirt=AssetDatabase.LoadAssetAtPath<Material>(Folder+"FieldEarth.mat");
            if(!dirt){
                var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Textures/Scene3/Materials/dirt_aerial_02_diff_4k.mat");
                dirt=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(dirt,Folder+"FieldEarth.mat");
            }
            dirt.SetColor("_BaseColor",new Color(.62f,.52f,.36f));
            dirt.SetTextureScale("_BaseMap",new Vector2(12,12));
            EditorUtility.SetDirty(dirt);
            var earth=GameObject.CreatePrimitive(PrimitiveType.Plane);
            earth.name="Continuous earth beneath fields and village";
            earth.transform.SetParent(world,false);
            earth.transform.position=new Vector3(10,-1.4f,32);
            earth.transform.localScale=new Vector3(23,1,23);
            earth.GetComponent<Renderer>().sharedMaterial=dirt;
            Object.DestroyImmediate(earth.GetComponent<Collider>());
            var fieldMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"FieldGrass.mat");
            if(!fieldMaterial){fieldMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(fieldMaterial,Folder+"FieldGrass.mat");}
            fieldMaterial.SetColor("_BaseColor",new Color(.26f,.29f,.12f));
            fieldMaterial.SetTexture("_BaseMap",dirt.GetTexture("_BaseMap"));
            fieldMaterial.SetTextureScale("_BaseMap",new Vector2(2.5f,2.5f));
            fieldMaterial.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(fieldMaterial);
            for(int row=0;row<6;row++)
            for(int col=0;col<8;col++)
            {
                float x=-48+col*14;float z=-1+row*15;
                if((x>2&&x<23&&z>10&&z<32)||(x>-20&&x<0&&z>26&&z<48)||(x>23&&x<43&&z>40&&z<62))continue;
                var plot=GameObject.CreatePrimitive(PrimitiveType.Cube);
                plot.name="Raised rice plot";
                plot.transform.SetParent(world,false);
                plot.transform.position=new Vector3(x,-1.43f,z);
                plot.transform.localScale=new Vector3(13.4f,.09f,14.35f);
                plot.GetComponent<Renderer>().sharedMaterial=fieldMaterial;
                Object.DestroyImmediate(plot.GetComponent<Collider>());
            }
            var house=root.transform.Find("Farmhouse and earthen yard");
            var plants=new List<Transform>();
            var random=new System.Random(73);
            var sourceRice=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"RiceClump.fbx");
            // Rice grows in separated plots, leaving an open lane and a dry house yard.
            for(float z=-9;z<78;z+=1.15f)
            for(float x=-52;x<63;x+=1.15f)
            {
                float lane=7f+Mathf.Sin(z*.065f)*4f;
                if(Mathf.Abs(x-lane)<1.7f || Mathf.Abs(x+11f)<.7f || Mathf.Abs(z-30f)<.75f)continue;
                if(z>14f&&z<28f&&x>7f&&x<22f)continue;
                if(z>32f&&z<44f&&x>-17f&&x<-4f)continue;
                if(z>45f&&z<57f&&x>25f&&x<38f)continue;
                float height=.92f+(float)random.NextDouble()*.22f;
                var plant=Place(sourceRice,riceMat,world,"Rice",new Vector3(x+(float)random.NextDouble()*.5f,-1.39f,z+(float)random.NextDouble()*.5f),height,random.Next(360));
                plant.localScale = new Vector3(plant.localScale.x * 1.65f, plant.localScale.y, plant.localScale.z * 1.65f);
                plants.Add(plant);
            }
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"BananaClump.fbx"),bananaMat,world,"Bananas beside kitchen",new Vector3(18.8f,-1.38f,24.4f),3.8f,25);
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"BananaClump.fbx"),bananaMat,world,"Bananas at cottage",new Vector3(-15f,-1.38f,39f),3.1f,145);
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"TimberCottage.fbx"),cottageMat,world,"Timber cottage across fields",new Vector3(-10f,-1.39f,38f),3.1f,154);
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"TimberCottage.fbx"),cottageMat,world,"Distant cottage",new Vector3(31f,-1.39f,51f),2.7f,175);
            var bamboo=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"VillageBamboo.fbx");
            var bambooMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"VillageBamboo.mat");
            Place(bamboo,bambooMat,world,"Bamboo at lane",new Vector3(-5f,-1.39f,4),4.8f,30);
            Place(bamboo,bambooMat,world,"Bamboo behind house",new Vector3(17f,-1.39f,27),4.5f,100);
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Folder+"MorningSky.mat");
            if(!sky){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,Folder+"MorningSky.mat");}
            sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));
            sky.SetColor("_GroundColor",new Color(.57f,.52f,.42f));
            sky.SetFloat("_AtmosphereThickness",.65f);
            sky.SetFloat("_Exposure",1.1f);
            EditorUtility.SetDirty(sky);
            sky.shader=Shader.Find("KuTy/CountrysideMorningSky");
            EditorUtility.SetDirty(sky);RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.55f,.61f,.68f);
            RenderSettings.ambientEquatorColor=new Color(.58f,.54f,.45f);
            RenderSettings.ambientGroundColor=new Color(.28f,.25f,.19f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.014f;RenderSettings.fogColor=new Color(.74f,.72f,.65f);
            for(int i=0;i<14;i++)
                Place(bamboo,bambooMat,world,"Distant tree belt",new Vector3(-63+i*10,-1.39f,87+(i%3)*4),6.5f+(i%4),i*37);
            foreach(Transform child in root.transform)
            {
                if(!child.name.Contains("Background image"))continue;
                child.gameObject.SetActive(false);
                child.position=new Vector3(0f,23f,135f);
                child.localScale=new Vector3(260f,173.33f,1f);
                child.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            var camera=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Dream camera").GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=240;
            var focus=BoundsOf(house).center;
            Vector3 first=new Vector3(-.38f,2.65f,-8.5f);
            camera.transform.SetPositionAndRotation(first,Quaternion.LookRotation(new Vector3(10,.3f,25)-first));
            camera.fieldOfView=48;
            var settings=new SerializedObject(root.GetComponent<Act2CountrysideShot>());
            settings.FindProperty("houseTarget").objectReferenceValue=house;
            var end=(Transform)settings.FindProperty("cameraEnd").objectReferenceValue;
            end.SetPositionAndRotation(new Vector3(2.55f,1.89f,-2.91f),Quaternion.LookRotation(new Vector3(12,.2f,23)-new Vector3(2.55f,1.89f,-2.91f)));
            var list=settings.FindProperty("rice");
            list.arraySize=plants.Count;
            for(int i=0;i<plants.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=plants[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(camera,"Temp/rural-wide.jpg");
            Vector3 approach=Vector3.ProjectOnPlane(end.position-focus,Vector3.up).normalized;
            camera.transform.SetPositionAndRotation(focus+approach*13+Vector3.up*10,Quaternion.LookRotation(-approach*13-Vector3.up*10));
            Capture(camera,"Temp/rural-high.jpg");
            Debug.Log("Rural landscape saved with "+plants.Count+" rice clumps.");
        }
        finally{if(opened){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
    static Bounds BoundsOf(Transform t){
        var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;
        foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    static Transform Place(GameObject source,Material mat,Transform parent,string name,Vector3 feet,float height,float yaw){
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.name=name;
        go.transform.rotation=Quaternion.Euler(0,yaw,0);
        foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
        var b=BoundsOf(go.transform);go.transform.localScale*=height/b.size.y;b=BoundsOf(go.transform);
        go.transform.position+=feet-new Vector3(b.center.x,b.min.y,b.center.z);return go.transform;
    }
    static Material ModelMaterial(string name){
        string path=Folder+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name+"Color.png"));
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+name+"Normal.png");
        importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name+"Normal.png"));mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Cull",0);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
    }
    static void Capture(Camera c,string path){
        var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);System.IO.File.WriteAllBytes(path,image.EncodeToJPG(90));}
        finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
    }
}
