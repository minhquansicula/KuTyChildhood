using UnityEngine;

/// <summary>One reversible camera/input lease for a world-space household activity.</summary>
public sealed class HouseholdWorkSession
{
    private readonly Object owner;
    private readonly GameManager manager;
    private readonly Camera camera;
    private readonly Transform parent;
    private readonly Vector3 position;
    private readonly Quaternion rotation;
    private readonly float fov, near;
    private readonly bool crosshair;
    private readonly CursorLockMode cursorLock;
    private readonly bool cursorVisible;
    private readonly Vector3 startPosition;
    private readonly Quaternion startRotation;
    private bool closed;
    public Camera Camera => camera;

    public HouseholdWorkSession(Object owner, Camera camera)
    {
        this.owner = owner;
        this.camera = camera;
        manager = GameManager.Instance;
        parent = camera.transform.parent;
        position = camera.transform.localPosition;
        rotation = camera.transform.localRotation;
        startPosition = camera.transform.position;
        startRotation = camera.transform.rotation;
        fov = camera.fieldOfView;
        near = camera.nearClipPlane;
        crosshair = HUDController.Instance && HUDController.Instance.CrosshairVisible;
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        if (manager) manager.AcquireInput(owner);
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        HUDController.Instance?.SetCrosshairVisible(false);
    }

    public void MoveTo(Vector3 destination, Quaternion facing, float fraction)
    {
        if (closed || !camera) return;
        float t = Mathf.SmoothStep(0, 1, fraction);
        camera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, destination, t),
            Quaternion.Slerp(startRotation, facing, t));
        camera.fieldOfView = Mathf.Lerp(fov, 48f, t);
        camera.nearClipPlane = Mathf.Min(near, .025f);
    }

    public void Close()
    {
        if (closed) return;
        closed = true;
        if (camera)
        {
            camera.transform.SetParent(parent, false);
            camera.transform.localPosition = position;
            camera.transform.localRotation = rotation;
            camera.fieldOfView = fov;
            camera.nearClipPlane = near;
        }
        if (manager) manager.ReleaseInput(owner);
        else { Cursor.lockState = cursorLock; Cursor.visible = cursorVisible; }
        HUDController.Instance?.SetCrosshairVisible(crosshair);
    }
}
