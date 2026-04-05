using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerNetworkSetup : NetworkBehaviour {
    [Header("Player Components")]
    [SerializeField] private ActiveWeapon activeWeapon;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private Transform weaponCameraTransform;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private FusionPlayerController fusionPlayerController;
    [SerializeField] private FirstPersonController firstPersonController;
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private PlayerInput playerInput;
#endif

    [Header("Visibility Setup")]
    [SerializeField] private GameObject[] localOnlyObjects;
    [SerializeField] private GameObject[] remoteOnlyObjects;
    [SerializeField] private UnityEngine.Behaviour[] localOnlyBehaviours;

    // This method is called when the player spawns in the game, 
    // and is responsible for setting up the player's components and references.
    public override void Spawned() {
        CacheComponents();
        ApplyVisibilityState();
        DisableLegacyControllerIfNeeded();

        if(!Object.HasInputAuthority) {
            if(weaponCamera != null) {
                weaponCamera.enabled = false;
            }
            return;
        }

        Debug.Log($"[PlayerNetworkSetup] Spawned local player. activeWeapon={activeWeapon}, playerHealth={playerHealth}, weaponCamera={weaponCamera}");

        if(activeWeapon == null || playerHealth == null || weaponCamera == null || weaponCameraTransform == null) {
            Debug.LogWarning("[PlayerNetworkSetup] Missing player component references. Check the spawned player prefab hierarchy.");
            return;
        }

        // Find the references to the UI and cameras in the scene,
        // including inactive objects, so that they can be passed to the player's components for initialization.
        var followCam = FindSceneCamera("Player Follow Camera");
        var deathCam = FindSceneCamera("Death Virtual Camera");
        var volume = FindFirstObjectByType<Volume>(FindObjectsInactive.Include);
        var canvas = GameObject.Find("Canvas");
        var crosshair = FindInChildren(canvas, "Crosshair");
        var ammoTextObject = FindInChildren(canvas, "Ammo text");
        var gameOver = FindInChildren(canvas, "Game over container");
        var shieldBars = new Image[10];
        for(int i = 0; i < shieldBars.Length; i++) {
            var shieldBarObject = FindInChildren(canvas, $"Shield bar ({i})");
            shieldBars[i] = shieldBarObject != null ? shieldBarObject.GetComponent<Image>() : null;
        }
        var zoomUI = new GameObject[] {
            FindInChildren(canvas, "SR Zoom"),
            FindInChildren(canvas, "RL Zoom")
        };
        var weaponIcons = new GameObject[] {
            FindInChildren(canvas, "Pistol"),
            FindInChildren(canvas, "MG"),
            FindInChildren(canvas, "SR"),
            FindInChildren(canvas, "RL")
        };
        var ammoText = ammoTextObject != null ? ammoTextObject.GetComponent<TMPro.TMP_Text>() : null;

        Debug.Log($"[PlayerNetworkSetup] crosshair={crosshair}, ammoText={ammoText}, gameOver={gameOver}, followCam={followCam}, deathCam={deathCam}, volume={volume}");

        if(followCam == null || deathCam == null || volume == null || crosshair == null || ammoText == null || gameOver == null) {
            Debug.LogWarning("[PlayerNetworkSetup] Missing scene references. Check object names/tags in the gameplay scene.");
            return;
        }

        // Initialize the player's components with the references to the UI and cameras,
        // so that they can set up their functionality correctly.
        activeWeapon.Initialize(weaponCamera, followCam, crosshair, zoomUI, weaponIcons, ammoText);
        playerHealth.Initialize(deathCam, weaponCameraTransform, shieldBars, gameOver, volume);
        weaponCamera.enabled = true;
        var target = cameraTarget != null ? cameraTarget : transform;
        followCam.Follow = target;
        followCam.LookAt = target;
    }

    private void CacheComponents() {
        if(activeWeapon == null) {
            activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
        }

        if(playerHealth == null) {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if(weaponCamera == null) {
            weaponCamera = GetComponentInChildren<Camera>(true);
        }

        if(weaponCameraTransform == null && weaponCamera != null) {
            weaponCameraTransform = weaponCamera.transform;
        }

        if(cameraTarget == null) {
            var target = transform.Find("PlayerCameraRoot");
            if(target != null) {
                cameraTarget = target;
            }
        }

        if(fusionPlayerController == null) {
            fusionPlayerController = GetComponent<FusionPlayerController>();
        }

        if(firstPersonController == null) {
            firstPersonController = GetComponent<FirstPersonController>();
        }

#if ENABLE_INPUT_SYSTEM
        if(playerInput == null) {
            playerInput = GetComponent<PlayerInput>();
        }
#endif

        DisableLegacyControllerIfNeeded();
    }

    // This method applies the visibility state to the player's components 
    // based on whether this is the local player or a remote player.
    private void ApplyVisibilityState() {
        bool isLocalPlayer = Object != null && Object.HasInputAuthority;

        SetObjectsActive(localOnlyObjects, isLocalPlayer);
        SetObjectsActive(remoteOnlyObjects, !isLocalPlayer);
        SetBehavioursEnabled(localOnlyBehaviours, isLocalPlayer);
#if ENABLE_INPUT_SYSTEM
        if(playerInput != null) {
            playerInput.enabled = isLocalPlayer;
        }
#endif
    }

    private void DisableLegacyControllerIfNeeded() {
        if(fusionPlayerController != null && firstPersonController != null) {
            firstPersonController.enabled = false;
        }
    }

    // Utility method to find a CinemachineVirtualCamera in the scene by name, including inactive objects.
    private static CinemachineVirtualCamera FindSceneCamera(string cameraName) {
        var cameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(var camera in cameras) {
            if(camera.name == cameraName) {
                return camera;
            }
        }

        return null;
    }

    // Utility method to set an array of GameObjects active or inactive, with null checks.
    private static void SetObjectsActive(GameObject[] objects, bool isActive) {
        if(objects == null) {
            return;
        }

        foreach(var obj in objects) {
            if(obj != null) {
                obj.SetActive(isActive);
            }
        }
    }
    
    // Utility method to enable or disable an array of UnityEngine.Behaviour components.
    private static void SetBehavioursEnabled(UnityEngine.Behaviour[] behaviours, bool isEnabled) {
        if(behaviours == null) {
            return;
        }

        foreach(var behaviour in behaviours) {
            if(behaviour != null) {
                behaviour.enabled = isEnabled;
            }
        }
    }

    // Utility method to find the first object of a given type in the scene, including inactive objects.
    private static GameObject FindInChildren(GameObject root, string objectName) {
        if(root == null) {
            return null;
        }

        var children = root.GetComponentsInChildren<Transform>(true);
        foreach(var child in children) {
            if(child.name == objectName) {
                return child.gameObject;
            }
        }

        return null;
    }
}
