using UnityEngine;

public class MobileLook : MonoBehaviour
{
    public float sensitivity = 0.2f;
    public Transform playerBody;

    float xRotation = 0f;

    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.position.x > Screen.width / 2)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    float mouseX = touch.deltaPosition.x * sensitivity;
                    float mouseY = touch.deltaPosition.y * sensitivity;

                    xRotation -= mouseY;
                    xRotation = Mathf.Clamp(xRotation, -80f, 80f);

                    transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                    playerBody.Rotate(Vector3.up * mouseX);
                }
            }
        }
    }
}