using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioManager — Singleton quản lý âm thanh game.
/// 
/// Chức năng:
/// - Play/Stop nhạc nền (BGM) với fade
/// - Play sound effects (SFX)
/// - Tự động đổi BGM theo GameState
/// 
/// Setup:
/// 1. Tạo GameObject "AudioManager"
/// 2. Thêm 2 AudioSource (1 cho BGM, 1 cho SFX)
/// 3. Gán SoundLibrary
/// 4. DontDestroyOnLoad
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static AudioManager Instance { get; private set; }

    // ========== REFERENCES ==========
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource ambientSource; // Optional: ambient loop

    [Header("Sound Library")]
    [SerializeField] private SoundLibrary soundLibrary;
    [SerializeField] private SfxCatalog sfxCatalog;

    [Header("Settings")]
    [SerializeField] private float bgmVolume = 0.4f;
    [SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField, Range(1, 12)] private int sfxPoolSize = 6;
    private Coroutine musicRoutine;
    private bool letterMusicActive;
    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private readonly Dictionary<SfxId, float> nextPlayTimes = new Dictionary<SfxId, float>();
    private readonly Dictionary<SfxId, int> lastVariantIndices = new Dictionary<SfxId, int>();
    private readonly HashSet<SfxId> missingCueWarnings = new HashSet<SfxId>();
    private readonly HashSet<string> missingLegacyWarnings = new HashSet<string>();
    private int poolCursor;

    public SfxCatalog Catalog => sfxCatalog;
    public SfxId LastPlayedSfx { get; private set; }
    public int SuccessfulSfxPlayCount { get; private set; }
    public int ActiveGameplaySfxVoices
    {
        get
        {
            int count = 0;
            for (int i = 0; i < sfxPool.Count; i++) if (sfxPool[i] != null && sfxPool[i].isPlaying) count++;
            return count;
        }
    }

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio sources
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.volume = bgmVolume;
        }

        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.volume = bgmVolume * 0.5f;
        }

        InitializeSfxSources();
    }

    private void Start()
    {
        // Subscribe vào GameState change để tự động đổi nhạc
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnPauseChanged += OnPauseChanged;
            OnPauseChanged(GameManager.Instance.IsPaused);
            OnGameStateChanged(GameManager.Instance.CurrentState);
        }
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        bool wasInstance = Instance == this;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnPauseChanged -= OnPauseChanged;
        }
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (wasInstance)
        {
            AudioListener.pause = false;
            Instance = null;
        }
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>Play nhạc nền với fade.</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying && musicRoutine == null) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(CrossFadeBGM(clip));
    }

    /// <summary>Dừng nhạc nền với fade out.</summary>
    public void StopBGM()
    {
        if (bgmSource == null) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(FadeOutBGM());
    }

    public void BeginLetterMusic()
    {
        if (soundLibrary == null || soundLibrary.bgmLetter == null) return;
        letterMusicActive = true;
        PlayBGM(soundLibrary.bgmLetter);
    }

    public void EndLetterMusic()
    {
        if (!letterMusicActive) return;
        letterMusicActive = false;
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.CurrentState);
        else StopBGM();
    }

    private void Update()
    {
        // These are complete songs, not authored seamless loops. Fade the tail before restarting.
        if (musicRoutine == null && bgmSource != null && bgmSource.isPlaying &&
            bgmSource.clip != null && bgmSource.clip.length > fadeDuration * 2f &&
            bgmSource.clip.length - bgmSource.time <= fadeDuration * .5f)
            musicRoutine = StartCoroutine(CrossFadeBGM(bgmSource.clip));
    }

    /// <summary>Play sound effect (1 lần).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>Play a typed global/UI cue. Returns false when missing, paused, or rate-limited.</summary>
    public bool PlaySFX(SfxId id)
    {
        SfxCue cue = ResolveCue(id);
        if (cue == null) return false;
        if (cue.category != SfxCategory.UI && GameManager.Instance != null && GameManager.Instance.IsPaused) return false;
        if (nextPlayTimes.TryGetValue(id, out float nextAllowed) && Time.unscaledTime < nextAllowed) return false;

        int variantIndex = ChooseVariant(cue, id);
        AudioClip clip = cue.GetVariant(variantIndex);
        if (clip == null)
        {
            WarnMissingCueOnce(id, "clip variant is missing");
            return false;
        }

        AudioSource target = cue.category == SfxCategory.UI ? uiSource : NextGameplaySource();
        if (target == null) return false;
        target.Stop();
        target.clip = clip;
        target.loop = false;
        target.playOnAwake = false;
        target.spatialBlend = 0f;
        target.ignoreListenerPause = cue.category == SfxCategory.UI;
        target.volume = Mathf.Clamp01(sfxVolume * cue.volume);
        target.pitch = Random.Range(cue.pitchMin, cue.pitchMax);
        target.Play();

        nextPlayTimes[id] = Time.unscaledTime + cue.cooldownSeconds;
        lastVariantIndices[id] = variantIndex;
        LastPlayedSfx = id;
        SuccessfulSfxPlayCount++;
        return true;
    }

    /// <summary>Play SFX theo tên (tiện dùng).</summary>
    public void PlaySFX(string sfxName)
    {
        if (TryGetLegacyId(sfxName, out SfxId id))
        {
            PlaySFX(id);
            return;
        }
        string key = string.IsNullOrWhiteSpace(sfxName) ? "<empty>" : sfxName;
        if (missingLegacyWarnings.Add(key)) Debug.LogWarning($"[AudioManager] Không tìm thấy legacy SFX key: {key}");
    }

    /// <summary>Play ambient sound.</summary>
    public void PlayAmbient(AudioClip clip)
    {
        if (ambientSource == null || clip == null) return;
        ambientSource.clip = clip;
        ambientSource.Play();
    }

    /// <summary>Dừng ambient.</summary>
    public void StopAmbient()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    // ========== AUTO BGM SWITCHING ==========

    private void OnGameStateChanged(GameState newState)
    {
        if (soundLibrary == null) return;
        letterMusicActive = false;

        switch (newState)
        {
            case GameState.MainMenu:
                PlayBGM(soundLibrary.bgmMainMenu);
                StopAmbient();
                break;
            case GameState.Act1_RealWorld:
                if (soundLibrary.bgmAct1 != null) PlayBGM(soundLibrary.bgmAct1);
                else StopBGM();
                StopAmbient();
                break;
            case GameState.Act2_MemoryWorld_Home:
                PlayBGM(soundLibrary.bgmAct2);
                PlayAmbient(soundLibrary.ambBirds);
                break;
            case GameState.Act3_MemoryWorld_OutSide:
                PlayBGM(soundLibrary.bgmVillage != null ? soundLibrary.bgmVillage : soundLibrary.bgmAct2);
                PlayAmbient(soundLibrary.ambBirds);
                break;
            case GameState.Act_Ending:
                PlayBGM(soundLibrary.bgmAct3);
                StopAmbient();
                break;
        }
    }

    // ========== HELPER ==========

    private static bool TryGetLegacyId(string name, out SfxId id)
    {
        switch ((name ?? string.Empty).ToLowerInvariant())
        {
            case "ui_select": case "button_click": id = SfxId.UIConfirm; return true;
            case "button_hover": id = SfxId.UIHover; return true;
            case "water_splash": id = SfxId.WaterSplash; return true;
            case "dish_clean": id = SfxId.DishClean; return true;
            case "marble_shoot": id = SfxId.MarbleShoot; return true;
            case "marble_hit": id = SfxId.MarbleHit; return true;
            case "marble_roll": id = SfxId.MarbleRoll; return true;
            case "purchase": id = SfxId.Purchase; return true;
            case "purchase_fail": id = SfxId.PurchaseFail; return true;
            case "memory_collected": id = SfxId.MemoryCollected; return true;
            case "all_memories": id = SfxId.AllMemoriesComplete; return true;
            default: id = SfxId.None; return false;
        }
    }

    public void StopTransientSfx()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            AudioSource source = sfxPool[i];
            if (source == null) continue;
            source.Stop();
            source.clip = null;
        }
        if (uiSource != null)
        {
            uiSource.Stop();
            uiSource.clip = null;
        }
    }

    private void InitializeSfxSources()
    {
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        ConfigurePooledSource(sfxSource, false);
        sfxPool.Clear();
        sfxPool.Add(sfxSource);
        for (int i = 1; i < Mathf.Max(1, sfxPoolSize); i++)
        {
            GameObject voice = new GameObject("SfxVoice_" + (i + 1));
            voice.transform.SetParent(transform, false);
            AudioSource source = voice.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            ConfigurePooledSource(source, false);
            sfxPool.Add(source);
        }
        if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();
        ConfigurePooledSource(uiSource, true);
    }

    private static void ConfigurePooledSource(AudioSource source, bool ignorePause)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = ignorePause;
    }

    private AudioSource NextGameplaySource()
    {
        if (sfxPool.Count == 0) return null;
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int index = (poolCursor + i) % sfxPool.Count;
            if (sfxPool[index] != null && !sfxPool[index].isPlaying)
            {
                poolCursor = (index + 1) % sfxPool.Count;
                return sfxPool[index];
            }
        }
        AudioSource replacement = sfxPool[poolCursor];
        poolCursor = (poolCursor + 1) % sfxPool.Count;
        return replacement;
    }

    private SfxCue ResolveCue(SfxId id)
    {
        if (id == SfxId.None) return null;
        if (sfxCatalog != null && sfxCatalog.TryGetCue(id, out SfxCue cue)) return cue;
        AudioClip legacy = GetLegacyClip(id);
        if (legacy != null)
        {
            return new SfxCue
            {
                id = id,
                category = id == SfxId.UIConfirm || id == SfxId.UIBack || id == SfxId.UIInvalid || id == SfxId.UIHover ? SfxCategory.UI : SfxCategory.Gameplay,
                variants = new[] { legacy },
                volume = 1f,
                pitchMin = 1f,
                pitchMax = 1f,
                cooldownSeconds = .05f
            };
        }
        WarnMissingCueOnce(id, sfxCatalog == null ? "catalog is not assigned" : "cue is not in catalog");
        return null;
    }

    private AudioClip GetLegacyClip(SfxId id)
    {
        if (soundLibrary == null) return null;
        switch (id)
        {
            case SfxId.WaterSplash: return soundLibrary.sfxWaterSplash;
            case SfxId.DishClean: return soundLibrary.sfxDishClean;
            case SfxId.MarbleShoot: return soundLibrary.sfxMarbleShoot;
            case SfxId.MarbleHit: return soundLibrary.sfxMarbleHit;
            case SfxId.MarbleRoll: return soundLibrary.sfxMarbleRoll;
            case SfxId.Purchase: return soundLibrary.sfxPurchase;
            case SfxId.PurchaseFail: return soundLibrary.sfxPurchaseFail;
            case SfxId.MemoryCollected: return soundLibrary.sfxMemoryCollected;
            case SfxId.AllMemoriesComplete: return soundLibrary.sfxAllMemoriesComplete;
            case SfxId.UIConfirm: return soundLibrary.sfxButtonClick;
            case SfxId.UIHover: return soundLibrary.sfxButtonHover;
            default: return null;
        }
    }

    private int ChooseVariant(SfxCue cue, SfxId id)
    {
        int count = cue.variants != null ? cue.variants.Length : 0;
        if (count <= 1) return 0;
        int selected = Random.Range(0, count);
        if (cue.avoidImmediateRepeat && lastVariantIndices.TryGetValue(id, out int previous) && selected == previous)
            selected = (selected + 1 + Random.Range(0, count - 1)) % count;
        return selected;
    }

    private void WarnMissingCueOnce(SfxId id, string reason)
    {
        if (missingCueWarnings.Add(id)) Debug.LogWarning($"[AudioManager] SFX '{id}' was skipped: {reason}.", this);
    }

    private void OnPauseChanged(bool paused)
    {
        AudioListener.pause = paused;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        StopTransientSfx();
        nextPlayTimes.Clear();
        AudioListener.pause = false;
    }

    private System.Collections.IEnumerator CrossFadeBGM(AudioClip newClip)
    {
        // Fade out current
        if (bgmSource.isPlaying)
        {
            float elapsed = 0f;
            float startVol = bgmSource.volume;

            while (elapsed < fadeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (fadeDuration * 0.5f));
                yield return null;
            }
        }

        // Switch clip
        bgmSource.volume = 0f;
        bgmSource.clip = newClip;
        bgmSource.Play();

        // Fade in new
        float elapsed2 = 0f;
        while (elapsed2 < fadeDuration * 0.5f)
        {
            elapsed2 += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, elapsed2 / (fadeDuration * 0.5f));
            yield return null;
        }

        bgmSource.volume = bgmVolume;
        musicRoutine = null;
    }

    private System.Collections.IEnumerator FadeOutBGM()
    {
        float elapsed = 0f;
        float startVol = bgmSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
        musicRoutine = null;
    }
}
