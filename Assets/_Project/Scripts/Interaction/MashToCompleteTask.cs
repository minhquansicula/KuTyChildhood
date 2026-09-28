using UnityEngine;

public class MashToCompleteTask : InteractableBase
{
    [Header("Mash Task Settings")]
    public int requiredPresses = 10;
    public string taskName = "Đang làm nhiệm vụ...";
    public string completedPrompt = "[E] Đã xong";
    public KeyCode mashKey = KeyCode.Space;

    private int currentPresses = 0;
    private bool isMashing = false;

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
        if (isMashing) return;

        isMashing = true;
        currentPresses = 0;
        
        GameManager.Instance?.AcquireInput(this);
        
        ProgressBarUI.Instance?.Show($"{taskName} — Nháy {GetKeyName(mashKey)} liên tục, Esc: Hủy");
        ProgressBarUI.Instance?.SetProgress(0f);
    }

    private void Update()
    {
        if (!isMashing) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTask();
            return;
        }

        if (Input.GetKeyDown(mashKey))
        {
            currentPresses++;
            float progress = Mathf.Clamp01((float)currentPresses / requiredPresses);
            ProgressBarUI.Instance?.SetProgress(progress);

            if (currentPresses >= requiredPresses)
            {
                CompleteTask();
            }
        }
    }

    private void CancelTask()
    {
        isMashing = false;
        currentPresses = 0;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }

    private void CompleteTask()
    {
        isMashing = false;
        hasBeenUsed = true;
        
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
        
        Debug.Log(taskName + " - Hoàn thành!");
        AudioManager.Instance?.PlaySFX("ui_select");
        
        // Update checklist
        KitchenFlowManager.Instance?.MarkTaskCompleted(transform.parent != null ? transform.parent.name : gameObject.name);
    }
}
