using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerNetworkSetup : NetworkBehaviour {
    [Header("Player Components")]
    [SerializeField] private ActiveWeapon activeWeapon;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private Transform weaponCameraTransform;
    [SerializeField] private Transform cameraTarget;

    // This method is called when the player spawns in the game, 
    // and is responsible for setting up the player's components and references.
    public override void Spawned() {
        CacheComponents();

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
