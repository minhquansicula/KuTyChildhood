using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2FiveTaskSetup
{
    public const string Output="Tools/Scene2FiveTasks";
    [MenuItem("KuTy/Tests/Check five-task checkpoint migration")]
    public static void CheckMigration()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Output);KuTyCheckpointTestScope.Capture();var report=new System.Text.StringBuilder();int assertions=0;
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);assertions++;report.AppendLine("PASS "+message);}
        try
        {
            const string prefix="KuTy.Scene2Checkpoint.";
            foreach(int version in new[]{1,2,3})
            {
                int max=version==1?14:version==2?12:5;
                for(int count=0;count<=max;count++)
                {
                    int equivalent=version==1?count-Mathf.Min(2,Mathf.Max(0,count-2)):count;
                    int expected=version==3?count:equivalent<=6?0:equivalent<=10?equivalent-6:equivalent==11?4:5;
                    Check(Scene2Checkpoint.MigrateHomeCount(version,count)==expected,"Migration v"+version+" raw="+count+" -> retained completed="+expected);
                    PlayerPrefs.SetInt(prefix+"Version",version);PlayerPrefs.SetInt(prefix+"Stage",1);PlayerPrefs.SetInt(prefix+"Chores",count);PlayerPrefs.SetInt(prefix+"Optional",3);
                    Check(Scene2Checkpoint.HomeChores==Mathf.Min(expected,4)&&Scene2Checkpoint.HomeOptional==0,"Home resume waits at actual retained task; removed optional rewards gone v"+version+" raw="+count);
                }
            }
            PlayerPrefs.SetInt(prefix+"Version",2);PlayerPrefs.SetInt(prefix+"Stage",1);PlayerPrefs.SetInt(prefix+"Chores",7);
            PlayerPrefs.SetInt(prefix+"WorkChore",(int)HouseholdChore.CookRice);PlayerPrefs.SetString(prefix+"Work","retained source cooking work");
            Check(Scene2Checkpoint.HomeChores==1&&Scene2Checkpoint.LoadWork(HouseholdChore.CookRice)=="retained source cooking work","V2 cooking work survives enum-stable migration");
            Scene2Checkpoint.SaveHome(1);
            Check(PlayerPrefs.GetInt(prefix+"Version")==3&&Scene2Checkpoint.HomeChores==1&&Scene2Checkpoint.LoadWork(HouseholdChore.CookRice)=="retained source cooking work","Saving migrated index does not erase compatible partial work");
            PlayerPrefs.SetInt(prefix+"Version",2);PlayerPrefs.SetInt(prefix+"Chores",4);PlayerPrefs.SetInt(prefix+"WorkChore",(int)HouseholdChore.MeasureRice);PlayerPrefs.SetString(prefix+"Work","retired work");
            Check(Scene2Checkpoint.LoadWork(HouseholdChore.MeasureRice)=="","Removed work cannot reopen");Scene2Checkpoint.SaveHome(0);
            Check(!PlayerPrefs.HasKey(prefix+"Work"),"First five-task save clears only incompatible removed work");
            foreach(int version in new[]{1,2,3})
            {
                PlayerPrefs.SetInt(prefix+"Version",version);PlayerPrefs.SetInt(prefix+"Stage",2);PlayerPrefs.SetInt(prefix+"Step",(int)QuestStep.PlayMarbles);PlayerPrefs.SetInt(prefix+"Money",1234);
                Check(Scene2Checkpoint.HasOutsideProgress&&Scene2Checkpoint.OutsideStep==QuestStep.PlayMarbles&&Scene2Checkpoint.OutsideMoney==1234,"Outside checkpoint remains intact v"+version);
                Check(Scene2Checkpoint.ResumeScene==SceneNames.Act2_Home&&Scene2Checkpoint.HomeChores==4,"Legacy outdoor continue returns to indoor letter, never removed Scene 3 v"+version);
            }
            File.WriteAllText(Output+"/migration-checks.txt","PASS "+assertions+" assertions\n"+report);
        }
        catch(Exception e){File.WriteAllText(Output+"/migration-checks.txt","FAIL "+assertions+" assertions\n"+report+e);throw;}
        finally{KuTyCheckpointTestScope.Restore();}
    }
    [MenuItem("KuTy/Scene 2/Keep five tasks and return mother letter indoors")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=Scene2MotherHomeSetup.Home)throw new InvalidOperationException("Saved Home Edit Mode required; preserve pending user edits.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory("Builds/FiveTasks-20261109");
        string backup="Builds/FiveTasks-20261109/Home-before.unity";
        if(File.Exists(backup))throw new InvalidOperationException("One-shot setup already attempted. Inspect state; do not overwrite user work.");
        File.Copy(scene.path,backup);
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var letter=all.Single(t=>t.name=="SM_Letter");var tabletop=letter.GetComponent<Scene2TabletopLetter>();
        var table=all.Single(t=>t.name=="bandai_khach");var support=table.GetComponent<MeshCollider>();
        if(!support||!support.enabled||!tabletop)throw new InvalidOperationException("Real indoor living-room table/letter binding missing.");
        var before=all.ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale,t.parent));
        Undo.RecordObject(tabletop,"Return letter to indoor table");Undo.RecordObject(letter,"Return letter indoors");
        foreach(var child in letter.GetComponentsInChildren<Transform>(true))Undo.RecordObject(child,"Fit original letter on indoor table");
        tabletop.Configure(support);EditorUtility.SetDirty(tabletop);
        foreach(var target in all.SelectMany(t=>t.GetComponents<HouseholdChoreInteractable>()))
            if(!Scene2HouseholdActivityPolicy.IsActive(target.Chore)){Undo.RecordObject(target,"Retire removed task only");target.enabled=false;EditorUtility.SetDirty(target);}
        // UI-only station markers must not keep the letter camera/guide outside.
        var sim=UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();var serialized=new SerializedObject(sim);var stations=serialized.FindProperty("stations");
        Transform surface=null,view=null;
        for(int i=0;i<stations.arraySize;i++)
        {
            var entry=stations.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("chore").intValue!=(int)HouseholdChore.ReadLetter)continue;
            surface=entry.FindPropertyRelative("surface").objectReferenceValue as Transform;view=entry.FindPropertyRelative("view").objectReferenceValue as Transform;
            if(surface){Undo.RecordObject(surface,"Keep letter marker indoors");surface.position=letter.position;}
            if(view){Undo.RecordObject(view,"Keep letter view indoors");view.position=letter.position+new Vector3(0,.7f,-.7f);view.LookAt(letter.position);}
            entry.FindPropertyRelative("existingFixture").objectReferenceValue=table.GetComponent<Renderer>();
        }
        serialized.ApplyModifiedProperties();
        foreach(var pose in before)
            if(pose.Key!=letter&&!pose.Key.IsChildOf(letter)&&pose.Key!=surface&&pose.Key!=view &&
               (pose.Key.localPosition!=pose.Value.Item1||pose.Key.localRotation!=pose.Value.Item2||pose.Key.localScale!=pose.Value.Item3||pose.Key.parent!=pose.Value.Item4))
                throw new InvalidOperationException("Unrelated prop moved: "+pose.Key.name);
        Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var camera=new GameObject("Letter indoor review pose").transform;camera.position=letter.position+new Vector3(1,1,-1.5f);camera.LookAt(letter.position);
        Scene2SourceKitchenBinding.Capture(camera,"letter-indoors.png");UnityEngine.Object.DestroyImmediate(camera.gameObject);
        File.Copy(Scene2SourceKitchenArticulation.Output+"/letter-indoors.png",Output+"/letter-indoors.png",true);
        File.WriteAllText(Output+"/binding.txt","Only five tasks remain; removed task components disabled, all objects retained.\nOriginal letter now supported by "+Scene2ExistingAssetReview.PathOf(table)+"; letter="+letter.position.ToString("F5")+"\nAll unrelated original local poses and parents preserved; only letter/model and UI letter markers moved.\n");
        Debug.Log("[FiveTasks] Five-task bindings saved; original mother letter moved onto the existing indoor living-room table.");
    }
}
