using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FieldOfView : MonoBehaviour {
    public float viewRadius;
    [Range(0, 360)] public float viewAngle;

    [Header("Layer")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    public bool canSeePlayer;
    public Vector3 lastKnownPosition;
    
    private List<Transform> visibleTargets = new List<Transform>();

    private void Start() {
        StartCoroutine(FindTargetsWithDelay(.2f));
    }

    private IEnumerator FindTargetsWithDelay(float delay) {
        while(true) {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets() {
        // Reset
        canSeePlayer = false;
        visibleTargets.Clear();

        // Find targets in view radius
        Collider[] targets = Physics.OverlapSphere(transform.position, viewRadius, playerLayer);
        for(int i = 0; i < targets.Length; i++) { 
            var target = targets[i].transform;
            var direction = (target.position - transform.position).normalized;

            // Check if target is in view angle
            var angle = Vector3.Angle(transform.forward, direction);
            if(angle < viewAngle / 2) {
                var distance = Vector3.Distance(transform.position, target.position);
                if(!Physics.Raycast(transform.position, direction, distance, obstacleLayer)) {
                    canSeePlayer = true; // Set canSeePlayer to true if we can see at least one target
                    lastKnownPosition = target.position; // Update last known position of the player
                    visibleTargets.Add(target);
                }
            }
        }
    }

    public Vector3 DirFromAngle(float angleInDegree, bool angleIsGlobal) { 
        if(!angleIsGlobal) { 
            angleInDegree += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegree * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegree * Mathf.Deg2Rad));
    }
}
