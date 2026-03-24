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

    const string PLAYER_STRING = "Player";
    const string ENEMY_STRING = "Enemy";

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
            Debug.Log(hitCollider.gameObject.name);
            if (hitCollider.CompareTag(PLAYER_STRING) && !hitCollider.isTrigger)
            {
                hitCollider.GetComponentInParent<PlayerHealth>().AdjustHealth(-damage / playerDamageModifier);
            }
            else if (hitCollider.CompareTag(ENEMY_STRING) && !hitCollider.isTrigger)
            {
                hitCollider.GetComponentInParent<EnemyHealth>().TakeDamage(damage);
            }
        }
        Instantiate(explosionFX,transform.position,Quaternion.identity);
        SoundFXManager.instance.PlaySoundFX(explosionClip,transform);
        Destroy(gameObject);
    }
}