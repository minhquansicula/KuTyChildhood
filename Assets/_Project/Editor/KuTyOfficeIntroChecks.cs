using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Integration checks use the real runtime components and CharacterController.Move.
// They are not keyboard/mouse UI automation; report that distinction explicitly.
[InitializeOnLoad]
public static class KuTyOfficeIntroChecks
{
    const string Key="KuTy.OfficeIntro.Checks";
    static OfficeIntroController intro;
    static readonly List<string> results=new List<string>();
    static int phase, waypoint, readyCount;
    static float timer, pausedElapsed;
    static Vector3 pausedPosition;
    static double last, started;
    static bool previousBackground;
    static readonly Vector3[] route={new Vector3(-2.7f,.04f,2.05f),new Vector3(-1.2f,.04f,2.05f),new Vector3(2.8f,.04f,2.05f),new Vector3(2.8f,.04f,-2.4f),new Vector3(3.55f,.04f,-2.4f)};
    static KuTyOfficeIntroChecks() { EditorApplication.update+=Tick; }
    [MenuItem("KuTy/Office Intro/Run Integration Checks")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before tests.");
        SessionState.SetBool(Key,true); EditorApplication.isPlaying=true;
    }
    static void Check(bool passed,string label)
    { results.Add((passed?"PASS: ":"FAIL: ")+label); if(!passed) throw new Exception(label); }
    static void Aim(Vector3 point)
    {
        Vector3 delta=point-intro.view.transform.position;
        intro.body.transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(delta).eulerAngles.y,0);
        intro.motor.SetPitch(Mathf.DeltaAngle(0,Quaternion.LookRotation(delta).eulerAngles.x));
        Physics.SyncTransforms();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if(!intro)
            {
                intro=UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
                if(!intro || intro.State==OfficeIntroController.IntroState.Initialize) return;
                last=started=EditorApplication.timeSinceStartup; results.Clear(); phase=0; timer=0; waypoint=0; readyCount=0;
                previousBackground=Application.runInBackground; Application.runInBackground=true;
                intro.ReadyAtDesk+=()=>readyCount++;
                Check(intro.State==OfficeIntroController.IntroState.OpeningDialogue,"T01 spawn/opening");
                Check(!intro.TryInteract() && !intro.motor.enabled,"T02 early interaction/motor locked");
                intro.Resume();
            }
            double now=EditorApplication.timeSinceStartup;
            EditorApplication.QueuePlayerLoopUpdate();
            // Background tooling can take focus away; only the two deliberate pause
            // checks keep it paused. Product focus behavior remains unchanged.
            if(intro.IsPaused && phase!=1 && phase!=6) intro.Resume();
            float dt=Mathf.Min((float)(now-last),.05f); last=now; timer+=dt;
            if(now-started>100) throw new Exception("Integration timeout at phase "+phase);
            if(phase==0 && intro.LineElapsed>.5f)
            { intro.SetPaused(true); pausedElapsed=intro.LineElapsed; timer=0; phase=1; }
            else if(phase==1 && timer>.6f)
            { Check(intro.IsPaused && intro.LineElapsed==pausedElapsed,"T05 subtitle pause freezes time"); intro.Resume(); phase=2; }
            else if(phase==2 && intro.State==OfficeIntroController.IntroState.ReturnToDesk)
            {
                Check(intro.DialogueIndex==intro.dialogue.Length,"T04/T06 fallback completes all beats");
                intro.motor.enabled=false;
                intro.motor.TeleportTo(new Vector3(-2.7f,.04f,2.1f),Quaternion.identity); Physics.SyncTransforms();
                Check(intro.door.PlayerBlocksSweep(intro.body) && !intro.door.TryOpen(intro.body),"T09 sweep blocks unsafe opening");
                intro.motor.TeleportTo(new Vector3(-3.8f,.04f,2.05f),Quaternion.identity);
                Aim(new Vector3(-2,1.2f,2.1f));
                Check(intro.GetInteractionPrompt()=="[E] Mở cửa" && intro.TryInteract(),"T08 raycast starts door");
                Check(!intro.TryInteract(),"T08 repeated interaction ignored"); timer=0; phase=3;
            }
            else if(phase==3 && intro.door.State==OfficeDoorInteractable.DoorState.Open)
            { phase=4; timer=0; }
            else if(phase==4)
            {
                Vector3 position=intro.body.transform.position, destination=route[waypoint];
                Vector3 delta=destination-position; delta.y=0;
                if(delta.magnitude<.08f)
                {
                    if(++waypoint==route.Length)
                    {
                        Check(intro.ExitedBossRoom,"T10 exit detected after real capsule traversal");
                        Aim(new Vector3(4.15f,.85f,-2.4f));
                        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
                        blocker.transform.position=Vector3.Lerp(intro.view.transform.position,new Vector3(4.15f,.85f,-2.4f),.6f);
                        blocker.transform.localScale=new Vector3(.15f,1,.8f); Physics.SyncTransforms();
                        Check(intro.GetInteractionPrompt()=="" && !intro.TryInteract(),"T07 solid obstruction hides seat interaction");
                        UnityEngine.Object.DestroyImmediate(blocker); Physics.SyncTransforms();
                        var visual=intro.chairRoot.Find("Visual_REPLACE"); visual.gameObject.SetActive(false);
                        Check(intro.GetInteractionPrompt()=="[E] Ngồi xuống","T19 chair visual replacement preserves interaction");
                        visual.gameObject.SetActive(true);
                        Check(intro.GetInteractionPrompt()=="[E] Ngồi xuống" && intro.TryInteract(),"T13 seat through occluding raycast");
                        Check(!intro.TryInteract(),"T13 duplicate seat suppressed"); timer=0; phase=5;
                    }
                }
                else intro.body.Move(delta.normalized*2*dt+Vector3.down*.1f);
                if(timer>20) throw new Exception("Capsule route blocked waypoint "+waypoint);
            }
            else if(phase==5 && timer>.2f)
            { intro.SetPaused(true); pausedPosition=intro.body.transform.position; timer=0; phase=6; }
            else if(phase==6 && timer>.5f)
            { Check(intro.body.transform.position==pausedPosition,"T14 pause freezes seating"); intro.Resume(); phase=7; timer=0; }
            else if(phase==7 && intro.State==OfficeIntroController.IntroState.ReadyAtDesk)
            {
                Check(readyCount==1 && !intro.TryInteract(),"T16 ReadyAtDesk exactly once; no further E action");
                Check(Vector3.Distance(intro.body.transform.position,intro.seat.position)<.01f && !intro.body.enabled,"T15 seat anchor and collision ownership");
                timer=0; phase=8;
            }
            else if(phase==8 && timer>.7f)
            {
                Check(readyCount==1,"T16 completion does not repeat");
                intro.enabled=false;
                Check(intro.body.enabled && Vector3.Distance(intro.body.transform.position,intro.spawn.position)<.01f,"Teardown restores safe collision position");
                Finish();
            }
        }
        catch(Exception error) { results.Add("FAIL: "+error.Message); Finish(); }
    }
    static void Finish()
    {
        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllLines("Assets/Screenshots/OfficeIntro_Integration.txt",results);
        SessionState.SetBool(Key,false); EditorApplication.isPlaying=false;
        Application.runInBackground=previousBackground;
        Debug.Log("OFFICE_INTRO_CHECKS: "+string.Join(" | ",results));
    }
}
