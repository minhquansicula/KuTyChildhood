using UnityEngine;

public class DishWashingGame : MonoBehaviour
{
    [SerializeField] private float washTimePerDish = 3f;
    [SerializeField] private int moneyPerDish = 3;
    [SerializeField] private GameObject[] dishes;
    [SerializeField] private Material cleanDishMaterial;
    [SerializeField] private ParticleSystem bubbleEffect;
    private bool isPlaying;
    private int currentDishIndex;
    private float currentProgress;
    public bool IsCompleted { get; private set; }
    public int TotalReward => (dishes == null ? 0 : dishes.Length) * moneyPerDish;
    public event System.Action<float, int, int> OnProgressChanged;
    public event System.Action<int> OnDishCleaned;
    public event System.Action<int> OnGameCompleted;

    public void StartGame()
    {
        if (isPlaying || IsCompleted) return;
        if (QuestManager.Instance != null && !QuestManager.Instance.CanWashDishes) return;
        if (dishes == null || dishes.Length == 0 || washTimePerDish <= 0 ||
            moneyPerDish <= 0 || CurrencyManager.Instance == null)
        {
            Debug.LogError("Dish washing setup is incomplete.", this);
            return;
        }
        isPlaying = true;
        GameManager.Instance?.AcquireInput(this);
        ProgressBarUI.Instance?.Show($"Chén {currentDishIndex + 1}/{dishes.Length} — giữ chuột trái, Esc: tạm nghỉ");
        ProgressBarUI.Instance?.SetProgress(currentProgress);
    }
    private void Update()
    {
        if (!isPlaying || (GameManager.Instance != null && GameManager.Instance.IsPaused)) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { CancelGame(); return; }
        if (!Input.GetMouseButton(0))
        {
            if (bubbleEffect != null) bubbleEffect.Stop();
            return;
        }
        WashFor(Time.deltaTime);
    }
    public void WashFor(float seconds)
    {
        if (!isPlaying || IsCompleted || seconds <= 0 || (GameManager.Instance != null && GameManager.Instance.IsPaused)) return;
        currentProgress = Mathf.Clamp01(currentProgress + seconds / washTimePerDish);
        if (bubbleEffect != null && !bubbleEffect.isPlaying) bubbleEffect.Play();
        ProgressBarUI.Instance?.SetProgress(currentProgress);
        OnProgressChanged?.Invoke(currentProgress, currentDishIndex, dishes.Length);
        if (currentProgress < 1f) return;
        if (dishes[currentDishIndex] != null)
        {
            var renderer = dishes[currentDishIndex].GetComponentInChildren<Renderer>();
            if (renderer != null && cleanDishMaterial != null) renderer.sharedMaterial = cleanDishMaterial;
        }
        CurrencyManager.Instance.AddMoney(moneyPerDish);
        AudioManager.Instance?.PlaySFX("dish_clean");
        OnDishCleaned?.Invoke(currentDishIndex);
        currentDishIndex++;
        currentProgress = 0;
        if (currentDishIndex < dishes.Length)
        {
            ProgressBarUI.Instance?.Show($"Chén {currentDishIndex + 1}/{dishes.Length} — giữ chuột trái, Esc: tạm nghỉ");
            return;
        }
        IsCompleted = true;
        CancelGame();
        OnGameCompleted?.Invoke(TotalReward);
    }
    public void CancelGame()
    {
        isPlaying = false;
        if (bubbleEffect != null) bubbleEffect.Stop();
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }
    private void OnDisable() { if (isPlaying) CancelGame(); }
}
