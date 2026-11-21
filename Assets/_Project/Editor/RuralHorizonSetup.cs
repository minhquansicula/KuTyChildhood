using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class RuralHorizonSetup
{
    [MenuItem("KuTy/Development/Update Rural Horizon")]
    static void Apply()
    {
        const string folder="Assets/_Project/Art/Countryside/";
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath("Assets/_Project/Scenes/Act1_CutScene2_Act2.unity");
        bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act1_CutScene2_Act2.unity",OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var sky=AssetDatabase.LoadAssetAtPath<Material>(folder+"MorningSky.mat");
            sky.SetTexture("_CloudMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"DawnClouds.png"));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox=sky;
            RenderSettings.fogDensity=.009f;
            var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.GetComponent<Act2CountrysideShot>());
            var world=root.transform.Find("Rural landscape");
            var old=world.Find("Distant hamlet");
            if(old)Object.DestroyImmediate(old.gameObject);
            var hamlet=new GameObject("Distant hamlet").transform;
            hamlet.SetParent(world,false);
            var positions=new[]{new Vector3(-38,-1.39f,60),new Vector3(-24,-1.39f,71),
                new Vector3(-5,-1.39f,64),new Vector3(15,-1.39f,77),
                new Vector3(40,-1.39f,66),new Vector3(57,-1.39f,79)};
            for(int i=0;i<positions.Length;i++)
            {
                string model=i%2==0?"TimberCottage":"FarmhouseCompound";
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+model+".fbx");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,hamlet);
                instance.name="Distant rural house "+(i+1);
                instance.transform.rotation=Quaternion.Euler(0,145+i*9,0);
                var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+model+".mat");
                var renderers=instance.GetComponentsInChildren<Renderer>();
                foreach(var r in renderers)r.sharedMaterial=mat;
                var b=renderers[0].bounds;
                foreach(var r in renderers)b.Encapsulate(r.bounds);
                instance.transform.localScale*= (3.0f+(i%3)*.25f)/b.size.y;
                b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                instance.transform.position+=positions[i]-new Vector3(b.center.x,b.min.y,b.center.z);
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var cam=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Dream camera").GetComponent<Camera>();
            Vector3 first = new Vector3(-.38f,2.65f,-8.5f);
            cam.transform.SetPositionAndRotation(first,Quaternion.LookRotation(new Vector3(10,.3f,25)-first));
            EditorSceneManager.SaveScene(scene);
            var initial=cam.transform.position;var rotation=cam.transform.rotation;
            Capture(cam,"Temp/rural-sky-wide.jpg");
            var house=root.transform.Find("Farmhouse and earthen yard");
            var rs=house.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;
            foreach(var r in rs)bounds.Encapsulate(r.bounds);
            Vector3 focus=bounds.center;focus.y=bounds.min.y+bounds.size.y*.53f;
            var settings=new SerializedObject(root.GetComponent<Act2CountrysideShot>());
            var end=(Transform)settings.FindProperty("cameraEnd").objectReferenceValue;
            var approach=Vector3.ProjectOnPlane(end.position-focus,Vector3.up).normalized;
            cam.transform.SetPositionAndRotation(focus+approach*13+Vector3.up*10,Quaternion.LookRotation(-approach*13-Vector3.up*10));
            Capture(cam,"Temp/rural-sky-high.jpg");
            cam.transform.SetPositionAndRotation(initial,rotation);
        }
        finally{if(opened){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
    static void Capture(Camera c,string path){
        var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);System.IO.File.WriteAllBytes(path,image.EncodeToJPG(90));}
        finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
    }
}
