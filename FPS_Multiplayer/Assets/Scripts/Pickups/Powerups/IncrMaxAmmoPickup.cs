using UnityEngine;

public class IncrMaxAmmoPickup : Pickup
{
    protected override void OnPickup(Collider other)
    {
        if(!TryGetPlayerSetup(other, out PlayerNetworkSetup playerSetup)) {
            return;
        }

        playerSetup.GrantIncreaseMaxAmmo();
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }
}
