using UnityEngine;

/// <summary>Standalone navigation preview for the replaceable office blockout.</summary>
public class OfficeBlockoutWalkthrough : MonoBehaviour
{
    public Transform doorPivot;
    public Transform seat;
    public Transform chairRoot;
    public Camera view;
    public FirstPersonController movement;
    private bool doorOpen, seated;
    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private string prompt = "";

    void Start() { LockCursor(true); }
    void OnDisable() { LockCursor(false); }
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        if (movement != null) movement.IsMovementLocked = !locked;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(Cursor.lockState != CursorLockMode.Locked);
        if (doorPivot != null)
            doorPivot.localRotation = Quaternion.RotateTowards(doorPivot.localRotation,
                Quaternion.Euler(0, doorOpen ? 95 : 0, 0), 100 * Time.deltaTime);
        prompt = "";
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (seated)
        {
            prompt = "[E] Stand up";
            if (Input.GetKeyDown(KeyCode.E)) StandUp();
            return;
        }
        if (!Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, 2.7f)) return;
        if (doorPivot != null && hit.transform.IsChildOf(doorPivot))
        {
            prompt = doorOpen ? "[E] Close door" : "[E] Open door";
            if (Input.GetKeyDown(KeyCode.E)) doorOpen = !doorOpen;
        }
        else if (chairRoot != null && hit.transform.IsChildOf(chairRoot))
        {
            prompt = "[E] Sit at your desk";
            if (Input.GetKeyDown(KeyCode.E)) SitDown();
        }
    }
    public void SitDown()
    {
        if (seated) return;
        standingPosition = transform.position;
        standingRotation = transform.rotation;
        var controller = GetComponent<CharacterController>();
        controller.enabled = false;
        transform.SetPositionAndRotation(seat.position, seat.rotation);
        view.transform.localPosition = new Vector3(0, 1.2f, 0);
        movement.ResetCameraVerticalRotation(8);
        movement.LockWalkingOnly = true;
        seated = true;
    }
    public void StandUp()
    {
        if (!seated) return;
        transform.SetPositionAndRotation(standingPosition, standingRotation);
        view.transform.localPosition = new Vector3(0, 1.65f, 0);
        movement.ResetCameraVerticalRotation();
        movement.LockWalkingOnly = false;
        GetComponent<CharacterController>().enabled = true;
        seated = false;
    }
    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(20, 20, 430, 64), "OFFICE / SPATIAL BLOCKOUT\nWASD: move   Mouse: look   E: interact   Esc: cursor");
        GUI.Label(new Rect(Screen.width / 2f - 10, Screen.height / 2f - 12, 20, 24), "+", style);
        if (prompt.Length > 0) GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height - 90, 300, 36), prompt);
    }
}
