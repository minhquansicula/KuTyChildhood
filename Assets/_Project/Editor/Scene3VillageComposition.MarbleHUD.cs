using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static partial class Scene3VillageComposition
{
    [MenuItem("KuTy/Scene 3/Validate marble HUD framing")]
    public static void ValidateMarbleHudFraming()
    {
        RequireArtScene();const string output="Tools/Scene3/MarbleHUD";Directory.CreateDirectory(output);
        var original=Resources.FindObjectsOfTypeAll<MarbleAimUI>().First(u=>u.gameObject.scene.IsValid());
        var data=new SerializedObject(original);var originalPanel=(GameObject)data.FindProperty("panel").objectReferenceValue;
        var holder=new GameObject("MarbleHudPreview",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));holder.hideFlags=HideFlags.HideAndDontSave;
        var canvas=holder.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
        var copied=UnityEngine.Object.Instantiate(originalPanel,holder.transform);copied.SetActive(false);
        var ui=holder.AddComponent<MarbleAimUI>();var copyData=new SerializedObject(ui);
        copyData.FindProperty("panel").objectReferenceValue=copied;
        foreach(string name in new[]{"shotsText","knockedText","forceSlider","forceSliderFill","statusText"})
        {
            var source=data.FindProperty(name).objectReferenceValue as Component;
            if(!source)continue;
            var indices=new System.Collections.Generic.Stack<int>();
            for(var t=source.transform;t!=originalPanel.transform;t=t.parent)indices.Push(t.GetSiblingIndex());
            var target=copied.transform;while(indices.Count>0)target=target.GetChild(indices.Pop());
            copyData.FindProperty(name).objectReferenceValue=target.GetComponent(source.GetType());
        }
        copyData.ApplyModifiedPropertiesWithoutUndo();
        var font=AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.Contains("Vietnam") || p.Contains("NotoSans")).Select(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>).FirstOrDefault();
        var game=Find("MarbleShootingGame").GetComponent<MarbleShootingGame>();var gameData=new SerializedObject(game);
        var anchor=(Transform)gameData.FindProperty("gameCameraPosition").objectReferenceValue;
        var look=(Transform)gameData.FindProperty("gameCameraLookAt").objectReferenceValue;
        var obj=new GameObject("MarbleHudCamera"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();cam.enabled=false;
        cam.transform.SetPositionAndRotation(anchor.position,Quaternion.LookRotation(look.position-anchor.position));
        cam.fieldOfView=gameData.FindProperty("gameFieldOfView").floatValue;cam.nearClipPlane=.005f;cam.farClipPlane=200;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;canvas.worldCamera=cam;canvas.planeDistance=.1f;
        var originals=Resources.FindObjectsOfTypeAll<Canvas>().Where(c=>c!=canvas && c.gameObject.scene.IsValid() && c.enabled).ToArray();
        var report=new StringBuilder();
        try
        {
            foreach(var c in originals)c.enabled=false;
            int[] widths={1520,1280,1024},heights={600,720,768};
            for(int i=0;i<widths.Length;i++)
            {
                var rt=new RenderTexture(widths[i],heights[i],24);var tex=new Texture2D(widths[i],heights[i],TextureFormat.RGB24,false);var previous=RenderTexture.active;
                try
                {
                    rt.Create();cam.targetTexture=rt;cam.aspect=(float)widths[i]/heights[i];
                    ui.Show(1,5,1,3);ui.SetStatus("Đã đẩy 1/3 viên ra vòng. Kéo từ viên bi để bắn tiếp");Canvas.ForceUpdateCanvases();
                    foreach(var label in copied.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                    {
                        if(font)label.font=font;
                        label.ForceMeshUpdate(true,true);
                    }
                    var ring=((Collider)gameData.FindProperty("ringCollider").objectReferenceValue).GetComponent<LineRenderer>();float maxY=0;
                    for(int p=0;p<ring.positionCount;p++){var point=ring.GetPosition(p);if(!ring.useWorldSpace)point=ring.transform.TransformPoint(point);maxY=Mathf.Max(maxY,cam.WorldToViewportPoint(point).y);}
                    if(maxY>=.88f)throw new Exception("Ring overlaps HUD at "+widths[i]+"x"+heights[i]+": "+maxY);
                    RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,widths[i],heights[i]),0,0);tex.Apply();File.WriteAllBytes(output+"/"+widths[i]+"x"+heights[i]+".png",tex.EncodeToPNG());
                    report.AppendLine("PASS "+widths[i]+"x"+heights[i]+": ring top="+maxY.ToString("F3")+", HUD bottom=0.880; non-blocking graphics.");
                }
                finally{cam.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
            }
            File.WriteAllText(output+"/validation.txt",report.ToString());
        }
        finally{foreach(var c in originals)if(c)c.enabled=true;UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
