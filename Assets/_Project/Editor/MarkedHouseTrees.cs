using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class MarkedHouseTrees
{
    [MenuItem("KuTy/Development/Plant Marked House Trees")]
    static void Apply()
    {
        const string path="Assets/_Project/Scenes/Act1_CutScene2_Act2.unity";
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.GetComponent<Act2CountrysideShot>());
            var garden=root.transform.Find("Rural landscape/Village garden");
            var old=garden.Find("Trees at marked positions");if(old)Object.DestroyImmediate(old.gameObject);
            var group=new GameObject("Trees at marked positions").transform;group.SetParent(garden,false);
            var cam=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Dream camera").GetComponent<Camera>();
            var start=cam.transform.position;var rotation=cam.transform.rotation;float aspect=cam.aspect;
            var house=root.transform.Find("Farmhouse and earthen yard");
            var rs=house.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var focus=bounds.center;
            var approach=Vector3.ProjectOnPlane(new Vector3(2.55f,1.89f,-2.91f)-focus,Vector3.up).normalized;
            cam.aspect=1280f/720f;
            cam.transform.SetPositionAndRotation(focus+approach*13+Vector3.up*10,Quaternion.LookRotation(-approach*13-Vector3.up*10));
            Vector2[] marks={new Vector2(80,586),new Vector2(64,632),new Vector2(122,654),
                new Vector2(186,448),new Vector2(242,386),new Vector2(1237,488),
                new Vector2(1172,586),new Vector2(1242,596),new Vector2(1176,677),
                new Vector2(1027,100),new Vector2(1069,152),new Vector2(1113,111),
                new Vector2(1187,105),new Vector2(1223,125),new Vector2(1267,44)};
            var ground=new Plane(Vector3.up,new Vector3(0,-1.37f,0));
            for(int i=0;i<marks.Length;i++)
            {
                var ray=cam.ViewportPointToRay(new Vector3(marks[i].x/1280f,1-marks[i].y/720f,0));
                if(!ground.Raycast(ray,out float distance))continue;
                Vector3 feet=ray.GetPoint(distance);
                string model=i%3==0?"CayTanRong":"CayVuon";
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Scene3/MaxtreeOptimized/"+model+".fbx");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(source,group);go.name="Marked garden tree "+(i+1);
                go.transform.rotation=Quaternion.Euler(0,(i*137)%360,0);
                var renderers=go.GetComponentsInChildren<Renderer>();
                foreach(var r in renderers){
                    var mats=r.sharedMaterials;
                    for(int j=0;j<mats.Length;j++){
                        if(!mats[j])continue;
                        var ids=AssetDatabase.FindAssets(mats[j].name+" t:Material",new[]{"Assets/_Project/Art/Materials/Scene3/MaxtreeOptimized"});
                        if(ids.Length>0)mats[j]=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(ids[0]));
                    }
                    r.sharedMaterials=mats;
                }
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                float height=i<9?3.8f+(i%3)*.45f:4.6f+(i%3)*.4f;
                go.transform.localScale*=height/b.size.y;
                b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                go.transform.position+=feet-new Vector3(b.center.x,b.min.y,b.center.z);
            }
            Capture(cam,"Temp/marked-trees-high.jpg");
            cam.transform.SetPositionAndRotation(start,rotation);cam.aspect=aspect;
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        finally{if(opened){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
    static void Capture(Camera c,string path){
        var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;var img=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1280,720),0,0);System.IO.File.WriteAllBytes(path,img.EncodeToJPG(90));}
        finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(img);}
    }
}
