using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

public class GameManager : NetworkBehaviour
{
    [SerializeField] TMP_Text enemiesLeftText;
    [SerializeField] GameObject exitButton;
    [SerializeField] GameObject youWinText;
    [SerializeField] Door exitDoor;

    [Networked] public int enemiesLeft { get; set; }

    const string ENEMIES_LEFT_STRING = "Enemies left: ";
    const string PLAYER_STRING = "Player";

    public override void Spawned() {
        if(Object.HasStateAuthority) { 
            enemiesLeft = 0;
        }
    }

    public override void Render() {
        enemiesLeftText.text = ENEMIES_LEFT_STRING + enemiesLeft.ToString();
    }

    public void AdjustEnemiesLeft(int amount)
    {
        if(Object == null || !Object.HasStateAuthority) {
            return;
        }

        int previousEnemiesLeft = enemiesLeft;
        enemiesLeft = Mathf.Max(0, enemiesLeft + amount);

        if (previousEnemiesLeft > 0 && enemiesLeft <= 0)
        {
            RPC_ShowWinText();
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowWinText() {
        youWinText.SetActive(true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayerWin(NetworkObject playerObj) {
        youWinText.GetComponent<TMP_Text>().text = "You win!";
        FusionPlayerController fusionPlayerController = playerObj.GetComponentInParent<FusionPlayerController>();
        FirstPersonController firstPersonController = playerObj.GetComponentInParent<FirstPersonController>();
        if (fusionPlayerController != null) {
            fusionPlayerController.enabled = false;
        } else if (firstPersonController != null) {
            firstPersonController.enabled = false;
        }
        StarterAssetsInputs starterAssetsInputs = FindAnyObjectByType<StarterAssetsInputs>();
        starterAssetsInputs?.SetCursorState(false);
        exitButton.SetActive(true);
    }

    void OnTriggerEnter(Collider other) {
        if(!Object.HasStateAuthority) {
            return;
        }
        if (other.CompareTag(PLAYER_STRING)) {
            RPC_PlayerWin(other.GetComponentInParent<NetworkObject>());
        }
    }
}
