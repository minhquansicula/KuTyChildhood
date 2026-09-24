using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance == null) new GameObject("GameManager").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("SceneLoader").AddComponent<SceneLoader>();
        if (AudioManager.Instance == null)
        {
            var prefab = Resources.Load<GameObject>("KuTyAudio");
            if (prefab != null) Object.Instantiate(prefab);
        }
    }
}
