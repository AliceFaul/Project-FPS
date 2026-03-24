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

    const string PLAYER_STRING = "Player";

    int currentRobotCount = 0;
    public bool IsActive { get; set; }

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        gameManager = FindFirstObjectByType<GameManager>();
        GetComponent<EnemyHealth>().Init(gameManager);
        StartCoroutine(SpawnEnemiesCoroutine());
        isActive = false;
    }

    IEnumerator SpawnEnemiesCoroutine()
    {
        while (player)
        {
            yield return new WaitForSeconds(enemySpawnInterval);
            if (isActive && currentRobotCount < maxRobotCount)
            {
                SoundFXManager.instance.PlaySoundFX(spawnRobotClip,transform);
                GameObject newEnemy = Instantiate(enemy, transform.position, transform.rotation);
                newEnemy.GetComponent<Robot>().Init(player,gameManager,this);
                currentRobotCount++;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(PLAYER_STRING)) {isActive = true;}
    }

    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag(PLAYER_STRING) && canBeReset) {isActive = false;}
    }

    public void ChildRobotDestroyed()
    {
        currentRobotCount--;
    }
}