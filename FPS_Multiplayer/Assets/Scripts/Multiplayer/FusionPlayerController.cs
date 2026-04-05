using Fusion;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// This is a custom player controller script for a multiplayer game using the Fusion networking library. 
// It handles player movement, jumping, and camera control, and is designed to work with the Starter Assets FirstPersonController. 
// The script includes settings for movement speed, jump height, gravity, and camera clamping, as well as input handling for both keyboard/mouse and gamepad controls.
public enum NetworkPlayerButtons {
    Jump,
    Sprint,
}

// Struct to hold player input data for network transmission
public struct NetworkPlayerInputData : INetworkInput {
    public Vector2 Move;
    public Vector2 Look;
    public NetworkButtons Buttons;
}

[RequireComponent(typeof(NetworkCharacterController))]
public class FusionPlayerController : NetworkBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float speedChangeRate = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15f;

    [Header("Grounded")]
    [SerializeField] private bool grounded = true;
    [SerializeField] private float groundedOffset = -0.14f;
    [SerializeField] private float groundedRadius = 0.5f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Camera")]  
    [SerializeField] private GameObject cinemachineCameraTarget;
    [SerializeField] private float topClamp = 89f;
    [SerializeField] private float bottomClamp = -89f;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobFrequency = 9f;
    [SerializeField] private float bobHorizontalAmplitude = 0.03f;
    [SerializeField] private float bobVerticalAmplitude = 0.035f;
    [SerializeField] private float bobSprintMultiplier = 1.35f;
    [SerializeField] private float bobReturnSpeed = 12f;
    [SerializeField] private float bobSmoothness = 16f;

    [Header("Camera Feel")]
    [SerializeField] private bool enableLookSway = true;
    [SerializeField] private float swayAmount = 2f;
    [SerializeField] private float swaySmoothness = 10f;
    [SerializeField] private bool enableLandingBump = true;
    [SerializeField] private float landingBumpAmount = 0.05f;
    [SerializeField] private float landingBumpRecoverSpeed = 8f;

    [Networked] private float Yaw { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private float _speed;
    private float _cinemachineTargetPitch;
    private float _rotationVelocity;
    private Vector2 _cachedMoveInput;
    private Vector2 _cachedLookInput;
    private Vector3 _cameraTargetBaseLocalPosition;
    private Vector3 _currentCameraLocalPosition;
    private float _bobTimer;
    private float _currentSwayZ;
    private float _landingOffsetY;
    private bool _wasGrounded;
    private NetworkCharacterController _controller;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    private const float Threshold = 0.01f;
    
    // Property to check if the current input device is a mouse (for camera rotation sensitivity)
    private bool IsCurrentDeviceMouse {
        get {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    public override void Spawned() {
        _controller = GetComponent<NetworkCharacterController>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogWarning("Input System is not enabled. Please enable the Input System package in the Package Manager.");
#endif
        if(cinemachineCameraTarget == null) {
            Transform target = transform.Find("PlayerCameraRoot");
            if(target != null) {
                cinemachineCameraTarget = target.gameObject;
            }
        }
        if(cinemachineCameraTarget != null) {
            _cameraTargetBaseLocalPosition = cinemachineCameraTarget.transform.localPosition;
            _currentCameraLocalPosition = _cameraTargetBaseLocalPosition;
        }
        _speed = 0f;
        _rotationVelocity = 0f;
        Yaw = transform.eulerAngles.y;

        if(_controller != null) {
            _controller.rotationSpeed = 0f;
            _controller.gravity = gravity;
            _controller.acceleration = Mathf.Max(speedChangeRate, 45f);
            _controller.braking = Mathf.Max(speedChangeRate * 5f, 160f);
            _controller.jumpImpulse = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _wasGrounded = _controller.Grounded;
        }
    }

    public override void FixedUpdateNetwork() {
        if(GetInput(out NetworkPlayerInputData inputData)) {
            if(Object != null && Object.HasInputAuthority) {
                _cachedMoveInput = inputData.Move;
                _cachedLookInput = inputData.Look;
            }

            if(Object.HasStateAuthority) {
                _rotationVelocity = inputData.Look.x * rotationSpeed;
                Yaw += _rotationVelocity;
                transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
                _speed = inputData.Buttons.IsSet(NetworkPlayerButtons.Sprint) ? sprintSpeed : moveSpeed;
                Vector3 moveDirection = transform.TransformDirection(new Vector3(inputData.Move.x, 0f, inputData.Move.y));
                grounded = _controller != null && _controller.Grounded;
                if(enableLandingBump && !_wasGrounded && grounded) {
                    _landingOffsetY = -landingBumpAmount;
                }
                if(grounded) {
                    if(inputData.Buttons.WasPressed(PreviousButtons, NetworkPlayerButtons.Jump)) {
                        _controller.Jump();
                    }
                }
                _controller.maxSpeed = _speed;
                _controller.Move(moveDirection.normalized);
                PreviousButtons = inputData.Buttons;
                _wasGrounded = grounded;
            }
        }
    }

    public override void Render() {
        if(Object != null && Object.HasInputAuthority) {
            CameraRotation(_cachedLookInput);
        } else {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, Yaw, 0f), 20f * Runner.DeltaTime);
        }
    }

    public void ChangeRotationSpeed(float newRotationSpeed) {
        rotationSpeed = newRotationSpeed;
    }

    private bool GroundedCheck() {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        return Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation(Vector2 lookInput) {
        if(cinemachineCameraTarget == null) {
            return;
        }   
        if(lookInput.sqrMagnitude >= Threshold) {
            // float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1f : Runner.DeltaTime;
            _cinemachineTargetPitch += lookInput.y * rotationSpeed;
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);
        }
        cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
        ApplyHeadBob();
        ApplyLandingBump();
        float targetSwayZ = enableLookSway ? Mathf.Clamp(-lookInput.x * swayAmount, -swayAmount, swayAmount) : 0f;
        _currentSwayZ = Mathf.Lerp(_currentSwayZ, targetSwayZ, swaySmoothness * Time.deltaTime);
        cinemachineCameraTarget.transform.localRotation *= Quaternion.Euler(0f, 0f, _currentSwayZ);
    }

    private void ApplyHeadBob() {
        if(cinemachineCameraTarget == null) {
            return;
        }

        Transform targetTransform = cinemachineCameraTarget.transform;

        if(!enableHeadBob) {
            _currentCameraLocalPosition = Vector3.Lerp(
                _currentCameraLocalPosition,
                _cameraTargetBaseLocalPosition,
                bobReturnSpeed * Time.deltaTime
            );
            targetTransform.localPosition = _currentCameraLocalPosition;
            return;
        }

        bool isMoving = _cachedMoveInput.sqrMagnitude > Threshold;
        Vector3 targetLocalPosition;
        if(isMoving && grounded) {
            float sprintMultiplier = _speed > moveSpeed + Threshold ? bobSprintMultiplier : 1f;
            _bobTimer += Time.deltaTime * bobFrequency * sprintMultiplier;

            float offsetX = Mathf.Sin(_bobTimer) * bobHorizontalAmplitude;
            float offsetY = (Mathf.Sin(_bobTimer * 2f) * 0.5f + 0.5f) * bobVerticalAmplitude;
            Vector3 bobOffset = new Vector3(offsetX, offsetY + _landingOffsetY, 0f);
            targetLocalPosition = _cameraTargetBaseLocalPosition + bobOffset;
        } else {
            _bobTimer = 0f;
            targetLocalPosition = _cameraTargetBaseLocalPosition + new Vector3(0f, _landingOffsetY, 0f);
        }

        _currentCameraLocalPosition = Vector3.Lerp(
            _currentCameraLocalPosition,
            targetLocalPosition,
            bobSmoothness * Time.deltaTime
        );
        targetTransform.localPosition = _currentCameraLocalPosition;
    }

    private void ApplyLandingBump() {
        if(!enableLandingBump) {
            return;
        }
        _landingOffsetY = Mathf.Lerp(_landingOffsetY, 0f, landingBumpRecoverSpeed * Time.deltaTime);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

    private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z), groundedRadius);
		}
}
