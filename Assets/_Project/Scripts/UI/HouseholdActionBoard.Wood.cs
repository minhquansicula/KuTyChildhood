using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HouseholdActionBoard
{
    private RectTransform woodBundlePreview;
    private float woodFinishTimer;

    private void BuildWoodPile()
    {
        mode = ActionMode.Placement;
        placementHasArt = true;
        AddArt("WoodMat", root, new Vector2(0f, 12f), new Vector2(505f, 183f), LoadArt("Wood_Mat"))
            .SetSiblingIndex(2);
        Label("WoodHeading", root, Vector2.zero, new Vector2(520f, 35f),
            "GOM CỦI CHO BẾP", 21f, Color.white);
        Label("WoodGuide", root, new Vector2(0f, 93f), new Vector2(490f, 26f),
            "Kéo từng khúc củi lên nia · từ dưới lên trên", 14f, new Color(.22f, .16f, .10f));

        for (int i = 0; i < 3; i++)
        {
            float y = 52f - i * 37f;
            targets[i] = Box("WoodSlot" + i, root, new Vector2(0f, y),
                new Vector2(310f, 34f), new Color(.21f, .31f, .25f, .22f));
            var ghost = AddArt("WoodGhost", targets[i], Vector2.zero,
                new Vector2(225f, 42f), LoadArt(i == 1 ? "Wood_Log_Dark" : "Firewood_Log"));
            ghost.GetComponent<Image>().color = new Color(1f, 1f, 1f, .18f);
            ghost.GetComponent<Image>().raycastTarget = false;
            itemHome[i] = new Vector2((i - 1) * 170f, -82f);
            items[i] = Box("WoodLog" + i, root, itemHome[i],
                new Vector2(154f, 53f), Color.clear);
            DrawProp(items[i], i);
            RectTransform icon = items[i].Find("PropIcon") as RectTransform;
            icon.anchoredPosition = new Vector2(0f, 8f);
            icon.localScale = Vector3.one * .72f;
            icon.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 5f);
            Label("WoodNumber", items[i], new Vector2(0f, -20f), new Vector2(90f, 20f),
                (i + 1).ToString(), 12f, new Color(.20f, .14f, .09f));
        }
        Report(0f, "0/3 khúc củi đã xếp");
    }

    private void BeginWoodTie()
    {
        dragging = false;
        actionArea = AddArt("WoodRope", root, new Vector2(-160f, 6f),
            new Vector2(215f, 46f), LoadArt("Wood_Rope"));
        gestureHome = actionArea.anchoredPosition;
        woodBundlePreview = AddArt("TiedWoodBundle", root, new Vector2(0f, 13f),
            new Vector2(370f, 157f), LoadArt("Wood_Bundle"));
        woodBundlePreview.gameObject.SetActive(false);
        root.Find("WoodGuide").GetComponent<TextMeshProUGUI>().text =
            "Nắm đầu dây bên trái và kéo sang phải để buộc bó củi";
        Report(.85f, "Kéo dây buộc sang phải");
        hint?.Invoke("Kéo dây buộc qua ba khúc củi.");
    }

    private void TickWoodTie()
    {
        if (Input.GetMouseButtonDown(0) && Inside(actionArea) && Pointer(out Vector2 start))
        {
            dragging = true;
            pointerStart = start;
        }
        if (dragging && Input.GetMouseButton(0) && Pointer(out Vector2 point))
        {
            float travel = Mathf.Clamp(point.x - pointerStart.x, 0f, 270f);
            actionArea.anchoredPosition = gestureHome + Vector2.right * travel;
            Report(.85f + .15f * travel / 250f, "Đang buộc bó củi");
            if (travel >= 250f)
            {
                dragging = false;
                stage = 2;
                woodFinishTimer = .65f;
                for (int i = 0; i < 3; i++)
                {
                    targets[i].gameObject.SetActive(false);
                    items[i].gameObject.SetActive(false);
                }
                actionArea.gameObject.SetActive(false);
                woodBundlePreview.gameObject.SetActive(true);
                woodBundlePreview.localScale = Vector3.one * .9f;
                pulse?.Invoke();
                Report(1f, "Bó củi đã buộc chắc");
                hint?.Invoke("Xong rồi! Bó củi đã sẵn sàng cho bếp.");
                return;
            }
        }
        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            mistakes++;
            hint?.Invoke("Kéo dây thêm một chút để buộc chặt bó củi.");
        }
        if (!dragging)
            actionArea.anchoredPosition = Vector2.MoveTowards(actionArea.anchoredPosition,
                gestureHome, Time.deltaTime * 650f);
    }
}
