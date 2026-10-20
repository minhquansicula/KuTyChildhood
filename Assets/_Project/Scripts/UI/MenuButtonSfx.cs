using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One hover cue per entry; click audio precedes the button's existing actions.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class MenuButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    private Button button;
    private Button.ButtonClickedEvent originalClick;
    private Button.ButtonClickedEvent wrappedClick;

    public static void Attach(Button target)
    {
        if (target != null && target.GetComponent<MenuButtonSfx>() == null)
            target.gameObject.AddComponent<MenuButtonSfx>();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        originalClick = button.onClick;
        wrappedClick = new Button.ButtonClickedEvent();
        wrappedClick.AddListener(Activate);
        button.onClick = wrappedClick;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.IsActive() && button.IsInteractable())
            AudioManager.Instance?.PlaySFX(SfxId.UIHover);
    }

    private void Activate()
    {
        if (!isActiveAndEnabled || !button.IsActive() || !button.IsInteractable()) return;
        bool back = button.name == "Btn_Back" || button.name == "CloseMusicCredits";
        AudioManager.Instance?.PlaySFX(back ? SfxId.UIBack : SfxId.UIConfirm);
        if (button.name == "Btn_Quit" && AudioManager.Instance != null)
        {
            // Let the click become audible before the process/editor Play Mode exits.
            button.interactable = false;
            StartCoroutine(QuitAfterClick());
        }
        else originalClick?.Invoke();
    }

    private IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(.15f);
        originalClick?.Invoke();
    }

    private void OnDestroy()
    {
        if (button != null && button.onClick == wrappedClick) button.onClick = originalClick;
    }
}
