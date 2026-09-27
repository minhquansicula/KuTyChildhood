using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-local layered audio and fluorescent lighting for Act 1.
/// Real clips can be dropped into the Inspector; generated clips keep the scene playable meanwhile.
/// </summary>
public class OfficeAtmosphere : MonoBehaviour
{
    [Header("3D audio sources")]
    [SerializeField] private AudioSource streetSource;
    [SerializeField] private AudioSource fluorescentSource;
    [SerializeField] private AudioSource keyboardSource;
    [SerializeField] private AudioSource bossSource;
    [SerializeField] private AudioSource propSource;
    [SerializeField] private AudioSource memorySource;

    [Header("Replace with production audio when ready")]
    [SerializeField] private AudioClip streetTraffic;
    [SerializeField] private AudioClip fluorescentHum;
    [SerializeField] private AudioClip keyPress;
    [SerializeField] private AudioClip bossFirstLine;
    [SerializeField] private AudioClip bossSecondLine;
    [SerializeField] private AudioClip playerReply;
    [SerializeField] private AudioClip deskSlam;
    [SerializeField] private AudioClip emailNotification;
    [SerializeField] private AudioClip printerSound;
    [SerializeField] private AudioClip waterPour;
    [SerializeField] private AudioClip paperRustle;
    [SerializeField] private AudioClip cupSetDown;
    [SerializeField] private AudioClip tiredSigh;
    [SerializeField] private AudioClip childhoodBridge;
    [SerializeField, Range(0f, 1f)] private float workTypingVolume = .18f;
    [SerializeField, Min(.05f)] private float bossDeskSlamInterval = .45f;
    [SerializeField, Min(0f)] private float bossVoiceAfterSlamDelay = .35f;

    [Header("Fluorescent lights")]
    [SerializeField] private Light[] ceilingLights;
    [SerializeField, Min(0.1f)] private float normalLightIntensity = 0.7f;
    [SerializeField, Min(0f)] private float flickerLightIntensity = 0.16f;
    [SerializeField] private bool deferAmbienceToOpening;

    private readonly List<AudioClip> generatedClips = new List<AudioClip>();
    private readonly List<AudioLowPassFilter> officeFilters = new List<AudioLowPassFilter>();
    private AudioClip generatedKey;
    private AudioClip generatedNotification;
    private AudioClip generatedPrinter;
    private AudioClip generatedWater;
    private AudioClip generatedRustle;
    private AudioClip generatedCup;
    private AudioClip generatedSigh;
    private AudioClip generatedChildhood;
    private float flickerTimer;
    private Coroutine bossSequenceRoutine;

    private void Start()
    {
        ConfigureLoop(streetSource, streetTraffic != null ? streetTraffic : GenerateTraffic(), deferAmbienceToOpening ? 0f : 0.68f, 0.82f, 5f, 26f);
        ConfigureLoop(fluorescentSource, fluorescentHum != null ? fluorescentHum : GenerateHum(), deferAmbienceToOpening ? 0f : 0.24f, 0.62f, 3f, 14f);
        ConfigureOneShotSource(keyboardSource, 0.34f, 0.8f, 2f, 8f);
        ConfigureOneShotSource(propSource, 0.72f, 0.68f, 2f, 12f);
        ConfigureOneShotSource(memorySource, 0.48f, 0f, 1f, 500f);
        ConfigureOneShotSource(bossSource, 1f, 1f, 2.5f, 12f);

        AddOfficeFilter(streetSource, 22000f);
        AddOfficeFilter(fluorescentSource, 22000f);
        AddOfficeFilter(keyboardSource, 22000f);
        AddOfficeFilter(propSource, 22000f);
        if (bossSource != null)
        {
            AudioLowPassFilter glassFilter = bossSource.GetComponent<AudioLowPassFilter>();
            if (glassFilter == null) glassFilter = bossSource.gameObject.AddComponent<AudioLowPassFilter>();
            glassFilter.cutoffFrequency = 2600f;
            glassFilter.lowpassResonanceQ = 1.2f;
        }
    }

