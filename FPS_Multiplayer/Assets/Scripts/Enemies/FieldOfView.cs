using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Fusion;

public class FieldOfView : MonoBehaviour {
    public float viewRadius;
    [Range(0, 360)] public float viewAngle;

    [Header("Layer")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    public bool canSeePlayer;
    public Vector3 lastKnownPosition;
    
    [HideInInspector] public List<Transform> visibleTargets = new List<Transform>();

    private NetworkObject networkObject;
    private Coroutine findTargetsRoutine;

    private void Awake() {
        networkObject = GetComponentInParent<NetworkObject>();
    }

    private void OnEnable() {
        findTargetsRoutine = StartCoroutine(FindTargetsWithDelay(.2f));
    }

    private void OnDisable() {
        if(findTargetsRoutine != null) {
            StopCoroutine(findTargetsRoutine);
            findTargetsRoutine = null;
        }

        ClearVisibleTargets();
    }

    private IEnumerator FindTargetsWithDelay(float delay) {
        WaitForSeconds wait = new WaitForSeconds(delay);

        while(true) {
            yield return wait;

            if(!CanScanOnThisPeer()) {
                ClearVisibleTargets();
                continue;
            }

            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets() {
        // Reset
        ClearVisibleTargets();

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

    private bool CanScanOnThisPeer() {
        if(networkObject == null) {
            networkObject = GetComponentInParent<NetworkObject>();
        }

        return networkObject == null || networkObject.HasStateAuthority;
    }

    private void ClearVisibleTargets() {
        canSeePlayer = false;
        visibleTargets.Clear();
    }

    public Vector3 DirFromAngle(float angleInDegree, bool angleIsGlobal) { 
        if(!angleIsGlobal) { 
            angleInDegree += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegree * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegree * Mathf.Deg2Rad));
    }
}
