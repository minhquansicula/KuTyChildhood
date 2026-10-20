using UnityEngine;
using UnityEngine.UI;

// Shared gesture feedback for the existing chore board; completion belongs to HouseholdChoreFlow.
public sealed partial class HouseholdActionBoard
{
    private readonly float[] stainRemaining = new float[3];
    private int mistakes;
    private bool breathUsed;
    private readonly RectTransform[] steam = new RectTransform[3];
    private float elapsed;
    private Vector2 gestureHome;
    private RectTransform tutorialHand;
    private RectTransform rinseZone;
    private AudioSource radioNoise;
    private AudioSource radioTune;
    private AudioClip noiseClip;
    private AudioClip tuneClip;

    private void BuildHandGuide()
    {
        tutorialHand = Box("GestureHand", root, Vector2.zero, new Vector2(18f, 23f), new Color(1f, .85f, .61f));
        Shape(tutorialHand, "IndexFinger", -5f, 15f, 6f, 24f, new Color(1f, .85f, .61f));
        Shape(tutorialHand, "Thumb", 11f, 1f, 8f, 16f, new Color(1f, .85f, .61f), -35f);
        tutorialHand.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
    }

    private void TickHandGuide()
    {
        if (tutorialHand == null) return;
        bool show = !dragging && (elapsed < 5f || idleSeconds >= 7f || mistakes >= 2);
        tutorialHand.gameObject.SetActive(show);
        if (!show) return;
        Vector2 from = Vector2.zero;
        Vector2 to = Vector2.right * 65f;
        if (mode == ActionMode.Placement)
        {
            if (stage == 1) { from = gestureHome; to = from + (chore == HouseholdChore.ArrangeKeepsakes ? Vector2.down : Vector2.right) * 85f; }
            else
                for (int i = 0; i < 3; i++)
                    if (!placed[i]) { from = items[i].anchoredPosition; to = targets[i].anchoredPosition; break; }
        }
        else if (mode == ActionMode.Dial || (mode == ActionMode.Rotary && stage == 0))
        {
            float angle = -Time.unscaledTime * 2f;
            from = actionArea.anchoredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 48f;
            to = from;
        }
        else if (mode == ActionMode.Scrub)
        {
            from = dish.anchoredPosition;
            if (stage == 0)
            {
                int stain = 0;
                while (stain < 2 && stainRemaining[stain] <= .01f) stain++;
                from = root.InverseTransformPoint(dirt[stain].transform.position);
                to = from + Vector2.right * 14f;
            }
            else to = stage == 1 ? rinseZone.anchoredPosition : rack.anchoredPosition;
        }
        else if (mode == ActionMode.Fetch || mode == ActionMode.Measure || mode == ActionMode.Fire)
        {
            from = token != null ? token.anchoredPosition : Vector2.zero;
            to = targets[0].anchoredPosition;
            if (mode == ActionMode.Measure && stage == 1) to = targets[1].anchoredPosition;
            if (mode == ActionMode.Measure && stage == 2) to = from + Vector2.down * 65f;
            if (mode == ActionMode.Fetch && stage == 2) to = targets[1].anchoredPosition;
            if (mode == ActionMode.Fire && stage == 2) from = to = targets[0].anchoredPosition;
        }
        else if (mode == ActionMode.Heat)
        { from = heatHandle.anchoredPosition; to = Vector2.zero; }
        else if (actionArea != null)
        {
            from = actionArea.anchoredPosition;
            to = from + (mode == ActionMode.Letter && stage == 1 ? Vector2.up :
                mode == ActionMode.Letter && stage == 2 ? Vector2.left : Vector2.right) * 75f;
        }
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Repeat(Time.unscaledTime * .55f, 1f));
        tutorialHand.anchoredPosition = Vector2.Lerp(from, to, t) + new Vector2(4f, -15f);
        tutorialHand.SetAsLastSibling();
    }

    private void UpdateRadioAudio(float clarity)
    {
        if (radioNoise == null)
        {
            // Quiet procedural placeholder station; both layers respond continuously to tuning.
            const int sampleRate = 22050;
            var noise = new float[sampleRate];
            var tune = new float[sampleRate * 4];
            var random = new System.Random(213);
            for (int i = 0; i < noise.Length; i++) noise[i] = ((float)random.NextDouble() * 2f - 1f) * .12f;
            float[] notes = { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 392f, 523.25f };
            for (int i = 0; i < tune.Length; i++)
            {
                float time = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.Repeat(time, .5f) * 2f * Mathf.PI);
                tune[i] = Mathf.Sin(time * notes[Mathf.Min(7, (int)(time * 2f))] * 2f * Mathf.PI) * envelope * .10f;
            }
            noiseClip = AudioClip.Create("RadioStatic", noise.Length, 1, sampleRate, false);
            noiseClip.SetData(noise, 0);
            tuneClip = AudioClip.Create("RadioPlaceholderMelody", tune.Length, 1, sampleRate, false);
            tuneClip.SetData(tune, 0);
            radioNoise = root.gameObject.AddComponent<AudioSource>();
            radioTune = root.gameObject.AddComponent<AudioSource>();
            radioNoise.clip = noiseClip;
            radioTune.clip = tuneClip;
            radioNoise.loop = radioTune.loop = true;
            radioNoise.spatialBlend = radioTune.spatialBlend = 0f;
            radioNoise.Play();
            radioTune.Play();
        }
        radioNoise.volume = (1f - clarity) * .45f;
        radioTune.volume = clarity * .6f;
    }

    private void StopRadioAudio()
    {
        if (radioNoise != null) { radioNoise.Stop(); Object.Destroy(radioNoise); }
        if (radioTune != null) { radioTune.Stop(); Object.Destroy(radioTune); }
        if (noiseClip != null) Object.Destroy(noiseClip);
        if (tuneClip != null) Object.Destroy(tuneClip);
        radioNoise = radioTune = null;
        noiseClip = tuneClip = null;
    }
}
