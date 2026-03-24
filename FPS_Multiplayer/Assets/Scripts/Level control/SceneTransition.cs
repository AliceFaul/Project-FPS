using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    const string PLAYER_STRING = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            other.GetComponentInChildren<ActiveWeapon>().Checkpoint();
            other.GetComponent<PlayerHealth>().LoadHealth();
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(++sceneIndex);
        }
    }
}
