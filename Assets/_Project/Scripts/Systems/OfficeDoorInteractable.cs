using System;
using UnityEngine;

public sealed class OfficeDoorInteractable : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open }
    public BoxCollider leaf;
    public float openAngle = 95, degreesPerSecond = 85;
    public AudioSource sound;
    public DoorState State { get; private set; }
    public event Action Opened;
    Quaternion closed;
    float angle;
    public void Initialize() { closed = transform.localRotation; angle = 0; State = DoorState.Closed; }

    // Sample the entire remaining sweep, inflated for the player's capsule.
    // Test against the player rather than moving a solid door through them.
    public bool PlayerBlocksSweep(CharacterController player)
    {
        if (!player || !leaf) return true;
        Vector3 playerCenter = player.transform.TransformPoint(player.center);
        Vector3 scale = leaf.transform.lossyScale;
        Vector3 localCenter = transform.InverseTransformPoint(leaf.transform.TransformPoint(leaf.center));
        Vector3 half = Vector3.Scale(leaf.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * .5f;
        if (Mathf.Abs(playerCenter.y - leaf.bounds.center.y) > half.y + player.height * .5f) return false;
        for (float sample = angle; sample <= openAngle + 4; sample += 4)
        {
            Quaternion delta = Quaternion.Euler(0, Mathf.Min(sample, openAngle) - angle, 0);
            Vector3 center = transform.TransformPoint(delta * localCenter);
            Quaternion orientation = transform.rotation * delta;
            Vector3 relative = Quaternion.Inverse(orientation) * (playerCenter - center);
            if (Mathf.Abs(relative.x) < half.x + player.radius + .06f &&
                Mathf.Abs(relative.z) < half.z + player.radius + .06f) return true;
        }
        return false;
    }
    public bool TryOpen(CharacterController player)
    {
        if (State != DoorState.Closed || PlayerBlocksSweep(player)) return false;
        State = DoorState.Opening;
        if (sound && sound.clip) sound.Play();
        return true;
    }
    public void Tick(float dt, CharacterController player)
    {
        if (State != DoorState.Opening || PlayerBlocksSweep(player)) return;
        angle = Mathf.MoveTowards(angle, openAngle, degreesPerSecond * dt);
        transform.localRotation = closed * Quaternion.Euler(0, angle, 0);
        if (Mathf.Approximately(angle, openAngle)) { State = DoorState.Open; Opened?.Invoke(); }
    }
}
