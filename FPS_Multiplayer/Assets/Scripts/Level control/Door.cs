using System.Collections;
using TMPro;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] GameObject leftDoor;
    [SerializeField] GameObject rightDoor;
    [SerializeField] AudioClip errorClip;
    [SerializeField] AudioClip openClip;
    [SerializeField] AudioClip closeClip;

    [SerializeField] float moveFactor = .55f;
    [SerializeField] float maxDistance = .1f;
    [SerializeField] bool isUnlocked = true;
    [SerializeField] string notificationText = "This door is won't open";

    Vector3 leftDoorStart;
    Vector3 leftDoorTarget;
    Vector3 leftDoorOpen;

    Vector3 rightDoorStart;
    Vector3 rightDoorTarget;
    Vector3 rightDoorOpen;

    const string PLAYER_STRING = "Player";

    void Start()
    {
        leftDoorStart = leftDoor.transform.position;
        rightDoorStart = rightDoor.transform.position;

        leftDoorTarget = leftDoorStart;
        rightDoorTarget = rightDoorStart;

        leftDoorOpen = leftDoorStart + transform.forward * -moveFactor;
        rightDoorOpen = rightDoorStart + transform.forward * moveFactor;
    }

    IEnumerator MoveRoutine(Vector3 destinationLeft, Vector3 destinationRight)
    {
        while(leftDoor.transform.position != destinationLeft)
        {
            leftDoor.transform.position = Vector3.MoveTowards(leftDoor.transform.position, destinationLeft, maxDistance * Time.deltaTime);
            rightDoor.transform.position = Vector3.MoveTowards(rightDoor.transform.position, destinationRight, maxDistance * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING) && isUnlocked)
        {
            SoundFXManager.instance.PlaySoundFX(openClip,transform.position);
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(leftDoorOpen, rightDoorOpen));
        }
        else if (other.CompareTag(PLAYER_STRING) && !isUnlocked)
        {
            SoundFXManager.instance.PlaySoundFX(errorClip,transform.position);
            NotificationManager.instance.FireNotification(notificationText);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            if (isUnlocked) SoundFXManager.instance.PlaySoundFX(closeClip,transform.position);
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(leftDoorStart, rightDoorStart));
        }
    }

    public void UnlockDoor()
    {
        isUnlocked = true;
    }
}
