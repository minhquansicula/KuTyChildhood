using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;
    public event System.Action<GameState> OnGameStateChanged;
    private readonly HashSet<Object> inputOwners = new HashSet<Object>();
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
        IsPaused = false;
        Time.timeScale = 1f;
        SetGameState(scene.name == SceneNames.Act1 ? GameState.Act1_RealWorld :
            scene.name == SceneNames.Act2 ? GameState.Act2_MemoryWorld :
            scene.name == SceneNames.Act3 ? GameState.Act3_Ending : GameState.MainMenu);
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
    public void AcquireInput(Object owner)
    {
        if (owner != null) inputOwners.Add(owner);
        RefreshCursor();
    }
    public void ReleaseInput(Object owner) { inputOwners.Remove(owner); RefreshCursor(); }
    public void RefreshCursor()
    {
        inputOwners.RemoveWhere(owner => owner == null);
        SetCursorState(InputBlocked || currentState == GameState.MainMenu || currentState == GameState.Act3_Ending);
    }
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void StartNewGame() => Navigate(SceneNames.Act1);
    public void EnterMemoryWorld() => Navigate(SceneNames.Act2);
    public void EnterEnding() => Navigate(SceneNames.Act3);
    public void ReturnToMainMenu() => Navigate(SceneNames.MainMenu);
    private void Navigate(string scene)
    {
        // Act2 owns currency, inventory and memories; loading it creates a fresh session.
        if (SceneLoader.Instance == null) { Debug.LogError("SceneLoader is missing."); return; }
        SceneLoader.Instance.LoadScene(scene);
    }
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
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
