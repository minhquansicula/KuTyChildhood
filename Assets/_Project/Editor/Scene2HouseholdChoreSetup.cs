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
            "HouseLayout/TraditionalHouseInterior", "HouseLayout/KitchenArea/RiceJar",
            "HouseLayout/KitchenArea/WallBasins",
            "HouseLayout/KitchenArea/FirewoodStove/StovePotVisual",
            "HouseLayout/KitchenArea/RicePot", "HouseLayout/KitchenArea/DiningTable",
            "HouseLayout/KitchenArea/Sink/DishwashingStationVisual",
            "HouseLayout/KitchenArea/DishCabinet/DishCabinetVisual"
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

        string[] targets = {
            "HouseLayout/TraditionalHouseInterior/WaterJarVisual",
            "HouseLayout/KitchenArea/RiceJar/cailugao",
            "HouseLayout/KitchenArea/WallBasins/thao",
            "HouseLayout/KitchenArea/FirewoodStove/StovePotVisual",
            "HouseLayout/KitchenArea/RiceCookingStove/beplua_naucom",
            "HouseLayout/KitchenArea/Sink/DishwashingStationVisual",
            "HouseLayout/KitchenArea/DishCabinet/DishCabinetVisual",
            "BedroomArea/BedroomDecor/StorageChest", "BedroomArea/BedroomDecor/Radio",
            "HouseLayout/YardProps/ToolSet_02", "HouseLayout/YardProps/GrainMill",
            "HouseLayout/YardProps/BambooBasket"
        };
        HouseholdChore[] tasks = {
            HouseholdChore.FetchWater, HouseholdChore.MeasureRice, HouseholdChore.WashRice,
            HouseholdChore.LightFire, HouseholdChore.CookRice, HouseholdChore.WashDishes,
            HouseholdChore.PutAwayDishes, HouseholdChore.ArrangeKeepsakes, HouseholdChore.TuneRadio,
            HouseholdChore.SortTools, HouseholdChore.GrindCorn, HouseholdChore.CarryWood
        };
        if (!GameObject.Find(targets[2]))
        {
            var basin = Placeholder(Need("HouseLayout/KitchenArea/WallBasins").transform,
                "Placeholder_RiceWashingBowl", new Vector3(-4.3f, .16f, -.75f), new Vector3(.55f, .25f, .55f));
            targets[2] = "HouseLayout/KitchenArea/WallBasins/" + basin.name;
        }
        // Preflight before replacing old bindings. Preserve physical colliders and model placements.
        foreach (string path in targets) Need(path);
        var bed = Need("BedroomArea/RusticBedroom_Visual/BedBlock");
        var living = Need("HouseLayout/TraditionalHouseInterior");
        foreach (var old in Object.FindObjectsOfType<HouseholdChoreInteractable>(true))
        {
            Undo.RecordObject(old, "Replace old chore binding");
            old.enabled = false;
        }
        for (int i = 0; i < targets.Length; i++) BindVisual(Need(targets[i]), tasks[i]);

        // Clean up old blanket triggers/placeholders
        var oldFold = bed.transform.Find("FoldBlanketChoreTrigger");
        if (oldFold != null) Object.DestroyImmediate(oldFold.gameObject);
        var oldBlanket = bed.transform.Find("Placeholder_Blanket");
        if (oldBlanket != null) Object.DestroyImmediate(oldBlanket.gameObject);
        var strayBlanket = GameObject.Find("Placeholder_Blanket");
        if (strayBlanket != null) Object.DestroyImmediate(strayBlanket);

        // Disable occluding sleep trigger on bed so chore interaction is clean
        var bedInteractTrigger = bed.transform.Find("InteractTrigger");
        if (bedInteractTrigger != null)
        {
            Undo.RecordObject(bedInteractTrigger.gameObject, "Disable occluding bed trigger in chore flow");
            bedInteractTrigger.gameObject.SetActive(false);
        }

        // Assign bedExitPoint to xuonggiuong GameObject and zero out offset
        var bedWakeUp = Object.FindObjectOfType<BedWakeUpController>();
        var xgObj = GameObject.Find("xuonggiuong");
        if (bedWakeUp != null)
        {
            var so = new SerializedObject(bedWakeUp);
            if (xgObj != null)
            {
                var propPoint = so.FindProperty("bedExitPoint");
                if (propPoint != null) propPoint.objectReferenceValue = xgObj.transform;
            }
            var propOffset = so.FindProperty("bedExitOffset");
            if (propOffset != null) propOffset.vector3Value = Vector3.zero;
            var propExitDist = so.FindProperty("exitRightDistance");
            if (propExitDist != null) propExitDist.floatValue = 0.55f;
            so.ApplyModifiedProperties();
        }

        EnsureHouseInteriorColliders(living);

        var tray = Placeholder(living.transform, "Placeholder_FamilyMealTray", new Vector3(6.5f, .18f, -3f), new Vector3(.8f, .24f, .6f));
        BindOne(tray, HouseholdChore.SetTable, tray.transform);
        var letter = Placeholder(living.transform, "Placeholder_MothersLetter", new Vector3(6.5f, .4f, -3f), new Vector3(.3f, .12f, .22f));
        BindOne(letter, HouseholdChore.ReadLetter, letter.transform);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        ValidateBindings();
        Debug.Log("[Scene2 chores] Bound 14 chores across four rooms; visible placeholders; house colliders ensured; scene saved.");
    }

    [MenuItem("KuTy/Scene 2/Ensure house interior colliders")]
    private static void EnsureHouseInteriorCollidersMenu()
    {
        if (!Ready()) return;
        var living = Need("HouseLayout/TraditionalHouseInterior");
        EnsureHouseInteriorColliders(living);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Scene2 chores] House interior colliders configured and scene saved.");
    }

    private static void EnsureHouseInteriorColliders(GameObject living)
    {
        int added = 0;
        foreach (Transform child in living.transform)
        {
            if (child.GetComponent<HouseholdChoreInteractable>() != null) continue;
            var mf = child.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.sharedMesh.vertexCount == 0 || mf.sharedMesh.bounds.size.sqrMagnitude < 0.001f) continue;

            var mc = child.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = Undo.AddComponent<MeshCollider>(child.gameObject);
                added++;
            }
            Undo.RecordObject(mc, "Add house interior mesh collider");
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            mc.isTrigger = false;
        }
        Debug.Log($"[Scene2 chores] Configured MeshColliders on TraditionalHouseInterior ({added} new colliders added).");
    }

    private static GameObject Placeholder(Transform parent, string name, Vector3 position, Vector3 size)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(obj, "Create chore placeholder");
        obj.name = name;
        obj.transform.SetParent(parent, true);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        var scale = obj.transform.lossyScale;
        obj.transform.localScale = new Vector3(size.x / Mathf.Abs(scale.x), size.y / Mathf.Abs(scale.y), size.z / Mathf.Abs(scale.z));
        obj.GetComponent<BoxCollider>().isTrigger = true;
        return obj;
    }

    private static void BindVisual(GameObject visual, HouseholdChore chore)
    {
        var mesh = visual.GetComponent<MeshFilter>();
        if (mesh == null || mesh.sharedMesh == null) throw new System.InvalidOperationException("No mesh: " + visual.name);
        string name = "ChoreTarget_" + chore;
        var child = visual.transform.Find(name);
        var target = child ? child.gameObject : new GameObject(name);
        if (!child) Undo.RegisterCreatedObjectUndo(target, "Create chore target");
        target.transform.SetParent(visual.transform, false);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale = Vector3.one;
        var collider = target.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(target);
        Undo.RecordObject(collider, "Fit interaction trigger to visible mesh");
        collider.center = mesh.sharedMesh.bounds.center;
        collider.size = mesh.sharedMesh.bounds.size * 1.15f;
        collider.isTrigger = true;
        // Old chore components on the same physical prop must not hide the new trigger's prompt.
        BindOne(target, chore, visual.transform);
    }

    [MenuItem("KuTy/Scene 2/Validate expanded chore bindings")]
    private static void ValidateBindings()
    {
        var counts = new System.Collections.Generic.Dictionary<HouseholdChore, int>();
        for (int i = 0; i < HouseholdChoreFlow.ChoreCount; i++)
            counts[HouseholdChoreFlow.Order[i]] = 0;

        foreach (var task in Object.FindObjectsOfType<HouseholdChoreInteractable>())
        {
            if (!task.enabled) continue;
            if (counts.ContainsKey(task.Chore))
            {
                counts[task.Chore]++;
            }
            var collider = task.GetComponent<Collider>();
            if (!collider || !collider.isTrigger) throw new System.InvalidOperationException("Expected non-blocking trigger: " + task.name);
        }
        foreach (var kvp in counts)
        {
            if (kvp.Value != 1) throw new System.InvalidOperationException("Expected one binding for " + kvp.Key + ": " + kvp.Value);
        }
        Debug.Log($"[Scene2 chores] VALIDATION PASS: {HouseholdChoreFlow.ChoreCount} unique enabled tasks, all interaction colliders are triggers.");
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
        var waterRoot = Need("HouseLayout/TraditionalHouseInterior");
        var oldWaterPoint = waterRoot.transform.Find("WaterChoreTrigger");
        if (oldWaterPoint != null) Undo.DestroyObjectImmediate(oldWaterPoint.gameObject);
        var waterJar = Need("HouseLayout/TraditionalHouseInterior/WaterJarVisual");
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

    [MenuItem("KuTy/Scene 2/Smoke test chore rewards (overwrites checkpoint)")]
    private static void SmokeTestRewards()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("[Scene2 chores] Enter Play Mode in Scene 2 before running the reward smoke test.");
            return;
        }
        var flow = HouseholdChoreFlow.Instance;
        if (flow == null) throw new System.InvalidOperationException("HouseholdChoreFlow is missing.");
        var quests = QuestManager.Instance;
        var currency = CurrencyManager.Instance;
        if (quests == null || currency == null || quests.CompleteHouseholdChores() ||
            quests.CurrentStep != QuestStep.WashDishes || currency.CurrentMoney != 0)
            throw new System.InvalidOperationException("Household quest advanced or paid before all chores.");
        var legacyDishes = Object.FindObjectOfType<DishWashingGame>();
        legacyDishes?.StartGame();
        legacyDishes?.WashFor(30f);
        if (quests.CanWashDishes || currency.CurrentMoney != 0)
            throw new System.InvalidOperationException("Legacy dish game bypassed the 14 household chores.");
        var wake = Object.FindObjectOfType<BedWakeUpController>();
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(wake, null);
        flow.ShowChecklist();
        var first = System.Array.Find(Object.FindObjectsOfType<HouseholdChoreInteractable>(), t => t.enabled && t.Chore == HouseholdChoreFlow.Order[0]);
        first.Interact();
        first.enabled = false; // OnDisable must cancel without a reward or input leak.
        first.enabled = true;
        if (flow.Pieces != 0 || (GameManager.Instance != null && GameManager.Instance.InputBlocked))
            throw new System.InvalidOperationException("Cancel leaked input or reward.");
        if (flow.Complete(HouseholdChore.ReadLetter) || flow.Complete((HouseholdChore)999) || flow.Pieces != 0)
            throw new System.InvalidOperationException("Out of order reward accepted.");
        for (int i = 0; i < HouseholdChoreFlow.ChoreCount; i++)
        {
            var chore = HouseholdChoreFlow.Order[i];
            if (!flow.Complete(chore) || flow.Complete(chore) || flow.Pieces != (i + 1) * 10)
                throw new System.InvalidOperationException("Chore reward failed at index " + i);
            if (i < HouseholdChoreFlow.ChoreCount - 1 && Scene2Checkpoint.HomeChores != i + 1)
                throw new System.InvalidOperationException("Checkpoint failed after chore " + (i + 1));
        }
        if (flow.ActiveIndex != HouseholdChoreFlow.ChoreCount ||
            QuestManager.Instance == null || QuestManager.Instance.CurrentStep != QuestStep.BuyCandyAndMarbles)
            throw new System.InvalidOperationException("Household story handoff failed.");
        if (currency.CurrentMoney != 2000 || quests.CompleteHouseholdChores() || currency.CurrentMoney != 2000)
            throw new System.InvalidOperationException("Shopping allowance was missing or granted twice.");
        if (!Scene2Checkpoint.HasOutsideProgress || Scene2Checkpoint.OutsideStep != QuestStep.BuyCandyAndMarbles ||
            Scene2Checkpoint.OutsideMoney != 2000)
            throw new System.InvalidOperationException("Outdoor checkpoint was not saved correctly.");
        Debug.Log($"[Scene2 chores] Reward smoke test passed: {HouseholdChoreFlow.ChoreCount} one-time chores, {HouseholdChoreFlow.TotalPieces}/{HouseholdChoreFlow.TotalPieces} pieces, story advanced.");
    }

    [MenuItem("KuTy/Scene 2/Validate outdoor mini-games at runtime")]
    private static void ValidateOutdoorRuntime()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneNames.Act3_OutSide)
            throw new System.InvalidOperationException("Enter Play Mode in the outdoor scene first.");
        if (QuestManager.Instance == null || ShopManager.Instance == null ||
            CurrencyManager.Instance == null || InventoryManager.Instance == null ||
            MemoryCollectionManager.Instance == null || ShopUI.Instance == null ||
            MarbleAimUI.Instance == null || JournalUI.Instance == null)
            throw new System.InvalidOperationException("An outdoor story or UI system is missing.");
        if (!MemoryCollectionManager.Instance.HasCollected(MemoryType.FilialLove))
            throw new System.InvalidOperationException("The household memory was not restored outdoors.");
        if (ShopManager.Instance.ShopItems.Count != 2 ||
            ShopManager.Instance.ShopItems[0].price + ShopManager.Instance.ShopItems[1].price != 2000)
            throw new System.InvalidOperationException("The two shop items must cost 2,000 đồng total.");
        RequireOutdoorInteractable<ShopInteractable>();
        RequireOutdoorInteractable<MarbleInteractable>();
        RequireOutdoorInteractable<JournalInteractable>();
        RequireOutdoorInteractable<DreamKeyInteractable>();
        RequireOutdoorInteractable<ChainedDoorInteractable>();
        var marble = Object.FindObjectOfType<MarbleShootingGame>();
        if (marble == null) throw new System.InvalidOperationException("MarbleShootingGame is missing.");
        var data = new SerializedObject(marble);
        foreach (string field in new[] { "playerMarble", "shootPosition", "ringCollider", "gameCameraPosition" })
            if (data.FindProperty(field)?.objectReferenceValue == null)
                throw new System.InvalidOperationException("Marble setup is missing " + field);
        if (data.FindProperty("targetMarbles")?.arraySize < 3)
            throw new System.InvalidOperationException("Marble setup needs at least three target marbles.");
        Debug.Log("[Scene2 outdoor] VALIDATION PASS: shop, marble game, journal, key, door and 2,000 đồng economy are configured.");
    }

    private static void RequireOutdoorInteractable<T>() where T : InteractableBase
    {
        var items = Object.FindObjectsOfType<T>(true);
        if (items.Length != 1 || items[0].GetComponent<Collider>() == null)
            throw new System.InvalidOperationException("Expected one outdoor " + typeof(T).Name + " with a collider.");
    }

    [MenuItem("KuTy/Scene 2/Check expanded chore targeting")]
    private static void CheckTargeting()
    {
        Physics.SyncTransforms();
        int reached = 0;
        var report = new StringBuilder();
        foreach (var task in Object.FindObjectsOfType<HouseholdChoreInteractable>())
        {
            if (!task.enabled) continue;
            var box = task.GetComponent<Collider>();
            bool hitTarget = false;
            foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                new Vector3(1,0,1).normalized, new Vector3(-1,0,1).normalized })
            {
                Vector3 origin = box.bounds.center + direction * 1.8f;
                origin.y = Mathf.Max(1.6f, origin.y);
                Vector3 delta = box.bounds.center - origin;
                var hits = Physics.RaycastAll(origin, delta.normalized, 3f, ~0, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int h = 0; h < hits.Length; h++)
                {
                    if (hits[h].collider == box) { hitTarget = true; break; }
                    if (!hits[h].collider.isTrigger) break; // Solid wall/obstacle blocks ray
                }
                if (hitTarget) break;
                report.AppendLine("  " + task.Chore + " from " + origin + " hit " + (hits.Length > 0 ? hits[0].collider.name : "nothing"));
            }
            report.AppendLine(task.Chore + ": " + (hitTarget ? "reachable ray" : "BLOCKED") + " at " + box.bounds.center);
            if (hitTarget) reached++;
        }
        System.IO.File.WriteAllText("Library/Scene2ChoreTargeting.txt", report.ToString());
        Debug.Log($"[Scene2 chores] TARGETING {reached}/{HouseholdChoreFlow.ChoreCount}. {report}");
    }

    [MenuItem("KuTy/Scene 2/Smoke test water targeting")]
    private static void SmokeTestWaterTargeting()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
            throw new System.InvalidOperationException("Enter Scene 2 Play Mode first.");
        var jar = Need("HouseLayout/TraditionalHouseInterior/WaterJarVisual");
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
        interaction.enabled = true;
        obj.layer = LayerMask.NameToLayer("Interactable");
        EditorUtility.SetDirty(interaction);
    }
}
