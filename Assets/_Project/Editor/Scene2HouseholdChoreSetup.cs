using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>One-time, idempotent Scene 2 binding. Does not touch imported FBX assets.</summary>
public static class Scene2HouseholdChoreSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";

    [MenuItem("KuTy/Scene 2/Inspect chore targets")]
    private static void Inspect()
    {
        if (!Ready()) return;
        var report = new StringBuilder("[Scene2 chores] Target bounds:\n");
        string[] paths =
        {
            "HouseLayout/lunuoc", "HouseLayout/KitchenArea/CaiLu",
            "HouseLayout/KitchenArea/ThauTreoTuong",
            "HouseLayout/KitchenArea/FirewoodStove/NoiBepLua",
            "HouseLayout/KitchenArea/RicePot", "HouseLayout/KitchenArea/DiningTable",
            "HouseLayout/KitchenArea/Sink/GocRuaChen",
            "HouseLayout/KitchenArea/DishCabinet/TuChen"
        };
        foreach (var path in paths)
        {
            var obj = GameObject.Find(path);
            if (obj == null) { report.AppendLine(path + " MISSING"); continue; }
            report.AppendLine(path + " at " + obj.transform.position);
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                report.AppendLine("  " + renderer.name + ": " + renderer.bounds.center + " size " + renderer.bounds.size);
        }
        Debug.Log(report.ToString());
    }

    [MenuItem("KuTy/Scene 2/Bind household chores")]
    private static void Bind()
    {
        if (!Ready()) return;
        var flowHost = Need("KitchenFlowManager");
        if (flowHost.GetComponent<HouseholdChoreFlow>() == null)
            Undo.AddComponent<HouseholdChoreFlow>(flowHost);

        BindWaterJar();

        var rice = Need("HouseLayout/KitchenArea/CaiLu");
        BindOne(rice, HouseholdChore.MeasureRice, rice.transform.Find("cailugao"));

        var basin = Need("HouseLayout/KitchenArea/ThauTreoTuong");
        var basinVisual = basin.transform.Find("thao") ?? basin.transform;
        var basinRenderer = basinVisual.GetComponent<Renderer>();
        var basinPosition = basinRenderer != null ? basinRenderer.bounds.center : basin.transform.position + Vector3.up;
        var basinPoint = Proxy(basin.transform, "RiceWashingChoreTrigger", basinPosition, new Vector3(.8f, .55f, .7f));
        BindOne(basinPoint, HouseholdChore.WashRice, basinVisual);

        BindReplace("HouseLayout/KitchenArea/FirewoodStove/NoiBepLua", HouseholdChore.LightFire,
            Need("HouseLayout/KitchenArea/FirewoodStove/NoiBepLua").transform, typeof(MashToCompleteTask));
        BindOne(Need("HouseLayout/KitchenArea/FirewoodStove/InteractTrigger"), HouseholdChore.LightFire,
            Need("HouseLayout/KitchenArea/FirewoodStove/NoiBepLua").transform);

        BindReplace("HouseLayout/KitchenArea/RicePot/InteractTrigger", HouseholdChore.CookRice,
            Need("HouseLayout/KitchenArea/RicePot").transform, typeof(HoldToCompleteTask));
        BindReplace("HouseLayout/KitchenArea/DiningTable/InteractTrigger", HouseholdChore.SetTable,
            Need("HouseLayout/KitchenArea/DiningTable").transform, typeof(HoldToCompleteTask));

        BindReplace("HouseLayout/KitchenArea/Sink/GocRuaChen", HouseholdChore.WashDishes,
            Need("HouseLayout/KitchenArea/Sink/GocRuaChen").transform, typeof(DishInteractable));
        BindReplace("HouseLayout/KitchenArea/DishCabinet/TuChen", HouseholdChore.PutAwayDishes,
            Need("HouseLayout/KitchenArea/DishCabinet/TuChen").transform, typeof(HoldToCompleteTask));

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Scene2 chores] Bound 8 chores and saved Scene 2.");
    }

    [MenuItem("KuTy/Scene 2/Fix water jar interaction")]
    private static void FixWaterJar()
    {
        if (!Ready()) return;
        BindWaterJar();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Scene2 chores] Water interaction moved onto tripo_part_10.");
    }

    private static void BindWaterJar()
    {
        // The FBX root is an entire group of household parts; its pivot is far from the actual water jar.
        var waterRoot = Need("HouseLayout/lunuoc");
        var oldWaterPoint = waterRoot.transform.Find("WaterChoreTrigger");
        if (oldWaterPoint != null) Undo.DestroyObjectImmediate(oldWaterPoint.gameObject);
        var waterJar = Need("HouseLayout/lunuoc/tripo_part_10");
        var waterMesh = waterJar.GetComponent<MeshFilter>();
        if (waterMesh == null || waterMesh.sharedMesh == null)
            throw new System.InvalidOperationException("Water jar mesh is missing.");
        var waterCollider = waterJar.GetComponent<BoxCollider>();
        if (waterCollider == null) waterCollider = Undo.AddComponent<BoxCollider>(waterJar);
        Undo.RecordObject(waterCollider, "Fit water jar interaction collider");
        waterCollider.center = waterMesh.sharedMesh.bounds.center;
        waterCollider.size = waterMesh.sharedMesh.bounds.size * 1.1f;
        waterCollider.isTrigger = true;
        BindOne(waterJar, HouseholdChore.FetchWater, waterJar.transform);
    }

    [MenuItem("KuTy/Scene 2/Smoke test chore rewards")]
    private static void SmokeTestRewards()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("[Scene2 chores] Enter Play Mode in Scene 2 before running the reward smoke test.");
            return;
        }
        var flow = HouseholdChoreFlow.Instance;
        if (flow == null) throw new System.InvalidOperationException("HouseholdChoreFlow is missing.");
        flow.ShowChecklist();
        for (int i = 0; i < HouseholdChoreFlow.ChoreCount; i++)
        {
            var chore = (HouseholdChore)i;
            if (!flow.Complete(chore) || flow.Complete(chore) || flow.Pieces != (i + 1) * 10)
                throw new System.InvalidOperationException("Chore reward failed at index " + i);
        }
        if (flow.ActiveIndex != HouseholdChoreFlow.ChoreCount ||
            QuestManager.Instance == null || QuestManager.Instance.CurrentStep != QuestStep.BuyCandyAndMarbles)
            throw new System.InvalidOperationException("Household story handoff failed.");
        Debug.Log("[Scene2 chores] Reward smoke test passed: 8 one-time chores, 80/80 pieces, story advanced.");
    }

    [MenuItem("KuTy/Scene 2/Smoke test water targeting")]
    private static void SmokeTestWaterTargeting()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
            throw new System.InvalidOperationException("Enter Scene 2 Play Mode first.");
        var jar = Need("HouseLayout/lunuoc/tripo_part_10");
        var collider = jar.GetComponent<BoxCollider>();
        var interaction = jar.GetComponent<HouseholdChoreInteractable>();
        var flow = HouseholdChoreFlow.Instance;
        if (collider == null || interaction == null || flow == null)
            throw new System.InvalidOperationException("Water jar interaction is incomplete.");
        flow.ShowChecklist();
        if (string.IsNullOrEmpty(interaction.GetPromptText()))
            throw new System.InvalidOperationException("Water jar prompt is hidden at 0/80.");
        Vector3 target = collider.bounds.center;
        foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            Vector3 origin = target + direction * 2f;
            if (Physics.Raycast(origin, target - origin, out var hit, 3f, ~0, QueryTriggerInteraction.Collide)
                && hit.collider == collider)
            {
                Debug.Log("[Scene2 chores] Water targeting passed from " + direction + ": " + interaction.GetPromptText());
                return;
            }
        }
        throw new System.InvalidOperationException("No nearby approach ray hits the water jar first.");
    }

    private static bool Ready()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
        {
            Debug.LogError("[Scene2 chores] Open Scene 2 in Edit Mode first: " + ScenePath);
            return false;
        }
        return true;
    }

    private static GameObject Need(string path)
    {
        var obj = GameObject.Find(path);
        if (obj == null) throw new System.InvalidOperationException("Missing Scene 2 target: " + path);
        return obj;
    }

    private static GameObject Proxy(Transform parent, string name, Vector3 worldPosition, Vector3 worldSize)
    {
        var existing = parent.Find(name);
        var obj = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null) { Undo.RegisterCreatedObjectUndo(obj, "Create " + name); obj.transform.SetParent(parent, true); }
        obj.transform.position = worldPosition;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        var collider = obj.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(obj);
        var scale = obj.transform.lossyScale;
        collider.size = new Vector3(worldSize.x / Mathf.Abs(scale.x), worldSize.y / Mathf.Abs(scale.y),
            worldSize.z / Mathf.Abs(scale.z));
        collider.isTrigger = true;
        obj.layer = LayerMask.NameToLayer("Interactable");
        return obj;
    }

    private static void BindReplace(string path, HouseholdChore chore, Transform anchor, System.Type oldType)
    {
        var obj = Need(path);
        foreach (var component in obj.GetComponents(oldType)) Undo.DestroyObjectImmediate(component);
        BindOne(obj, chore, anchor);
    }

    private static void BindOne(GameObject obj, HouseholdChore chore, Transform anchor)
    {
        var interaction = obj.GetComponent<HouseholdChoreInteractable>();
        if (interaction == null) interaction = Undo.AddComponent<HouseholdChoreInteractable>(obj);
        Undo.RecordObject(interaction, "Configure Scene 2 chore");
        interaction.Configure(chore, anchor);
        obj.layer = LayerMask.NameToLayer("Interactable");
        EditorUtility.SetDirty(interaction);
    }
}
