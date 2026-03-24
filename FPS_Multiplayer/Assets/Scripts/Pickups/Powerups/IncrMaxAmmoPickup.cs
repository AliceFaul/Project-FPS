using UnityEngine;

public class IncrMaxAmmoPickup : Pickup
{
    protected override void OnPickup(Collider other)
    {
        other.GetComponentInChildren<ActiveWeapon>().IncreaseMaxAmmo();
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        Destroy(gameObject);
    }
}
