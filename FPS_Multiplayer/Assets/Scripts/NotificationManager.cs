using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    [SerializeField] TMP_Text UINotification;
    [SerializeField] float notifDuration = 3f;

    void Awake()
    {
        if(!instance) instance = this;
    }

    public void FireNotification(string notification)
    {
        StopAllCoroutines();
        StartCoroutine(NotificationRoutine(notification));
    }

    IEnumerator NotificationRoutine(string notificationText)
    {
        UINotification.text = notificationText;
        UINotification.enabled = true;
        yield return new WaitForSeconds(notifDuration);
        UINotification.enabled = false;
    }
}
