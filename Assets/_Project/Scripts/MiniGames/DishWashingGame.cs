using System.Collections;
using UnityEngine;

/// <summary>
/// DishWashingGame — Mini-game rửa chén.
/// 
/// Cách chơi:
/// - Player giữ chuột trái → thanh progress tăng
/// - Thả chuột → thanh dừng (KHÔNG giảm — game thư giãn)
/// - Bar đầy → chén sạch → chuyển chén tiếp
/// - Hoàn thành tất cả → nhận tiền + mảnh ký ức
/// 
/// Setup trong Unity:
/// 1. Tạo GameObject "DishWashingGame" trong scene Act2
/// 2. Gán script này
/// 3. Tạo các dish GameObjects (model chén) và gán vào dishes array
/// 4. Gán ProgressBarUI reference
/// 5. Gán DishInteractable để trigger game
/// </summary>
public class DishWashingGame : MonoBehaviour
{
    // ========== SETTINGS ==========
    [Header("Game Settings")]
    [SerializeField] private int totalDishes = 5;           // Tổng số chén cần rửa
    [SerializeField] private float washTimePerDish = 3f;    // Thời gian giữ chuột để rửa sạch 1 chén (giây)
    [SerializeField] private int moneyPerDish = 2;          // Tiền thưởng mỗi chén (tổng: 10)

    [Header("References")]
    [SerializeField] private GameObject[] dishes;                       // Các model chén trong scene
    [SerializeField] private Material cleanDishMaterial;                // Material sáng cho chén sạch
    [SerializeField] private Transform cameraLookTarget;               // Điểm camera nhìn vào khi rửa
    [SerializeField] private ParticleSystem bubbleEffect;               // Hiệu ứng bọt nước (optional)

    [Header("Player Reference")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    // ========== STATE ==========
    private bool isPlaying = false;
    private int currentDishIndex = 0;
    private float currentProgress = 0f;     // 0 → 1

    // ========== EVENTS ==========
    /// <summary>Gọi khi progress thay đổi. Param: progress (0-1), dishIndex, totalDishes</summary>
    public System.Action<float, int, int> OnProgressChanged;

    /// <summary>Gọi khi 1 chén rửa xong.</summary>
    public System.Action<int> OnDishCleaned;

    /// <summary>Gọi khi hoàn thành toàn bộ.</summary>
    public System.Action<int> OnGameCompleted;

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Bắt đầu mini-game. Gọi bởi DishInteractable khi player bấm E.
    /// </summary>
    public void StartGame()
    {
        if (isPlaying) return;

        isPlaying = true;
        currentDishIndex = 0;
        currentProgress = 0f;

        // Lock player
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();
        if (playerInteraction == null)
            playerInteraction = FindObjectOfType<PlayerInteraction>();

        if (playerController != null) playerController.LockMovement(true);
        if (playerInteraction != null) playerInteraction.LockInteraction(true);

        // Hiện cursor cho mini-game
        GameManager.Instance?.SetCursorState(true);

        // Hiện progress bar UI
        if (ProgressBarUI.Instance != null)
        {
            ProgressBarUI.Instance.Show($"Chén {currentDishIndex + 1}/{totalDishes}");
            ProgressBarUI.Instance.SetProgress(0f);
        }

        Debug.Log("[DishWashing] Bắt đầu rửa chén! Giữ chuột trái để rửa.");
    }

    // ========== LIFECYCLE ==========
    private void Update()
    {
        if (!isPlaying) return;

        // Giữ chuột trái → progress tăng
        if (Input.GetMouseButton(0))
        {
            currentProgress += Time.deltaTime / washTimePerDish;

            // Play bubble effect
            if (bubbleEffect != null && !bubbleEffect.isPlaying)
                bubbleEffect.Play();

            // Update UI
            OnProgressChanged?.Invoke(currentProgress, currentDishIndex, totalDishes);
            if (ProgressBarUI.Instance != null)
                ProgressBarUI.Instance.SetProgress(currentProgress);

            // Chén xong?
            if (currentProgress >= 1f)
            {
                CleanCurrentDish();
            }
        }
        else
        {
            // Thả chuột → dừng effect
            if (bubbleEffect != null && bubbleEffect.isPlaying)
                bubbleEffect.Stop();
        }
    }

    // ========== PRIVATE METHODS ==========

    private void CleanCurrentDish()
    {
        // Đổi material/visual cho chén sạch
        if (dishes != null && currentDishIndex < dishes.Length && dishes[currentDishIndex] != null)
        {
            Renderer renderer = dishes[currentDishIndex].GetComponent<Renderer>();
            if (renderer != null && cleanDishMaterial != null)
            {
                renderer.material = cleanDishMaterial;
            }

            // Có thể thêm animation hoặc đổi model ở đây
        }

        // Thưởng tiền
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(moneyPerDish);
        }

        OnDishCleaned?.Invoke(currentDishIndex);
        Debug.Log($"[DishWashing] Chén {currentDishIndex + 1}/{totalDishes} sạch rồi! +{moneyPerDish} đồng");

        // Chén tiếp theo
        currentDishIndex++;
        currentProgress = 0f;

        if (currentDishIndex >= totalDishes)
        {
            // Hoàn thành tất cả!
            CompleteGame();
        }
        else
        {
            // Update UI cho chén tiếp
            if (ProgressBarUI.Instance != null)
            {
                ProgressBarUI.Instance.SetProgress(0f);
                ProgressBarUI.Instance.UpdateLabel($"Chén {currentDishIndex + 1}/{totalDishes}");
            }
        }
    }

    private void CompleteGame()
    {
        isPlaying = false;

        // Ẩn progress bar
        if (ProgressBarUI.Instance != null)
            ProgressBarUI.Instance.Hide();

        // Stop effects
        if (bubbleEffect != null)
            bubbleEffect.Stop();

        int totalMoney = totalDishes * moneyPerDish;
        Debug.Log($"[DishWashing] ✅ Hoàn thành! Tổng thưởng: {totalMoney} đồng");

        OnGameCompleted?.Invoke(totalMoney);

        // Thu thập mảnh ký ức
        if (MemoryCollectionManager.Instance != null)
        {
            MemoryCollectionManager.Instance.CollectMemory(MemoryType.Mother);
        }

        // Hiện dialogue
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowDialogue(
                MemoryCollectionManager.GetMemoryName(MemoryType.Mother),
                MemoryCollectionManager.GetMemoryDescription(MemoryType.Mother),
                3f
            );
        }

        // Unlock player (sau khi dialogue hiện)
        StartCoroutine(UnlockPlayerAfterDelay(2f));
    }

    private IEnumerator UnlockPlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerController != null) playerController.LockMovement(false);
        if (playerInteraction != null) playerInteraction.LockInteraction(false);

        // Ẩn cursor (quay về FPS mode)
        GameManager.Instance?.SetCursorState(false);
    }
}
