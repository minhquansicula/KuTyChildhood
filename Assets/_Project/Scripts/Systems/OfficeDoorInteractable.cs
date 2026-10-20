using System;
using UnityEngine;

public sealed class OfficeDoorInteractable : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open, Closing }
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
    public bool PlayerBlocksSweep(CharacterController player) => PlayerBlocksSweepTo(player, openAngle);

    public bool PlayerBlocksSweepTo(CharacterController player, float targetAngle)
    {
        if (!player || !leaf) return true;
        Vector3 playerCenter = player.transform.TransformPoint(player.center);
        Vector3 scale = leaf.transform.lossyScale;
        Vector3 localCenter = transform.InverseTransformPoint(leaf.transform.TransformPoint(leaf.center));
        Vector3 half = Vector3.Scale(leaf.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * .5f;
        if (Mathf.Abs(playerCenter.y - leaf.bounds.center.y) > half.y + player.height * .5f) return false;
        int samples = Mathf.CeilToInt(Mathf.Abs(targetAngle - angle) / 4f);
        for (int i = 0; i <= samples; i++)
        {
            float sample = Mathf.MoveTowards(angle, targetAngle, i * 4f);
            Quaternion delta = Quaternion.Euler(0, sample - angle, 0);
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
    public bool TryClose(CharacterController player)
    {
        if (State != DoorState.Open || PlayerBlocksSweepTo(player, 0f)) return false;
        State = DoorState.Closing;
        if (sound && sound.clip) sound.Play();
        return true;
    }
    public void Tick(float dt, CharacterController player)
    {
        if (State != DoorState.Opening && State != DoorState.Closing) return;
        float targetAngle = State == DoorState.Opening ? openAngle : 0f;
        if (PlayerBlocksSweepTo(player, targetAngle)) return;
        angle = Mathf.MoveTowards(angle, targetAngle, degreesPerSecond * dt);
        transform.localRotation = closed * Quaternion.Euler(0, angle, 0);
        if (Mathf.Approximately(angle, targetAngle))
        {
            State = targetAngle == 0f ? DoorState.Closed : DoorState.Open;
            if (State == DoorState.Open) Opened?.Invoke();
        }
    }
}
