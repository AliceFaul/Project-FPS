using UnityEngine;
using Fusion;
using UnityEngine.AI;
using System.Collections.Generic;

// AdultRobot is a stronger version of Robot. Using Behavior Tree to control it and make it more intelligent.
// They exhibit the following behaviors:
// 1. Patrol : They will patrol around the map when they don't see the player. They will have a set of waypoints to follow and will move between them in a loop.
// 2. Shoot: If player in field of view and within shooting distance, they will stop and shoot at the player. They will have a cooldown between shots. If there is a hiding place nearby, they will try to move to the hiding place while shooting at the player. They will prioritize shooting at the player over moving to the hiding place, but if they are low on health, they will prioritize moving to the hiding place over shooting at the player.
// 3. Chase: If player in field of view but out of shooting distance, they will chase the player until they are within shooting distance or lose sight of the player. If they lose sight of the player, they will return to patrolling.
// 4. Survive: If they are low on health, they will try to find hiding spots to avoid being shot by the player. They will have a set of hiding spots on the map and will move to the closest one when they are low on health. They will stay there until they are healed or the player is out of sight.
// 5. Search: If they lose sight of the player, they will search the area around the last known position of the player for a certain amount of time before returning to patrolling. They will have a search radius and will move randomly within that radius while searching.

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class AdultRobot : NetworkBehaviour {
    [Header("Perception")]
    [SerializeField] private FieldOfView fieldOfView;
    [SerializeField] private Transform eyePoint;
    [SerializeField] private float viewRadius = 25f;
    [SerializeField] [Range(0, 360)] private float viewAngle = 120f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetUpdateInterval = .25f;

    [Header("Combat")]
    [SerializeField] private AdultRobotCombat combat;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private float shootingDistance;
    [SerializeField] private float fireCooldown = 1.25f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float aimTurnSpeed = 8f;
    [SerializeField] private float tacticalCoverDistance = 12f;

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private float reloadCooldown = 2f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private float waypointReachDistance = 1.25f;

    [Header("Survive")]
    [SerializeField] private Transform[] hidingSpots;
    [SerializeField] private float lowHealthThreshold = 1f;
    [SerializeField] private float hidingReachDistance = 1.5f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private float searchPointInterval = 1.5f;
    [SerializeField] private float searchPointReachDistance = 1.25f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.4f;

    private float nextFootstepTime;
    public float pathUpdateInterval = 1f; // Time interval for updating the path to the player

    private GameManager gameManager;
    private PlayerHealth player;
    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;
    private Animator animator;

    private Transform activeHidingSpot;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 currentDestination;
    private Vector3 currentSearchPoint;
    private int patrolIndex;
    private float pathUpdateDeadline;
    private float targetUpdateDeadline;
    private float fireDeadline;
    private float currentAmmo;
    private float coverBlendTarget;
    private float searchDeadline;
    private float nextSearchPointTime;
    private bool hasCurrentDestination;
    private bool hasLastKnownPlayerPosition;
    private bool hasSearchPoint;
    private bool isSearching;
    private bool isReloading;
    private bool inCombat;
    private bool inCover;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int CoverHash = Animator.StringToHash("Cover");
    private static readonly int CombatHash = Animator.StringToHash("Combat");
    private static readonly int ShootHash = Animator.StringToHash("Shooting");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");

    public Node rootNode; // Root node of the behavior tree

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        currentAmmo = magazineSize;
        if(combat == null) {
            combat = GetComponentInChildren<AdultRobotCombat>();
        }
        if(fieldOfView == null) {
            fieldOfView = GetComponent<FieldOfView>();
        }

        InitializeBehaviorTree(); // Initialize the behavior tree in Awake or Start method
    }

    public override void Spawned() {
        if(agent == null) {
            Debug.LogError("[AdultRobot]: Missing NavMeshAgent component.");
            return;
        }

        if(!Object.HasStateAuthority) {
            agent.enabled = false;
        } else {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
            
            if(shootingDistance <= 0f) {
                shootingDistance = agent.stoppingDistance; // Set shooting distance to the stopping distance of the NavMeshAgent
            }
        }
        gameManager = FindFirstObjectByType<GameManager>();
        if(gameManager != null) {
            enemyHealth = GetComponent<EnemyHealth>();
            if(enemyHealth != null) {
                enemyHealth.Init(gameManager);
            }
            Debug.Log("[AdultRobot]: Initialize enemyHealth successfully");
        } else {
            Debug.LogError("[AdultRobot]: GameManager is NULL");
        }
    }

    public override void FixedUpdateNetwork() {
        if(!Object.HasStateAuthority) {
            return;
        }

        if(rootNode == null) {
            InitializeBehaviorTree();
        }

        rootNode.Evaluate();
        UpdateAnimator();
        UpdateFootsteps();
    }

    private void InitializeBehaviorTree() { 
        rootNode = new Selector(new List<Node> {
            // Survive behavior
            new Sequence(new List<Node> {
                new TaskNode(IsLowHealth),
                new TaskNode(NeedsReloadNode),
                new TaskNode(ReloadWeapon),
                new TaskNode(HasVisiblePlayer),
                new TaskNode(HasHidingSpot),
                new TaskNode(MoveToHidingSpot)
            }),
            // Shoot behavior
            new Sequence(new List<Node> {
                new TaskNode(HasVisiblePlayer),
                new TaskNode(IsTargetInShootingRange),
                new TaskNode(ShootTarget)
            }),
            // Chase behavior
            new Sequence(new List<Node> {
                new TaskNode(HasVisiblePlayer),
                new TaskNode(ChaseTarget)
            }),
            // Search behavior
            new Sequence(new List<Node> {
                new TaskNode(HasSearchMemory),
                new TaskNode(SearchLastKnownPosition)
            }),
            // Patrol behavior
            new TaskNode(Patrol)
        });
    }

    private NodeStatus IsLowHealth() {
        if(enemyHealth == null || enemyHealth.NetworkHealth > lowHealthThreshold) {
            return NodeStatus.Failure;
        }

        return NodeStatus.Success;
    }

    private NodeStatus NeedsReloadNode() {
        return NeedsReload() ? NodeStatus.Success : NodeStatus.Failure;
    }

    private NodeStatus ReloadWeapon() {
        if(!isReloading) {
            isReloading = true;
            StopAgent();
            animator.SetTrigger(ReloadHash);
        }
        return NodeStatus.Running;
    }

    private NodeStatus HasVisiblePlayer() {
        RefreshTarget();

        if(player == null || !CanSeePlayer(player)) {
            if(hasLastKnownPlayerPosition && !isSearching) {
                StartSearch();
            }
            return NodeStatus.Failure;
        }

        lastKnownPlayerPosition = player.transform.position;
        hasLastKnownPlayerPosition = true;
        isSearching = false;
        hasSearchPoint = false;
        return NodeStatus.Success;
    }

    private NodeStatus HasHidingSpot() {
        activeHidingSpot = FindClosestHidingSpot(float.PositiveInfinity);
        return activeHidingSpot != null ? NodeStatus.Success : NodeStatus.Failure;
    }

    private NodeStatus MoveToHidingSpot() {
        if(activeHidingSpot == null) {
            activeHidingSpot = FindClosestHidingSpot(float.PositiveInfinity);
        }

        if(activeHidingSpot == null) {
            return NodeStatus.Failure;
        }

        if(IsAtPosition(activeHidingSpot.position, hidingReachDistance)) {
            StopAgent();
            LookAtTarget();
            inCover = true;
            coverBlendTarget = 1f;
            if(IsTargetInShootingRange() == NodeStatus.Success) {
                TryShoot();
            }
            return NodeStatus.Running;
        }

        MoveTo(activeHidingSpot.position);
        inCover = false;
        coverBlendTarget = 0f;
        return NodeStatus.Running;
    }

    private NodeStatus IsTargetInShootingRange() {
        if(player == null) {
            return NodeStatus.Failure;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= shootingDistance ? NodeStatus.Success : NodeStatus.Failure;
    }

    private NodeStatus ShootTarget() {
        inCombat = true;
        StopOrMoveToTacticalCover();
        LookAtTarget();
        TryShoot();
        return NodeStatus.Running;
    }

    private NodeStatus ChaseTarget() {
        if(player == null) {
            return NodeStatus.Failure;
        }

        coverBlendTarget = 0f;
        inCombat = false;
        activeHidingSpot = null;
        MoveTo(player.transform.position);
        return NodeStatus.Running;
    }

    private NodeStatus HasSearchMemory() {
        return hasLastKnownPlayerPosition ? NodeStatus.Success : NodeStatus.Failure;
    }

    private NodeStatus SearchLastKnownPosition() {
        if(!hasLastKnownPlayerPosition) {
            return NodeStatus.Failure;
        }

        if(!isSearching) {
            StartSearch();
        }

        if(Time.time >= searchDeadline) {
            ResetSearch();
            return NodeStatus.Failure;
        }

        if(!hasSearchPoint || IsAtPosition(currentSearchPoint, searchPointReachDistance) || Time.time >= nextSearchPointTime) {
            currentSearchPoint = GetRandomSearchPoint();
            hasSearchPoint = true;
            nextSearchPointTime = Time.time + searchPointInterval;
        }

        MoveTo(currentSearchPoint);
        return NodeStatus.Running;
    }

    private NodeStatus Patrol() {
        ResetSearch();
        activeHidingSpot = null;
        coverBlendTarget = 0f;

        if(patrolWaypoints == null || patrolWaypoints.Length == 0) {
            StopAgent();
            return NodeStatus.Running;
        }

        Transform waypoint = patrolWaypoints[patrolIndex];
        if(waypoint == null) {
            AdvancePatrolIndex();
            return NodeStatus.Running;
        }

        if(IsAtPosition(waypoint.position, waypointReachDistance)) {
            AdvancePatrolIndex();
            waypoint = patrolWaypoints[patrolIndex];
        }

        if(waypoint != null) {
            MoveTo(waypoint.position);
        }

        return NodeStatus.Running;
    }

    private void RefreshTarget() {
        PlayerHealth visiblePlayer = GetClosestVisiblePlayerFromFieldOfView();
        if(visiblePlayer != null) {
            player = visiblePlayer;
            lastKnownPlayerPosition = player.transform.position;
            hasLastKnownPlayerPosition = true;
            targetUpdateDeadline = Time.time + targetUpdateInterval;
            return;
        }

        if(Time.time < targetUpdateDeadline) {
            return;
        }

        targetUpdateDeadline = Time.time + targetUpdateInterval;
        player = GetClosestVisiblePlayer();
        if(player != null) {
            lastKnownPlayerPosition = player.transform.position;
            hasLastKnownPlayerPosition = true;
        }
    }

    private PlayerHealth GetClosestVisiblePlayerFromFieldOfView() {
        if(fieldOfView == null || fieldOfView.visibleTargets == null || fieldOfView.visibleTargets.Count == 0) {
            return null;
        }

        PlayerHealth closest = null;
        float closestDistance = Mathf.Infinity;

        foreach(Transform target in fieldOfView.visibleTargets) {
            if(target == null) {
                continue;
            }

            PlayerHealth candidate = target.GetComponentInParent<PlayerHealth>();
            if(candidate == null) {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if(distance < closestDistance) {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private PlayerHealth GetClosestVisiblePlayer() {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        PlayerHealth closest = null;
        float closestDistance = Mathf.Infinity;

        foreach(PlayerHealth candidate in players) {
            if(candidate == null || !CanSeePlayer(candidate)) {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if(distance < closestDistance) {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private bool CanSeePlayer(PlayerHealth target) {
        if(target == null) {
            return false;
        }

        if(fieldOfView != null && fieldOfView.canSeePlayer) {
            foreach(Transform visibleTarget in fieldOfView.visibleTargets) {
                if(visibleTarget != null && visibleTarget.GetComponentInParent<PlayerHealth>() == target) {
                    return true;
                }
            }
        }

        Vector3 origin = GetEyePosition();
        Vector3 targetPosition = GetTargetPoint(target).position;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if(distance > viewRadius || distance <= 0f) {
            return false;
        }

        direction /= distance;
        if(Vector3.Angle(transform.forward, direction) > viewAngle * .5f) {
            return false;
        }

        return obstacleLayer.value == 0 || !Physics.Raycast(origin, direction, distance, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private Transform GetTargetPoint(PlayerHealth target) {
        Transform cameraRoot = target.transform.Find("PlayerCameraRoot");
        return cameraRoot != null ? cameraRoot : target.transform;
    }

    private Transform FindClosestHidingSpot(float maxDistance) {
        if(hidingSpots == null || hidingSpots.Length == 0) {
            return null;
        }

        Transform closest = null;
        float closestDistance = maxDistance;

        foreach(Transform hidingSpot in hidingSpots) {
            if(hidingSpot == null) {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hidingSpot.position);
            if(distance < closestDistance) {
                closestDistance = distance;
                closest = hidingSpot;
            }
        }

        return closest;
    }

    private void StopOrMoveToTacticalCover() {
        Transform nearbyCover = FindClosestHidingSpot(tacticalCoverDistance);
        if(nearbyCover != null) {
            if(!IsAtPosition(nearbyCover.position, hidingReachDistance)) {
                MoveTo(nearbyCover.position);
                inCover = false;
                coverBlendTarget = 0f;
                if(animator != null) {
                    animator.SetFloat(CoverHash, 0f);
                }
                return;
            }

            StopAgent();
            inCover = true;
            coverBlendTarget = 1f;
            return;
        }

        StopAgent();
        inCover = false;
        coverBlendTarget = 0f;
    }

    private void TryShoot() {
        if(player == null || Time.time < fireDeadline || isReloading || currentAmmo <= 0f) {
            return;
        }

        fireDeadline = Time.time + fireCooldown;
    }

    public void AnimationShootEvent() {
        if(player == null) {
            return;
        }
        
        Transform targetPoint = GetTargetPoint(player);
        if(combat != null && combat.TryShootAt(targetPoint, damage, out Vector3 trailStart, out Vector3 trailEnd)) {
            if(Runner != null) {
                RPC_PlayCombatTrail(trailStart, trailEnd);
            } else {
                combat.PlayBulletTrail(trailStart, trailEnd);
            }
        }

        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        PlayShootSound();
    }

    public void AnimationReloadEvent() {
        currentAmmo = magazineSize;
        isReloading = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCombatTrail(Vector3 trailStart, Vector3 trailEnd) {
        if(combat == null) {
            combat = GetComponentInChildren<AdultRobotCombat>();
        }

        combat?.PlayBulletTrail(trailStart, trailEnd);
    }

    private void PlayShootSound() {
        if(SoundFXManager.instance != null && shootClip != null) {
            SoundFXManager.instance.PlaySoundFX(shootClip, transform);
        }
    }

    private void MoveTo(Vector3 destination) {
        if(agent == null || !agent.enabled || !agent.isOnNavMesh) {
            return;
        }

        if(agent.isStopped) {
            agent.isStopped = false;
        }

        agent.SetDestination(destination);
    }

    private void StopAgent() {
        if(agent == null || !agent.enabled || !agent.isOnNavMesh) {
            return;
        }

        agent.isStopped = true;
    }

    private bool IsAtPosition(Vector3 position, float reachDistance) {
        float distance = Vector3.Distance(transform.position, position);
        if(distance <= reachDistance) {
            return true;
        }

        if(agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending) {
            return false;
        }

        return agent.hasPath && agent.remainingDistance <= Mathf.Max(reachDistance, agent.stoppingDistance);
    }

    private void PlayFootstep() {
        if(footstepClips == null || footstepClips.Length == 0 || SoundFXManager.instance == null) {
            return;
        }

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        SoundFXManager.instance.PlaySoundFX(clip, transform);
    }

    private void UpdateFootsteps() {
        if(agent == null || !agent.enabled || agent.velocity.sqrMagnitude <= 0.01f) {
            return;
        }

        if(Time.time < nextFootstepTime) {
            return;
        }

        PlayFootstep();
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
        nextFootstepTime = Time.time + Mathf.Lerp(footstepInterval, footstepInterval * 0.5f, speedRatio);
    }

    private void StartSearch() {
        isSearching = true;
        hasSearchPoint = false;
        searchDeadline = Time.time + searchDuration;
        nextSearchPointTime = 0f;
    }

    private void ResetSearch() {
        isSearching = false;
        hasSearchPoint = false;
        hasLastKnownPlayerPosition = false;

        inCombat = false;
        animator.SetBool(CombatHash, false);
    }

    private bool NeedsReload() {
        return currentAmmo <= 0f;
    }

    private Vector3 GetRandomSearchPoint() {
        for(int i = 0; i < 8; i++) {
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = lastKnownPlayerPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if(NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas)) {
                return hit.position;
            }
        }

        return lastKnownPlayerPosition;
    }

    private void AdvancePatrolIndex() {
        if(patrolWaypoints == null || patrolWaypoints.Length == 0) {
            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolWaypoints.Length;
    }

    private Vector3 GetEyePosition() {
        return eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.4f;
    }

    private void UpdateAnimator() {
        if(animator == null || agent == null || !agent.enabled) {
            return;
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        animator.SetBool(ShootHash, IsTargetInShootingRange() == NodeStatus.Success);
        animator.SetBool(CombatHash, inCombat);

        float current = animator.GetFloat(CoverHash);
        float next = Mathf.MoveTowards(current, coverBlendTarget, Time.deltaTime * 5f);
        animator.SetFloat(CoverHash, next);
    }

    private void LookAtTarget() { 
        if(player == null) {
            return;
        }

        Vector3 lookPosition = player.transform.position - transform.position;
        lookPosition.y = 0; // Keep the y-axis unchanged to prevent tilting
        if(lookPosition.sqrMagnitude <= 0f) {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookPosition);
        float rotateStep = Runner != null ? aimTurnSpeed * Runner.DeltaTime : .2f;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateStep); // Smoothly rotate towards the target
    }

}
