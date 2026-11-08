using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Additive, repeatable authoring; never rebuilds the house, kitchen or player.</summary>
public static class Scene2SimulatorSetup
{
    [MenuItem("KuTy/Scene 2/Install household simulator")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open the home scene first; this installer will not discard another scene.");
        var flow = UnityEngine.Object.FindObjectOfType<HouseholdChoreFlow>();
        if (!flow) throw new InvalidOperationException("Household flow missing.");
        var simulator = flow.GetComponent<Scene2HouseholdSimulator>();
        if (!simulator) simulator = Undo.AddComponent<Scene2HouseholdSimulator>(flow.gameObject);
        var root = GameObject.Find("Scene2SimulatorStations");
        if (!root) { root = new GameObject("Scene2SimulatorStations"); Undo.RegisterCreatedObjectUndo(root, "Simulator stations"); }
        var stations = new List<Scene2HouseholdSimulator.Station>();
        Add(stations, root.transform, HouseholdChore.ArrangeKeepsakes, new Vector3(10.85f, .83f, 9.0f), 0);
        Add(stations, root.transform, HouseholdChore.FetchWater, new Vector3(-5.95f, 1.02f, -.65f), -90);
        Add(stations, root.transform, HouseholdChore.MeasureRice, new Vector3(-5.95f, 1.02f, -2.25f), -90);
        Add(stations, root.transform, HouseholdChore.WashDishes, new Vector3(-2.95f, .93f, -.35f), 0);
        Add(stations, root.transform, HouseholdChore.PutAwayDishes, new Vector3(.95f, 1.02f, -.60f), 0);
        // Use the actual authored dining surface rather than the retired BambooTable.
        var table = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true)
            .FirstOrDefault(c => c.sharedMesh && c.sharedMesh.name == "tripo_part_3 Instance");
        if (!table) throw new InvalidOperationException("Actual dining surface tripo_part_3 is missing.");
        Bounds bounds = table.bounds;
        Add(stations, root.transform, HouseholdChore.SetTable,
            new Vector3(bounds.center.x, bounds.max.y + .04f, bounds.center.z), 0);
        Add(stations, root.transform, HouseholdChore.ReadLetter,
            new Vector3(bounds.center.x, bounds.max.y + .055f, bounds.center.z), 0);
        Undo.RecordObject(simulator, "Bind household simulator");
        simulator.Configure(stations.ToArray());
        EditorUtility.SetDirty(simulator);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[HouseholdSimulator] Installed 7 world-space activities. Existing radio, wood, kitchen and optional tasks retained.");
    }

    private static void Add(List<Scene2HouseholdSimulator.Station> result, Transform root,
        HouseholdChore chore, Vector3 point, float yaw)
    {
        var surface = root.Find(chore.ToString());
        if (!surface)
        {
            surface = new GameObject(chore.ToString()).transform;
            surface.SetParent(root, false); Undo.RegisterCreatedObjectUndo(surface.gameObject, "Simulator station");
        }
        Undo.RecordObject(surface, "Simulator station pose");
        surface.SetPositionAndRotation(point, Quaternion.Euler(0, yaw, 0)); surface.localScale = Vector3.one;
        var camera = surface.Find("Close view");
        if (!camera) { camera = new GameObject("Close view").transform; camera.SetParent(surface, false); }
        camera.localPosition = new Vector3(0, .84f, -.98f);
        camera.rotation = Quaternion.LookRotation(surface.TransformPoint(new Vector3(0, .06f, 0)) - camera.position, Vector3.up);
        result.Add(new Scene2HouseholdSimulator.Station { chore = chore, surface = surface, view = camera });
    }
}
