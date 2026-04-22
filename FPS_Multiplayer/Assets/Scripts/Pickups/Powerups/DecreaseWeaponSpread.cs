using UnityEngine;
using Fusion;

public class DecreaseWeaponSpread : Pickup
{
    [SerializeField] float decreaseAmount = .2f;
 
    protected override void OnPickup(Collider other)
    {
        if(!TryGetPlayerSetup(other, out PlayerNetworkSetup playerSetup)) {
            return;
        }

        playerSetup.GrantDecreaseSpread(decreaseAmount);
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }
}
