using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Read-only audit. Does not open/save scenes or touch player checkpoints.</summary>
public static class Scene2MotherInteractionAudit
{
    [MenuItem("KuTy/Tests/Audit mother and household targets")]
    public static void Audit()
    {
        var report = new StringBuilder("Scene 2 interaction audit (read-only)\n");
        var targets = Object.FindObjectsOfType<HouseholdChoreInteractable>(true)
            .Where(t => t.gameObject.scene.IsValid()).ToArray();
        var order = HouseholdChoreFlow.Order;
        report.AppendLine($"Required: {order.Length}; optional: SortTools, GrindCorn; total pieces: {HouseholdChoreFlow.TotalPieces}");
        int missing = 0;
        foreach (HouseholdChore chore in order.Concat(new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn }))
        {
            var matches = targets.Where(t => t.Chore == chore).ToArray();
            int active = matches.Count(t => t.isActiveAndEnabled);
            report.AppendLine($"{chore}: authored={matches.Length}, active={active}");
            foreach (var target in matches)
            {
                var colliders = target.GetComponentsInChildren<Collider>(true);
                report.AppendLine($"  {Path(target.transform)} | enabled={target.isActiveAndEnabled} | colliders={colliders.Length} | world={target.transform.position}");
            }
            if (matches.Length == 0) missing++;
        }
        var flow = Object.FindObjectOfType<HouseholdChoreFlow>(true);
        report.AppendLine("Flow: " + (flow ? Path(flow.transform) : "MISSING"));
        if (flow)
        {
            var sequence = flow.GetComponent<Scene2RadioKitchenSequence>();
            report.AppendLine("Mother standing point: " + (sequence && sequence.kitchenSpot ? Path(sequence.kitchenSpot) : "MISSING"));
            report.AppendLine("Physical radio: " + (sequence && sequence.Handles(HouseholdChore.TuneRadio)));
            report.AppendLine("Physical wood: " + (sequence && sequence.Handles(HouseholdChore.CarryWood)));
            // SetTable and WashRice can be bound by flow.Awake; absence in Edit Mode is not proof of a broken runtime target.
            report.AppendLine("Dining surface candidates: " + Object.FindObjectsOfType<MeshCollider>(true).Count(c => c.sharedMesh && c.sharedMesh.name == "tripo_part_3 Instance"));
        }
        report.AppendLine($"Missing authored task types: {missing}. Runtime binding and playthrough still require verification.");
        Debug.Log(report.ToString());
    }

    private static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
}
