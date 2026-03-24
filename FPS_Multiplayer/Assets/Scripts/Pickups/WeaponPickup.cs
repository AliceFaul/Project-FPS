using UnityEngine;

public class WeaponPickup : Pickup
{
    [SerializeField] WeaponSO weaponSO;

    protected override void OnPickup(Collider other)
    {
        ActiveWeapon activeWeapon = other.GetComponentInChildren<ActiveWeapon>();
        activeWeapon.SwitchWeapon(weaponSO);
        activeWeapon.AdjustAmmo(weaponSO.ammoOnPickup);
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        Destroy(gameObject);
    }
}