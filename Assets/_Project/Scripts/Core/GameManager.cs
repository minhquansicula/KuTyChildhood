using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action<bool> OnPauseChanged;
    private readonly HashSet<Object> inputOwners = new HashSet<Object>();
    private readonly HashSet<Object> hiddenCursorOwners = new HashSet<Object>();
    public bool IsPaused { get; private set; }
    public bool InputBlocked => IsPaused || inputOwners.Count > 0 ||
        (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading);
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inputOwners.Clear();
        hiddenCursorOwners.Clear();
        bool wasPaused = IsPaused;
        IsPaused = false;
        Time.timeScale = 1f;
        if (wasPaused) OnPauseChanged?.Invoke(false);
        SetGameState(scene.name == SceneNames.Act1 ? GameState.Act1_RealWorld :
            scene.name == SceneNames.Act2_Home ? GameState.Act2_MemoryWorld_Home :
            scene.name == SceneNames.Act3_OutSide ? GameState.Act3_MemoryWorld_OutSide :
            scene.name == SceneNames.Act_Ending ? GameState.Act_Ending : GameState.MainMenu);
        RefreshCursor();
    }
    public void SetGameState(GameState state)
    {
        bool changed = currentState != state;
        currentState = state;
        RefreshCursor();
        if (changed) OnGameStateChanged?.Invoke(state);
    }
    // Closing one modal cannot unlock another modal.
    public void AcquireInput(Object owner, bool showCursor = true)
    {
        if (owner != null)
        {
            inputOwners.Add(owner);
            if (showCursor) hiddenCursorOwners.Remove(owner);
            else hiddenCursorOwners.Add(owner);
        }
        RefreshCursor();
    }
    public void ReleaseInput(Object owner)
    {
        inputOwners.Remove(owner);
        hiddenCursorOwners.Remove(owner);
        RefreshCursor();
    }
    public void RefreshCursor()
    {
        inputOwners.RemoveWhere(owner => owner == null);
        hiddenCursorOwners.RemoveWhere(owner => owner == null || !inputOwners.Contains(owner));
        bool showCursor = IsPaused ||
            (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) ||
            currentState == GameState.MainMenu || currentState == GameState.Act_Ending;
        foreach (Object owner in inputOwners)
            if (!hiddenCursorOwners.Contains(owner)) { showCursor = true; break; }
        SetCursorState(showCursor);
    }
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void StartNewGame()
    {
        Scene2Checkpoint.Clear();
        Navigate(SceneNames.Act1);
    }
    public void ContinueGame()
    {
        if (!Scene2Checkpoint.HasCheckpoint) return;
        Navigate(Scene2Checkpoint.ResumeScene);
    }
    public void EnterMemoryWorld() => Navigate(SceneNames.Act2_Home);
    public void EnterMemoryWorldOutSide() => Navigate(SceneNames.Act3_OutSide);
    public void EnterEnding() => Navigate(SceneNames.Act_Ending);
    public void ReturnToMainMenu() => Navigate(SceneNames.MainMenu);
    private void Navigate(string scene)
    {
        // Act2 owns currency, inventory and memories; loading it creates a fresh session.
        if (SceneLoader.Instance == null) { Debug.LogError("SceneLoader is missing."); return; }
        SceneLoader.Instance.LoadScene(scene);
    }
    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        OnPauseChanged?.Invoke(paused);
        RefreshCursor();
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
