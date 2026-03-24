using UnityEngine;

public class Hazard : MonoBehaviour
{
    const string PLAYER_STRING = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            other.GetComponent<PlayerHealth>().AdjustHealth(-99);
        }
    }
}