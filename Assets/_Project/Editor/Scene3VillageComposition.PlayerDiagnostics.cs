using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    public static void RepairInvisibleJournalCollision()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Repair the outside village in Edit Mode.");
        string folder=OutputFolder+"/PlayerDiagnostics";Directory.CreateDirectory(folder);string backup=folder+"/before-invisible-journal-collision.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up scene.");
        var table=Find("JournalTable");var journal=Find("Journal");if(!table || !journal || !journal.GetComponent<JournalInteractable>())throw new Exception("Expected existing journal interaction.");
        if(table.GetComponentsInChildren<Renderer>().Any(r=>r.enabled && r.gameObject.activeInHierarchy))throw new Exception("The journal table is visible; do not remove its physical collision.");
        bool hadBlockingTable=table.GetComponents<Collider>().Any(c=>c.enabled && !c.isTrigger);
        var before=GameplaySnapshot();var report=new StringBuilder();Physics.SyncTransforms();
        var stuck=new Vector3(-6.3031f,.28f,.2445f);var direction=Vector3.back;
        var hit=Physics.CapsuleCastAll(stuck+Vector3.up*.3f,stuck+Vector3.up*1.47f,.26f,direction,2.5f,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>!h.collider.GetComponent<CharacterController>()).OrderBy(h=>h.distance).FirstOrDefault();
        report.AppendLine("BEFORE: southward player-sized capsule at reported stuck position hits "+(hit.collider?HierarchyPath(hit.collider.transform):"nothing")+" at "+hit.distance.ToString("F3")+" m.");
        float beforeRemaining=WalkJournalCrossing(false,report);
        if(hit.collider && hit.collider.gameObject!=table && hit.collider.gameObject!=journal)throw new Exception("Unexpected blocker: "+HierarchyPath(hit.collider.transform));
        foreach(var c in table.GetComponents<Collider>()){Undo.RecordObject(c,"Remove collision of hidden prototype journal table");c.enabled=false;EditorUtility.SetDirty(c);}
        foreach(var c in journal.GetComponents<Collider>()){Undo.RecordObject(c,"Keep journal ray interaction without blocking the road");c.isTrigger=true;EditorUtility.SetDirty(c);}
        Physics.SyncTransforms();float afterRemaining=WalkJournalCrossing(true,report);
        if((hadBlockingTable && beforeRemaining<1) || afterRemaining>.12f)throw new Exception("Did not reproduce and clear the invisible obstacle.");
        var interaction=journal.GetComponent<JournalInteractable>();var aim=journal.GetComponent<Collider>().bounds.center;
        var eye=new Vector3(-6.5f,1.58f,-2.7f);if(!Physics.Raycast(eye,(aim-eye).normalized,out var journalHit,3f,~0,QueryTriggerInteraction.Collide) || journalHit.collider.GetComponent<JournalInteractable>()!=interaction)throw new Exception("Journal interaction ray was lost.");
        VerifyGameplay(before);VerifyReferenceWalking();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save collision repair.");
        report.AppendLine("PASS: disabled only the invisible JournalTable solid collider; Journal remains on its interaction layer with an enabled trigger. Real CharacterController crossed both directions; journal interaction ray and existing village routes passed. Gameplay components unchanged.");
        File.WriteAllText(folder+"/collision-repair.txt",report.ToString());File.WriteAllText(folder+"/collision-result.json","{\"passed\":true}");Inspect(true);
    }
    private static float WalkJournalCrossing(bool mustPass,StringBuilder report)
    {
        var player=Find("Player").GetComponent<CharacterController>();var obj=new GameObject("JournalCollisionWalkProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=obj.AddComponent<CharacterController>();
        cc.height=player.height;cc.radius=player.radius;cc.center=player.center;cc.skinWidth=player.skinWidth;cc.stepOffset=player.stepOffset;cc.slopeLimit=player.slopeLimit;cc.minMoveDistance=player.minMoveDistance;
        Physics.IgnoreCollision(cc,player,true);float largest=0;
        var a=new Vector3(-6.3031f,.28f,.2445f);var b=new Vector3(-6.3031f,.28f,-2.1f);
        try
        {
            foreach(var route in new[]{new[]{a,b},new[]{b,a}})
            {
                cc.enabled=false;obj.transform.position=route[0];cc.enabled=true;Physics.SyncTransforms();
                for(int step=0;step<130;step++){var delta=route[1]-obj.transform.position;delta.y=0;if(delta.magnitude<.06f)break;cc.Move(Vector3.ClampMagnitude(delta,.04f)+Vector3.down*.02f);}
                var remaining=route[1]-obj.transform.position;remaining.y=0;largest=Mathf.Max(largest,remaining.magnitude);
                report.AppendLine((mustPass?"AFTER":"BEFORE")+" CharacterController "+route[0].ToString("F3")+" -> "+route[1].ToString("F3")+": stopped "+obj.transform.position.ToString("F3")+", remaining="+remaining.magnitude.ToString("F3")+"m");
                if(mustPass && (remaining.magnitude>.12f || obj.transform.position.y<.1f))throw new Exception("Journal crossing still blocked.");
            }
        }
        finally{Physics.IgnoreCollision(cc,player,false);UnityEngine.Object.DestroyImmediate(obj);}
        return largest;
    }
    private static void TickVillageResume()
    {
        int phase=SessionState.GetInt("VillageResumePhase",0);
        if(phase==0 || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(phase==1 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt("VillageResumePhase",2);SessionState.SetFloat("VillageResumeReady",0);EditorApplication.EnterPlaymode();return;
        }
        if(phase!=2 || !EditorApplication.isPlaying || !GameManager.Instance)return;
        float ready=SessionState.GetFloat("VillageResumeReady",0);
        if(ready==0){SessionState.SetFloat("VillageResumeReady",(float)EditorApplication.timeSinceStartup+2);return;}
        if(EditorApplication.timeSinceStartup>=ready)FinishVillageResume();
    }
    public static void ResumeVillagePlayer()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Expected outside village.");
        var player=AllTransforms().FirstOrDefault(t=>t.gameObject.activeInHierarchy && t.GetComponent<FirstPersonController>());
        if(player && SessionState.GetInt("VillageResumePhase",0)==0)
        {
            SessionState.SetString("VillageResumePosition",JsonUtility.ToJson(player.position));SessionState.SetString("VillageResumeRotation",JsonUtility.ToJson(player.eulerAngles));
            var camera=player.GetComponent<FirstPersonController>().PlayerCamera;SessionState.SetFloat("VillageResumePitch",camera?Mathf.DeltaAngle(0,camera.localEulerAngles.x):0);
        }
        if(EditorApplication.isPlaying && GameManager.Instance)
        {FinishVillageResume();return;}
        // A script reload during Play Mode clears static singletons; recreate a
        // normal Play session instead of leaving a half-initialized live scene.
        if(EditorApplication.isPlaying){SessionState.SetInt("VillageResumePhase",1);EditorApplication.ExitPlaymode();}
        else{SessionState.SetInt("VillageResumePhase",2);EditorApplication.EnterPlaymode();}
    }
    private static void FinishVillageResume()
    {
        SessionState.SetInt("VillageResumePhase",0);var report=new StringBuilder();
        try
        {
            foreach(var pause in UnityEngine.Object.FindObjectsOfType<PauseUI>())pause.Resume();
            if(!GameManager.Instance)throw new Exception("GameManager did not initialize.");GameManager.Instance.SetPaused(false);EditorApplication.isPaused=false;
            var player=UnityEngine.Object.FindObjectOfType<FirstPersonController>();if(!player)throw new Exception("Player missing.");
            string saved=SessionState.GetString("VillageResumePosition","");if(saved.Length>0){player.TeleportTo(JsonUtility.FromJson<Vector3>(saved),Quaternion.Euler(JsonUtility.FromJson<Vector3>(SessionState.GetString("VillageResumeRotation","{}"))));player.SetPitch(SessionState.GetFloat("VillageResumePitch",0));}
            if(GameManager.Instance.InputBlocked || player.IsMovementLocked || player.LockWalkingOnly || !player.enabled || !player.GetComponent<PlayerInteraction>().enabled || player.GetComponent<PlayerInteraction>().IsInteractionLocked || Time.timeScale!=1)throw new Exception("A gameplay modal or controller still blocks input.");
            // Verify real character motion at the stuck location, then restore the
            // position. No transform, pause or quest state is saved into the scene.
            var cc=player.GetComponent<CharacterController>();var at=player.transform.position;var rotation=player.transform.rotation;float pitch=Mathf.DeltaAngle(0,player.PlayerCamera.localEulerAngles.x);Physics.SyncTransforms();cc.Move(player.transform.forward*.3f);
            float moved=Vector3.Distance(at,player.transform.position);player.TeleportTo(at,rotation);player.SetPitch(pitch);if(moved<.2f)throw new Exception("Physical movement still blocked at previous position: "+moved);
            var gameView=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().Name=="GameView");
            if(!gameView){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).FirstOrDefault(t=>t!=null);if(type!=null)gameView=EditorWindow.GetWindow(type);}
            if(gameView)gameView.Focus();GameManager.Instance.RefreshCursor();
            report.AppendLine("PASS: timeScale=1; Unity pause off; GameManager input unblocked; movement/look and interaction controllers enabled; actual character moved "+moved.ToString("F3")+"m at previous location then was restored. Game view focused. No scene assets changed.");
        }
        catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
        Directory.CreateDirectory(OutputFolder+"/PlayerDiagnostics");File.WriteAllText(OutputFolder+"/PlayerDiagnostics/resume.txt",report.ToString());DiagnoseVillagePlayer();
    }
    public static void DiagnoseVillagePlayer()
    {
        var report=new StringBuilder();report.AppendLine("Scene="+SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused+" timeScale="+Time.timeScale+" cursor="+Cursor.lockState+" focused="+(EditorWindow.focusedWindow?EditorWindow.focusedWindow.GetType().Name:"none"));
        foreach(var go in Selection.gameObjects)
        {
            report.AppendLine("SELECTED "+HierarchyPath(go.transform)+" position="+go.transform.position+" scale="+go.transform.lossyScale);
            foreach(var c in go.GetComponents<Component>())if(c)report.AppendLine(" COMPONENT "+c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
            foreach(var c in go.GetComponentsInChildren<Collider>(true))report.AppendLine(" SELECTED COLLIDER "+HierarchyPath(c.transform)+" enabled="+c.enabled+" trigger="+c.isTrigger+" bounds="+c.bounds);
        }
        var player=AllTransforms().FirstOrDefault(t=>t.gameObject.activeInHierarchy && t.GetComponent<FirstPersonController>());
        if(player)
        {
            var controller=player.GetComponent<FirstPersonController>();var interaction=player.GetComponent<PlayerInteraction>();var cc=player.GetComponent<CharacterController>();
            report.AppendLine("PLAYER "+player.position.ToString("F4")+" rotation="+player.eulerAngles+" enabled="+controller.enabled+" movementLock="+controller.IsMovementLocked+" walkingLock="+controller.LockWalkingOnly+" interactionEnabled="+(interaction && interaction.enabled)+" interactionLock="+(interaction && interaction.IsInteractionLocked)+" ccEnabled="+cc.enabled+" ccBounds="+cc.bounds);
            report.AppendLine("PLAYER COMPONENT "+EditorJsonUtility.ToJson(cc));
            foreach(var camera in player.GetComponentsInChildren<Camera>(true))report.AppendLine("PLAYER CAMERA "+HierarchyPath(camera.transform)+" enabled="+camera.enabled+" pos="+camera.transform.position+" rot="+camera.transform.eulerAngles);
            foreach(var collider in UnityEngine.Object.FindObjectsOfType<Collider>().Where(c=>c!=cc && c.enabled && c.gameObject.activeInHierarchy))
            {
                var eye=player.position+Vector3.up*.75f;if(collider.bounds.SqrDistance(eye)>64)continue;
                // Non-convex mesh ClosestPoint is unsupported; its AABB is enough
                // for this broad-phase diagnostic list, not a collision verdict.
                var closest=collider is MeshCollider mc && !mc.convex?collider.bounds.ClosestPoint(eye):collider.ClosestPoint(eye);float distance=Vector3.Distance(closest,eye);
                if(distance<8)report.AppendLine("NEAR COLLIDER "+HierarchyPath(collider.transform)+" type="+collider.GetType().Name+" distance="+distance.ToString("F3")+" trigger="+collider.isTrigger+" layer="+collider.gameObject.layer+" center="+collider.bounds.center.ToString("F3")+" size="+collider.bounds.size.ToString("F3"));
            }
        }
        var game=GameManager.Instance;if(game){report.AppendLine("GAME blocked="+game.InputBlocked+" paused="+game.IsPaused+" state="+game.CurrentState);var field=typeof(GameManager).GetField("inputOwners",BindingFlags.Instance|BindingFlags.NonPublic);foreach(UnityEngine.Object owner in (System.Collections.IEnumerable)field.GetValue(game))report.AppendLine(" INPUT OWNER "+(owner?owner.name+" ("+owner.GetType().Name+")":"destroyed"));}
        foreach(var go in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name.EndsWith("Panel")))report.AppendLine("OPEN PANEL "+HierarchyPath(go));
        Directory.CreateDirectory(OutputFolder+"/PlayerDiagnostics");File.WriteAllText(OutputFolder+"/PlayerDiagnostics/inspection.txt",report.ToString());
    }
}
