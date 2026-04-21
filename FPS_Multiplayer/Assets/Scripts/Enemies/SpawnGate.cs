using System.Collections;
using Fusion;
using UnityEngine;

public class SpawnGate : NetworkBehaviour {
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

    public override void Spawned() {
        gameManager = FindFirstObjectByType<GameManager>();
        isActive = false;

        if(Object != null && Object.HasStateAuthority) {
            StartCoroutine(SpawnEnemiesCoroutine());
        }
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
        if(Object == null || !Object.HasStateAuthority) {
            return;
        }

        if(other.GetComponentInParent<PlayerHealth>() != null) {
            player = other.GetComponentInParent<PlayerHealth>();

            if(isActive) {
                return;
            }

            isActive = true;

            if (currentRobotCount < maxRobotCount) {
                SpawnEnemy();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(Object == null || !Object.HasStateAuthority) {
            return;
        }

        if(other.GetComponentInParent<PlayerHealth>() != null && canBeReset) {
            isActive = false;
        }
    }

    public void ChildRobotDestroyed()
    {
        currentRobotCount = Mathf.Max(0, currentRobotCount - 1);
    }

    private void SpawnEnemy() {
        if(Object == null || !Object.HasStateAuthority) {
            return;
        }
        if (enemy == null) {
            return;
        }
        if(player == null) {
            player = GetClosestPlayer();
        }
        if(gameManager == null) { 
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (player == null || gameManager == null) {
            Debug.LogError("[SpawnGate]: Missing player or gameManager");
            return;
        }

        SoundFXManager.instance.PlaySoundFX(spawnRobotClip, transform);
        var newEnemy = Runner.Spawn(enemy, transform.position, transform.rotation);
        if(newEnemy == null) {
            return;
        }

        Robot robot = newEnemy.GetComponent<Robot>();
        if(robot == null) {
            Debug.LogError("[SpawnGate]: Spawned enemy is missing Robot component");
            return;
        }

        robot.Init(player, gameManager, this);
        currentRobotCount++;
    }

    // helper method get closest player in game
    private PlayerHealth GetClosestPlayer() {
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        PlayerHealth closest = null;
        var minDist = Mathf.Infinity;

        foreach(var player in players) {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if(dist < minDist) { 
                minDist = dist;
                closest = player;
            }
        }
        return closest;
    }
}
