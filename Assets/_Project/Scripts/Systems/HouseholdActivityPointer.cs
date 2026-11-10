using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>UI hit testing shared by household activities; never treats world raycasters as UI.</summary>
public sealed class HouseholdActivityPointer
{
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    public bool OverUi(Vector2 screen)
    {
        if (!EventSystem.current) return false;
        hits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        foreach (var hit in hits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }
}
