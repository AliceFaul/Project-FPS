using UnityEngine;

public class MobileFPSController : MonoBehaviour
{
    public CharacterController controller;
    public Joystick moveJoystick;
    public float speed = 5f;
    public float gravity = -9.8f;
    public float jumpHeight = 2f;

    private Vector3 velocity;
    private bool isGrounded;

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = moveJoystick.Horizontal;
        float z = moveJoystick.Vertical;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}