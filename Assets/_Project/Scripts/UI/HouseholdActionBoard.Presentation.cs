using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HouseholdActionBoard
{
    // Shared presentation only; chore state and rewards remain in their existing owners.
    private void ApplyTaskPolish()
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || image.sprite == circleSprite || image.name == "WorkCloth") continue;
            if (image.GetComponent<Shadow>() == null)
            {
                var shadow = image.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(.08f, .06f, .04f, .24f);
                shadow.effectDistance = new Vector2(1.5f, -2.5f);
            }
        }
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Min(11f, label.fontSize);
            label.fontSizeMax = label.fontSize;
            if (label.transform.parent == root && label != status &&
                !label.name.EndsWith("Heading") && label.name != "PlacementGuide")
                label.color = new Color(.20f, .14f, .09f);
        }
        if (mode == ActionMode.Heat)
            Label("HeatTarget", root, new Vector2(0f, -47f), new Vector2(300f, 25f),
                "NHỎ     ·     VỪA     ·     LỚN", 13f, new Color(.20f, .14f, .09f));
    }
}
