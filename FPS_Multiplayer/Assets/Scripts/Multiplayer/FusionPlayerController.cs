using Fusion;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum NetworkPlayerButtons {
    Jump = 0,
    Sprint = 1,
}

public struct NetworkPlayerInputData : INetworkInput {
    public Vector2 Move;
    public Vector2 Look;
    public NetworkButtons Buttons;
}

[RequireComponent(typeof(NetworkCharacterController))]
public class FusionPlayerController : NetworkBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float rotationSpeed = 1.0f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15.0f;
    [SerializeField] private float jumpTimeout = 0.1f;

    [Header("Cinemachine")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 90.0f;
    [SerializeField] private float bottomClamp = -90.0f;

    [Networked] private float Yaw { get; set; }
    [Networked] private TickTimer JumpCooldown { get; set; }

    private const float Threshold = 0.01f;

    private NetworkCharacterController _networkCharacterController;
    private StarterAssetsInputs _starterAssetsInputs;
    private float _localPitch;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    private bool IsCurrentDeviceMouse {
        get {
#if ENABLE_INPUT_SYSTEM
            return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    public override void Spawned() {
        _networkCharacterController = GetComponent<NetworkCharacterController>();
        _starterAssetsInputs = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif

        if(cameraTarget == null) {
            Transform target = transform.Find("PlayerCameraRoot");
            if(target != null) {
                cameraTarget = target;
            }
        }

        Vector3 currentEuler = transform.rotation.eulerAngles;
        Yaw = currentEuler.y;
        _localPitch = 0f;

        _networkCharacterController.rotationSpeed = 0f;
        _networkCharacterController.gravity = gravity;
        _networkCharacterController.jumpImpulse = Mathf.Sqrt(jumpHeight * -2f * gravity);
        _networkCharacterController.acceleration = 30f;
        _networkCharacterController.braking = 30f;

        ApplyViewRotation();
    }

    public override void FixedUpdateNetwork() {
        if(GetInput(out NetworkPlayerInputData input)) {
            Simulate(input);
        }
    }

    public override void Render() {
        ApplyViewRotation();
    }

    private void Simulate(NetworkPlayerInputData input) {
        HandleLook(input.Look);

        transform.rotation = Quaternion.Euler(0.0f, Yaw, 0.0f);

        float targetSpeed = input.Buttons.IsSet((int)NetworkPlayerButtons.Sprint) ? sprintSpeed : moveSpeed;
        Vector3 moveDirection = transform.right * input.Move.x + transform.forward * input.Move.y;

        _networkCharacterController.maxSpeed = targetSpeed;
        _networkCharacterController.gravity = gravity;
        _networkCharacterController.jumpImpulse = Mathf.Sqrt(jumpHeight * -2f * gravity);
        _networkCharacterController.Move(moveDirection.normalized);

        if(input.Buttons.IsSet((int)NetworkPlayerButtons.Jump) && JumpCooldown.ExpiredOrNotRunning(Runner)) {
            _networkCharacterController.Jump();
            JumpCooldown = TickTimer.CreateFromSeconds(Runner, jumpTimeout);
        }

        if(_starterAssetsInputs != null) {
            _starterAssetsInputs.jump = false;
        }
    }

    private void HandleLook(Vector2 lookInput) {
        if(lookInput.sqrMagnitude < Threshold) {
            return;
        }

        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Runner.DeltaTime;
        _localPitch += lookInput.y * rotationSpeed * deltaTimeMultiplier;
        float rotationVelocity = lookInput.x * rotationSpeed * deltaTimeMultiplier;

        _localPitch = ClampAngle(_localPitch, bottomClamp, topClamp);
        Yaw += rotationVelocity;
    }

    private void ApplyViewRotation() {
        if(Object == null || !Object.HasInputAuthority) {
            if(cameraTarget != null) {
                cameraTarget.localRotation = Quaternion.identity;
            }
            return;
        }

        if(cameraTarget != null) {
            cameraTarget.localRotation = Quaternion.Euler(_localPitch, 0.0f, 0.0f);
        }
    }

    private static float ClampAngle(float angle, float min, float max) {
        if(angle < -360f) {
            angle += 360f;
        }
        if(angle > 360f) {
            angle -= 360f;
        }
        return Mathf.Clamp(angle, min, max);
    }
}
