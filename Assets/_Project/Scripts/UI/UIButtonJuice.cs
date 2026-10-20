using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIButtonJuice — Hiệu ứng phản hồi mượt mà cho nút bấm giao diện (hover phóng to, click nhún, đổi viền sáng).
/// Hoạt động cả khi Time.timeScale = 0 (menu Pause).
/// </summary>
[DisallowMultipleComponent]
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private bool playClickSfx = true;

    [Header("Border Highlight")]
    [SerializeField] private Image targetBorder;
    [SerializeField] private Color normalBorderColor = new Color(0.85f, 0.68f, 0.38f, 0.5f);
    [SerializeField] private Color hoverBorderColor = new Color(1f, 0.90f, 0.45f, 1f);

    private Vector3 originalScale;
    private Coroutine scaleRoutine;
    private Selectable selectable;

    private void Awake()
    {
        originalScale = transform.localScale;
        selectable = GetComponent<Selectable>();
        if (targetBorder != null)
        {
            targetBorder.color = normalBorderColor;
        }
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        if (targetBorder != null)
        {
            targetBorder.color = normalBorderColor;
        }
    }

    public void SetBorder(Image border, Color normal, Color hover)
    {
        targetBorder = border;
        normalBorderColor = normal;
        hoverBorderColor = hover;
        if (targetBorder != null)
        {
            targetBorder.color = normalBorderColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        AnimateScale(originalScale * hoverScale);
        if (targetBorder != null)
        {
            targetBorder.color = hoverBorderColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateScale(originalScale);
        if (targetBorder != null)
        {
            targetBorder.color = normalBorderColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        AnimateScale(originalScale * clickScale);
        if (playClickSfx) AudioManager.Instance?.PlaySFX(SfxId.UIConfirm);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        AnimateScale(originalScale * hoverScale);
    }

    private void AnimateScale(Vector3 target)
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.localScale = target;
            return;
        }
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleTo(target));
    }

    private IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth ease out
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(start, target, ease);
            yield return null;
        }
        transform.localScale = target;
    }
}
