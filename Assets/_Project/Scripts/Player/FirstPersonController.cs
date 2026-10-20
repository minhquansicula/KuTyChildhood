using UnityEngine;

/// <summary>
/// FirstPersonController — Điều khiển nhân vật góc nhìn thứ nhất.
/// 
/// Chức năng:
/// - Di chuyển WASD (CharacterController)
/// - Xoay camera theo chuột (Mouse Look)
/// - Chạy nhanh (giữ Shift)
/// - Gravity
/// 
/// Setup trong Unity:
/// 1. Tạo Empty GameObject "Player"
/// 2. Thêm CharacterController component
/// 3. Tạo Camera là child của Player
/// 4. Gán Camera vào playerCamera field
/// 5. Gán script này lên Player
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    // ========== SETTINGS ==========
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float gravity = -19.62f;   // -9.81 * 2 cho cảm giác nặng hơn
    [SerializeField] private bool enableJump;
    [SerializeField] private float jumpHeight = .85f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float maxLookAngle = 85f;  // Giới hạn nhìn lên/xuống
    [SerializeField] private Transform playerCamera;    // Camera con của Player

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;

    // ========== STATE ==========
    private CharacterController characterController;
    private Vector3 velocity;
    private float xRotation = 0f;   // Góc xoay dọc (nhìn lên/xuống)
    private bool isGrounded;

    /// <summary>
    /// Khi true, player không thể di chuyển hoặc xoay camera.
    /// Dùng khi mở UI, chơi mini-game, cutscene, v.v.
    /// </summary>
    public bool IsMovementLocked { get; set; } = false;

    /// <summary>
    /// Khi true, chỉ khóa di chuyển WASD và trọng lực, vẫn cho phép xoay camera nhìn quanh (khi ngồi ghế).
    /// </summary>
    public bool LockWalkingOnly { get; set; } = false;

    public Transform PlayerCamera => playerCamera;
    public float WaterSpeedMultiplier { get; set; } = 1f;
    public float VerticalSpeed => velocity.y;

    public void ResetCameraVerticalRotation(float angle = 0f)
    {
        xRotation = angle;
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Tự tìm camera nếu chưa gán
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>()?.transform;
            if (playerCamera == null)
            {
                Debug.LogError("[FirstPersonController] Không tìm thấy Camera! Hãy gán playerCamera.");
                enabled = false;
            }
        }
    }

    private void Update()
    {
        if (IsMovementLocked || (GameManager.Instance != null && GameManager.Instance.InputBlocked)) return;

        if (!LockWalkingOnly)
        {
            Vector2 movementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            MoveCharacter(movementInput, Input.GetKey(KeyCode.LeftShift), Time.deltaTime);
        }
        HandleMouseLook();
    }

    // ========== MOVEMENT ==========
    private void MoveCharacter(Vector2 input, bool running, float deltaTime)
    {
        HandleGroundCheck();

        // Tính hướng di chuyển theo hướng nhìn của player
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Chạy nhanh khi giữ Shift
        float currentSpeed = (running ? runSpeed : walkSpeed) * Mathf.Clamp(WaterSpeedMultiplier, .4f, 1f);

        // Tính trọng lực trước rồi chỉ Move một lần. Hai lần Move trong cùng frame
        // khiến CharacterController tự giải va chạm hai lần và làm camera nảy ở
        // mép collider/sàn dốc.
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (enableJump && isGrounded && velocity.y <= 0f && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(Mathf.Max(.1f, jumpHeight) * -2f * gravity);
        }

        velocity.y += gravity * deltaTime;

        Vector3 frameVelocity = moveDirection * currentSpeed;
        frameVelocity.y = velocity.y;
        CollisionFlags flags = characterController.Move(frameVelocity * deltaTime);
        if ((flags & CollisionFlags.Below) != 0 && velocity.y < 0f)
        {
            velocity.y = -2f;
            isGrounded = true;
        }
    }

#if UNITY_EDITOR
    public void SimulateMovementForTests(Vector2 input, bool running, float deltaTime)
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        MoveCharacter(input, running, deltaTime);
    }
#endif

    // ========== MOUSE LOOK ==========
    private void HandleMouseLook()
    {
        // Lấy input chuột
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.02f;

        // Xoay dọc (nhìn lên/xuống) — xoay camera, KHÔNG xoay body
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Xoay ngang (nhìn trái/phải) — xoay body (player transform)
        transform.Rotate(Vector3.up * mouseX);
    }


    // ========== GROUND CHECK ==========
    private void HandleGroundCheck()
    {
        if (characterController.isGrounded) { isGrounded = true; return; }
        // SphereCast xuống để check mặt đất
        isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * (characterController.radius + 0.05f),
            characterController.radius * 0.9f,
            Vector3.down,
            out _,
            groundCheckDistance + characterController.skinWidth,
            groundMask
        );
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Khóa/mở di chuyển và xoay camera.
    /// Gọi khi mở mini-game, shop UI, dialogue, v.v.
    /// </summary>
    public void LockMovement(bool locked)
    {
        IsMovementLocked = locked;
        if (locked) velocity = Vector3.zero;
    }

    /// <summary>
    /// Teleport player đến vị trí mới (dùng khi chuyển khu vực nếu cần).
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        velocity = Vector3.zero;
        xRotation = 0f;
        characterController.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        characterController.enabled = true;
    }

    public void SetPitch(float pitch)
    {
        xRotation = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}
