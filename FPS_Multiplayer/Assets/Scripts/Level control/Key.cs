using Fusion;
using UnityEngine;

public class Key : Pickup
{
    [SerializeField] Door door;

    protected override void OnPickup(Collider other)
    {
        RPC_UnlockDoor();
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UnlockDoor() {
        if (door)
        {
            door.UnlockDoor();
        }
        else Debug.Log("No door connected to key");
    }
}
