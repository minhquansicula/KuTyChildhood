using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MarbleShootingGame — Mini-game bắn bi.
/// 
/// Cách chơi:
/// - Camera chuyển sang góc top-down nhìn xuống sân bi
/// - Kéo chuột từ bi → hiện hướng + thanh lực
/// - Thả chuột → bi bay theo vật lý (Rigidbody.AddForce)
/// - Đẩy bi đối thủ ra ngoài vòng tròn
/// 
/// Setup trong Unity:
/// 1. Tạo sân bi (Plane với Physics Material smooth)
/// 2. Đặt bi người chơi (Sphere + Rigidbody + MarbleTag)
/// 3. Đặt 5 bi mục tiêu (Sphere + Rigidbody)
/// 4. Tạo vòng tròn (Cylinder collider, isTrigger = true)
/// 5. Gán references
/// </summary>
public class MarbleShootingGame : MonoBehaviour
{
    // ========== SETTINGS ==========
    [Header("Game Settings")]
    [SerializeField] private int targetMarblesToKnock = 3;  // Cần đẩy 3/5 bi ra ngoài
    [SerializeField] private int maxShots = 5;              // Giới hạn lượt bắn
    [SerializeField] private float minForce = 3f;
    [SerializeField] private float maxForce = 15f;

    [Header("References")]
    [SerializeField] private Rigidbody playerMarble;        // Bi người chơi
    [SerializeField] private List<Rigidbody> targetMarbles; // 5 bi mục tiêu
    [SerializeField] private Transform shootPosition;        // Vị trí đặt bi bắn
    [SerializeField] private Collider ringCollider;          // Vòng tròn (isTrigger)

    [Header("Camera")]
    [SerializeField] private Transform gameCameraPosition;   // Vị trí camera top-down
    [SerializeField] private Transform gameCameraLookAt;     // Điểm camera nhìn vào

    [Header("Aim Visual")]
    [SerializeField] private LineRenderer aimLine;           // Đường chỉ hướng bắn

    [Header("Player Reference")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    // ========== STATE ==========
    private bool isPlaying = false;
    private bool isAiming = false;
    private bool waitingForMarblesToStop = false;
    private int currentShot = 0;
    private int marblesKnocked = 0;
    private Vector3 dragStartPos;
    private Camera mainCamera;

    // Lưu camera transform ban đầu để restore
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform originalCameraParent;

    // Track bi nào đã ra ngoài
    private HashSet<Rigidbody> knockedMarbles = new HashSet<Rigidbody>();

    // ========== EVENTS ==========
    public System.Action<int, int> OnShotFired;          // currentShot, maxShots
    public System.Action<int, int> OnMarbleKnocked;      // marblesKnocked, targetNeeded
    public System.Action<bool> OnGameEnded;              // won?

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Bắt đầu mini-game. Gọi bởi MarbleInteractable.
    /// </summary>
    public void StartGame()
    {
        if (isPlaying) return;

        isPlaying = true;
        currentShot = 0;
        marblesKnocked = 0;
        knockedMarbles.Clear();

        mainCamera = Camera.main;

        // Lock player
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();
        if (playerInteraction == null)
            playerInteraction = FindObjectOfType<PlayerInteraction>();

        if (playerController != null) playerController.LockMovement(true);
        if (playerInteraction != null) playerInteraction.LockInteraction(true);

        // Chuyển camera sang top-down view
        SetupGameCamera();

        // Hiện cursor
        GameManager.Instance?.SetCursorState(true);

        // Reset bi player về vị trí bắn
        ResetPlayerMarble();

        // Update UI
        if (MarbleAimUI.Instance != null)
        {
            MarbleAimUI.Instance.Show(currentShot, maxShots, marblesKnocked, targetMarblesToKnock);
        }

        Debug.Log("[MarbleGame] Bắt đầu! Kéo chuột để nhắm, thả để bắn.");
    }

    // ========== LIFECYCLE ==========
    private void Update()
    {
        if (!isPlaying) return;

        if (waitingForMarblesToStop)
        {
            // Chờ tất cả bi dừng hẳn
            if (AllMarblesStopped())
            {
                waitingForMarblesToStop = false;
                OnAllMarblesStopped();
            }
            return;
        }

        HandleAiming();
    }

    // ========== AIMING & SHOOTING ==========

    private void HandleAiming()
    {
        // Bắt đầu kéo
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, playerMarble.transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                dragStartPos = ray.GetPoint(distance);
                isAiming = true;

                if (aimLine != null)
                    aimLine.enabled = true;
            }
        }

