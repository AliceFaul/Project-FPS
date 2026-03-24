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
        player = FindFirstObjectByType<PlayerHealth>();
        gameManager = FindFirstObjectByType<GameManager>();
        GetComponent<EnemyHealth>().Init(gameManager);
        StartCoroutine(FireRoutine());
    }

    void Update()
    {
        if (playerTargetPoint)
        {
            targeting.LookAt(playerTargetPoint.position);
            euler = targeting.eulerAngles;
            turretHead.eulerAngles = new Vector3(euler.x, euler.y, turretHead.eulerAngles.z);
        } 
    }

    IEnumerator FireRoutine()
    {
        while (player)
        {
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
                    proj.transform.LookAt(playerTargetPoint);
                    proj.GetComponent<Projectile>().Init(damage);
                    isShotReady = false;
                }
            }
        }
    }
}