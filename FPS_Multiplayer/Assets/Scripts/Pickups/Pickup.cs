using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    [SerializeField] protected AudioClip pickupClip;
    [SerializeField] string notificationText = "Notification";
    [SerializeField] float rotationSpeed = 100f;

    const string PLAYER_STRING = "Player";

    void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            OnPickup(other);
        }
    }

    protected void Notification()
    {
        NotificationManager.instance.FireNotification(notificationText);
    }

    protected abstract void OnPickup(Collider other);
}