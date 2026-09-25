using UnityEngine;

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
    [SerializeField] private AudioSource ambientSource; // Optional: ambient loop

    [Header("Sound Library")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Settings")]
    [SerializeField] private float bgmVolume = 0.4f;
    [SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private float fadeDuration = 1.5f;
    private Coroutine musicRoutine;

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
            bgmSource.volume = bgmVolume;
        }

        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.volume = bgmVolume * 0.5f;
        }
    }

    private void Start()
    {
        // Subscribe vào GameState change để tự động đổi nhạc
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            OnGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>Play nhạc nền với fade.</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // Đang play rồi

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

    /// <summary>Play sound effect (1 lần).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>Play SFX theo tên (tiện dùng).</summary>
    public void PlaySFX(string sfxName)
    {
        if (soundLibrary == null) return;

        AudioClip clip = GetClipByName(sfxName);
        if (clip != null)
            PlaySFX(clip);
        else
            Debug.LogWarning($"[AudioManager] Không tìm thấy SFX: {sfxName}");
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

        switch (newState)
        {
            case GameState.MainMenu:
                PlayBGM(soundLibrary.bgmMainMenu);
                StopAmbient();
                break;
            case GameState.Act1_RealWorld:
                PlayBGM(soundLibrary.bgmAct1);
                break;
            case GameState.Act2_MemoryWorld_Home:
            case GameState.Act3_MemoryWorld_OutSide:
                PlayBGM(soundLibrary.bgmAct2);
                PlayAmbient(soundLibrary.ambBirds);
                break;
            case GameState.Act_Ending:
                PlayBGM(soundLibrary.bgmAct3);
                StopAmbient();
                break;
        }
    }

    // ========== HELPER ==========

    private AudioClip GetClipByName(string name)
    {
        if (soundLibrary == null) return null;

        switch (name.ToLower())
        {
            case "water_splash": return soundLibrary.sfxWaterSplash;
            case "dish_clean": return soundLibrary.sfxDishClean;
            case "marble_shoot": return soundLibrary.sfxMarbleShoot;
            case "marble_hit": return soundLibrary.sfxMarbleHit;
            case "marble_roll": return soundLibrary.sfxMarbleRoll;
            case "purchase": return soundLibrary.sfxPurchase;
            case "purchase_fail": return soundLibrary.sfxPurchaseFail;
            case "memory_collected": return soundLibrary.sfxMemoryCollected;
            case "all_memories": return soundLibrary.sfxAllMemoriesComplete;
            case "button_click": return soundLibrary.sfxButtonClick;
            case "button_hover": return soundLibrary.sfxButtonHover;
            default: return null;
        }
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
    }
}
