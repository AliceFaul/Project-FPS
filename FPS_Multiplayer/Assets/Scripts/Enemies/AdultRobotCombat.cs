using UnityEngine;
using System.Collections;

public class AdultRobotCombat : MonoBehaviour {
    [Header("Combat Settings")]
    public Transform shootPoint;
    public Transform bulletPoint;
    public LayerMask layerMask = ~0;
    public float maxDistance = 100f;
    public float bulletSpeed = 100f;

    [Header("Gun Settings")]
    public Vector3 spread = new Vector3(0.06f, 0.06f, 0.06f);
    public TrailRenderer bulletTrail;

    public void Shoot() {
        if(TryShootAt(null, 0, out Vector3 trailStart, out Vector3 trailEnd)) {
            PlayBulletTrail(trailStart, trailEnd);
        }
    }

    public bool TryShootAt(Transform targetPoint, int damage, out Vector3 trailStart, out Vector3 trailEnd) {
        Vector3 origin = GetShootOrigin();
        Vector3 aimDirection = GetAimDirection(origin, targetPoint);
        Vector3 direction = GetDirection(aimDirection);

        trailStart = GetTrailStart(origin);
        trailEnd = origin + direction * maxDistance;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        foreach(RaycastHit hit in hits) {
            if(hit.collider == null || hit.collider.transform.IsChildOf(transform)) {
                continue;
            }

            trailEnd = hit.point;
            Debug.DrawLine(origin, hit.point, Color.red, 1f);

            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if(playerHealth != null && damage > 0) {
                playerHealth.AdjustHealth(-damage);
            }

            return true;
        }

        Debug.DrawLine(origin, trailEnd, Color.red, 1f);
        return true;
    }

    public void PlayBulletTrail(Vector3 startPoint, Vector3 targetPoint) {
        if(bulletTrail == null) {
            return;
        }

        TrailRenderer trail = Instantiate(bulletTrail, startPoint, Quaternion.identity);
        StartCoroutine(SpawnBulletTrail(trail, targetPoint));
    }

    private IEnumerator SpawnBulletTrail(TrailRenderer trail, Vector3 targetPoint) {
        Vector3 startPoint = trail.transform.position;
        float distance = Vector3.Distance(startPoint, targetPoint);
        float travelTime = distance / Mathf.Max(1f, bulletSpeed);

        float elapsedTime = 0f;
        while(elapsedTime < travelTime) {
            trail.transform.position = Vector3.Lerp(startPoint, targetPoint, elapsedTime / travelTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = targetPoint;
        Destroy(trail.gameObject, trail.time); // Destroy after the trail duration
    }

    private Vector3 GetShootOrigin() {
        return shootPoint != null ? shootPoint.position : transform.position + Vector3.up * 1.4f;
    }

    private Vector3 GetTrailStart(Vector3 fallbackOrigin) {
        return bulletPoint != null ? bulletPoint.position : fallbackOrigin;
    }

    private Vector3 GetAimDirection(Vector3 origin, Transform targetPoint) {
        if(targetPoint == null) {
            return shootPoint != null ? shootPoint.forward : transform.forward;
        }

        Vector3 direction = targetPoint.position - origin;
        return direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;
    }

    private Vector3 GetDirection(Vector3 aimDirection) {
        Vector3 direction = aimDirection;
        direction += new Vector3(
            Random.Range(-spread.x, spread.x),
            Random.Range(-spread.y, spread.y),
            Random.Range(-spread.z, spread.z)
        );

        return direction.normalized;
    }
}
