using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keep the imported ground-level washing pot outside the legacy sink's solid bounds.</summary>
public static class Scene2KitchenAccessSetup
{
    [MenuItem("KuTy/Scene 2/Clear imported rice washing approach")]
    public static void Repair()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first.");
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        var sink = GameObject.Find("HouseLayout/KitchenArea/Sink/DishwashingStationVisual");
        if (!kitchen || !kitchen.importedKitchen || !sink || !sink.TryGetComponent<BoxCollider>(out var solid))
            throw new InvalidOperationException("Expected imported kitchen and legacy sink are missing.");
        Bounds bounds = solid.bounds;
        Vector3 water = kitchen.waterHit.bounds.center;
        var chair = GameObject.Find("HouseLayout/TraditionalHouseInterior/Chair");
        var chairVisual = chair ? chair.GetComponent<Renderer>() : null;
        Bounds potFootprint = new Bounds(water, new Vector3(.50f, .30f, .50f));
        bool obstructed = bounds.Intersects(potFootprint) || chairVisual && chairVisual.bounds.Intersects(potFootprint);
        if (!obstructed)
        { Debug.Log("[Kitchen access] Washing pot is already clear of the legacy sink; no changes."); return; }
        Undo.RecordObjects(new UnityEngine.Object[] { kitchen.washRoot, kitchen.washView }, "Clear rice washing access");
        Vector3 destination = water;
        destination.z = bounds.min.z - .80f;
        if (chairVisual) destination.x = chairVisual.bounds.max.x + .45f;
        Vector3 delta = destination - water;
        kitchen.washRoot.position += delta;
        water += delta;
        kitchen.washView.SetPositionAndRotation(water + new Vector3(.65f, .78f, -.60f),
            Quaternion.LookRotation(-new Vector3(.65f, .78f, -.60f), Vector3.up));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("[Kitchen access] Moved only the rice-washing cluster by " + delta.ToString("F3") + "; preserved sink solids, stove, house, characters and chore IDs.");
    }
}
