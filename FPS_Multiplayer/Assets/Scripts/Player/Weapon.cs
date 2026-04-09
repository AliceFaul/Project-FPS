using UnityEngine;
using Unity.Cinemachine;

public class Weapon : MonoBehaviour
{
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] LayerMask interactionLayer;
    [SerializeField] CinemachineImpulseSource impulseSource;
    [SerializeField] AudioClip gunshotClip;
    [SerializeField] GameObject spawnedProjectile;
    [SerializeField] Transform projectileSpawnReference;

    static float spreadControl = 1f;

    public void Shoot(WeaponSO weaponSO)
    {
        muzzleFlash.Play();
        SoundFXManager.instance.PlaySoundFX(gunshotClip, transform);
        impulseSource.GenerateImpulse();
        Vector3 accuracyModifier = new(
            Random.Range(-weaponSO.accuracyMod * spreadControl, weaponSO.accuracyMod * spreadControl),
            Random.Range(-weaponSO.accuracyMod * spreadControl, weaponSO.accuracyMod * spreadControl),
            0f
        );
        _ = new Vector3(projectileSpawnReference.position.x, projectileSpawnReference.position.y, projectileSpawnReference.position.z - 5);

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward + accuracyModifier, out RaycastHit hit, Mathf.Infinity, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            if (weaponSO.HitScan)
            {
                Instantiate(weaponSO.HitVFXPrefab, hit.point, Quaternion.identity);

                EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(weaponSO.Damage);
                    return;
                }

                PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null && playerHealth.transform.root != transform.root)
                {
                    playerHealth.AdjustHealth(-Mathf.RoundToInt(weaponSO.Damage));
                }
            }
            else
            {
                GameObject projectile = Instantiate(spawnedProjectile, projectileSpawnReference.position, projectileSpawnReference.rotation);
                projectile.transform.LookAt(hit.point);
            }
        }
    }

    public void DecreaseWeaponSpread(float decreasePercent)
    {
        spreadControl -= decreasePercent;
    }
}
