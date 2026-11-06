using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

internal static class OfficeDepartureVideo
{
    private const float DeskHoldSeconds = 1.25f;
    private const float FadeSeconds = 1.35f;

    internal static IEnumerator Play(OfficeAtmosphere atmosphere, Action onFinished)
    {
        var root = new GameObject("Office Departure Cutscene", typeof(RectTransform), typeof(Canvas));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;

        var backdrop = AddOverlay<Image>("Fade", root.transform);
        backdrop.color = Color.clear;
        RenderTexture target = null;
        var camera = Camera.main;
        var focus = new FirstPersonFocusTransition("Departure Focus", camera);
        try
        {
            yield return new WaitForSeconds(DeskHoldSeconds);
            for (float elapsed = 0f; elapsed < FadeSeconds; elapsed += Time.deltaTime)
            {
                float progress = elapsed / FadeSeconds;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, 1f, progress));
                focus.Set(Mathf.SmoothStep(0f, 1f, progress), -.45f * fade);
                backdrop.color = new Color(0f, 0f, 0f, fade);
                atmosphere?.SetMuffle(fade);
                yield return null;
            }

            backdrop.color = Color.black;
            atmosphere?.SetMuffle(1f);
            atmosphere?.StopOfficeAmbience();
            AudioManager.Instance?.StopBGM();
            // Present the black frame before asking the video decoder to prepare.
            yield return null;

            var clip = Resources.Load<VideoClip>("Cutscenes/OfficeToHome_Final");
            VideoPlayer player = null;
            bool failed = false;
            bool ended = false;
            if (clip != null)
            {
                player = root.AddComponent<VideoPlayer>();
                player.playOnAwake = false;
                player.clip = clip;
                player.waitForFirstFrame = true;
                if (clip.audioTrackCount > 0)
                {
                    var audio = root.AddComponent<AudioSource>();
                    audio.playOnAwake = false;
                    player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    player.controlledAudioTrackCount = 1;
                    player.EnableAudioTrack(0, true);
                    player.SetTargetAudioSource(0, audio);
                }
                player.renderMode = VideoRenderMode.RenderTexture;
                target = new RenderTexture((int)clip.width, (int)clip.height, 0);
                player.targetTexture = target;
                player.errorReceived += (_, message) =>
                {
                    failed = true;
                    Debug.LogWarning("Office departure video: " + message);
                };
                player.loopPointReached += _ => ended = true;
                player.Prepare();
            }

            if (player != null)
            {
                float prepareDeadline = Time.realtimeSinceStartup + 5f;
                while (!player.isPrepared && !failed && Time.realtimeSinceStartup < prepareDeadline)
                    yield return null;

                if (player.isPrepared && !failed)
                {
                    var picture = AddOverlay<RawImage>("Video", root.transform);
                    picture.texture = target;
                    var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fit.aspectRatio = (float)clip.width / clip.height;
                    picture.enabled = false;
                    player.Play();

                    float playbackElapsed = 0f;
                    bool paused = false;
                    bool started = false;
                    while (!ended && !failed && playbackElapsed < clip.length + 3.0)
                    {
                        bool shouldPause = Time.timeScale == 0f;
                        if (shouldPause != paused)
                        {
                            paused = shouldPause;
                            if (paused) player.Pause();
                            else player.Play();
                        }
                        if (!paused)
                        {
                            playbackElapsed += Time.unscaledDeltaTime;
                            if (!started && player.frame >= 0)
                            {
                                started = true;
                                picture.enabled = true;
                            }
                        }
                        yield return null;
                    }
                    picture.enabled = false;
                    player.Stop();
                }
            }

            onFinished();
            // Let SceneLoader take over the black frame before removing this canvas.
            yield return null;
        }
        finally
        {
            focus.Dispose();
            if (target != null) Object.Destroy(target);
            Object.Destroy(root);
        }
    }

    private static T AddOverlay<T>(string name, Transform parent) where T : Graphic
    {
        var child = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return child.AddComponent<T>();
    }
}
