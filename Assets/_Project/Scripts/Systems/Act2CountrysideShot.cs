using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public sealed class Act2CountrysideShot : MonoBehaviour
{
    [SerializeField] private bool playFootage;
    [SerializeField] private VideoClip countrysideClip;
    [SerializeField] private Camera shotCamera;
    [SerializeField] private Transform cameraEnd;
    [SerializeField] private Transform houseTarget;
    [SerializeField, Min(1f)] private float overheadDuration = 5f;
    [SerializeField] private AudioSource ambience;
    [SerializeField] private Transform[] rice;
    [SerializeField, Min(.1f)] private float duration = 5.5f;
    [SerializeField, Min(0f)] private float fadeDuration = .55f;

    private static bool continueToHome;
    private float black;

    public static void EnterFromOffice()
    {
        continueToHome = true;
        var loader = SceneLoader.Instance;
        if (loader && loader.PrepareSleepTransition(SceneNames.Act1_CutScene2_Act2))
            loader.CompleteSleepTransition();
        else
            SceneManager.LoadScene(SceneNames.Act1_CutScene2_Act2);
    }

    private IEnumerator Start()
    {
        bool followStory = continueToHome;
        continueToHome = false;
        black = 1f;
        var loader = SceneLoader.Instance;
        while (loader && loader.IsLoading) yield return null;

        if (followStory) GameManager.Instance?.AcquireInput(this, showCursor: false);
        // Start loading the home scene while the shot plays; only its activation waits for the last frame.
        bool homePreloaded = followStory && loader != null && loader.PrepareSleepTransition(SceneNames.Act2_Home);
        try
        {
            if (ambience && ambience.clip)
            {
                KuTyAudioMix.Route(ambience, KuTyAudioBus.Ambience);
                ambience.Play();
            }

            var clip = playFootage
                ? (countrysideClip ? countrysideClip : Resources.Load<VideoClip>("Cutscenes/CountrysideDream"))
                : null;
            if (clip)
            {
                shotCamera.cullingMask = 0;
                black = 0f;
                yield return OfficeDepartureVideo.PlayClip(clip, () =>
                {
                    black = 1f;
                    if (ambience) ambience.Stop();
                    if (followStory) ContinueToHome(loader, homePreloaded);
                });
                yield break;
            }

            Vector3 startPosition = shotCamera.transform.position;
            Quaternion startRotation = shotCamera.transform.rotation;
            var house = houseTarget ? houseTarget : transform.Find("Farmhouse and earthen yard");
            var houseRenderers = house ? house.GetComponentsInChildren<Renderer>() : null;
            bool hasHouse = houseRenderers != null && houseRenderers.Length > 0;
            Vector3 focus = Vector3.zero;

            if (hasHouse)
            {
                Bounds bounds = houseRenderers[0].bounds;
                for (int i = 1; i < houseRenderers.Length; i++) bounds.Encapsulate(houseRenderers[i].bounds);
                focus = bounds.center;
                focus.y = bounds.min.y + bounds.size.y * .53f;
            }
            var plantRotations = new Quaternion[rice.Length];
            for (int i = 0; i < rice.Length; i++)
                if (rice[i]) plantRotations[i] = rice[i].localRotation;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                shotCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, cameraEnd.position, t),
                    Quaternion.Slerp(startRotation, cameraEnd.rotation, t));
                for (int i = 0; i < rice.Length; i++)
                    if (rice[i]) rice[i].localRotation = plantRotations[i] *
                        Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(elapsed * 2f + i * .42f));
                black = fadeDuration > 0f ? 1f - Mathf.Clamp01(Mathf.Min(elapsed,
                    hasHouse ? elapsed : (followStory ? duration - elapsed : elapsed)) / fadeDuration) : 0f;
                yield return null;
            }

            shotCamera.transform.SetPositionAndRotation(cameraEnd.position, cameraEnd.rotation);
            if (hasHouse)
            {
                Vector3 approach = Vector3.ProjectOnPlane(cameraEnd.position - focus, Vector3.up).normalized;
                for (float elapsed = 0f; elapsed < overheadDuration; elapsed += Time.deltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / overheadDuration);
                    Vector3 position = focus + approach * Mathf.Lerp(13f, 10.5f, t) +
                        Vector3.up * Mathf.Lerp(10f, 8f, t);
                    shotCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
                    black = followStory && fadeDuration > 0f
                        ? 1f - Mathf.Clamp01((overheadDuration - elapsed) / fadeDuration) : 0f;
                    yield return null;
                }
            }
            if (ambience) ambience.Stop();
            black = followStory ? 1f : 0f;
            if (followStory) ContinueToHome(loader, homePreloaded);
        }
        finally
        {
            if (followStory) GameManager.Instance?.ReleaseInput(this);
        }
    }

    private static void ContinueToHome(SceneLoader loader, bool preloaded = false)
    {
        if (preloaded)
        {
            loader.CompleteSleepTransition();
            return;
        }
        if (loader && loader.PrepareSleepTransition(SceneNames.Act2_Home))
            loader.CompleteSleepTransition();
        else
            SceneManager.LoadScene(SceneNames.Act2_Home);
    }

    private void OnGUI()
    {
        if (black <= 0f || Event.current.type != EventType.Repaint) return;
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, black);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
