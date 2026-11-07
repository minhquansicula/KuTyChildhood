using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Unity 2022.3-only authoring adapter. Runtime uses public AudioMixer APIs only.</summary>
public static class KuTyAudioMixSetup
{
    private const string Path = "Assets/_Project/Resources/KuTyMix.mixer";
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("KuTy/Audio/Configure category mixer")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Path);
        if (!mixer) throw new InvalidOperationException("Create KuTyMix.mixer before configuring it.");
        // Signatures verified against UnityCsReference 2022.3 AudioMixerController.cs.
        var type = mixer.GetType();
        var create = type.GetMethod("CreateNewGroup", Flags);
        var parent = type.GetMethod("AddChildToParent", Flags);
        var master = type.GetProperty("masterGroup", Flags)?.GetValue(mixer);
        var exposed = type.GetProperty("exposedParameters", Flags);
        if (create == null || parent == null || master == null || exposed == null)
            throw new NotSupportedException("Audio mixer authoring API differs from Unity 2022.3; no changes made.");
        var masterGroup = (AudioMixerGroup)master;
        Undo.RecordObject(mixer, "Configure KuTy category mixer");
        foreach (KuTyAudioBus bus in Enum.GetValues(typeof(KuTyAudioBus)))
        {
            string name = bus.ToString();
            var group = mixer.FindMatchingGroups(name).SingleOrDefault(g => g.name == name);
            if (!group)
            {
                group = (AudioMixerGroup)create.Invoke(mixer, new object[] { name, true });
                parent.Invoke(mixer, new object[] { group, masterGroup });
            }
            object guid = group.GetType().GetMethod("GetGUIDForVolume", Flags).Invoke(group, null);
            bool contains = (bool)type.GetMethod("ContainsExposedParameter", Flags).Invoke(mixer, new[] { guid });
            if (!contains)
            {
                var pathType = type.Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", true);
                var parameterPath = Activator.CreateInstance(pathType, new[] { (object)group, guid });
                type.GetMethod("AddExposedParameter", Flags).Invoke(mixer, new[] { parameterPath });
            }
            var parameters = (Array)exposed.GetValue(mixer);
            for (int i = 0; i < parameters.Length; i++)
            {
                object parameter = parameters.GetValue(i);
                var parameterType = parameter.GetType();
                if (!Equals(parameterType.GetField("guid").GetValue(parameter), guid)) continue;
                parameterType.GetField("name").SetValue(parameter, name + "Volume");
                parameters.SetValue(parameter, i);
            }
            exposed.SetValue(mixer, parameters);
            EditorUtility.SetDirty(group);
        }
        // A mixer created through CreateDefaultAsset has no editor views yet.
        var viewsProperty = type.GetProperty("views", Flags);
        var views = (Array)viewsProperty.GetValue(mixer);
        if (views.Length == 0)
        {
            var viewType = views.GetType().GetElementType();
            object view = Activator.CreateInstance(viewType);
            viewType.GetField("name").SetValue(view, "All categories");
            var guidsField = viewType.GetField("guids");
            var allGroups = mixer.FindMatchingGroups("");
            var guids = Array.CreateInstance(guidsField.FieldType.GetElementType(), allGroups.Length);
            for (int i = 0; i < allGroups.Length; i++)
                guids.SetValue(allGroups[i].GetType().GetProperty("groupID", Flags).GetValue(allGroups[i]), i);
            guidsField.SetValue(view, guids);
            var initialViews = Array.CreateInstance(viewType, 1);
            initialViews.SetValue(view, 0);
            viewsProperty.SetValue(mixer, initialViews);
        }
        EditorUtility.SetDirty(masterGroup);
        EditorUtility.SetDirty(mixer);
        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("KuTy/Audio/Validate category mixer")]
    public static void Validate()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Path);
        if (!mixer) throw new InvalidOperationException("Missing KuTy mixer.");
        var parameters = (Array)mixer.GetType().GetProperty("exposedParameters", Flags).GetValue(mixer);
        foreach (KuTyAudioBus bus in Enum.GetValues(typeof(KuTyAudioBus)))
        {
            if (mixer.FindMatchingGroups(bus.ToString()).Count(g => g.name == bus.ToString()) != 1)
                throw new InvalidOperationException("Missing/duplicate group: " + bus);
            if (!parameters.Cast<object>().Any(p => (string)p.GetType().GetField("name").GetValue(p) == bus + "Volume"))
                throw new InvalidOperationException("Missing exposed gain: " + bus);
        }
        System.IO.Directory.CreateDirectory("Tools/Music");
        System.IO.File.WriteAllText("Tools/Music/mixer-validation.txt", "PASS Music / SFX / Voice / Ambience; four exposed user gains.\n");
        Debug.Log("PASS KuTy mixer: Music / SFX / Voice / Ambience, four exposed user gains.");
    }
}
