using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KuTyMenuSfxSetup
{
    [MenuItem("KuTy/Audio/Install menu button sounds")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/_Project/Data/Audio/SfxCatalog.asset");
        if (!catalog || !catalog.TryGetCue(SfxId.TaskProgress, out var tick) || tick.variants.Length == 0)
            throw new InvalidOperationException("Existing short UI tick is missing.");
        var cues = catalog.Cues.Where(c => c.id != SfxId.UIHover).ToList();
        cues.Add(new SfxCue {
            id = SfxId.UIHover, category = SfxCategory.UI, variants = tick.variants,
            volume = .2f, pitchMin = 1.12f, pitchMax = 1.16f, cooldownSeconds = .055f
        });
        Undo.RecordObject(catalog, "Install menu hover sound");
        catalog.ReplaceCuesForEditor(cues);
        EditorUtility.SetDirty(catalog);
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/_Project/Generated/SoundLibrary.asset");
        if (library != null)
        {
            Undo.RecordObject(library, "Bind legacy button audio");
            library.sfxButtonHover = tick.variants[0];
            if (catalog.TryGetCue(SfxId.UIConfirm, out var click) && click.variants.Length > 0)
                library.sfxButtonClick = click.variants[0];
            EditorUtility.SetDirty(library);
        }
        AssetDatabase.SaveAssets();
        var issues = catalog.CollectValidationIssues(true);
        if (issues.Count != 0) throw new InvalidOperationException(string.Join("\n", issues));
        Debug.Log("[Menu SFX] Hover, confirm and back ready; existing catalog cues preserved.");
    }
}
