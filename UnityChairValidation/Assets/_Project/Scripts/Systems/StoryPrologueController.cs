using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Optional sound slots are ready for the user's own typing, traffic and voice assets.
public class StoryPrologueController : MonoBehaviour
{
    [SerializeField] private GameObject overlay;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI subtitle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typing;
    [SerializeField] private AudioClip traffic;
    [SerializeField] private AudioClip bossVoice;
    [SerializeField] private AudioClip sigh;
    [SerializeField] private float introSeconds = 7f;
    public bool IsComplete { get; private set; }
    private Coroutine sequence;
    private void Start()
    {
        GameManager.Instance?.AcquireInput(this);
        if (overlay != null) overlay.SetActive(true);
        if (background != null) background.color = Color.black;
        if (subtitle != null) subtitle.text = "";
        sequence = StartCoroutine(Play());
    }
    private void Update() { if (!IsComplete && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))) Skip(); }
    private IEnumerator Play()
    {
        PlayClip(typing);
        yield return new WaitForSecondsRealtime(introSeconds * 0.18f);
        PlayClip(traffic);
        if (subtitle != null) { subtitle.color = new Color(0.9f, 0.15f, 0.15f); subtitle.text = "Báo cáo làm thế này à? Cậu có biết dùng não không? Làm lại ngay!"; }
        PlayClip(bossVoice);
        yield return new WaitForSecondsRealtime(introSeconds * 0.55f);
        if (subtitle != null) { subtitle.color = Color.white; subtitle.text = "Mình mệt quá..."; }
        PlayClip(sigh);
        yield return new WaitForSecondsRealtime(introSeconds * 0.27f);
        Complete();
    }
    private void PlayClip(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    public void Skip() { if (sequence != null) StopCoroutine(sequence); Complete(); }
    private void Complete()
    {
        if (IsComplete) return;
        IsComplete = true;
        if (overlay != null) overlay.SetActive(false);
        GameManager.Instance?.ReleaseInput(this);
        DialogueUI.Instance?.ShowText("Cánh cửa gỗ của căn nhà tuổi thơ...", 2f);
    }
    private void OnDestroy() { GameManager.Instance?.ReleaseInput(this); }
}
