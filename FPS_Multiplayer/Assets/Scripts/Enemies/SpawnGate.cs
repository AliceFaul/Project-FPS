using System.Collections;
using UnityEngine;

public class SpawnGate : MonoBehaviour
{
    [SerializeField] GameObject enemy;
    [SerializeField] AudioClip spawnRobotClip;
    [SerializeField] float enemySpawnInterval = 5f;
    [SerializeField] bool isActive = true;
    [SerializeField] bool canBeReset = false;
    [SerializeField] int maxRobotCount = 3;

    GameManager gameManager;
    PlayerHealth player;

    int currentRobotCount = 0;
    public bool IsActive { get; set; }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        GetComponent<EnemyHealth>().Init(gameManager);
        StartCoroutine(SpawnEnemiesCoroutine());
        isActive = false;
    }

    IEnumerator SpawnEnemiesCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(enemySpawnInterval);

            if (player == null) {
                player = FindFirstObjectByType<PlayerHealth>();
            }

            if (player == null || gameManager == null) {
                continue;
            }

            if (isActive && currentRobotCount < maxRobotCount)
            {
                SpawnEnemy();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentInParent<PlayerHealth>() != null) {
            player = other.GetComponentInParent<PlayerHealth>();
            isActive = true;

            if (currentRobotCount < maxRobotCount) {
                SpawnEnemy();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(other.GetComponentInParent<PlayerHealth>() != null && canBeReset) {
            isActive = false;
        }
    }

    public void ChildRobotDestroyed()
    {
        currentRobotCount--;
    }

    private void SpawnEnemy() {
        if (enemy == null || player == null || gameManager == null) {
            return;
        }

        SoundFXManager.instance.PlaySoundFX(spawnRobotClip, transform);
        GameObject newEnemy = Instantiate(enemy, transform.position, transform.rotation);
        newEnemy.GetComponent<Robot>().Init(player, gameManager, this);
        currentRobotCount++;
    }
}
