using UnityEngine;

public class AmmoPickup : Pickup
{
    [SerializeField] WeaponSO[] weapons4AmmoGain;

    [SerializeField] float ammoPortion = .7f;

    protected override void OnPickup(Collider other)
    {
        ActiveWeapon activeWeapon = other.GetComponentInChildren<ActiveWeapon>();
        foreach(WeaponSO weaponSO in weapons4AmmoGain)
        {
            if (activeWeapon.CurrentAmmoList[weaponSO.ID] < activeWeapon.MaxAmmoList[weaponSO.ID] && weaponSO.PickedUp)
            {
                activeWeapon.AdjustAmmo(weaponSO, ammoPortion);
                SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
                Destroy(gameObject);
            }
        }
    }
} 