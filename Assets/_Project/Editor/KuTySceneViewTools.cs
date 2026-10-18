using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class KuTySceneViewTools
{
    [MenuItem("KuTy/View/Hide Scene Grid")]
    public static void HideSceneGrid()
    {
        var hiddenCount = 0;

        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            if (TrySetGridVisibility(sceneView, false))
            {
                hiddenCount++;
                sceneView.Repaint();
            }
        }

        Selection.objects = Array.Empty<UnityEngine.Object>();

        if (hiddenCount == 0)
            throw new InvalidOperationException("Unity Scene View grid API could not be found.");

        Debug.Log($"SCENE_GRID_HIDDEN: disabled in {hiddenCount} Scene View window(s).");
    }

    private static bool TrySetGridVisibility(SceneView sceneView, bool visible)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var sceneViewType = typeof(SceneView);
        var gridMember = (MemberInfo)sceneViewType.GetProperty("grid", flags)
                         ?? sceneViewType.GetField("m_Grid", flags);

        object grid = null;
        if (gridMember is PropertyInfo gridProperty)
            grid = gridProperty.GetValue(sceneView);
        else if (gridMember is FieldInfo gridField)
            grid = gridField.GetValue(sceneView);

        if (grid == null)
            return false;

        var gridType = grid.GetType();
        var visibilityProperty = gridType.GetProperty("showGrid", flags);
        if (visibilityProperty != null && visibilityProperty.CanWrite)
        {
            visibilityProperty.SetValue(grid, visible);
            return true;
        }

        var visibilityField = gridType.GetField("showGrid", flags)
                              ?? gridType.GetField("m_ShowGrid", flags);
        if (visibilityField == null)
            return false;

        visibilityField.SetValue(grid, visible);
        return true;
    }
}
