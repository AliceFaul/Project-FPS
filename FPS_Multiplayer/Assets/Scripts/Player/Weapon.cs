using System;
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

    public ParticleSystem MuzzleFlash => muzzleFlash;

    public void Shoot(WeaponSO weaponSO, PlayerNetworkSetup shootingSync = null, float spreadControl = 1f)
    {
        muzzleFlash.Play();
        SoundFXManager.instance.PlaySoundFX(gunshotClip, transform);
        impulseSource.GenerateImpulse();

        Transform shootTransform = Camera.main != null ? Camera.main.transform : projectileSpawnReference;
        Vector3 accuracyModifier = new(
            UnityEngine.Random.Range(-weaponSO.accuracyMod * spreadControl, weaponSO.accuracyMod * spreadControl),
            UnityEngine.Random.Range(-weaponSO.accuracyMod * spreadControl, weaponSO.accuracyMod * spreadControl),
            0f
        );
        Vector3 shootDirection = (shootTransform.forward + accuracyModifier).normalized;

        if (weaponSO.HitScan)
        {
            Vector3 targetPoint = shootTransform.position + shootDirection * 100f;
            bool didHit = false;

            if (TryGetFirstValidHit(shootTransform.position, shootDirection, out RaycastHit hit))
            {
                didHit = true;
                targetPoint = hit.point;
                Instantiate(weaponSO.HitVFXPrefab, hit.point, Quaternion.identity);
                ApplyDamage(hit, weaponSO);
            }

            shootingSync?.NotifyShot(weaponSO.ID, targetPoint, didHit);
            return;
        }

        Vector3 projectileTargetPoint = projectileSpawnReference.position + projectileSpawnReference.forward * 100f;
        if (TryGetFirstValidHit(shootTransform.position, shootDirection, out RaycastHit projectileHit))
        {
            projectileTargetPoint = projectileHit.point;
        }

        shootingSync?.NotifyShot(weaponSO.ID, projectileTargetPoint, false);

        GameObject projectile = Instantiate(spawnedProjectile, projectileSpawnReference.position, projectileSpawnReference.rotation);
        projectile.transform.LookAt(projectileTargetPoint);
    }

    private bool TryGetFirstValidHit(Vector3 origin, Vector3 direction, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
            {
                continue;
            }

            if (hit.collider.transform.root == transform.root)
            {
                continue;
            }

            validHit = hit;
            return true;
        }

        validHit = default;
        return false;
    }

    private void ApplyDamage(RaycastHit hit, WeaponSO weaponSO)
    {
        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.RPC_TakeDamage(-Mathf.RoundToInt(weaponSO.Damage));
            return;
        }

        PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.transform.root != transform.root)
        {
            playerHealth.AdjustHealth(-Mathf.RoundToInt(weaponSO.Damage));
        }
    }
}
