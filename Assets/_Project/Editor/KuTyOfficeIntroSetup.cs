using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class KuTyOfficeIntroSetup
{
    [MenuItem("KuTy/Office Intro/Add Placeholder Sound")]
    public static void AddSound()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var intro=UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        if(!intro) throw new InvalidOperationException("Install intro first.");
        if(intro.chairSound) return;
        const string folder="Assets/_Project/Generated/OfficeBlockout/Audio";
        Directory.CreateDirectory(folder);
        foreach(string name in new[]{"DoorHandle","ChairShift","Ventilation"})
        {
            int sampleRate=22050, count=name=="Ventilation"?44100:11025;
            using(var stream=new BinaryWriter(File.Create(folder+"/"+name+".wav")))
            {
                stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); stream.Write(36+count*2);
                stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); stream.Write(16); stream.Write((short)1); stream.Write((short)1);
                stream.Write(sampleRate); stream.Write(sampleRate*2); stream.Write((short)2); stream.Write((short)16);
                stream.Write(System.Text.Encoding.ASCII.GetBytes("data")); stream.Write(count*2);
                var random=new System.Random(42); float filtered=0;
                for(int i=0;i<count;i++)
                {
                    float time=(float)i/sampleRate;
                    filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.14f);
                    float envelope=name=="Ventilation"?.3f:name=="DoorHandle"?Mathf.Exp(-time*35)+.35f*Mathf.Exp(-Mathf.Abs(time-.18f)*75):Mathf.Sin(Mathf.PI*i/count)*.5f;
                    float value=filtered*envelope;
                    stream.Write((short)(Mathf.Clamp(value,-1,1)*16000));
                }
            }
            AssetDatabase.ImportAsset(folder+"/"+name+".wav");
        }
        intro.door.sound=Audio("DoorHandle",intro.transform,folder+"/DoorHandle.wav",intro.door.transform.position+Vector3.up,.35f,false);
        intro.chairSound=Audio("ChairShift",intro.transform,folder+"/ChairShift.wav",intro.seat.position+Vector3.up,.3f,false);
        var ventilation=Audio("Ventilation",intro.transform,folder+"/Ventilation.wav",new Vector3(0,2.7f,0),.12f,true);
        intro.ambience=intro.ambience.Concat(new[]{ventilation}).ToArray();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
    static Transform Find(string name) => GameObject.Find(name)?.transform ?? throw new InvalidOperationException("Missing " + name);
    static GameObject Child(string name, Transform parent)
    { var obj=new GameObject(name); obj.transform.SetParent(parent,false); return obj; }
    static GameObject UIChild(string name, Transform parent)
    { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false); return obj; }
    static RectTransform Rect(GameObject obj, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
    {
        var r=obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
        r.anchorMin=min; r.anchorMax=max; r.sizeDelta=size; r.anchoredPosition=position; return r;
    }
    static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, Vector2 anchor, Vector2 dimensions, Vector2 position)
    {
        var obj=UIChild(name,parent); Rect(obj,anchor,anchor,dimensions,position);
        var text=obj.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=size;
        text.alignment=TextAlignmentOptions.Center; text.color=new Color(.95f,.95f,.91f);
        text.raycastTarget=false; text.text=""; return text;
    }
    static AudioSource Audio(string name, Transform parent, string path, Vector3 position, float volume, bool loop)
    {
        var obj=Child(name,parent); obj.transform.position=position;
        var source=obj.AddComponent<AudioSource>(); source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        source.playOnAwake=false; source.loop=loop; source.volume=volume;
        source.spatialBlend=loop? .75f : .25f; source.minDistance=2; source.maxDistance=18;
        return source;
    }
    [MenuItem("KuTy/Office Intro/Install on Current Map")]
    public static void Install()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=KuTyOfficeBlockoutBuilder.ScenePath)
            throw new InvalidOperationException("Open Act1_Office_Blockout outside Play Mode.");
        if(UnityEngine.Object.FindObjectOfType<OfficeIntroController>()) throw new InvalidOperationException("Intro already installed; edit existing references instead.");
        var player=Find("Player"); var cam=player.GetComponentInChildren<Camera>();
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Generated/Vietnamese.asset");
        if(!font) throw new InvalidOperationException("Vietnamese TMP font missing.");
        var anchors=new GameObject("OfficeIntroAnchors").transform;
        var spawn=Child("OpeningSpawn",anchors).transform; spawn.SetPositionAndRotation(player.position,player.rotation);
        var systems=new GameObject("OfficeIntroSystems"); var intro=systems.AddComponent<OfficeIntroController>();
        intro.motor=player.GetComponent<FirstPersonController>(); intro.body=player.GetComponent<CharacterController>(); intro.view=cam;
        intro.spawn=spawn; intro.seat=Find("TySeatAnchor"); intro.chairRoot=Find("TyChair_SLOT");
        var legacy=player.GetComponent<OfficeBlockoutWalkthrough>(); if(legacy) UnityEngine.Object.DestroyImmediate(legacy);
        var oldInteraction=player.GetComponent<PlayerInteraction>(); if(oldInteraction) oldInteraction.enabled=false;
        var motorSettings=new SerializedObject(intro.motor);
        motorSettings.FindProperty("walkSpeed").floatValue=2; motorSettings.FindProperty("runSpeed").floatValue=2;
        motorSettings.ApplyModifiedPropertiesWithoutUndo();
        intro.door=Find("BossDoor_Hinge").gameObject.AddComponent<OfficeDoorInteractable>();
        intro.door.leaf=intro.door.GetComponentInChildren<BoxCollider>();
        intro.dialogue=new[] {
            new OfficeIntroController.Line {speaker="SẾP",text="Số liệu trong file này là sao?\nNhiều ô còn sai mà cậu đã gửi rồi à?",duration=7},
            new OfficeIntroController.Line {speaker="SẾP",text="Khách hàng đang chờ bản chốt.\nTôi đã nhắc cậu từ sáng rồi.",duration=5},
            new OfficeIntroController.Line {speaker="",text="",duration=.8f,slam=true},
            new OfficeIntroController.Line {speaker="SẾP",text="Quay về bàn, kiểm tra lại file Excel.\nXong thì in một bản, mang lại đây cho tôi ký.",duration=5},
            new OfficeIntroController.Line {speaker="TÝ",text="Dạ… em kiểm tra lại ngay ạ.",duration=3},
            new OfficeIntroController.Line {speaker="",text="",duration=1}
        };
        intro.voice=Audio("DialogueVoice",systems.transform,"",new Vector3(-4.8f,1.5f,4.3f),.85f,false);
        intro.deskSlam=Audio("DeskSlam",systems.transform,"Assets/_Project/Audio/SFX/Boss_Desk_Slam.mp3",new Vector3(-4.8f,.8f,3.5f),.45f,false);
        intro.ambience=new[] {
            Audio("Traffic",systems.transform,"Assets/_Project/Audio/Ambience/Traffic_Outside.mp3",new Vector3(7,1,0),.13f,true),
            Audio("Keyboard",systems.transform,"Assets/_Project/Audio/SFX/Keyboard_Distant.wav",new Vector3(-.1f,1,-2),.09f,true)
        };
        // Search by exact filename so existing project audio folder conventions are respected.
        foreach(var source in systems.GetComponentsInChildren<AudioSource>())
        {
            string key=source.name=="Traffic"?"Traffic_Outside":source.name=="Keyboard"?"Keyboard_Distant":source.name=="DeskSlam"?"Boss_Desk_Slam":null;
            if(key!=null && !source.clip) source.clip=AssetDatabase.FindAssets(key+" t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault();
        }
        var ui=new GameObject("OfficeIntroUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        ui.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var hud=ui.AddComponent<OfficeIntroHUD>(); intro.hud=hud;
        hud.objective=Text("Objective",ui.transform,font,26,new Vector2(.5f,1),new Vector2(1000,60),new Vector2(0,-65));
        hud.prompt=Text("Interaction",ui.transform,font,28,new Vector2(.5f,0),new Vector2(1000,60),new Vector2(0,210));
        hud.crosshair=Text("Crosshair",ui.transform,font,22,new Vector2(.5f,.5f),new Vector2(30,30),Vector2.zero); hud.crosshair.text="·";
        hud.subtitlePanel=UIChild("SubtitlePanel",ui.transform);
        Rect(hud.subtitlePanel,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(1180,160),new Vector2(0,105));
        var panel=hud.subtitlePanel.AddComponent<Image>(); panel.color=new Color(.025f,.035f,.04f,.88f); panel.raycastTarget=false;
        hud.subtitle=Text("Subtitle",hud.subtitlePanel.transform,font,30,new Vector2(.5f,.5f),new Vector2(1110,145),Vector2.zero);
        hud.pausePanel=UIChild("PausePanel",ui.transform); Rect(hud.pausePanel,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        hud.pausePanel.AddComponent<Image>().color=new Color(.02f,.025f,.03f,.9f);
        Text("Title",hud.pausePanel.transform,font,48,new Vector2(.5f,.5f),new Vector2(800,90),new Vector2(0,90)).text="Tạm dừng";
        var button=UIChild("Resume",hud.pausePanel.transform); Rect(button,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(340,70),new Vector2(0,-30));
        button.AddComponent<Image>().color=new Color(.2f,.29f,.3f); hud.resumeButton=button.AddComponent<Button>();
        Text("Label",button.transform,font,30,new Vector2(.5f,.5f),new Vector2(330,65),Vector2.zero).text="Tiếp tục";
        hud.pausePanel.SetActive(false); hud.subtitlePanel.SetActive(false);
        if(!UnityEngine.Object.FindObjectOfType<EventSystem>())
        { var es=Child("EventSystem",ui.transform); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }
        string allText=string.Join("",intro.dialogue.Select(l=>l.speaker+l.text))+"Quay lại bàn làm việc.Mở báo cáo Excel trên máy tính.NHIỆM VỤTạm dừngTiếp tục[E] Mở cửa[E] Ngồi xuốngLùi lại một chút để mở cửa.·PHÒNG GIÁM ĐỐC";
        if(!font.HasCharacters(allText,out uint[] missing,true,true)) throw new InvalidOperationException("Font missing glyphs: "+string.Join(",",missing));
        EditorUtility.SetDirty(font);
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TextMesh>())
        {
            if(label.text!="DIRECTOR") continue;
            var obj=label.gameObject; UnityEngine.Object.DestroyImmediate(label);
            var renderer=obj.GetComponent<MeshRenderer>(); if(renderer) UnityEngine.Object.DestroyImmediate(renderer);
            var filter=obj.GetComponent<MeshFilter>(); if(filter) UnityEngine.Object.DestroyImmediate(filter);
            var tmp=obj.AddComponent<TextMeshPro>(); tmp.font=font; tmp.text="PHÒNG GIÁM ĐỐC"; tmp.fontSize=1.2f;
            tmp.alignment=TextAlignmentOptions.Center; tmp.rectTransform.sizeDelta=new Vector2(1.25f,.23f);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Debug.Log("OFFICE_INTRO_INSTALLED: six dialogue beats, independent input, TMP glyphs verified.");
    }
}
