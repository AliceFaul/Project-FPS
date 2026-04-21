using UnityEngine;

public class AmmoPickup : Pickup
{
    [SerializeField] WeaponSO[] weapons4AmmoGain;

    [SerializeField] float ammoPortion = .7f;

    protected override void OnPickup(Collider other)
    {
        if(!TryGetPlayerSetup(other, out PlayerNetworkSetup playerSetup)) {
            return;
        }

        foreach(WeaponSO weaponSO in weapons4AmmoGain) {
            if(weaponSO == null) {
                continue;
            }

            playerSetup.GrantAmmoPickup(weaponSO.ID, ammoPortion);
        }

        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }
} 
