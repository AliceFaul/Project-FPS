using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] AudioClip explosionClip;
    [SerializeField] float radius = 2f;
    [SerializeField] int exlosionDamage = 1;

    const string PLAYER_STRING = "Player";

    void Start()
    {
        Explode();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    void Explode()
    {
        SoundFXManager.instance.PlaySoundFX(explosionClip,transform);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(PLAYER_STRING))
            {
                hitCollider.GetComponent<PlayerHealth>().AdjustHealth(-exlosionDamage);
                break;
            }
        }
        Destroy(gameObject,explosionClip.length);
    }
}
