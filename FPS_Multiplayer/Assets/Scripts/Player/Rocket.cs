using System.Collections;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float radius = 1.5f;
    [SerializeField] int damage = 10;
    [SerializeField] int playerDamageModifier = 2;
    [SerializeField] GameObject explosionFX;
    [SerializeField] AudioClip explosionClip;
    
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = speed * transform.forward;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    void OnCollisionEnter(Collision collision)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.isTrigger)
            {
                continue;
            }

            PlayerHealth playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && playerHealth.transform.root != transform.root)
            {
                playerHealth.AdjustHealth(-damage / playerDamageModifier);
                continue;
            }

            EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.RPC_TakeDamage(-damage);
            }
        }

        Instantiate(explosionFX, transform.position, Quaternion.identity);
        SoundFXManager.instance.PlaySoundFX(explosionClip, transform);
        Destroy(gameObject);
    }
}
