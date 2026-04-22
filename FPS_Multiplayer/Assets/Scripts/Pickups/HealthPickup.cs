using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField] int healthGain = 3;

    protected override void OnPickup(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.CurrentHealth < playerHealth.StartHealth)
        {
            playerHealth.AdjustHealth(healthGain);
            SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
            ConsumePickup();
        }
    }
}
