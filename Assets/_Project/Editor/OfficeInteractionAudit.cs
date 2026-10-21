using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class OfficeInteractionAudit
{
    [MenuItem("KuTy/Tests/Prepare office spatial interactions only")]
    public static void PrepareSpatialTest()
    {
        KuTyOfficeBlockoutGameplaySmoke.Prepare();
        SessionState.SetBool("KuTy.OfficeSpatialOnly", true);
    }
    [MenuItem("KuTy/Office Intro/Align interaction zones with visible props")]
    public static void Align()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Act1_Office_Blockout")
            throw new System.InvalidOperationException("Open Act1_Office_Blockout first.");
        AlignProp("WaterCooler_SLOT", new[] { "PF_Office_WaterCooler_01",
            "PF_Office_MeshPart_Transparent_blue_plastic_procedural_01" });
        AlignProp("Printer_SLOT", new[] { "PF_Office_Printer_Base_01", "PF_Office_Printer_Top_01" });
        AlignProp("Clock_SLOT", new[] { "PF_Office_MeshPart_Clock_face_01" });
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Inspect();
    }

    static void AlignProp(string slotName, string[] models)
    {
        var slot = GameObject.Find(slotName);
        if (!slot) throw new System.InvalidOperationException("Missing " + slotName);
        var renders = models.SelectMany(name =>
        {
            var model = GameObject.Find(name);
            if (!model) throw new System.InvalidOperationException("Missing " + name);
            return model.GetComponentsInChildren<Renderer>().Where(r => r.enabled);
        }).ToArray();
        if (renders.Length == 0) throw new System.InvalidOperationException("No visible renderers for " + slotName);
        Bounds bounds = renders[0].bounds;
        foreach (var render in renders.Skip(1)) bounds.Encapsulate(render.bounds);
        Undo.RecordObject(slot.transform, "Align office interaction");
        slot.transform.position = new Vector3(bounds.center.x, slot.transform.position.y, bounds.center.z);
        foreach (var old in slot.GetComponentsInChildren<Collider>())
        {
            Undo.RecordObject(old, "Disable obsolete interaction collider");
            old.enabled = false;
        }
        var trigger = slot.GetComponent<BoxCollider>();
        if (!trigger) trigger = Undo.AddComponent<BoxCollider>(slot);
        Undo.RecordObject(trigger, "Fit visible prop interaction");
        slot.layer = LayerMask.NameToLayer("Interactable");
        trigger.enabled = true;
        trigger.isTrigger = true;
        trigger.center = slot.transform.InverseTransformPoint(bounds.center);
        trigger.size = bounds.size + Vector3.one * .08f;
        // Keep physical collision on the appliance itself, not in the old empty space.
        if (slotName != "Clock_SLOT")
        {
            var body = slot.transform.Find("InteractionBody");
            if (!body)
            {
                var obj = new GameObject("InteractionBody");
                Undo.RegisterCreatedObjectUndo(obj, "Create appliance collision");
                obj.transform.SetParent(slot.transform, false);
                body = obj.transform;
            }
            var collision = body.GetComponent<BoxCollider>();
            if (!collision) collision = Undo.AddComponent<BoxCollider>(body.gameObject);
            Undo.RecordObject(collision, "Fit appliance collision");
            var solid = renders[0].bounds;
            collision.center = body.InverseTransformPoint(solid.center);
            collision.size = solid.size;
            collision.enabled = true;
            collision.isTrigger = false;
            body.gameObject.layer = slot.layer;
        }
        EditorUtility.SetDirty(slot);
    }

    [MenuItem("KuTy/Tests/Inspect office interaction geometry")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        foreach (var item in Object.FindObjectsOfType<MonoBehaviour>(true).Where(x =>
            x is IInteractable || x.name == "PF_Office_WaterCooler_01"))
        {
            if (!(item is IInteractable)) continue;
            report.AppendLine($"{item.GetType().Name}: {Path(item.transform)} position={item.transform.position:F3}");
            Describe(item.gameObject, report);
        }
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                     .Where(x => x.name.StartsWith("PF_Office_")))
        {
            report.AppendLine($"ART: {root.name} position={root.transform.position:F3}");
            Describe(root, report);
        }
        Directory.CreateDirectory("Tools/ProjectAudit");
        File.WriteAllText("Tools/ProjectAudit/office-interaction-geometry.txt", report.ToString());
    }

    static void Describe(GameObject go, StringBuilder report)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            report.AppendLine($"  collider {Path(c.transform)} active={c.gameObject.activeInHierarchy} enabled={c.enabled} layer={c.gameObject.layer} trigger={c.isTrigger} bounds={c.bounds}");
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            report.AppendLine($"  render {Path(r.transform)} active={r.gameObject.activeInHierarchy} enabled={r.enabled} bounds={r.bounds}");
    }
    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
}
