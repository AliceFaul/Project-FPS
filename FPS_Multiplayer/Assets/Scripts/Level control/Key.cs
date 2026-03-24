using UnityEngine;

public class Key : Pickup
{
    [SerializeField] Door door;

    protected override void OnPickup(Collider other)
    {
        if (door)
        {
            door.UnlockDoor();
        }
        else Debug.Log("No door connected to key");
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        Destroy(gameObject);
    }
}
