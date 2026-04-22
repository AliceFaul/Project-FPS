using UnityEngine;
using Fusion;

public class EnemyHealth : NetworkBehaviour {
    [SerializeField] GameObject[] ammoDrop;
    [SerializeField] GameObject healthDrop;
    [SerializeField] GameObject smallExplosionFX;
    [SerializeField] float startHealth = 3f;
    [SerializeField] float chance4drop = 1f;
    [SerializeField] float chance4ammoDrop = 0.6f;

    SpawnGate parentGate = null;
    GameManager gameManager;
    Vector3 dropSpawnPos;
    float currentHealth;

    private bool isDead = false;

    [Networked] public float NetworkHealth { get; set; }

    public override void Spawned() {
        if(Object.HasStateAuthority) {
            NetworkHealth = startHealth;
        }
    }

    public void Init(GameManager gameManager)
    {
        if(gameManager == null) {
            Debug.LogError("[EnemyHealth.Init()]: GameManager is null");
            return;       
        }
        this.gameManager = gameManager;
        if(Object.HasStateAuthority && this.gameManager != null) {
            this.gameManager.AdjustEnemiesLeft(1);
        } else {
            Debug.LogWarning("[EnemyHealth]: EnemyHealth gameManager missing");
        }
    }

    public void Init(GameManager gameManager, SpawnGate parentGate)
    {
        Init(gameManager);
        this.parentGate = parentGate;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float amount) {
        TakeDamage(amount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayExplosionFX(Vector3 position) {
        Instantiate(smallExplosionFX, position, Quaternion.identity);
    }

    public void TakeDamage(float amount) {
        float nextHealth = Mathf.Clamp(NetworkHealth + amount, 0, startHealth);
        if(nextHealth == NetworkHealth) {
            return;
        }
        NetworkHealth = nextHealth;
        if(NetworkHealth <= 0) {
            SelfDestruct();
        }
    }

    public void SelfDestruct()
    {
        if(isDead) {
            return;
        }
        isDead = true;
        if(!Object.HasStateAuthority || gameManager == null) {
            return;
        }
        dropSpawnPos = Vector3.zero; // reset drop pos
        gameManager?.AdjustEnemiesLeft(-1);
        // Play fx in all player
        RPC_PlayExplosionFX(transform.position);
        // Spawn drop item by network
        if(Random.value < chance4drop) {
            dropSpawnPos = transform.position - new Vector3(0, .5f, 0);
            if(Random.value > chance4ammoDrop && Runner != null) {
                TrySpawnDrop(healthDrop, dropSpawnPos);
            } else {
                int lootNr = Random.Range(0, ammoDrop.Length);
                TrySpawnDrop(ammoDrop[lootNr], dropSpawnPos);
            }
        }
        parentGate?.ChildRobotDestroyed();
        Runner.Despawn(Object);
    }

    private void TrySpawnDrop(GameObject dropPrefab, Vector3 position) {
        if(dropPrefab == null) {
            Debug.LogWarning("[EnemyHealth]: Drop prefab is null.");
            return;
        }

        if(Runner == null) {
            return;
        }

        if(dropPrefab.GetComponent<NetworkObject>() == null) {
            Debug.LogError($"[EnemyHealth]: Drop prefab '{dropPrefab.name}' is missing NetworkObject, cannot spawn with Runner.Spawn.");
            return;
        }

        Runner.Spawn(dropPrefab, position, Quaternion.identity);
    }
}
