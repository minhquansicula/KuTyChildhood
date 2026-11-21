using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class RuralGardenLayout
{
    const string Folder="Assets/_Project/Art/Countryside/";
    [MenuItem("KuTy/Development/Make Open Village Garden")]
    static void Apply()
    {
        const string path="Assets/_Project/Scenes/Act1_CutScene2_Act2.unity";
        var before=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try{
            var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.GetComponent<Act2CountrysideShot>());
            var world=root.transform.Find("Rural landscape");
            var plants=new List<Transform>();
            var existingChildren = new List<Transform>();
            foreach(Transform child in world) existingChildren.Add(child);
            foreach(Transform t in existingChildren)
            {
                var p=t.position;
                if(t.name=="Rice"){
                    if((p.x < -22 && p.z>8 && p.z<28)||(p.x>28&&p.x<48&&p.z>33&&p.z<47))plants.Add(t);
                    else Object.DestroyImmediate(t.gameObject);
                }
            }
            var props=world.Find("Village garden");
            if(props)Object.DestroyImmediate(props.gameObject);
            props=new GameObject("Village garden").transform;props.SetParent(world,false);
            var grass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"FieldGrass.mat");
            var earth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"FieldEarth.mat");
            foreach(Transform t in existingChildren)
                if(t && t.name=="Continuous earth beneath fields and village")t.GetComponent<Renderer>().sharedMaterial=grass;
            var pathMaterial=new Material(earth);pathMaterial.name="Garden path";
            pathMaterial.SetColor("_BaseColor",new Color(.78f,.72f,.60f));
            pathMaterial.SetTextureScale("_BaseMap",new Vector2(.25f,.25f));
            var existing=AssetDatabase.LoadAssetAtPath<Material>(Folder+"GardenPath.mat");
            if(existing){Object.DestroyImmediate(pathMaterial);pathMaterial=existing;}
            else AssetDatabase.CreateAsset(pathMaterial,Folder+"GardenPath.mat");
            Patch("Open yard in front of house",new Vector3(12,-1.345f,17),new Vector2(15,12),pathMaterial);
            Vector3[] points={new Vector3(3,-1.34f,-15),new Vector3(5,-1.34f,0),new Vector3(7,-1.34f,13),new Vector3(12,-1.34f,18),new Vector3(15,-1.34f,34),new Vector3(25,-1.34f,51),new Vector3(20,-1.34f,80)};
            for(int i=1;i<points.Length;i++)Segment("Village dirt road",points[i-1],points[i],3.3f);
            Segment("Lane to cottage",new Vector3(14,-1.335f,33),new Vector3(-10,-1.335f,38),2.2f);
            Segment("Lane to far houses",new Vector3(19,-1.335f,64),new Vector3(-30,-1.335f,68),2f);
            var random=new System.Random(928);
            for(int i=0;i<37;i++)
            {
                float x=i<15?-18+(float)random.NextDouble()*18:25+(float)random.NextDouble()*36;
                float z=i<15?4+(float)random.NextDouble()*33:5+(float)random.NextDouble()*76;
                Tree(new Vector3(x,-1.37f,z),4.3f+(float)random.NextDouble()*3.5f,i);
            }
            for(int i=0;i<13;i++)Tree(new Vector3(-57+i*10,-1.39f,91+(i%3)*3),7+(i%4),i+40);
            var settings=new SerializedObject(root.GetComponent<Act2CountrysideShot>());
            var rice=settings.FindProperty("rice");rice.arraySize=plants.Count;
            for(int i=0;i<plants.Count;i++)rice.GetArrayElementAtIndex(i).objectReferenceValue=plants[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            var camera=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Dream camera").GetComponent<Camera>();
            var start=new Vector3(-.38f,2.65f,-8.5f);camera.transform.SetPositionAndRotation(start,Quaternion.LookRotation(new Vector3(10,.3f,25)-start));
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(camera,"Temp/garden-wide.jpg");
            var house=root.transform.Find("Farmhouse and earthen yard");
            var rs=house.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var end=(Transform)settings.FindProperty("cameraEnd").objectReferenceValue;var focus=b.center;
            var approach=Vector3.ProjectOnPlane(end.position-focus,Vector3.up).normalized;
            camera.transform.SetPositionAndRotation(focus+approach*13+Vector3.up*10,Quaternion.LookRotation(-approach*13-Vector3.up*10));
            Capture(camera,"Temp/garden-high.jpg");
            camera.transform.SetPositionAndRotation(start,Quaternion.LookRotation(new Vector3(10,.3f,25)-start));
            void Patch(string label,Vector3 pos,Vector2 size,Material mat){
                var go=GameObject.CreatePrimitive(PrimitiveType.Plane);Object.DestroyImmediate(go.GetComponent<Collider>());
                go.name=label;go.transform.SetParent(props,false);go.transform.position=pos;go.transform.localScale=new Vector3(size.x/10,1,size.y/10);go.GetComponent<Renderer>().sharedMaterial=mat;
            }
            void Segment(string label,Vector3 a,Vector3 b,float width){
                Patch(label,(a+b)*.5f,new Vector2(width,Vector3.Distance(a,b)+.6f),pathMaterial);
                props.GetChild(props.childCount-1).rotation=Quaternion.LookRotation(b-a);
            }
            void Tree(Vector3 pos,float height,int index){
                string model=index%3==0?"CayTanRong":"CayVuon";
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Scene3/MaxtreeOptimized/"+model+".fbx");
                if(!source)return;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(source,props);go.name="Garden tree "+index;
                go.transform.rotation=Quaternion.Euler(0,index*137,0);
                var rs=go.GetComponentsInChildren<Renderer>();
                foreach(var r in rs){
                    var mats=r.sharedMaterials;
                    for(int j=0;j<mats.Length;j++){
                        if(!mats[j])continue;
                        string[] ids=AssetDatabase.FindAssets(mats[j].name+" t:Material",new[]{"Assets/_Project/Art/Materials/Scene3/MaxtreeOptimized"});
                        if(ids.Length>0)mats[j]=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(ids[0]));
                    }
                    r.sharedMaterials=mats;
                }
                var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                go.transform.localScale*=height/bounds.size.y;bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                go.transform.position+=pos-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            }
        }finally{if(opened){SceneManager.SetActiveScene(before);EditorSceneManager.CloseScene(scene,true);}}
    }
    static void Capture(Camera c,string path){
        var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;var img=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1280,720),0,0);System.IO.File.WriteAllBytes(path,img.EncodeToJPG(90));}
        finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(img);}
    }
}
