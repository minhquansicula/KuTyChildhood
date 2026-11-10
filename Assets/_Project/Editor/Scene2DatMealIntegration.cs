using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Dat's meal art, driven exclusively by the existing SetTable task and checkpoint IDs.</summary>
public static class Scene2DatMealIntegration
{
    [MenuItem("KuTy/Scene 2/Integrate Dat meal with current table task")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Open saved Home in Edit Mode first; never overwrite pending scene edits.");
        var layout = UnityEngine.Object.FindObjectOfType<Scene2AuthoredTableLayout>();
        var meal = GameObject.Find("MamCom_GiaDinh");
        if (!layout || !layout.IsReady || !meal) throw new InvalidOperationException("Merged meal and authored table are required.");
        if (layout.useFoodAssets)
        {
            // Earlier table layouts borrowed their visuals from the washing station.
            // Keep those real sources available; only the meal's own clones are hidden at runtime.
            var washing = layout.GetComponent<Scene2DishwashingLayout>();
            bool changed = false;
            if (washing && washing.IsReady)
                foreach (var dish in washing.dishes)
                    if (!dish.source.gameObject.activeSelf)
                    { Undo.RecordObject(dish.source.gameObject, "Keep washing sources available"); dish.source.gameObject.SetActive(true); changed = true; }
            if (changed)
            { Undo.FlushUndoRecordObjects(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Check(); return;
        }
        var names = new[] { "Canh", "ThitKho", "RauXao" };
        var labels = new[] { "Tô canh", "Đĩa thịt kho", "Đĩa rau xào" };
        var dishes = names.Select(n => meal.transform.Find(n)).ToArray();
        if (dishes.Any(t => !t || !t.GetComponentInChildren<MeshFilter>())) throw new InvalidOperationException("Incomplete Dat meal.");
        var sim = layout.GetComponent<Scene2HouseholdSimulator>();
        var settings = new SerializedObject(sim);
        var stations = settings.FindProperty("stations");
        Transform surface = null;
        for (int i = 0; i < stations.arraySize; i++)
        {
            var station = stations.GetArrayElementAtIndex(i);
            if (station.FindPropertyRelative("chore").intValue == (int)HouseholdChore.SetTable)
                surface = station.FindPropertyRelative("surface").objectReferenceValue as Transform;
        }
        if (!surface) throw new InvalidOperationException("Existing table work surface is missing.");
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
        try
        {
            // The former table visuals are shared with washing; leave their poses/visibility untouched.
            for (int i = 0; i < dishes.Length; i++)
            {
                var dish = dishes[i];
                Undo.RecordObject(dish, "Fit Dat meal to current task surface");
                // Keep model orientation/materials. Fit the footprint to the existing three lanes.
                var bounds = Scene2DishwashingLayout.VisualBounds(dish);
                float diameter = i == 0 ? .27f : .28f;
                dish.localScale *= diameter / Mathf.Max(bounds.size.x, bounds.size.z);
                bounds = Scene2DishwashingLayout.VisualBounds(dish);
                var home = surface.TransformPoint(layout.sourceLocal[i]);
                dish.position += new Vector3(home.x - bounds.center.x, home.y + .003f - bounds.min.y, home.z - bounds.center.z);
                // Remove only the incoming standalone input owner: it bypasses task/save/reward rules.
                foreach (var standalone in dish.GetComponents<Scene2FoodInteractable>()) Undo.DestroyObjectImmediate(standalone);
                var target = dish.GetComponent<HouseholdChoreInteractable>() ?? Undo.AddComponent<HouseholdChoreInteractable>(dish.gameObject);
                target.Configure(HouseholdChore.SetTable, dish);
                PrefabUtility.RecordPrefabInstancePropertyModifications(dish);
                EditorUtility.SetDirty(target);
            }
            Undo.RecordObject(layout, "Bind Dat meal to existing stable task IDs");
            layout.dishes = dishes; layout.labels = labels; layout.useFoodAssets = true;
            EditorUtility.SetDirty(layout);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save integrated Home.");
            Check();
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }

    [MenuItem("KuTy/Tests/Check Dat meal integration")]
    public static void Check()
    {
        var layout = UnityEngine.Object.FindObjectOfType<Scene2AuthoredTableLayout>();
        if (!layout || !layout.IsReady || !layout.useFoodAssets) throw new InvalidOperationException("Dat meal not bound.");
        for (int i = 0; i < 3; i++)
        {
            var dish = layout.dishes[i];
            if (dish.GetComponent<Scene2FoodInteractable>() || dish.GetComponent<HouseholdChoreInteractable>()?.Chore != HouseholdChore.SetTable)
                throw new InvalidOperationException("Dish must use the current task input owner: " + dish.name);
            foreach (var mesh in dish.GetComponentsInChildren<MeshFilter>())
                if (!AssetDatabase.GetAssetPath(mesh.sharedMesh).Contains("/Models/Kitchen/Food/")) throw new InvalidOperationException("Expected Dat food mesh.");
            var bounds = Scene2DishwashingLayout.VisualBounds(dish);
            if (Mathf.Max(bounds.size.x, bounds.size.z) > .29f || bounds.size.y > .30f)
                throw new InvalidOperationException("Meal footprint does not fit task lanes: " + dish.name);
        }
        if (HouseholdChoreFlow.Order.Length != 5 || HouseholdChoreFlow.TotalPieces != 50)
            throw new InvalidOperationException("Existing five-task/reward flow changed.");
        Debug.Log("[DatMeal] PASS: three original Dat food meshes/materials bound to existing table input, stable checkpoint slots and five-task/50-piece flow.");
    }
}
