using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class KuTyMainMenuIntegration
{
    public const string PrefabPath = "Assets/_Project/UI/MainMenu_Canvas.prefab";
    public const string ButtonArtworkPath = "Assets/_Project/UI/button.png";

    [MenuItem("KuTy/Main menu/Use illustrated wooden buttons")]
    public static void UseIllustratedButtons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before updating the menu prefab.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ApplyButtonArtwork(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("[MainMenu] Applied UI/button.png to all four main buttons; logo, layout and events preserved.");
    }

    private static void ApplyButtonArtwork(GameObject root)
    {
        var artwork = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonArtworkPath);
        if (!artwork) throw new InvalidOperationException("Illustrated wooden button sprite missing.");
        foreach (string name in new[] { "Btn_NewGame", "Btn_LoadGame", "Btn_Settings", "Btn_Quit" })
        {
            var target = root.transform.Find("ButtonGroup/" + name);
            if (!target) throw new InvalidOperationException("Main menu button missing: " + name);
            var image = target.GetComponent<Image>();
            var button = target.GetComponent<Button>();
            image.sprite = artwork;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            button.transition = Selectable.Transition.ColorTint;
            button.spriteState = new SpriteState();
        }
    }

    [MenuItem("KuTy/Main menu/Integrate checkpoint buttons")]
    public static void Integrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before updating the menu prefab.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var controller = root.GetComponent<MainMenuUI>();
            if (controller == null) throw new InvalidOperationException("Menu controller missing.");
            var buttons = root.GetComponentsInChildren<Button>(true);
            var newGame = buttons.Single(b => b.name == "Btn_NewGame");
            var resume = buttons.Single(b => b.name == "Btn_LoadGame");
            var quit = buttons.Single(b => b.name == "Btn_Quit");
            Bind(newGame, controller.OnPlayButtonClicked);
            Bind(resume, controller.OnContinueButtonClicked);
            Bind(quit, controller.OnQuitButtonClicked);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("continueButton").objectReferenceValue = resume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // The incoming prefab also attached a menu controller to Quit. All actions use one owner.
            foreach (var duplicate in root.GetComponentsInChildren<MainMenuUI>(true))
                if (duplicate != controller) UnityEngine.Object.DestroyImmediate(duplicate);
            foreach (var back in buttons.Where(b => b.name == "Btn_Back"))
            {
                var label = back.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Quay lại";
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Generated/Vietnamese.asset");
            if (font == null) throw new InvalidOperationException("Vietnamese font missing.");
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }
            ApplyButtonArtwork(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("[MainMenu] Integrated New Game, checkpoint Continue, Quit and back labels; one controller.");
    }

    private static void Bind(Button button, UnityAction action)
    {
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
