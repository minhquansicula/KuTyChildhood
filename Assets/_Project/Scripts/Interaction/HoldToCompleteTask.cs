using UnityEngine;

public class HoldToCompleteTask : InteractableBase
{
    [Header("Hold Task Settings")]
    public float holdDuration = 3f;
    public string taskName = "Đang làm nhiệm vụ...";
    public string completedPrompt = "[E] Đã xong";
    public KeyCode holdKey = KeyCode.Mouse0; // Default to Left Mouse Click

    private float currentHoldTime = 0f;
    private bool isHolding = false;

    private void Start()
    {
        isOneTimeUse = false;
    }

    private string GetKeyName(KeyCode k)
    {
        if (k == KeyCode.Mouse0) return "Chuột Trái";
        if (k == KeyCode.Mouse1) return "Chuột Phải";
        return k.ToString();
    }

    protected override void OnInteract()
    {
        if (isHolding) return;

        // Bắt đầu task
        isHolding = true;
        currentHoldTime = 0f;
        
        // Khóa input di chuyển
        GameManager.Instance?.AcquireInput(this);
        
        // Hiển thị UI tiến độ
        ProgressBarUI.Instance?.Show($"{taskName} — Giữ {GetKeyName(holdKey)}, Esc: Hủy");
        ProgressBarUI.Instance?.SetProgress(0f);
    }

    private void Update()
    {
        if (!isHolding) return;

        // Nếu bấm Esc thì hủy
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTask();
            return;
        }

        // Tích lũy thời gian giữ phím
        if (Input.GetKey(holdKey))
        {
            currentHoldTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentHoldTime / holdDuration);
            ProgressBarUI.Instance?.SetProgress(progress);

            if (currentHoldTime >= holdDuration)
            {
                CompleteTask();
            }
        }
    }

    private void CancelTask()
    {
        isHolding = false;
        currentHoldTime = 0f;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }

    private void CompleteTask()
    {
        isHolding = false;
        hasBeenUsed = true;
        
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
        
        Debug.Log(taskName + " - Hoàn thành!");
        // Play success sound...
        AudioManager.Instance?.PlaySFX("ui_select");
        
        // Update checklist
        KitchenFlowManager.Instance?.MarkTaskCompleted(transform.parent != null ? transform.parent.name : gameObject.name);
    }
}
