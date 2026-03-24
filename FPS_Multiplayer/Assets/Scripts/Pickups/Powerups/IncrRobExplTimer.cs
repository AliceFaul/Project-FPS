using UnityEngine;

public class IncrRobExplTimer : Pickup
{
    protected override void OnPickup(Collider other)
    {
        Robot.IncreaseExplTimer();
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        Destroy(gameObject);
    }
}