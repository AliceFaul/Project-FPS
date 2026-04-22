using System.Collections;
using Fusion;
using UnityEngine;

public class MovingPlatform : NetworkBehaviour {
    [SerializeField] Vector3 pointA;
    [SerializeField] Vector3 pointB;
    [SerializeField] float speed = 10f;
    [SerializeField] float delay = .5f;

    Vector3 goal;

    void Start() {
        goal = pointA;
    }

    public override void FixedUpdateNetwork() {
        if(!Object.HasStateAuthority) {
            return;
        }
        MovePlatform();
    }

    private void MovePlatform() {
        transform.position = Vector3.MoveTowards(transform.position, goal, Runner.DeltaTime * speed);
        if (transform.position == pointA) {
            StartCoroutine(SwitchTargetRoutine(pointB));
        } else if (transform.position == pointB) {
            StartCoroutine(SwitchTargetRoutine(pointA));
        }
    }

    IEnumerator SwitchTargetRoutine(Vector3 target) {
        yield return new WaitForSeconds(delay);
        goal = target;
    }
}
