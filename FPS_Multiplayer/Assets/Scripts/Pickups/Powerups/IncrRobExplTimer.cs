using Fusion;
using UnityEngine;

public class IncrRobExplTimer : Pickup
{
    protected override void OnPickup(Collider other)
    {
        RPC_IncreaseRobotExplodeTimer();
        Notification();
        SoundFXManager.instance.PlaySoundFX(pickupClip, other.transform);
        ConsumePickup();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_IncreaseRobotExplodeTimer() {
        Robot.IncreaseExplTimer();
    }
}
