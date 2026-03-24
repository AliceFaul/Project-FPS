using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] GameObject projectileHitVFX;

    Rigidbody rb;
    int projectileDamage;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.linearVelocity = projectileSpeed * transform.forward;
    }

    public void Init(int projectileDamage)
    {
        this.projectileDamage = projectileDamage;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.isTrigger) return;
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        playerHealth?.AdjustHealth(-projectileDamage);
        Instantiate(projectileHitVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
