using UnityEngine;

public class PlayerPlatformAttach : MonoBehaviour
{
    CharacterController controller;
    Transform currentPlatform;
    Vector3 lastPlatformPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (currentPlatform != null)
        {
            Vector3 platformDelta = currentPlatform.position - lastPlatformPos;
            controller.Move(platformDelta);
            lastPlatformPos = currentPlatform.position;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Platform"))
        {
            currentPlatform = other.transform;
            lastPlatformPos = currentPlatform.position;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Platform") && currentPlatform == other.transform)
        {
            currentPlatform = null;
        }
    }
}