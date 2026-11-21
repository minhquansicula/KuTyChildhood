using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class NaturalVillagePaths
{
    [MenuItem("KuTy/Development/Soften Village Paths")]
    static void Apply()
    {
        const string folder="Assets/_Project/Art/Countryside/";
        const string path="Assets/_Project/Scenes/Act1_CutScene2_Act2.unity";
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.GetComponent<Act2CountrysideShot>());
            var garden=root.transform.Find("Rural landscape/Village garden");
            var remove=new List<GameObject>();
            foreach(Transform t in garden)
                if(t.name.Contains("road")||t.name.Contains("Lane")||t.name.Contains("yard")||t.name=="Natural paths")remove.Add(t.gameObject);
            foreach(var item in remove)Object.DestroyImmediate(item);
            var group=new GameObject("Natural paths").transform;group.SetParent(garden,false);
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"SoftEarthPath.mat");
            if(!material){material=new Material(Shader.Find("KuTy/Soft Earth Path"));AssetDatabase.CreateAsset(material,folder+"SoftEarthPath.mat");}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Material>(folder+"FieldEarth.mat").GetTexture("_BaseMap"));
            EditorUtility.SetDirty(material);
            var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            void Ribbon(Vector3[] points,float width,float height)
            {
                for(int segment=0;segment<points.Length-1;segment++)
                for(int step=0;step<=18;step++)
                {
                    float t=step/18f;
                    Vector3 p0=points[Mathf.Max(0,segment-1)],p1=points[segment],p2=points[segment+1],p3=points[Mathf.Min(points.Length-1,segment+2)];
                    Vector3 center=Curve(p0,p1,p2,p3,t);
                    Vector3 ahead=Curve(p0,p1,p2,p3,Mathf.Min(1,t+.015f));
                    Vector3 tangent=(ahead-center).normalized;if(tangent.sqrMagnitude<.1f)tangent=(p2-p1).normalized;
                    Vector3 side=Vector3.Cross(Vector3.up,tangent);
                    float half=width*.5f*(.93f+.13f*Mathf.Sin(center.z*.51f)+.06f*Mathf.Sin(center.x*1.1f));
                    int first=vertices.Count;
                    float[] across={-1.42f,-1f,0,1f,1.42f};
                    for(int j=0;j<5;j++){
                        Vector3 v=center+side*half*across[j];v.y=height;
                        vertices.Add(v);colors.Add(new Color(1,1,1,j==0||j==4?0:1));
                    }
                    if(step>0)for(int j=0;j<4;j++){int a=first-5+j,b=first+j;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                }
            }
            Ribbon(new[]{new Vector3(3,0,-16),new Vector3(4,0,-3),new Vector3(7,0,8),new Vector3(12,0,17),new Vector3(15,0,34),new Vector3(23,0,51),new Vector3(20,0,81)},3.1f,-1.332f);
            Ribbon(new[]{new Vector3(15,0,33),new Vector3(7,0,35),new Vector3(-2,0,39),new Vector3(-10,0,38)},2.1f,-1.331f);
            Ribbon(new[]{new Vector3(20,0,64),new Vector3(2,0,65),new Vector3(-15,0,69),new Vector3(-31,0,68)},1.9f,-1.330f);
            // Nested irregular rings produce a worn yard with a feathered perimeter.
            int centerIndex=vertices.Count;vertices.Add(new Vector3(13,-1.329f,19));colors.Add(Color.white);
            for(int ring=0;ring<2;ring++)for(int i=0;i<72;i++){
                float a=i*Mathf.PI*2/72;float radius=(ring==0?1f:1.18f)*(1+.045f*Mathf.Sin(a*5)+.035f*Mathf.Sin(a*9));
                vertices.Add(new Vector3(13+Mathf.Cos(a)*8.1f*radius,-1.329f,19+Mathf.Sin(a)*5.9f*radius));
                colors.Add(new Color(1,1,1,ring==0?1:0));
            }
            for(int i=0;i<72;i++){
                int a=centerIndex+1+i,b=centerIndex+1+(i+1)%72,c=a+72,d=b+72;
                triangles.AddRange(new[]{centerIndex,b,a,a,b,c,c,b,d});
            }
            var mesh=new Mesh{name="Curved village lanes and soft yard"};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            const string meshPath=folder+"NaturalPaths.asset";
            var stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(stored){EditorUtility.CopySerialized(mesh,stored);Object.DestroyImmediate(mesh);mesh=stored;EditorUtility.SetDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,meshPath);
            var go=new GameObject("Curved earth with feathered grass edges",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            var cam=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Dream camera").GetComponent<Camera>();
            Vector3 start=new Vector3(-.38f,2.65f,-8.5f);var rotation=Quaternion.LookRotation(new Vector3(10,.3f,25)-start);
            cam.transform.SetPositionAndRotation(start,rotation);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(cam,"Temp/soft-path-wide.jpg");
            var house=root.transform.Find("Farmhouse and earthen yard");var rs=house.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var focus=bounds.center;var approach=Vector3.ProjectOnPlane(new Vector3(2.55f,1.89f,-2.91f)-focus,Vector3.up).normalized;
            cam.transform.SetPositionAndRotation(focus+approach*13+Vector3.up*10,Quaternion.LookRotation(-approach*13-Vector3.up*10));
            Capture(cam,"Temp/soft-path-high.jpg");cam.transform.SetPositionAndRotation(start,rotation);
        }
        finally{if(opened){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
    static Vector3 Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)=>.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
    static void Capture(Camera c,string path){
        var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;var img=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1280,720),0,0);System.IO.File.WriteAllBytes(path,img.EncodeToJPG(90));}
        finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(img);}
    }
}
