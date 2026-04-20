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

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        //GetComponent<EnemyHealth>().Init(gameManager);
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
        if(!Object.HasStateAuthority) {
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
        newEnemy.GetComponent<Robot>().Init(player, gameManager, this);
        currentRobotCount++;
    }

    private PlayerHealth GetClosestPlayer() { 
        if(Runner == null) { 
            return null;
        }
        PlayerHealth closest = null;
        float minDist = Mathf.Infinity;

        foreach(var player in Runner.ActivePlayers) {
            var obj = Runner.GetPlayerObject(player);
            if(obj == null) continue;
            var health = obj.GetComponent<PlayerHealth>();
            if(health == null) continue;
            float dist = Vector3.Distance(transform.position, health.transform.position);
            if(dist < minDist) { 
                minDist = dist;
                closest = health;
            }
        }
        return closest;
    }
}
