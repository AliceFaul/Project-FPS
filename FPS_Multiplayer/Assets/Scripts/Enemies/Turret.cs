using System.Collections;
using Fusion;
using UnityEngine;

public class Turret : NetworkBehaviour {
    [SerializeField] GameObject projectilePrefab;  
    [SerializeField] Transform turretHead;
    [SerializeField] Transform playerTargetPoint;
    [SerializeField] Transform targeting;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] LayerMask interactionLayer;
    [SerializeField] AudioClip shootClip;
    
    [SerializeField] float fireRate = 2f;
    [SerializeField] float maxDistance = 30f;
    [SerializeField] int damage = 2;

    PlayerHealth player;
    GameManager gameManager;
    Vector3 euler;

    bool isShotReady = false;

    public override void Spawned() {
        gameManager = FindFirstObjectByType<GameManager>();
        if(gameManager != null) {
            GetComponent<EnemyHealth>().Init(gameManager);
        } else {
            Debug.LogError("[Turret]: GameManager is NULL");
        }
        // run only in server
        if(Object.HasStateAuthority) {
            StartCoroutine(FireRoutine());
        }
    }

    void Update()
    {
        EnsurePlayerTarget();
        if(playerTargetPoint) {
            targeting.LookAt(playerTargetPoint.position);
            euler = targeting.eulerAngles;
            turretHead.eulerAngles = new Vector3(euler.x, euler.y, turretHead.eulerAngles.z);
        }
    }

    IEnumerator FireRoutine()
    {
        while (true)
        {
            if(!Object.HasStateAuthority) { 
                yield return null;
                continue;
            }
            EnsurePlayerTarget();
            if (player == null || playerTargetPoint == null) {
                yield return null;
                continue;
            }
            if (!isShotReady) {
                yield return new WaitForSeconds(fireRate);
                isShotReady = true;
            }
            else { yield return new WaitForEndOfFrame(); }

            if (Physics.Raycast(turretHead.transform.position, turretHead.transform.forward, out RaycastHit hit, maxDistance, interactionLayer, QueryTriggerInteraction.Ignore)) {
                if (hit.collider.GetComponentInParent<PlayerHealth>() != null && isShotReady) {
                    Shoot();
                    isShotReady = false;
                }
            }
        }
    }

    // function to shoot in network
    private void Shoot() {
        var obj = Runner.Spawn(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        if(obj == null) {
            return;
        }
        obj.transform.LookAt(playerTargetPoint.position);
        obj.GetComponent<Projectile>().Init(damage);
        SoundFXManager.instance.PlaySoundFX(shootClip, transform);
    }

    private void EnsurePlayerTarget() {
        if (player == null) {
            player = GetClosestPlayer();
        }

        if (player == null) {
            playerTargetPoint = null;
            return;
        }

        if (playerTargetPoint == null) {
            var target = player.transform.Find("PlayerCameraRoot");
            playerTargetPoint = target != null ? target : player.transform;
        }
    }

    // helper method help get closest player
    private PlayerHealth GetClosestPlayer() {
        if(Runner == null) {
            Debug.LogError("[Turret]: Runner is NULL");
            return null;
        }
        PlayerHealth closest = null;
        float minDist = Mathf.Infinity;

        foreach(var player in Runner.ActivePlayers) { 
            var obj = Runner.GetPlayerObject(player);
            if(obj == null) continue;

            var playerHealth = obj.GetComponent<PlayerHealth>();
            if (playerHealth != null) continue;

            float dist = Vector3.Distance(transform.position, playerHealth.transform.position);
            if(dist < minDist) { 
                minDist = dist;
                closest = playerHealth;
            }
        }
        return closest;
    }
}
