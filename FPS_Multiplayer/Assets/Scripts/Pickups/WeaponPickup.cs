using UnityEngine;

public class WeaponPickup : Pickup
{
    [SerializeField] WeaponSO weaponSO;

    protected override void OnPickup(Collider other)
    {
        if(weaponSO == null) {
            return;
        }

        if(!TryGetPlayerSetup(other, out PlayerNetworkSetup playerSetup)) {
            return;
        }

        playerSetup.GrantWeaponPickup(weaponSO.ID, weaponSO.ammoOnPickup);
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }
}
