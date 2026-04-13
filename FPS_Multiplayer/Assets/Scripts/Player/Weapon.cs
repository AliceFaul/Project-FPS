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
            0
        );

        if (Physics.Raycast(
            Camera.main.transform.position,
            Camera.main.transform.forward + accuracyModifier,
            out RaycastHit hit,
            Mathf.Infinity,
            interactionLayer,
            QueryTriggerInteraction.Ignore))
        {
            //  HIỆU ỨNG TRÚNG
            Instantiate(weaponSO.HitVFXPrefab, hit.point, Quaternion.identity);

            //  1. BẮN ENEMY
            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(weaponSO.Damage);
                return;
            }

            //  2. BẮN PLAYER
            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && playerHealth.gameObject != transform.root.gameObject)
            {
                //  tên người bắn
                string attackerName = transform.root.name;

                //  gửi attacker vào PlayerHealth
                playerHealth.SetLastAttacker(attackerName);

                //  trừ máu
                playerHealth.AdjustHealth(-Mathf.RoundToInt(weaponSO.Damage));
            }

            // 3. PROJECTILE (nếu không phải hitscan)
            if (!weaponSO.HitScan)
            {
                GameObject projectile = Instantiate(
                    spawnedProjectile,
                    projectileSpawnReference.position,
                    projectileSpawnReference.rotation
                );

                projectile.transform.LookAt(hit.point);
            }
        }
    }

    public void DecreaseWeaponSpread(float decreasePercent)
    {
        spreadControl -= decreasePercent;
    }
}