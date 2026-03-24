using UnityEngine;

public class DecreaseWeaponSpread : Pickup
{
    [SerializeField] float decreaseAmount = .2f;
 
    protected override void OnPickup(Collider other)
    {
        other.GetComponentInChildren<Weapon>().DecreaseWeaponSpread(decreaseAmount);
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        Destroy(gameObject);
    }
}
