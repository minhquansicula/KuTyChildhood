using UnityEngine;

public class RiceWashingTask : InteractableBase
{
    private int state = 0; // 0: Idle, 1: Drawing circles, 2: Pouring water, 3: Done waiting for Esc
    private float accumulatedAngle = 0f;
    private float previousAngle = 0f;
    private int targetCircles = 3;

    private void Start()
    {
        isOneTimeUse = false;
        promptText = "[E] Vo gạo";
    }

    protected override void OnInteract()
    {
        if (state != 0) return;

        state = 1;
        accumulatedAngle = 0f;
        
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePos = Input.mousePosition;
        previousAngle = Mathf.Atan2(mousePos.y - center.y, mousePos.x - center.x) * Mathf.Rad2Deg;
        
        GameManager.Instance?.AcquireInput(this);
        
        ProgressBarUI.Instance?.Show("Dùng chuột xoay tròn 3 vòng để vo gạo, Esc: Hủy");
        ProgressBarUI.Instance?.SetProgress(0f);
    }

    private void Update()
    {
        if (state == 0) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == 3) FinishTask();
            else CancelTask();
            return;
        }

        if (state == 1) // Vo gạo (xoay chuột)
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = Input.mousePosition;
            float currentAngle = Mathf.Atan2(mousePos.y - center.y, mousePos.x - center.x) * Mathf.Rad2Deg;
            
            float delta = Mathf.DeltaAngle(previousAngle, currentAngle);
            accumulatedAngle += delta;
            previousAngle = currentAngle;

            float progress = Mathf.Clamp01(Mathf.Abs(accumulatedAngle) / (360f * targetCircles));
            ProgressBarUI.Instance?.SetProgress(progress);

            if (progress >= 1f)
            {
                state = 2;
                ProgressBarUI.Instance?.Show("Gạo đã sạch! Bấm CHUỘT PHẢI để đổ nước đục");
            }
        }
        else if (state == 2) // Đổ nước
        {
            if (Input.GetMouseButtonDown(1)) // Right click
            {
                state = 3;
                ProgressBarUI.Instance?.Show("Vo gạo xong! Bấm ESC để thoát và đi lấy nước mới");
                AudioManager.Instance?.PlaySFX("ui_select");
            }
        }
    }

    private void CancelTask()
    {
        state = 0;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }

    private void FinishTask()
    {
        state = 0;
        hasBeenUsed = true;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
        Debug.Log("Vo gạo xong!");
        
        KitchenFlowManager.Instance?.MarkTaskCompleted(transform.parent != null ? transform.parent.name : gameObject.name);
    }
}
