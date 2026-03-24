using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] Vector3 pointA;
    [SerializeField] Vector3 pointB;
    [SerializeField] float speed = 10f;
    [SerializeField] float delay = .5f;

    Vector3 goal;

    void Start()
    {
        goal = pointA;
    }

    void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, goal, Time.deltaTime * speed);
        if (transform.position == pointA)
        {
            StartCoroutine(SwitchTargetRoutine(pointB));
        }
        else if(transform.position == pointB)
        {
            StartCoroutine(SwitchTargetRoutine(pointA));
        }
    }

    IEnumerator SwitchTargetRoutine(Vector3 target)
    {
        yield return new WaitForSeconds(delay);
        goal = target;
    }
}
