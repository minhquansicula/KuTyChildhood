using System;
using System.Collections;
using UnityEngine;

public sealed class Scene2FoodCarry : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float reachDistance = 1.45f;
    [SerializeField] private Vector3 holdOffset = new Vector3(.22f, -.24f, .69f);
    private Scene2FoodInteractable held;
    private Transform view;
    private bool moving;

    public bool IsHolding => held != null;
    public event Action<Scene2FoodInteractable, Transform> FoodPlaced;

    private void Awake() => view = GetComponent<FirstPersonController>().PlayerCamera;

    internal bool CanUse(Collider target)
    {
        if (moving || view == null || target == null || !target.enabled) return false;
        var ray = new Ray(view.position, view.forward);
        return target.Raycast(ray, out _, reachDistance);
    }

    internal bool CanPickUp => held == null;
    internal bool IsHoldingItem(Scene2FoodInteractable food) => held == food;

    internal void PickUp(Scene2FoodInteractable food)
    {
        if (!CanPickUp || !CanUse(food.Target)) return;
        held = food;
        food.Target.enabled = false;
        food.Slot.Target.enabled = true;
        var item = food.transform;
        var slot = food.Slot.transform;
        Quaternion upright = Quaternion.Inverse(Quaternion.Euler(0f, view.eulerAngles.y, 0f)) * slot.rotation;
        item.SetParent(view, true);
        StartCoroutine(Move(food, holdOffset, upright, false));
    }

    internal void Place(Scene2FoodInteractable food)
    {
        if (!IsHoldingItem(food) || !CanUse(food.Slot.Target)) return;
        food.Slot.Target.enabled = false;
        food.transform.SetParent(food.Slot.transform, true);
        StartCoroutine(Move(food, Vector3.zero, Quaternion.identity, true));
    }

    private IEnumerator Move(Scene2FoodInteractable food, Vector3 position, Quaternion rotation, bool placing)
    {
        moving = true;
        Transform item = food.transform;
        Vector3 start = item.localPosition;
        Quaternion startRotation = item.localRotation;
        const float duration = .24f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            item.SetLocalPositionAndRotation(Vector3.Lerp(start, position, t), Quaternion.Slerp(startRotation, rotation, t));
            yield return null;
        }
        item.SetLocalPositionAndRotation(position, rotation);
        moving = false;
        if (!placing) yield break;

        item.SetParent(food.Slot.transform.parent, true);
        item.localScale = food.Slot.transform.localScale;
        item.SetSiblingIndex(food.OriginalSiblingIndex);
        held = null;
        food.Target.enabled = true;
        FoodPlaced?.Invoke(food, food.Slot.transform);
    }
}
