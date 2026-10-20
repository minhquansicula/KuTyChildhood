using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KuTyOfficeIntroVisualChecks
{
    [MenuItem("KuTy/Office Intro/Capture Delivery Views")]
    public static void Capture()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var intro=UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        if(!intro) throw new InvalidOperationException("Open the office intro scene.");
        var transforms=UnityEngine.Object.FindObjectsOfType<Transform>(true);
        var poses=transforms.Select(t=>(t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf)).ToArray();
        byte[] before=File.ReadAllBytes(KuTyOfficeBlockoutBuilder.ScenePath);
        KuTyOfficeBlockoutBuilder.Capture();
        bool same=transforms.Select((t,i)=>t && t.localPosition==poses[i].localPosition && t.localRotation==poses[i].localRotation && t.localScale==poses[i].localScale && t.gameObject.activeSelf==poses[i].activeSelf).All(x=>x);
        if(!same || !before.SequenceEqual(File.ReadAllBytes(KuTyOfficeBlockoutBuilder.ScenePath))) throw new Exception("Capture mutated scene.");
        CaptureUI(intro,1920,1080); CaptureUI(intro,1366,768);
        File.WriteAllText("Assets/Screenshots/OfficeIntro_VisualChecks.txt","PASS T22: Capture preserved all local poses, active states and saved scene bytes.\nPASS T20: Subtitle mesh fits at 1920x1080 and 1366x768. Inspect PNGs for visual QA.\n");
    }
    static void CaptureUI(OfficeIntroController intro,int width,int height)
    {
        var hud=intro.hud; var canvas=hud.GetComponent<Canvas>();
        var oldMode=canvas.renderMode; var oldCamera=canvas.worldCamera; float oldPlane=canvas.planeDistance, oldScale=canvas.scaleFactor;
        string subtitle=hud.subtitle.text, objective=hud.objective.text, prompt=hud.prompt.text;
        bool oldSubtitle=hud.subtitlePanel.activeSelf, oldPause=hud.pausePanel.activeSelf, oldCrosshair=hud.crosshair.gameObject.activeSelf;
        var obj=new GameObject("TemporaryHUDCapture"); var camera=obj.AddComponent<Camera>(); camera.CopyFrom(intro.view);
        obj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camera.transform.SetPositionAndRotation(intro.spawn.position+Vector3.up*1.65f,Quaternion.Euler(8,intro.spawn.eulerAngles.y,0));
        var target=RenderTexture.GetTemporary(width,height,24); var oldRT=RenderTexture.active;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target; camera.aspect=(float)width/height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=.5f;
            canvas.scaleFactor=Mathf.Sqrt((width/1920f)*(height/1080f));
            hud.SetPaused(false); hud.crosshair.gameObject.SetActive(false); hud.objective.text=""; hud.SetPrompt("");
            hud.SetSubtitle(intro.dialogue[0].speaker,intro.dialogue[0].text);
            Canvas.ForceUpdateCanvases(); hud.subtitle.ForceMeshUpdate();
            if(hud.subtitle.isTextOverflowing) throw new Exception("Subtitle overflow at "+width);
            camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            File.WriteAllBytes("Assets/Screenshots/OfficeIntro_HUD_"+width+".png",texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode=oldMode; canvas.worldCamera=oldCamera; canvas.planeDistance=oldPlane; canvas.scaleFactor=oldScale;
            hud.subtitle.text=subtitle; hud.objective.text=objective; hud.prompt.text=prompt;
            hud.subtitlePanel.SetActive(oldSubtitle); hud.pausePanel.SetActive(oldPause); hud.crosshair.gameObject.SetActive(oldCrosshair);
            camera.targetTexture=null; RenderTexture.active=oldRT; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(obj); Canvas.ForceUpdateCanvases();
        }
    }
}
