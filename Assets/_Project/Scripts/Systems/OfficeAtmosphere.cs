using UnityEngine;

// Scene-local 3D audio. Placeholder loops are generated only when no real clips are assigned.
public class OfficeAtmosphere : MonoBehaviour
{
    [Header("3D audio sources in the office")]
    [SerializeField] private AudioSource streetSource;
    [SerializeField] private AudioSource fluorescentSource;
    [SerializeField] private AudioSource keyboardSource;
    [SerializeField] private AudioSource bossSource;
    [Header("Replace these with your own assets")]
    [SerializeField] private AudioClip streetTraffic;
    [SerializeField] private AudioClip fluorescentHum;
    [SerializeField] private AudioClip keyPress;
    [SerializeField] private AudioClip bossFirstLine;
    [SerializeField] private AudioClip bossSecondLine;
    [SerializeField] private Light ceilingLight;
    private AudioClip generatedTraffic, generatedHum, generatedKey;
    private float flickerTimer;
    private void Start()
    {
        if (streetSource != null)
        {
            streetSource.clip = streetTraffic != null ? streetTraffic : generatedTraffic = MakeTraffic();
            streetSource.loop = true;
            streetSource.spatialBlend = .8f;
            streetSource.minDistance = 5f;
            streetSource.maxDistance = 25f;
            streetSource.volume = .7f;
            streetSource.Play();
        }
        if (fluorescentSource != null)
        {
            fluorescentSource.clip = fluorescentHum != null ? fluorescentHum : generatedHum = MakeHum();
            fluorescentSource.loop = true;
            fluorescentSource.spatialBlend = .65f;
            fluorescentSource.minDistance = 4f;
            fluorescentSource.volume = .27f;
            fluorescentSource.Play();
        }
        if (keyboardSource != null)
        {
            keyboardSource.spatialBlend = .8f;
            keyboardSource.minDistance = 2f;
            keyboardSource.volume = .35f;
        }
        if (bossSource != null)
        {
            bossSource.spatialBlend = 1f;
            bossSource.minDistance = 3f;
            bossSource.volume = 1f;
        }
    }
    private void Update()
    {
        if (ceilingLight == null) return;
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0) flickerTimer = Random.Range(2f, 6f);
        ceilingLight.intensity = flickerTimer < .11f ? 1.9f : 3.4f;
    }
    public void PlayTyping()
    {
        if (keyboardSource == null) return;
        if (keyPress == null && generatedKey == null) generatedKey = MakeKey();
        keyboardSource.PlayOneShot(keyPress != null ? keyPress : generatedKey);
    }
    public float PlayBoss(bool second)
    {
        var clip = second ? bossSecondLine : bossFirstLine;
        if (bossSource == null || clip == null) return 0f;

        bossSource.Stop();
        bossSource.PlayOneShot(clip);
        return clip.length;
    }
    private static AudioClip Create(string name, float seconds, System.Func<int, float> sample)
    {
        const int rate = 22050;
        int count = Mathf.RoundToInt(seconds * rate);
        var values = new float[count];
        for (int i = 0; i < count; i++) values[i] = Mathf.Clamp(sample(i), -.9f, .9f);
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(values, 0);
        return clip;
    }
    private static AudioClip MakeTraffic()
    {
        var random = new System.Random(38);
        return Create("OfficeTrafficPlaceholder", 4f, i =>
        {
            float t = i / 22050f;
            float road = .07f * Mathf.Sin(2f * Mathf.PI * 58f * t) + (float)(random.NextDouble() - .5) * .12f;
            float passing = .06f * Mathf.Sin(2f * Mathf.PI * (75f + 17f * Mathf.Sin(t * 2f)) * t);
            float horn = (t > 1.15f && t < 1.75f) || (t > 2.75f && t < 3.18f)
                ? .19f * Mathf.Sin(2f * Mathf.PI * 390f * t) + .08f * Mathf.Sin(2f * Mathf.PI * 470f * t) : 0f;
            return road + passing + horn;
        });
    }
    private static AudioClip MakeHum() => Create("FluorescentPlaceholder", 2f, i =>
    {
        float t = i / 22050f;
        return .13f * Mathf.Sin(2f * Mathf.PI * 100f * t) + .04f * Mathf.Sin(2f * Mathf.PI * 200f * t);
    });
    private static AudioClip MakeKey()
    {
        var random = new System.Random(17);
        return Create("KeyboardPlaceholder", .065f, i =>
        {
            float envelope = 1f - i / (22050f * .065f);
            return (float)(random.NextDouble() - .5) * .4f * envelope;
        });
    }
    private void OnDestroy()
    {
        if (generatedTraffic != null) Destroy(generatedTraffic);
        if (generatedHum != null) Destroy(generatedHum);
        if (generatedKey != null) Destroy(generatedKey);
    }
}