    private void Update()
    {
        if (ceilingLights == null || ceilingLights.Length == 0) return;
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f) flickerTimer = Random.Range(2.4f, 6.8f);
        float intensity = flickerTimer < 0.09f ? flickerLightIntensity : normalLightIntensity;
        for (int i = 0; i < ceilingLights.Length; i++)
            if (ceilingLights[i] != null) ceilingLights[i].intensity = intensity;
    }

    public void StartWorkTyping()
    {
        if (keyboardSource == null) return;
        AudioClip clip = keyPress != null ? keyPress : generatedKey ?? (generatedKey = GenerateKey());
        if (clip == null) return;

        keyboardSource.Stop();
        keyboardSource.clip = clip;
        keyboardSource.loop = true;
        keyboardSource.playOnAwake = false;
        keyboardSource.volume = workTypingVolume;
        keyboardSource.Play();
    }

    public void StopWorkTyping()
    {
        if (keyboardSource == null) return;
        keyboardSource.Stop();
        keyboardSource.loop = false;
    }

    public void PlayTyping()
    {
        if (keyboardSource != null && keyboardSource.loop && keyboardSource.isPlaying) return;
        Play(keyboardSource, keyPress != null ? keyPress : generatedKey ?? (generatedKey = GenerateKey()));
    }

    public float PlayBoss(bool reminder)
    {
        AudioClip clip = reminder ? bossSecondLine : bossFirstLine;
        if (bossSource == null || clip == null) return 0f;

        if (bossSequenceRoutine != null)
        {
            StopCoroutine(bossSequenceRoutine);
            bossSequenceRoutine = null;
        }
        bossSource.Stop();

        if (reminder || deskSlam == null)
        {
            bossSource.PlayOneShot(clip);
            return clip.length;
        }

        bossSequenceRoutine = StartCoroutine(PlayBossAfterTwoDeskSlams(clip));
        return bossDeskSlamInterval + bossVoiceAfterSlamDelay + clip.length;
    }

    private IEnumerator PlayBossAfterTwoDeskSlams(AudioClip clip)
    {
        PlayDeskSlam();
        yield return new WaitForSecondsRealtime(bossDeskSlamInterval);
        PlayDeskSlam();
        yield return new WaitForSecondsRealtime(bossVoiceAfterSlamDelay);
        if (bossSource != null && clip != null) bossSource.PlayOneShot(clip);
        bossSequenceRoutine = null;
    }

    public float PlayPlayerReply()
    {
        if (playerReply == null) return 0f;
        Play(propSource, playerReply);
        return playerReply.length;
    }

    public void PlayEmailNotification() => Play(propSource,
        emailNotification != null ? emailNotification : generatedNotification ?? (generatedNotification = GenerateNotification()));
    public void PlayPrinter() => Play(propSource, printerSound != null ? printerSound : generatedPrinter ?? (generatedPrinter = GeneratePrinter()));
    public void PlayWater() => Play(propSource, waterPour != null ? waterPour : generatedWater ?? (generatedWater = GenerateWater()));
    public void PlayPaper() => Play(propSource, paperRustle != null ? paperRustle : generatedRustle ?? (generatedRustle = GenerateRustle()));
    public void PlayCup() => Play(propSource, cupSetDown != null ? cupSetDown : generatedCup ?? (generatedCup = GenerateCup()));
    public void PlaySigh() => Play(propSource, tiredSigh != null ? tiredSigh : generatedSigh ?? (generatedSigh = GenerateSigh()));
    public void PlayMemoryBridge() => Play(memorySource,
        childhoodBridge != null ? childhoodBridge : generatedChildhood ?? (generatedChildhood = GenerateChildhood()));

    public void SetMuffle(float amount)
    {
        float cutoff = Mathf.Lerp(22000f, 620f, Mathf.Clamp01(amount));
        for (int i = 0; i < officeFilters.Count; i++)
            if (officeFilters[i] != null) officeFilters[i].cutoffFrequency = cutoff;
    }

    public void StopOfficeAmbience()
    {
        if (streetSource != null) streetSource.Stop();
        if (fluorescentSource != null) fluorescentSource.Stop();
        if (keyboardSource != null) keyboardSource.Stop();
        if (propSource != null) propSource.Stop();
        if (bossSource != null) bossSource.Stop();
    }

    private void PlayDeskSlam() => Play(propSource, deskSlam);

    private void AddOfficeFilter(AudioSource source, float cutoff)
    {
        if (source == null) return;
        AudioLowPassFilter filter = source.GetComponent<AudioLowPassFilter>();
        if (filter == null) filter = source.gameObject.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = cutoff;
        officeFilters.Add(filter);
    }

    private static void ConfigureLoop(AudioSource source, AudioClip clip, float volume, float spatial,
        float minDistance, float maxDistance)
    {
        if (source == null) return;
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = spatial;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        if (clip != null) source.Play();
    }

    private static void ConfigureOneShotSource(AudioSource source, float volume, float spatial,
        float minDistance, float maxDistance)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.volume = volume;
        source.spatialBlend = spatial;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    private static void Play(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

    private AudioClip Remember(AudioClip clip)
    {
        generatedClips.Add(clip);
        return clip;
    }

    private AudioClip Create(string name, float seconds, System.Func<int, float> sample)
    {
        const int rate = 22050;
        int count = Mathf.RoundToInt(seconds * rate);
        float[] values = new float[count];
        for (int i = 0; i < count; i++) values[i] = Mathf.Clamp(sample(i), -0.9f, 0.9f);
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(values, 0);
        return Remember(clip);
    }

    private AudioClip GenerateTraffic()
    {
        var random = new System.Random(38);
        return Create("OfficeTrafficPlaceholder", 5f, i =>
        {
            float t = i / 22050f;
            float road = 0.055f * Mathf.Sin(2f * Mathf.PI * 58f * t) + (float)(random.NextDouble() - 0.5) * 0.1f;
            float passing = 0.045f * Mathf.Sin(2f * Mathf.PI * (75f + 17f * Mathf.Sin(t * 2f)) * t);
            float horn = (t > 1.25f && t < 1.7f) || (t > 3.2f && t < 3.65f)
                ? 0.15f * Mathf.Sin(2f * Mathf.PI * 390f * t) + 0.06f * Mathf.Sin(2f * Mathf.PI * 470f * t) : 0f;
            return road + passing + horn;
        });
    }

    private AudioClip GenerateHum() => Create("FluorescentPlaceholder", 2f, i =>
    {
        float t = i / 22050f;
        return 0.11f * Mathf.Sin(2f * Mathf.PI * 100f * t) + 0.035f * Mathf.Sin(2f * Mathf.PI * 200f * t);
    });

    private AudioClip GenerateKey()
    {
        var random = new System.Random(17 + generatedClips.Count);
        return Create("KeyboardPlaceholder", 0.065f, i =>
        {
            float envelope = 1f - i / (22050f * 0.065f);
            return (float)(random.NextDouble() - 0.5) * 0.4f * envelope;
        });
    }

    private AudioClip GenerateNotification() => Create("EmailNotificationPlaceholder", 0.3f, i =>
    {
        float t = i / 22050f;
        float envelope = Mathf.Clamp01(1f - t / 0.3f);
        return 0.22f * envelope * (Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1175f * t));
    });

    private AudioClip GeneratePrinter()
    {
        var random = new System.Random(61);
        return Create("PrinterPlaceholder", 1.25f, i =>
        {
            float t = i / 22050f;
            return 0.08f * Mathf.Sin(2f * Mathf.PI * (82f + 9f * Mathf.Sin(t * 35f)) * t) +
                   (float)(random.NextDouble() - 0.5) * 0.045f;
        });
    }

    private AudioClip GenerateWater()
    {
        var random = new System.Random(72);
        return Create("WaterPlaceholder", 1.1f, i =>
        {
            float envelope = Mathf.Sin(Mathf.PI * i / (22050f * 1.1f));
            return (float)(random.NextDouble() - 0.5) * 0.14f * envelope;
        });
    }

    private AudioClip GenerateRustle()
    {
        var random = new System.Random(27);
        return Create("PaperPlaceholder", 0.55f, i => (float)(random.NextDouble() - 0.5) * 0.18f *
            Mathf.Sin(Mathf.PI * i / (22050f * 0.55f)));
    }

    private AudioClip GenerateCup() => Create("CupPlaceholder", 0.22f, i =>
    {
        float t = i / 22050f;
        return 0.24f * Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-18f * t);
    });

    private AudioClip GenerateSigh() => Create("SighPlaceholder", 1.15f, i =>
    {
        float t = i / 22050f;
        float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.15f));
        return 0.035f * Mathf.Sin(2f * Mathf.PI * (145f - 45f * t) * t) * envelope;
    });

    private AudioClip GenerateChildhood() => Create("ChildhoodBridgePlaceholder", 2.2f, i =>
    {
        float t = i / 22050f;
        float crickets = 0.018f * Mathf.Sin(2f * Mathf.PI * (3100f + 180f * Mathf.Sin(t * 6f)) * t);
        float bird = t > 0.55f && t < 0.9f ? 0.055f * Mathf.Sin(2f * Mathf.PI * (1650f + 500f * (t - 0.55f)) * t) : 0f;
        return crickets + bird;
    });

    private void OnDestroy()
    {
        CancelInvoke();
        for (int i = 0; i < generatedClips.Count; i++)
            if (generatedClips[i] != null) Destroy(generatedClips[i]);
    }
}
