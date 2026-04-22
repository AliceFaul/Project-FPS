using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class Robot : NetworkBehaviour {
    [SerializeField] GameObject robotExplosionVFX;
    [SerializeField] AudioClip explosionTimerClip;
    [SerializeField] bool isSpawnedByGate = true;

    static float explodeTimer = 1.5f;

    SpawnGate parentGate = null;
    PlayerHealth player;
    NavMeshAgent agent;
    GameManager gameManager;

    private float targetUpdateTimer = 0f;
    private float targetUpdateInterval = 0.5f;

    bool initializedSelfDestruct = false;

    const string PLAYER_STRING = "Player";

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Spawned() {
        if(!Object.HasStateAuthority) {
            agent.enabled = false;
        } else {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    public override void FixedUpdateNetwork() {
        if(!Object.HasStateAuthority) {
            return;
        }
        targetUpdateTimer += Runner.DeltaTime;
        if(targetUpdateTimer >= targetUpdateInterval) {
            player = GetClosestPlayer();
            targetUpdateTimer = 0f;
        }
        if(player == null) {
            return;
        }
        agent?.SetDestination(player.transform.position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnExplosionFX(Vector3 position) {
        Instantiate(robotExplosionVFX, position, Quaternion.identity);
    }

    public void Init(PlayerHealth player, GameManager gameManager, SpawnGate parentGate)
    {
        if(!Object.HasStateAuthority) return;
        this.parentGate = parentGate;
        this.player = player;
        this.gameManager = gameManager;
        GetComponent<EnemyHealth>().Init(gameManager, parentGate);
    }

    private void OnTriggerEnter(Collider other) {
        if(!Object.HasStateAuthority) {
            return;
        }     
        if(initializedSelfDestruct) {
            return;
        }
        if(other.GetComponentInParent<PlayerHealth>() != null && !initializedSelfDestruct) {
            initializedSelfDestruct = true;
            StartCoroutine(SelfDestructRoutine());
        }
    }

    IEnumerator SelfDestructRoutine() {
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        SoundFXManager.instance.PlaySoundFX(explosionTimerClip,transform);
        yield return new WaitForSeconds(explodeTimer);
        RPC_SpawnExplosionFX(transform.position);
        enemyHealth.SelfDestruct();
    }

    // function help enemy find a closest player
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

    static public void IncreaseExplTimer()
    {
        explodeTimer *= 1.5f;
    }
}
