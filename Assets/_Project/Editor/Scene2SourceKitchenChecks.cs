using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Scene2SourceKitchenChecks
{
    const string Pending="KuTy.SourceKitchenChecks";
    [MenuItem("KuTy/Tests/Stop pending source regression queue only")]
    public static void StopQueue(){SessionState.SetInt("KuTy.SourceKitchen.RegressionStage",0);SessionState.SetBool(Pending,false);}
    [MenuItem("KuTy/Tests/Source kitchen regression ending journey")]
    public static void Journey(){Arm();Scene2MergedJourneyChecks.Run();}
    static void Arm()
    {
        if(!SessionState.GetBool("KuTy.SourceKitchen.FramePump",false))SessionState.SetBool("KuTy.SourceKitchen.BackgroundBeforeQA",Application.runInBackground);
        SessionState.SetBool("KuTy.SourceKitchen.FramePump",true);
    }
    static Scene2SourceKitchenChecks()
    {
        EditorApplication.update+=()=> {if(EditorApplication.isPlaying&&SessionState.GetBool("KuTy.SourceKitchen.FramePump",false))EditorApplication.QueuePlayerLoopUpdate();};
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("KuTy.SourceKitchen.FramePump",false))Application.runInBackground=true;
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                if(SessionState.GetBool("KuTy.SourceKitchen.FramePump",false))Application.runInBackground=SessionState.GetBool("KuTy.SourceKitchen.BackgroundBeforeQA",false);
                SessionState.SetBool("KuTy.SourceKitchen.FramePump",false);
                if(SessionState.GetInt("KuTy.SourceKitchen.RegressionStage",0)>0)EditorApplication.delayCall+=NextRegression;
            }
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false)) return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=()=>new GameObject("Source kitchen pointer QA").AddComponent<Scene2SourceKitchenCheckRunner>();
        };
    }
    [MenuItem("KuTy/Tests/Check original source kitchen pointer and lifecycle")]
    public static void Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=Scene2MotherHomeSetup.Home||!UnityEngine.Object.FindObjectOfType<Scene2SourceKitchenLayout>())
            throw new InvalidOperationException("Saved source-bound Home in Edit Mode required.");
        Arm();KuTyCheckpointTestScope.Capture();SessionState.SetBool(Pending,true);EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.EnterPlaymode();
    }
    [MenuItem("KuTy/Tests/Source kitchen regression four activities")]
    public static void Four(){Arm();Scene2FourActivitiesChecks.Run();}
    [MenuItem("KuTy/Tests/Source kitchen regression dishwashing")]
    public static void Dishes(){Arm();EditorApplication.ExecuteMenuItem("KuTy/Tests/Check user-arranged dishwashing simulator");}
    [MenuItem("KuTy/Tests/Source kitchen regression targeting")]
    public static void Targeting(){Arm();KuTyMergeValidation.RunScene2Targeting();}
    [MenuItem("KuTy/Tests/Run source kitchen and retained activity regressions")]
    public static void Regressions()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
        SessionState.SetInt("KuTy.SourceKitchen.RegressionStage",1);Run();
    }
    static void NextRegression()
    {
        int stage=SessionState.GetInt("KuTy.SourceKitchen.RegressionStage",0);
        SessionState.SetInt("KuTy.SourceKitchen.RegressionStage",stage+1);
        try
        {
            if(stage==1)Four();else if(stage==2)Dishes();else if(stage==3)Targeting();
            else if(stage==4){Arm();Scene2MergedJourneyChecks.Run();}
            else
            {
                var report=new StringBuilder();
                Scene2FiveTaskSetup.CheckMigration();
                foreach(string path in new[]{Scene2SourceKitchenArticulation.Output+"/pointer-checks.txt",Scene2FourActivitiesSetup.Output+"/checks.txt",Scene2AuthoredDishwashingSetup.Output+"/checks.txt","Tools/ProjectAudit/scene2-precise-targeting.txt",Scene2MergedJourneyChecks.Report,Scene2FiveTaskSetup.Output+"/migration-checks.txt"})
                {var lines=File.ReadAllLines(path);report.AppendLine(path+": "+lines[0]);}
                File.WriteAllText(Scene2SourceKitchenArticulation.Output+"/regression-summary.txt",report.ToString());
                SessionState.SetInt("KuTy.SourceKitchen.RegressionStage",0);
                Debug.Log("[SourceKitchen] Regression sequence finished; inspect summary, not menu executed.");
            }
        }
        catch(Exception e){SessionState.SetInt("KuTy.SourceKitchen.RegressionStage",0);File.WriteAllText(Scene2SourceKitchenArticulation.Output+"/regression-summary.txt","FAIL regression orchestration: "+e);Debug.LogException(e);}
    }
}
public sealed class Scene2SourceKitchenCheckRunner:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    Scene2KitchenHandsOn kitchen;HouseholdChoreFlow flow;Camera view;bool finished,background;float started;int assertions;
    readonly StringBuilder report=new StringBuilder();
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,Flags).Invoke(owner,args);
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Flags).GetValue(owner);
    void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);assertions++;report.AppendLine("PASS "+message);}
    void Start(){background=Application.runInBackground;Application.runInBackground=true;started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;StartCoroutine(Guarded());}
    void Update(){if(!finished&&Time.realtimeSinceStartup-started>100)Finish(false,"Timeout");}
    void Log(string text,string stack,LogType type){if(!finished&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))Finish(false,text+"\n"+stack);}
    IEnumerator Guarded()
    {
        var run=Run();while(!finished){object next;try{if(!run.MoveNext()){Finish(true,"Source provenance, shared-pointer fire/cooking and lifecycle passed. Not an independent OS mouse/standalone playtest.");yield break;}next=run.Current;}catch(Exception e){Finish(false,e.ToString());yield break;}yield return next;}
    }
    void Pointer(Vector2 p,bool down=false,bool held=false,bool up=false,float seconds=.02f,bool focused=true)
    {Physics.SyncTransforms();Call(kitchen,"ProcessKitchenPointer",p,down,held,up,seconds,focused);}
    Vector2 Visible(Collider collider)
    {
        Physics.SyncTransforms();var b=collider.bounds;
        foreach(float x in new[]{0f,-.65f,.65f})foreach(float y in new[]{0f,.65f,-.65f})foreach(float z in new[]{0f,-.65f,.65f})
        {
            var p=view.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
            if(p.z>0&&p.x>0&&p.x<Screen.width&&p.y>0&&p.y<Screen.height&&(bool)Call(kitchen,"OverAt",collider,(Vector2)p))
            {Check(true,"Real unoccluded ray reaches "+collider.name);return p;}
        }
        var ray=view.ScreenPointToRay(view.WorldToScreenPoint(b.center));
        throw new InvalidOperationException("No visible ray to "+collider.name+"; bounds="+b+"; camera="+view.transform.position+"; blockers="+string.Join(",",Physics.RaycastAll(ray,4,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance)));
    }
    void Open(HouseholdChore chore)
    {
        flow.Voice.Stop();DialogueUI.Instance?.HideDialogue();kitchen.SilenceMother();
        FindObjectsOfType<HouseholdChoreInteractable>().Single(t=>t.isActiveAndEnabled&&t.Chore==chore).Interact();
        Check(kitchen.IsWorking&&kitchen.CurrentTask==chore,"E opens original source activity "+chore);
    }
    void Place(int i)
    {
        var from=Visible(kitchen.kindlingHits[i]);Pointer(from,true);
        Check(Field<int>(kitchen,"draggedLog")==i,"Pointer grabs source kindling "+i);
        var to=Visible(kitchen.hearthHit);Pointer(to,false,true);Pointer(to,false,false,true);
        Check(kitchen.KindlingCount==i,"Wood release starts controlled settling, not instant completion "+i);
        Pointer(to,false,false,false,.1f);Pointer(to,false,false,false,.1f);
        Check(kitchen.KindlingCount==i+1,"Real mouth drop places source kindling "+i);
    }
    void Breath(float duration)
    {
        var p=Visible(kitchen.blowHit);Pointer(p,true);for(float t=0;t<duration-.001f;t+=.05f)Pointer(p,false,true,false,.05f);Pointer(p,false,false,true);
    }
    void DragFuel(float amount)
    {
        var pointer=Visible(kitchen.fuelHit);
        Vector2 axis=view.WorldToScreenPoint(kitchen.fuelLog.position+kitchen.fuelAxis.forward*.32f)-view.WorldToScreenPoint(kitchen.fuelLog.position);
        Pointer(pointer,true);Pointer(pointer+axis*amount,false,true);Pointer(pointer,false,false,true);
    }
    void ValidateControls(string name)
    {
        Call(kitchen,"UpdateControls");Canvas.ForceUpdateCanvases();
        var label=Field<TMPro.TextMeshProUGUI>(kitchen,"controlText");label.ForceMeshUpdate();
        Check(!label.isTextOverflowing&&label.preferredHeight<=label.rectTransform.rect.height+1,name+" text fits its actual panel");
    }
    IEnumerator Run()
    {
        yield return null;flow=HouseholdChoreFlow.Instance;kitchen=flow.GetComponent<Scene2KitchenHandsOn>();view=Camera.main;
        Call(FindObjectOfType<BedWakeUpController>(),"GetOutOfBed");yield return new WaitForSeconds(1.5f);
        FindObjectOfType<FirstPersonController>().enabled=false;flow.Voice.Stop();DialogueUI.Instance?.HideDialogue();
        var layout=kitchen.GetComponent<Scene2SourceKitchenLayout>();
        Check(layout&&layout.IsReady,"Original source provenance is complete");
        Check(!layout.formerFireRoot.gameObject.activeSelf&&!layout.formerCookRoot.gameObject.activeSelf,"Former BlenderKitchen activity roots retained inactive");
        foreach(var root in new[]{kitchen.fireRoot,kitchen.cookRoot})
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            {string path=AssetDatabase.GetAssetPath(f.sharedMesh);Check(path.StartsWith(Scene2SourceKitchenArticulation.Art+"/"),"Visible gameplay mesh is a derived original source, not a BlenderKitchen/procedural prop: "+f.name);}
        Check(flow.IsCurrent(HouseholdChore.LightFire),"New game starts at source fire without removed preparation tasks");
        Open(HouseholdChore.LightFire);yield return new WaitForSeconds(.5f);kitchen.SilenceMother();
        ValidateControls("Wood placement");ScreenCapture.CaptureScreenshot(Scene2SourceKitchenArticulation.Output+"/fire-ux-ui.png");
        foreach(var hit in kitchen.kindlingHits)
            Check(!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)Field<TMPro.TextMeshProUGUI>(kitchen,"controlText").transform.parent,Visible(hit),null),"Instruction panel does not cover pickable wood");
        yield return new WaitForEndOfFrame();yield return null;
        Scene2SourceKitchenBinding.Capture(kitchen.fireView,"source-fire-runtime.png");
        var home=kitchen.kindling[0].position;var p0=Visible(kitchen.kindlingHits[0]);Pointer(p0,true);
        Pointer(new Vector2(Screen.width*.95f,Screen.height*.9f),false,true);Pointer(new Vector2(Screen.width*.95f,Screen.height*.9f),false,false,true);
        Check(kitchen.KindlingCount==0&&Vector3.Distance(kitchen.kindling[0].position,home)<.001f,"Wrong drop returns wood without progress");
        p0=Visible(kitchen.kindlingHits[0]);Pointer(p0,true);Pointer(p0,false,true,false,.05f,false);
        Check(Field<int>(kitchen,"draggedLog")==-1&&Vector3.Distance(kitchen.kindling[0].position,home)<.001f,"Focus loss releases source wood");
        Pointer(p0,true);GameManager.Instance.SetPaused(true);Pointer(p0,false,true);
        Check(kitchen.KindlingCount==0&&Field<int>(kitchen,"draggedLog")==-1,"Pause blocks pointer placement and releases wood");
        GameManager.Instance.SetPaused(false);yield return null;yield return null;
        // An actual solid occluder must block a valid source hit; never special-case stove names.
        var ray=view.ScreenPointToRay(Visible(kitchen.kindlingHits[0]));var blocker=new GameObject("QA occluder");blocker.transform.position=ray.GetPoint(.4f);var wall=blocker.AddComponent<BoxCollider>();wall.size=Vector3.one*.16f;
        Physics.SyncTransforms();Pointer(view.WorldToScreenPoint(kitchen.kindlingHits[0].bounds.center),true);
        Check(Field<int>(kitchen,"draggedLog")==-1,"Solid obstacle blocks kitchen grabbing");Destroy(blocker);yield return null;
        Place(0);int reward=flow.Pieces;kitchen.End();
        Check(!GameManager.Instance.InputBlocked&&flow.Pieces==reward&&Scene2Checkpoint.LoadWork(HouseholdChore.LightFire).Length>0,"Cancel saves partial original-source wood work and releases input without reward");
        Call(kitchen,"RestoreWorld");Open(HouseholdChore.LightFire);yield return new WaitForSeconds(.5f);
        Check(kitchen.KindlingCount==1,"Checkpoint restore reconstructs placed source wood");Place(1);Place(2);
        Breath(.1f);Check(!kitchen.FireLit,"Too-short pointer breath does not ignite");
        Breath(1.2f);Check(!kitchen.FireLit&&Field<float>(kitchen,"ember")>0,"Long mouse hold contributes a gentle breath without penalizing embers");
        Breath(2f);
        Check(kitchen.FireLit&&flow.IsCurrent(HouseholdChore.CookRice)&&!GameManager.Instance.InputBlocked,"Continuous mouse hold automatically breathes/rests and unlocks cooking once");
        Open(HouseholdChore.CookRice);yield return new WaitForSeconds(.5f);kitchen.SilenceMother();
        ValidateControls("Cooking heat");ScreenCapture.CaptureScreenshot(Scene2SourceKitchenArticulation.Output+"/cooking-ux-ui.png");
        Check(!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)Field<TMPro.TextMeshProUGUI>(kitchen,"controlText").transform.parent,Visible(kitchen.fuelHit),null),"Instruction panel does not cover the fuel grip");
        yield return new WaitForEndOfFrame();yield return null;
        var fuelStart=kitchen.fuelLog.position;var fuelPoint=Visible(kitchen.fuelHit);
        Vector2 fuelScreenAxis=view.WorldToScreenPoint(kitchen.fuelLog.position+kitchen.fuelAxis.forward*.32f)-view.WorldToScreenPoint(kitchen.fuelLog.position);
        float fuelBefore=Field<float>(kitchen,"fuel");
        Vector2 perpendicular=new Vector2(-fuelScreenAxis.y,fuelScreenAxis.x).normalized*30;
        Pointer(fuelPoint,true);Pointer(fuelPoint+perpendicular,false,true);Pointer(fuelPoint,false,false,true);
        Check(Mathf.Abs(Field<float>(kitchen,"fuel")-fuelBefore)<.001f,"Dragging perpendicular to the fuel axis does not change heat control");
        Pointer(fuelPoint,true);Pointer(fuelPoint-fuelScreenAxis*.38f,false,true);Pointer(fuelPoint,false,false,true);
        Check(Vector3.Distance(fuelStart,kitchen.fuelLog.position)>.08f,"Pointer drag moves original source fuel, not a hidden keyboard substitute");
        for(int i=0;i<35;i++)Pointer(Vector2.zero,false,false,false,.1f);
        float beforeBadHeat=kitchen.Cooked;
        DragFuel(.7f);
        for(int i=0;i<40;i++)Pointer(Vector2.zero,false,false,false,.1f);
        Check(kitchen.Heat>.66f&&kitchen.Cooked>=beforeBadHeat,"Excessive heat never resets already-cooked progress");
        float hotCooked=kitchen.Cooked;
        for(int i=0;i<10;i++)Pointer(Vector2.zero,false,false,false,.1f);
        Check(Mathf.Approximately(kitchen.Cooked,hotCooked),"Cooking pauses instead of losing progress at excessive heat");
        DragFuel(-1f);
        for(int i=0;i<45;i++)Pointer(Vector2.zero,false,false,false,.1f);
        Check(kitchen.Heat<.42f&&kitchen.Cooked>=hotCooked,"Weak heat also preserves cooked progress");
        DragFuel(.5f);
        float wait=Time.realtimeSinceStartup;
        while(kitchen.Cooked<12&&Time.realtimeSinceStartup-wait<35)
        {
            if(!Application.isFocused)Pointer(Vector2.zero,false,false,false,Time.deltaTime);
            yield return null;
        }
        Check(kitchen.Cooked>=12,"Real-time gentle simmer cooks after source fuel adjustment");
        var closed=kitchen.lid.parent.TransformPoint(Field<Vector3>(kitchen,"lidHome"));var lidPoint=Visible(kitchen.lidHit);Pointer(lidPoint,true);Pointer(lidPoint+Vector2.up*(Screen.height*.06f),false,true);
        Check(Mathf.Abs(kitchen.lid.position.y-closed.y-.11f)<.004f,"Shared pointer half-lift moves repaired lid 11cm in world metres");
        Pointer(lidPoint,false,true,false,.02f,false);Check(Vector3.Distance(kitchen.lid.position,closed)<.001f,"Focus loss closes partial source lid without completion");
        lidPoint=Visible(kitchen.lidHit);Pointer(lidPoint,true);Pointer(lidPoint+Vector2.up*(Screen.height*.06f),false,true);GameManager.Instance.SetPaused(true);
        Check(Vector3.Distance(kitchen.lid.position,closed)<.001f&&flow.IsCurrent(HouseholdChore.CookRice),"Pause closes partial repaired lid without reward");
        GameManager.Instance.SetPaused(false);yield return null;yield return null;
        lidPoint=Visible(kitchen.lidHit);Pointer(lidPoint,true);Pointer(lidPoint,false,false,true);
        Pointer(lidPoint,false,false,false,.1f);
        Check(Field<bool>(kitchen,"lidAutoOpening")&&kitchen.lid.position.y>closed.y,"Short lid click starts assisted opening instead of requiring a drag");
        Pointer(lidPoint,false,false,false,.05f,false);
        Check(!Field<bool>(kitchen,"lidAutoOpening")&&Vector3.Distance(kitchen.lid.position,closed)<.001f,"Focus loss cancels assisted opening safely without reward");
        kitchen.End();Call(kitchen,"RestoreWorld");Open(HouseholdChore.CookRice);yield return new WaitForSeconds(.5f);
        Check(kitchen.Cooked>=12,"Cooked checkpoint persists across cancel and restore");
        lidPoint=Visible(kitchen.lidHit);Pointer(lidPoint,true);Pointer(lidPoint,false,false,true);
        for(int i=0;i<7&&kitchen.IsWorking;i++)Pointer(lidPoint,false,false,false,.1f);
        Check(flow.IsCurrent(HouseholdChore.SetTable)&&!kitchen.IsWorking&&!GameManager.Instance.InputBlocked,"Pointer full lift completes once and releases input");
        var bounds=Scene2DishwashingLayout.VisualBounds(kitchen.lid);Physics.SyncTransforms();
        var support=Physics.RaycastAll(bounds.center+Vector3.up,Vector3.down,3,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(kitchen.cookRoot)&&h.normal.y>.95f&&h.point.y<=bounds.min.y+.001f).OrderBy(h=>h.distance).ToArray();
        Check(support.Length>0&&bounds.min.y-support[0].point.y>=-.001f&&bounds.min.y-support[0].point.y<=.006f,"Repaired lid rests on an actual support within 6mm");
        Scene2SourceKitchenBinding.Capture(kitchen.cookView,"source-cook-open-runtime.png");
        var pose=new GameObject("Source supported lid evidence").transform;pose.position=Vector3.Lerp(kitchen.lid.position,kitchen.cookRoot.position,.5f)+new Vector3(1.7f,1.4f,1.7f);pose.LookAt(Vector3.Lerp(kitchen.lid.position,kitchen.cookRoot.position,.5f)+Vector3.up*.35f);
        Scene2SourceKitchenBinding.Capture(pose,"source-lid-supported-runtime.png");Destroy(pose.gameObject);
        reward=flow.Pieces;Pointer(lidPoint,true,true,true);Check(flow.Pieces==reward,"Stale pointer events cannot duplicate cooking reward");
    }
    void Finish(bool pass,string detail)
    {
        if(finished)return;finished=true;Application.logMessageReceived-=Log;if(kitchen)kitchen.End();Application.runInBackground=background;
        File.WriteAllText(Scene2SourceKitchenArticulation.Output+"/pointer-checks.txt",(pass?"PASS ":"FAIL ")+assertions+" assertions\n"+report+detail);
        Debug.Log("[SourceKitchenChecks] "+(pass?"PASS ":"FAIL ")+assertions+" "+detail);EditorApplication.isPlaying=false;
    }
}
