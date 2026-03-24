using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Robot : MonoBehaviour
{
    [SerializeField] GameObject robotExplosionVFX;
    [SerializeField] AudioClip explosionTimerClip;
    [SerializeField] bool isSpawnedByGate = true;

    static float explodeTimer = 1.5f;

    SpawnGate parentGate = null;
    PlayerHealth player;
    NavMeshAgent agent;
    GameManager gameManager;

    bool initializedSelfDestruct = false;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (!isSpawnedByGate)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            gameManager = FindFirstObjectByType<GameManager>();
            GetComponent<EnemyHealth>().Init(gameManager);
        }
    }

    void Update()
    {
        if (!player) return;
        agent.SetDestination(player.transform.position);
    }

    public void Init(PlayerHealth player, GameManager gameManager, SpawnGate parentGate)
    {
        this.parentGate = parentGate;
        this.player = player;
        this.gameManager = gameManager;
        GetComponent<EnemyHealth>().Init(gameManager, parentGate);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING) && !initializedSelfDestruct)
        {
            initializedSelfDestruct = true;
            StartCoroutine(SelfDestructRoutine());
        }
    }

    IEnumerator SelfDestructRoutine()
    {
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        SoundFXManager.instance.PlaySoundFX(explosionTimerClip,transform);
        yield return new WaitForSeconds(explodeTimer);
        Instantiate(robotExplosionVFX, transform.position, Quaternion.identity);
        enemyHealth.SelfDestruct();
    }

    static public void IncreaseExplTimer()
    {
        explodeTimer *= 1.5f;
    }
}
