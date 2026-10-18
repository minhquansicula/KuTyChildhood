using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class KuTyOfficeOriginalMaterials
{
    const string Textures="Assets/_Project/Art/Office/RetroOffice/Textures";
    public const string Materials="Assets/_Project/Art/Office/RetroOffice/Materials";
    [Serializable] class Source { public Entry[] materials; public Tex[] textures; public Img[] images; }
    [Serializable] class Tex { public int source; }
    [Serializable] class Img { public string mimeType; }
    [Serializable] class Map { public int index=-1; public float scale=1; public float strength=1; public MapExtensions extensions; }
    [Serializable] class MapExtensions { public TransformInfo KHR_texture_transform; }
    [Serializable] class TransformInfo { public float[] offset; public float[] scale; }
    [Serializable] class Pbr { public float[] baseColorFactor; public Map baseColorTexture; public Map metallicRoughnessTexture; public float metallicFactor=1; public float roughnessFactor=1; }
    [Serializable] class Emission { public float emissiveStrength=1; }
    [Serializable] class Extensions { public Emission KHR_materials_emissive_strength; }
    [Serializable] class Entry
    {
        public string name,alphaMode; public bool doubleSided; public float alphaCutoff=.5f;
        public Pbr pbrMetallicRoughness; public Map normalTexture,occlusionTexture,emissiveTexture;
        public float[] emissiveFactor; public Extensions extensions;
    }
    static string ImagePath(Source data,Map map)
    {
        if(map==null || map.index<0) return null;
        int index=data.textures[map.index].source;
        return Textures+"/image_"+index+(data.images[index].mimeType=="image/png"?".png":".jpg");
    }
    static Texture2D Texture(Source data,Map map,bool srgb,bool normal=false)
    {
        string path=ImagePath(data,map); if(path==null) return null;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        if(importer.sRGBTexture!=srgb || importer.textureType!=type || importer.maxTextureSize!=4096)
        {
            importer.textureType=type; importer.sRGBTexture=srgb; importer.maxTextureSize=4096;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Color Factor(float[] value,Color fallback)
    { return value==null || value.Length<3?fallback:new Color(value[0],value[1],value[2],value.Length>3?value[3]:1); }
    [MenuItem("KuTy/Office Intro/Restore Original Materials")]
    public static void Restore()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play before restoring materials.");
        var data=JsonUtility.FromJson<Source>(File.ReadAllText(Textures+"/source-materials.json"));
        Directory.CreateDirectory(Materials); Directory.CreateDirectory(Textures+"/UnityPacked");
        AssetDatabase.Refresh();
        var mapping=new Dictionary<string,Material>();
        var notes=new List<string>();
        foreach(var entry in data.materials)
        {
            var pbr=entry.pbrMetallicRoughness??new Pbr();
            string path=Materials+"/"+entry.name.Replace('/','_')+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",Factor(pbr.baseColorFactor,Color.white).gamma);
            material.SetTexture("_BaseMap",Texture(data,pbr.baseColorTexture,true));
            var transform=pbr.baseColorTexture?.extensions?.KHR_texture_transform;
            if(transform!=null)
            {
                Vector2 scale=transform.scale==null?Vector2.one:new Vector2(transform.scale[0],transform.scale[1]);
                Vector2 offset=transform.offset==null?Vector2.zero:new Vector2(transform.offset[0],1-transform.offset[1]-scale.y);
                material.SetTextureScale("_BaseMap",scale); material.SetTextureOffset("_BaseMap",offset);
            }
            material.SetFloat("_Metallic",pbr.metallicFactor); material.SetFloat("_Smoothness",1-pbr.roughnessFactor);
            if(pbr.metallicRoughnessTexture!=null && pbr.metallicRoughnessTexture.index>=0)
            {
                // glTF: G roughness, B metallic. URP: R metallic, A smoothness.
                string raw=ImagePath(data,pbr.metallicRoughnessTexture);
                var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true); image.LoadImage(File.ReadAllBytes(raw));
                Color32[] pixels=image.GetPixels32();
                for(int i=0;i<pixels.Length;i++)
                { var c=pixels[i]; pixels[i]=new Color32((byte)Mathf.RoundToInt(c.b*pbr.metallicFactor),0,0,(byte)Mathf.RoundToInt(255-c.g*pbr.roughnessFactor)); }
                image.SetPixels32(pixels); image.Apply();
                string packed=Textures+"/UnityPacked/"+entry.name.Replace('/','_')+"_MetalSmooth.png";
                File.WriteAllBytes(packed,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(packed);
                var importer=(TextureImporter)AssetImporter.GetAtPath(packed); importer.sRGBTexture=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
                material.SetFloat("_Metallic",1); material.SetFloat("_Smoothness",1); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetTexture("_BumpMap",Texture(data,entry.normalTexture,false,true));
            if(entry.normalTexture!=null && entry.normalTexture.index>=0) { material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale",entry.normalTexture.scale); }
            // Occlusion uses R in glTF; URP uses G. Source ORM commonly has different channels.
            if(entry.occlusionTexture!=null && entry.occlusionTexture.index>=0)
            {
                var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true); image.LoadImage(File.ReadAllBytes(ImagePath(data,entry.occlusionTexture)));
                var pixels=image.GetPixels32(); for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(pixels[i].r,pixels[i].r,pixels[i].r,255);
                image.SetPixels32(pixels); image.Apply(); string ao=Textures+"/UnityPacked/"+entry.name.Replace('/','_')+"_AO.png";
                File.WriteAllBytes(ao,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); AssetDatabase.ImportAsset(ao);
                var importer=(TextureImporter)AssetImporter.GetAtPath(ao); importer.sRGBTexture=false; importer.SaveAndReimport();
                material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ao)); material.SetFloat("_OcclusionStrength",entry.occlusionTexture.strength); material.EnableKeyword("_OCCLUSIONMAP");
            }
            var emission=Factor(entry.emissiveFactor,Color.black)*(entry.extensions?.KHR_materials_emissive_strength?.emissiveStrength??1);
            material.SetTexture("_EmissionMap",Texture(data,entry.emissiveTexture,true)); material.SetColor("_EmissionColor",emission);
            if(emission.maxColorComponent>0) material.EnableKeyword("_EMISSION");
            material.SetFloat("_Cull",entry.doubleSided?0:2);
            if(entry.alphaMode=="BLEND")
            {
                material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0); material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000;
            }
            else if(entry.alphaMode=="MASK") { material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",entry.alphaCutoff); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450; }
            EditorUtility.SetDirty(material); mapping.Add(entry.name,material);
            notes.Add(entry.name+" base="+(ImagePath(data,pbr.baseColorTexture)??"factor")+" normal="+(entry.normalTexture!=null && entry.normalTexture.index>=0)+" metalRough="+(pbr.metallicRoughnessTexture!=null && pbr.metallicRoughnessTexture.index>=0));
        }
        var importerModel=(ModelImporter)AssetImporter.GetAtPath(KuTyOfficeAssetImport.ModelPath);
        var sourceNames=AssetDatabase.LoadAllAssetsAtPath(KuTyOfficeAssetImport.ModelPath).OfType<Material>().Select(m=>m.name).ToArray();
        foreach(var name in sourceNames)
        {
            if(!mapping.TryGetValue(name,out var mat)) throw new Exception("Unmapped FBX material: "+name);
            importerModel.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
        }
        importerModel.SaveAndReimport();
        int changed=0;
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
        {
            bool imported=false; for(var t=renderer.transform;t;t=t.parent) if(t.name.StartsWith("Imported")) imported=true;
            if(!imported) continue;
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m && mapping.TryGetValue(m.name,out var mat)?mat:m).ToArray(); changed++;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        notes.Insert(0,"Original GLB materials="+mapping.Count+"; scene renderers updated="+changed+". URP/Lit approximation: glTF specular/sheen extensions are not reconstructed.");
        File.WriteAllLines("Assets/Screenshots/OfficeOriginalMaterials_Report.txt",notes);
        KuTyOfficeIntroVisualChecks.Capture();
    }
}
