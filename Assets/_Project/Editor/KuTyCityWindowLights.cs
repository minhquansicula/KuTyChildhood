using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyCityWindowLights
{
    const string Folder="Assets/_Project/Art/Environment/City";
    [MenuItem("KuTy/Office Intro/Light City Windows")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/_Project/Scenes/Act1_Office_Blockout.unity") throw new InvalidOperationException("Open office first.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/MAT_City_material.mat");
        if(!material) throw new InvalidOperationException("City material missing.");
        // UV rectangles follow facade islands in the original 4096px atlas.
        // Coordinates below use a 1600px reference, with origin at the top left.
        const int size=2048;
        var pixels=new Color32[size*size];
        var rng=new System.Random(213);
        Grid(pixels,size,rng,790,1015,176,530,16,32, .36f);
        Grid(pixels,size,rng,1011,559,320,225,14,10, .42f);
        Grid(pixels,size,rng,1378,449,176,440,24,31, .30f);
        Grid(pixels,size,rng,790,548,166,314,22,20, .30f);
        Grid(pixels,size,rng,79,484,128,330,18,29, .36f);
        Grid(pixels,size,rng,245,466,179,423,16,11, .40f);
        Grid(pixels,size,rng,467,550,262,339,16,10, .36f);
        Grid(pixels,size,rng,1005,839,140,277,20,20, .30f);
        Grid(pixels,size,rng,1204,835,105,300,16,26, .30f);
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
        texture.SetPixels32(pixels); texture.Apply();
        string path=Folder+"/Textures/T_CityWindowEmission.png";
        File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture=false; importer.mipmapEnabled=true; importer.wrapMode=TextureWrapMode.Clamp;
        importer.maxTextureSize=2048; importer.SaveAndReimport();
        Undo.RecordObject(material,"Light city windows");
        material.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        material.SetColor("_EmissionColor",new Color(2.5f,1.5f,.65f));
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        KuTyOfficeSunsetLighting.Capture();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("CITY_WINDOWS_LIT: warm scattered emissive windows on existing facade UVs.");
    }
    static void Grid(Color32[] pixels,int size,System.Random rng,float x,float y,float width,float height,int columns,int rows,float occupancy)
    {
        float factor=size/1600f,cellW=width/columns,cellH=height/rows;
        for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
        {
            if(rng.NextDouble()>occupancy) continue;
            byte brightness=(byte)rng.Next(120,256);
            var color=new Color32(brightness,brightness,brightness,255);
            int x0=Mathf.RoundToInt((x+(col+.22f)*cellW)*factor);
            int x1=Mathf.RoundToInt((x+(col+.78f)*cellW)*factor);
            int y0=Mathf.RoundToInt((y+(row+.23f)*cellH)*factor);
            int y1=Mathf.RoundToInt((y+(row+.77f)*cellH)*factor);
            for(int iy=y0;iy<y1;iy++) for(int ix=x0;ix<x1;ix++)
                pixels[(size-1-iy)*size+ix]=color;
        }
    }
}
