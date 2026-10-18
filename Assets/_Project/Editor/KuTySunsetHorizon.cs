using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTySunsetHorizon
{
    const string Folder = "Assets/_Project/Art/Environment/City";
    [MenuItem("KuTy/Office Intro/Add Sunset Horizon")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Act1_Office_Blockout.unity")
            throw new InvalidOperationException("Open the office scene first.");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/T_SunsetHorizon.png");
        if (!texture) throw new InvalidOperationException("Sunset texture missing.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        var parent = GameObject.Find("OFFICE_BLOCKOUT/05_Exterior_Backdrop");
        if (!parent) throw new InvalidOperationException("Exterior parent missing.");
        if (parent.transform.Find("SunsetHorizon_Backdrop"))
            throw new InvalidOperationException("Sunset horizon already exists; edit it instead.");

        const int segments = 128;
        var vertices = new Vector3[(segments+1)*2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments*6];
        for (int i=0; i<=segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            var p = new Vector3(Mathf.Cos(angle)*70, 0, Mathf.Sin(angle)*70);
            vertices[i*2] = p + Vector3.down*63;
            vertices[i*2+1] = p + Vector3.up*117;
            uv[i*2] = new Vector2((float)i/segments,0);
            uv[i*2+1] = new Vector2((float)i/segments,1);
            if (i==segments) continue;
            int k=i*6, a=i*2;
            triangles[k]=a; triangles[k+1]=a+1; triangles[k+2]=a+2;
            triangles[k+3]=a+2; triangles[k+4]=a+1; triangles[k+5]=a+3;
        }
        var mesh = new Mesh { name="SunsetHorizonCylinder", vertices=vertices, uv=uv, triangles=triangles };
        mesh.RecalculateBounds(); mesh.RecalculateNormals();
        string meshPath=Folder+"/Models/SunsetHorizonCylinder.asset";
        var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (savedMesh) { EditorUtility.CopySerialized(mesh,savedMesh); UnityEngine.Object.DestroyImmediate(mesh); mesh=savedMesh; }
        else AssetDatabase.CreateAsset(mesh,meshPath);
        string matPath=Folder+"/Materials/MAT_SunsetHorizon.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat,matPath); }
        mat.SetTexture("_BaseMap",texture); mat.SetColor("_BaseColor",Color.white);
        mat.SetFloat("_Cull",0); mat.SetFloat("_Surface",0); mat.SetFloat("_ZWrite",1);
        mat.renderQueue=2000; EditorUtility.SetDirty(mat);
        var go=new GameObject("SunsetHorizon_Backdrop",typeof(MeshFilter),typeof(MeshRenderer));
        Undo.RegisterCreatedObjectUndo(go,"Add sunset horizon");
        go.transform.SetParent(parent.transform,true);
        go.transform.position=Vector3.zero;
        go.transform.rotation=Quaternion.Euler(0,180,0);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        AssetDatabase.SaveAssets();
        KuTyCityBackdropImporter.CaptureCityPreview();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("SUNSET_HORIZON_READY: surrounding opaque backdrop; existing city objects preserved.");
    }
}
