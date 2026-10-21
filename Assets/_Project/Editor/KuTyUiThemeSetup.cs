using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class KuTyUiThemeSetup
{
    [MenuItem("KuTy/UI/Create shared menu theme")]
    public static void CreateTheme()
    {
        const string path = "Assets/_Project/Resources/GameUiTheme.asset";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KuTyMainMenuIntegration.PrefabPath);
        var source = prefab.transform.Find("ButtonGroup/Btn_NewGame");
        if (!source)
            foreach (var button in prefab.GetComponentsInChildren<Button>(true))
                if (button.name == "Btn_NewGame") { source = button.transform; break; }
        if (!source) throw new InvalidOperationException("Main menu button not found.");
        var theme = AssetDatabase.LoadAssetAtPath<GameUiTheme>(path);
        if (!theme)
        {
            theme = ScriptableObject.CreateInstance<GameUiTheme>();
            AssetDatabase.CreateAsset(theme, path);
        }
        theme.menuFrame = source.GetComponent<Image>().sprite;
        theme.font = source.GetComponentInChildren<TMP_Text>(true).font;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        Debug.Log("[UI Theme] Shared theme references the existing Main Menu frame and font.");
    }
}
