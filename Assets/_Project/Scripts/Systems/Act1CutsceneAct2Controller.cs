using System.Collections;
using UnityEngine;
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
        try
        {
            yield return OfficeDepartureVideo.PlayClip(
                clip != null ? clip : Resources.Load<VideoClip>("Cutscenes/OfficeToHome_Final"),
                () => { });
            Act2CountrysideShot.EnterFromOffice();
        }
        finally
        {
            GameManager.Instance?.ReleaseInput(this);
        }
    }

}
