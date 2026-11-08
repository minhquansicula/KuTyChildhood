using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sound design for the ending film. Everything here is synthesised at start-up (wind bed, heartbeat,
/// pentatonic bells, riser, whoosh), so it is key-agnostic and needs no extra assets. The two songs and the
/// mother's recorded line still come from the project's own audio.
/// </summary>
public sealed class EndingAudioMix : MonoBehaviour
{
    private const int Rate = 22050;
    private const string MotherLineId = "act2_mom_letter_001";

    private AudioSource wind, fx, voice;
    private AudioClip heartbeat, riser, whoosh;
    private readonly Dictionary<int, AudioClip> bells = new Dictionary<int, AudioClip>();
    private static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };
    private Coroutine windRoutine;

    public bool Ready { get; private set; }

    public void Initialize()
    {
        if (Ready) return;
        wind = NewSource("Ending wind bed", true, 0f);
        wind.clip = MakeWind();
        wind.Play();
        fx = NewSource("Ending fx", false, 1f);
        voice = NewSource("Ending mother voice", false, 1f);
        heartbeat = MakeHeartbeat();
        riser = MakeRiser();
        whoosh = MakeWhoosh();
        Ready = true;
    }

    private AudioSource NewSource(string title, bool loop, float volume)
    {
        var child = new GameObject(title);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.ignoreListenerPause = true;
        return source;
    }

    // ----- public cues -----
    public float PlayVoiceLine(string lineId, float volume = .95f)
    {
        if (string.IsNullOrEmpty(lineId) || voice == null) return 0f;
        var clip = FindVoiceClip("EndingVoices", lineId) ?? FindVoiceClip("HomeGameplayVoices", lineId);
        if (clip == null) return 0f;
        voice.Stop();
        voice.clip = clip;
        voice.volume = volume;
        voice.Play();
        return clip.length;
    }

    public float PlayMotherLine() => PlayVoiceLine(MotherLineId, .95f);

    private static AudioClip FindVoiceClip(string resourceName, string lineId)
    {
        var recordings = Resources.Load<DialogueSequence>(resourceName);
        if (recordings == null) return null;
        foreach (var line in recordings.Lines)
        {
            if (line != null && line.id == lineId && line.voiceClip != null)
                return line.voiceClip;
        }
        return null;
    }

    public void FadeWind(float volume, float seconds)
    {
        if (windRoutine != null) StopCoroutine(windRoutine);
        windRoutine = StartCoroutine(FadeSource(wind, volume, seconds));
    }

    public void Bell(int step, float volume = .5f)
    {
        if (!Ready) return;
        if (!bells.TryGetValue(step, out var clip))
        {
            float freq = Pentatonic[step % Pentatonic.Length] * (step / Pentatonic.Length >= 1 ? 2f : 1f);
            clip = MakeBell(freq);
            bells[step] = clip;
        }
        fx.PlayOneShot(clip, volume);
    }

    public void Heartbeat(float volume = .9f) { if (Ready) fx.PlayOneShot(heartbeat, volume); }
    public void Riser(float volume = .8f) { if (Ready) fx.PlayOneShot(riser, volume); }
    public void Whoosh(float volume = .55f) { if (Ready) fx.PlayOneShot(whoosh, volume); }

    public void StopVoice() { if (voice != null) voice.Stop(); }

    public void SilenceAll(float seconds)
    {
        FadeWind(0f, seconds);
        StartCoroutine(FadeSource(voice, 0f, seconds));
    }

    private static IEnumerator FadeSource(AudioSource source, float target, float seconds)
    {
        if (source == null) yield break;
        float start = source.volume, t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / Mathf.Max(.01f, seconds)));
            yield return null;
        }
        source.volume = target;
    }

    // ----- synthesis -----
    private static AudioClip Build(string title, float[] data)
    {
        var clip = AudioClip.Create(title, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        clip.hideFlags = HideFlags.DontSave;
        return clip;
    }

    private static AudioClip MakeWind()
    {
        int n = Rate * 6;
        var data = new float[n];
        var rng = new System.Random(77);
        float low1 = 0f, low2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float white = (float)(rng.NextDouble() * 2 - 1);
            // Slowly breathing cut-off gives the gusts their swell.
            float cutoff = .018f + .014f * (.5f + .5f * Mathf.Sin(t * Mathf.PI * 2f / 6f * 2f));
            low1 += (white - low1) * cutoff;
            low2 += (low1 - low2) * cutoff * 1.6f;
            float gust = .55f + .45f * Mathf.Sin(t * Mathf.PI * 2f / 6f + 1.2f);
            data[i] = low2 * 5.2f * gust;
        }
        // Cross-fade the tail into the head so the loop has no seam.
        int fade = Rate / 4;
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            data[i] = Mathf.Lerp(data[n - fade + i], data[i], k);
        }
        System.Array.Resize(ref data, n - fade);
        Normalize(data, .55f);
        return Build("EndingWind", data);
    }

    private static AudioClip MakeHeartbeat()
    {
        int n = Mathf.RoundToInt(Rate * 1.1f);
        var data = new float[n];
        AddThump(data, 0f, 1f);
        AddThump(data, .27f, .62f);
        Normalize(data, .9f);
        return Build("EndingHeartbeat", data);
    }

    private static void AddThump(float[] data, float start, float gain)
    {
        int offset = Mathf.RoundToInt(start * Rate);
        int length = Mathf.RoundToInt(Rate * .42f);
        for (int i = 0; i < length && offset + i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Exp(-t * 14f) * Mathf.Min(1f, t * 300f);
            float pitch = 62f - 18f * Mathf.Min(1f, t * 5f);
            data[offset + i] += Mathf.Sin(2f * Mathf.PI * pitch * t) * env * gain;
        }
    }

    private static AudioClip MakeRiser()
    {
        float rise = 2.4f, tail = 2.6f;
        int n = Mathf.RoundToInt(Rate * (rise + tail));
        var data = new float[n];
        var rng = new System.Random(5);
        float low = 0f, band = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            if (t < rise)
            {
                float k = t / rise;
                float cutoff = Mathf.Lerp(.015f, .5f, k * k);
                float white = (float)(rng.NextDouble() * 2 - 1);
                low += (white - low) * cutoff;
                band += (low - band) * cutoff;
                data[i] = (low - band * .6f) * Mathf.Pow(k, 2.2f) * 3.2f;
            }
            else
            {
                float tt = t - rise;
                float env = Mathf.Exp(-tt * 1.7f) * Mathf.Min(1f, tt * 80f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 46f * tt) * env * .95f +
                          Mathf.Sin(2f * Mathf.PI * 92f * tt) * env * .25f;
            }
        }
        Normalize(data, .85f);
        return Build("EndingRiser", data);
    }

    private static AudioClip MakeWhoosh()
    {
        int n = Mathf.RoundToInt(Rate * 1.5f);
        var data = new float[n];
        var rng = new System.Random(11);
        float low = 0f, band = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)n;
            float cutoff = Mathf.Lerp(.02f, .25f, Mathf.SmoothStep(0f, 1f, k));
            float white = (float)(rng.NextDouble() * 2 - 1);
            low += (white - low) * cutoff;
            band += (low - band) * cutoff;
            data[i] = (low - band) * Mathf.Sin(k * Mathf.PI) * 4f;
        }
        Normalize(data, .6f);
        return Build("EndingWhoosh", data);
    }

    private static AudioClip MakeBell(float freq)
    {
        int n = Mathf.RoundToInt(Rate * 2.2f);
        var data = new float[n];
        float[] ratio = { 1f, 2.01f, 2.76f, 5.4f };
        float[] amp = { 1f, .32f, .22f, .08f };
        float[] decay = { 2.2f, 3.4f, 4.6f, 7f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float v = 0f;
            for (int p = 0; p < ratio.Length; p++)
                v += Mathf.Sin(2f * Mathf.PI * freq * ratio[p] * t) * amp[p] * Mathf.Exp(-t * decay[p]);
            data[i] = v * Mathf.Min(1f, t * 400f);
        }
        Normalize(data, .7f);
        return Build("EndingBell" + Mathf.RoundToInt(freq), data);
    }

    private static void Normalize(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
        if (max < .0001f) return;
        float scale = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= scale;
    }
}
