using Fusion;
using UnityEngine;

public abstract class Pickup : NetworkBehaviour
{
    [SerializeField] protected AudioClip pickupClip;
    [SerializeField] string notificationText = "Notification";
    [SerializeField] float rotationSpeed = 100f;

    const string PLAYER_STRING = "Player";
    bool isPicked = false;

    public override void FixedUpdateNetwork() {
        transform.Rotate(0f, rotationSpeed * Runner.DeltaTime, 0f);
    }

    void OnTriggerEnter(Collider other) {
        if(!Object.HasStateAuthority) { 
            return;
        }
        if(isPicked) {
            return;
        }
        if (other.CompareTag(PLAYER_STRING)) {
            OnPickup(other);
            isPicked = true;
        }
    }

    protected void Notification()
    {
        NotificationManager.instance.FireNotification(notificationText);
    }

    protected abstract void OnPickup(Collider other);
}