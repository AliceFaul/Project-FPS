using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour
{
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

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        GetComponent<EnemyHealth>().Init(gameManager);
        StartCoroutine(FireRoutine());
    }

    void Update()
    {
        EnsurePlayerTarget();

        if (playerTargetPoint)
        {
            targeting.LookAt(playerTargetPoint.position);
            euler = targeting.eulerAngles;
            turretHead.eulerAngles = new Vector3(euler.x, euler.y, turretHead.eulerAngles.z);
        } 
    }

    IEnumerator FireRoutine()
    {
        while (true)
        {
            EnsurePlayerTarget();

            if (player == null || playerTargetPoint == null)
            {
                yield return null;
                continue;
            }

            if (!isShotReady)
            {
                yield return new WaitForSeconds(fireRate);
                isShotReady = true;
            }
            else { yield return new WaitForEndOfFrame(); }

            if (Physics.Raycast(turretHead.transform.position, turretHead.transform.forward, out RaycastHit hit, maxDistance, interactionLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<PlayerHealth>() && isShotReady)
                {
                    SoundFXManager.instance.PlaySoundFX(shootClip,transform);
                    GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
                    proj.transform.LookAt(playerTargetPoint.position);
                    proj.GetComponent<Projectile>().Init(damage);
                    isShotReady = false;
                }
            }
        }
    }

    private void EnsurePlayerTarget()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
        }

        if (player == null)
        {
            playerTargetPoint = null;
            return;
        }

        if (playerTargetPoint == null)
        {
            var target = player.transform.Find("PlayerCameraRoot");
            playerTargetPoint = target != null ? target : player.transform;
        }
    }
}
