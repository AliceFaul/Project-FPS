using UnityEngine;

public class Hazard : MonoBehaviour
{
    const string PLAYER_STRING = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            playerHealth?.AdjustHealth(-99);
        }
    }
}
