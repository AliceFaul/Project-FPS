using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour {
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] GameObject projectileHitVFX;

    private Rigidbody rb;
    private bool hasHit = false;

    [Networked] public int projectileDamage { get; set; }


    public override void Spawned() {
        rb = GetComponent<Rigidbody>();
        if(Object.HasStateAuthority) {
            rb.linearVelocity = projectileSpeed * transform.forward;
        } else {
            rb.isKinematic = true;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayVFX(Vector3 position) {
        Instantiate(projectileHitVFX, position, Quaternion.identity);
    }

    public void Init(int projectileDamage) {
        if(!Object.HasStateAuthority) return;
        this.projectileDamage = projectileDamage;
    }

    private void OnTriggerEnter(Collider other) {
        if(!Object.HasStateAuthority) return;
        if(hasHit) return;
        if(other.isTrigger) return;
        hasHit = true;
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        playerHealth?.AdjustHealth(-projectileDamage);
        RPC_PlayVFX(transform.position);
        Runner.Despawn(Object);
    }
}
