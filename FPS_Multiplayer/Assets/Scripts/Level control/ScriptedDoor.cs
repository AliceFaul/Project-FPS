using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class ScriptedDoor : MonoBehaviour
{

    [SerializeField] GameObject leftDoor;
    [SerializeField] GameObject rightDoor;

    [SerializeField] float moveFactor = 2f;
    [SerializeField] float maxDistance = .1f;
    [SerializeField] bool isOpen = false;

    Vector3 leftDoorDest;
    Vector3 rightDoorDest;

    const string PLAYER_STRING = "Player";

    void Start()
    {
        leftDoorDest = leftDoor.transform.position + transform.right * moveFactor;
        rightDoorDest = rightDoor.transform.position + transform.right * -moveFactor;
    }

    // void Update()
    // {
    //     leftDoor.transform.position = Vector3.MoveTowards(leftDoor.transform.position, leftDoorDest, maxDistance * Time.deltaTime);
    //     rightDoor.transform.position = Vector3.MoveTowards(rightDoor.transform.position, rightDoorDest, maxDistance * Time.deltaTime);
    // }

    void OnTriggerEnter(Collider other)
    {
        if (!isOpen && other.CompareTag(PLAYER_STRING))
        {
            Debug.Log("Opening");
            isOpen = true;
            StartCoroutine(MoveRoutine(leftDoorDest, rightDoorDest));
        }
    }
    
    IEnumerator MoveRoutine(Vector3 leftDoorDest, Vector3 rightDoorDest)
    {
        while(leftDoor.transform.position != leftDoorDest)
        {
            leftDoor.transform.position = Vector3.MoveTowards(leftDoor.transform.position, leftDoorDest, maxDistance * Time.deltaTime);
            rightDoor.transform.position = Vector3.MoveTowards(rightDoor.transform.position, rightDoorDest, maxDistance * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
    }
}
