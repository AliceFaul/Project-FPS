using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_Text enemiesLeftText;
    [SerializeField] GameObject exitButton;
    [SerializeField] GameObject youWinText;
    [SerializeField] Door exitDoor;

    int enemiesLeft = 0;

    const string ENEMIES_LEFT_STRING = "Enemies left: ";
    const string PLAYER_STRING = "Player";

    public void AdjustEnemiesLeft(int amount)
    {
        enemiesLeft += amount;
        enemiesLeftText.text = ENEMIES_LEFT_STRING + enemiesLeft.ToString();

        if (enemiesLeft <= 0)
        {
            youWinText.SetActive(true);
            exitDoor.UnlockDoor();
        }
    }

    public void RestartLevelButton()
    {
        var runnerManager = FindFirstObjectByType<NetworkRunnerManager>();
        if (runnerManager != null)
        {
            runnerManager.RestartCurrentScene();
            return;
        }

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);
    }

    public void QuitButton()
    {
        Debug.LogWarning("Does not work in Unity Editor");
        Application.Quit();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            youWinText.GetComponent<TMP_Text>().text = "You win!";
            other.GetComponentInParent<FirstPersonController>().enabled = false;
            StarterAssetsInputs starterAssetsInputs = FindAnyObjectByType<StarterAssetsInputs>();
            starterAssetsInputs.SetCursorState(false);
            exitButton.SetActive(true);
        }
    }
}
