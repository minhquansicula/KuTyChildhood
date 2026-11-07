using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>Standalone bridge scene: plays the approved clip, then hands off to the existing wake-up.</summary>
public sealed class Act1CutsceneAct2Controller : MonoBehaviour
{
    [SerializeField] private VideoClip clip;

    private IEnumerator Start()
    {
        // The incoming loader must finish releasing its black overlay before playback starts.
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading)
            yield return null;

        GameManager.Instance?.AcquireInput(this, showCursor: false);
        AudioManager.Instance?.StopBGM();
        AudioManager.Instance?.StopAmbient();
        SceneLoader.Instance?.PrepareSleepTransition(SceneNames.Act2_Home);
        try
        {
            yield return OfficeDepartureVideo.PlayClip(
                clip != null ? clip : Resources.Load<VideoClip>("Cutscenes/OfficeToHome_Final"),
                Complete);
        }
        finally
        {
            GameManager.Instance?.ReleaseInput(this);
        }
    }

    private void Complete()
    {
        var loader = SceneLoader.Instance;
        if (loader == null)
            SceneManager.LoadScene(SceneNames.Act2_Home);
        else if (loader.IsSleepTransitionCovered)
            loader.CompleteSleepTransition();
        else
            loader.LoadSceneWithColor(SceneNames.Act2_Home, Color.black, 0f);
    }
}
