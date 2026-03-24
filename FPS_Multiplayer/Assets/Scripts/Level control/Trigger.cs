using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] GameObject[] gameObjects;

    const string PLAYER_STRING = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            foreach (GameObject go in gameObjects)
            {
                go.SetActive(true);
            }
            Destroy(this);
        }
    }
}