        // Đang kéo — update aim visual
        if (isAiming && Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, playerMarble.transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 currentDragPos = ray.GetPoint(distance);
                Vector3 dragVector = dragStartPos - currentDragPos;

                // Hướng bắn = ngược hướng kéo
                Vector3 shootDirection = dragVector.normalized;
                float forceMagnitude = Mathf.Clamp(dragVector.magnitude * 3f, minForce, maxForce);

                // Update aim line
                if (aimLine != null)
                {
                    aimLine.SetPosition(0, playerMarble.transform.position);
                    aimLine.SetPosition(1, playerMarble.transform.position + shootDirection * (forceMagnitude / maxForce) * 2f);
                }

                // Update UI lực
                float forcePercent = (forceMagnitude - minForce) / (maxForce - minForce);
                if (MarbleAimUI.Instance != null)
                    MarbleAimUI.Instance.UpdateForceIndicator(forcePercent);
            }
        }

        // Thả chuột — bắn!
        if (isAiming && Input.GetMouseButtonUp(0))
        {
            isAiming = false;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, playerMarble.transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 currentDragPos = ray.GetPoint(distance);
                Vector3 dragVector = dragStartPos - currentDragPos;

                Vector3 shootDirection = new Vector3(dragVector.x, 0f, dragVector.z).normalized;
                float forceMagnitude = Mathf.Clamp(dragVector.magnitude * 3f, minForce, maxForce);

                // Bắn!
                Shoot(shootDirection, forceMagnitude);
            }

            // Ẩn aim line
            if (aimLine != null)
                aimLine.enabled = false;
        }
    }

    private void Shoot(Vector3 direction, float force)
    {
        currentShot++;

        // Apply force
        playerMarble.AddForce(direction * force, ForceMode.Impulse);

        // Play SFX
        // AudioManager.Instance?.PlaySFX("marble_shoot");

        OnShotFired?.Invoke(currentShot, maxShots);
        Debug.Log($"[MarbleGame] Bắn lượt {currentShot}/{maxShots}, lực: {force:F1}");

        // Chờ bi dừng
        waitingForMarblesToStop = true;

        // Update UI
        if (MarbleAimUI.Instance != null)
            MarbleAimUI.Instance.UpdateShots(currentShot, maxShots);
    }

    // ========== MARBLE PHYSICS ==========

    private bool AllMarblesStopped()
    {
        float threshold = 0.05f;

        if (playerMarble.velocity.magnitude > threshold) return false;

        foreach (var marble in targetMarbles)
        {
            if (marble != null && marble.velocity.magnitude > threshold) return false;
        }

        return true;
    }

    private void OnAllMarblesStopped()
    {
        // Check bi nào đã ra ngoài vòng
        CheckKnockedMarbles();

        // Update UI
        if (MarbleAimUI.Instance != null)
            MarbleAimUI.Instance.UpdateKnocked(marblesKnocked, targetMarblesToKnock);

        // Kiểm tra kết quả
        if (marblesKnocked >= targetMarblesToKnock)
        {
            // THẮNG!
            WinGame();
            return;
        }

        if (currentShot >= maxShots)
        {
            // Hết lượt — THUA
            LoseGame();
            return;
        }

        // Còn lượt → reset bi player
        ResetPlayerMarble();
    }

    private void CheckKnockedMarbles()
    {
        foreach (var marble in targetMarbles)
        {
            if (marble == null) continue;
            if (knockedMarbles.Contains(marble)) continue;

            // Check bi có nằm ngoài vòng tròn không
            // Đơn giản: dùng khoảng cách từ tâm vòng
            if (ringCollider != null)
            {
                Vector3 ringCenter = ringCollider.bounds.center;
                float ringRadius = ringCollider.bounds.extents.x;
                float distFromCenter = Vector3.Distance(
                    new Vector3(marble.transform.position.x, 0, marble.transform.position.z),
                    new Vector3(ringCenter.x, 0, ringCenter.z)
                );

                if (distFromCenter > ringRadius)
                {
                    knockedMarbles.Add(marble);
                    marblesKnocked++;
                    OnMarbleKnocked?.Invoke(marblesKnocked, targetMarblesToKnock);
                    Debug.Log($"[MarbleGame] 🎯 Bi bị đẩy ra! ({marblesKnocked}/{targetMarblesToKnock})");
                }
            }
        }
    }

    private void ResetPlayerMarble()
    {
        if (playerMarble == null || shootPosition == null) return;

        playerMarble.velocity = Vector3.zero;
        playerMarble.angularVelocity = Vector3.zero;
        playerMarble.transform.position = shootPosition.position;
    }

    // ========== GAME END ==========

    private void WinGame()
    {
        Debug.Log("[MarbleGame] 🎉 THẮNG! Nhận mảnh ký ức về Bạn bè!");

        OnGameEnded?.Invoke(true);

        // Thu thập mảnh ký ức
        if (MemoryCollectionManager.Instance != null)
        {
            MemoryCollectionManager.Instance.CollectMemory(MemoryType.Friends);
        }

        // Hiện dialogue
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowDialogue(
                MemoryCollectionManager.GetMemoryName(MemoryType.Friends),
                MemoryCollectionManager.GetMemoryDescription(MemoryType.Friends),
                3f
            );
        }

        StartCoroutine(EndGameCoroutine());
    }

    private void LoseGame()
    {
        Debug.Log("[MarbleGame] Chưa đủ bi! Chơi lại nhé.");

        OnGameEnded?.Invoke(false);

        // Reset và cho chơi lại (không penalty)
        StartCoroutine(RetryCoroutine());
    }

    private IEnumerator RetryCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        // Reset tất cả bi về vị trí ban đầu
        currentShot = 0;
        marblesKnocked = 0;
        knockedMarbles.Clear();

        ResetPlayerMarble();
        // TODO: Reset target marbles về vị trí ban đầu (cần lưu positions lúc Start)

        if (MarbleAimUI.Instance != null)
            MarbleAimUI.Instance.Show(currentShot, maxShots, marblesKnocked, targetMarblesToKnock);

        Debug.Log("[MarbleGame] Chơi lại!");
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(2f);

        isPlaying = false;

        // Ẩn UI
        if (MarbleAimUI.Instance != null)
            MarbleAimUI.Instance.Hide();

        // Restore camera về FPS
        RestoreCamera();

        // Unlock player
        if (playerController != null) playerController.LockMovement(false);
        if (playerInteraction != null) playerInteraction.LockInteraction(false);

        GameManager.Instance?.SetCursorState(false);
    }

    // ========== CAMERA ==========

    private void SetupGameCamera()
    {
        if (mainCamera == null || gameCameraPosition == null) return;

        // Lưu vị trí ban đầu
        originalCameraParent = mainCamera.transform.parent;
        originalCameraPos = mainCamera.transform.localPosition;
        originalCameraRot = mainCamera.transform.localRotation;

        // Detach camera và di chuyển đến vị trí top-down
        mainCamera.transform.SetParent(null);
        mainCamera.transform.position = gameCameraPosition.position;

        if (gameCameraLookAt != null)
            mainCamera.transform.LookAt(gameCameraLookAt);
        else
            mainCamera.transform.rotation = gameCameraPosition.rotation;
    }

    private void RestoreCamera()
    {
        if (mainCamera == null) return;

        // Gắn lại camera về player
        mainCamera.transform.SetParent(originalCameraParent);
        mainCamera.transform.localPosition = originalCameraPos;
        mainCamera.transform.localRotation = originalCameraRot;
    }
}
