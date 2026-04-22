using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public struct IPlayerMetaData : INetworkStruct {
    public NetworkString<_16> Name;
}

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

    [Header("Remote Shooting FX")]
    [SerializeField] private bool enableRemoteShotFx = true;
    [SerializeField] private float remoteShotDistance = 100f;
    [SerializeField] private float remoteTracerDuration = 0.06f;
    [SerializeField] private float remoteTracerWidth = 0.035f;
    [SerializeField] private Color remoteTracerColor = new(1f, 0.8f, 0.35f, 0.9f);
    [SerializeField] private GameObject remoteMuzzleFlashPrefab;
    [SerializeField] private float remoteMuzzleFlashLifetime = 0.18f;
    [SerializeField] private float remoteMuzzleFlashForwardOffset = 0.12f;
    [SerializeField] private GameObject remoteImpactVfxPrefab;
    [SerializeField] private float remoteImpactVfxLifetime = 1.2f;
    [SerializeField] private float remoteImpactOffset = 0.03f;

    private static Material _remoteTracerMaterial;
    private ParticleSystem[] _remoteWeaponMuzzleFlashPrefabs;

    [Networked] public NetworkDictionary<PlayerRef, IPlayerMetaData> Players => default;

    public override void Spawned() {
        CacheComponents();
        ApplyVisibilityState();
        DisableLegacyControllerIfNeeded();
        TryPushLocalPlayerMetaData();

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

        activeWeapon.Initialize(weaponCamera, followCam, crosshair, zoomUI, weaponIcons, ammoText);
        playerHealth.Initialize(deathCam, weaponCameraTransform, shieldBars, gameOver, volume);
        weaponCamera.enabled = true;
        var target = cameraTarget != null ? cameraTarget : transform;
        followCam.Follow = target;
        followCam.LookAt = target;
    }

    #region Set Player Data in game
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_UpdatePlayerMetaData(PlayerRef playerRef, IPlayerMetaData metaData) {
        Players.Set(playerRef, metaData);
    }

    public void TryPushLocalPlayerMetaData() {
        if(Runner == null || Object == null || !Object.HasInputAuthority) {
            return;
        }

        IPlayerMetaData metaData = default;
        metaData.Name = PlayerNameStorage.GetPlayerName();
        RPC_UpdatePlayerMetaData(Object.InputAuthority, metaData);
    }

    public bool TryGetPlayerMetaData(PlayerRef playerRef, out IPlayerMetaData metaData) {
        return Players.TryGet(playerRef, out metaData);
    } 

    public void GrantAmmoPickup(int weaponId, float ammoPortion) {
        if(Object == null) {
            return;
        }

        RPC_GrantAmmoPickup(weaponId, ammoPortion);
    }

    public void GrantWeaponPickup(int weaponId, int ammoAmount) {
        if(Object == null) {
            return;
        }

        RPC_GrantWeaponPickup(weaponId, ammoAmount);
    }

    public void GrantDecreaseSpread(float decreaseAmount) {
        if(Object == null) {
            return;
        }

        RPC_GrantDecreaseSpread(decreaseAmount);
    }

    public void GrantIncreaseMaxAmmo() {
        if(Object == null) {
            return;
        }

        RPC_GrantIncreaseMaxAmmo();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_GrantAmmoPickup(int weaponId, float ammoPortion) {
        if(activeWeapon == null) {
            return;
        }

        activeWeapon.GrantAmmoForWeapon(weaponId, ammoPortion);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_GrantWeaponPickup(int weaponId, int ammoAmount) {
        if(activeWeapon == null) {
            return;
        }

        activeWeapon.GrantWeaponPickup(weaponId, ammoAmount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_GrantDecreaseSpread(float decreaseAmount) {
        if(activeWeapon == null) {
            return;
        }

        activeWeapon.DecreaseSpread(decreaseAmount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_GrantIncreaseMaxAmmo() {
        if(activeWeapon == null) {
            return;
        }

        activeWeapon.IncreaseMaxAmmo();
    }
 
    public IPlayerMetaData GetLocalPlayerData() { 
        if(Runner == null) {
            Debug.LogWarning("[PlayerNetworkSetup] Runner is NULL");
        } else {
            var playerRef = Runner.LocalPlayer;
            if(Players.TryGet(playerRef, out var metaData)) { 
                return metaData;
            }
        }
        return default;
    }
    #endregion

    #region Remote Shoot FX
    public void NotifyShot(int weaponId, Vector3? targetPoint = null, bool spawnImpactVfx = false) {
        if(!enableRemoteShotFx || Runner == null || Object == null) {
            return;
        }

        Vector3 resolvedTargetPoint = targetPoint ?? (GetShotOrigin() + GetShotForward() * remoteShotDistance);
        RPC_PlayRemoteShotFx(weaponId, resolvedTargetPoint, spawnImpactVfx);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayRemoteShotFx(int weaponId, Vector3 targetPoint, bool spawnImpactVfx) {
        if(!enableRemoteShotFx) {
            return;
        }

        if(Object != null && Object.HasInputAuthority) {
            return;
        }

        PlayRemoteShotFx(weaponId, targetPoint, spawnImpactVfx);
    }

    private void PlayRemoteShotFx(int weaponId, Vector3 targetPoint, bool spawnImpactVfx) {
        Vector3 origin = GetShotOrigin();
        Vector3 shotDirection = targetPoint - origin;
        if(shotDirection.sqrMagnitude <= 0.0001f) {
            shotDirection = GetShotForward();
        }
        else {
            shotDirection.Normalize();
        }

        CreateTracer(origin, targetPoint);
        SpawnRemoteMuzzleFlash(weaponId, origin + shotDirection * remoteMuzzleFlashForwardOffset, Quaternion.LookRotation(shotDirection));

        if(spawnImpactVfx) {
            Vector3 impactPosition = targetPoint - shotDirection * remoteImpactOffset;
            SpawnRemoteImpactVfx(impactPosition, Quaternion.LookRotation(-shotDirection));
        }
    }

    private void CreateTracer(Vector3 origin, Vector3 targetPoint) {
        GameObject tracerObject = new("RemoteShotTracer");
        LineRenderer lineRenderer = tracerObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, targetPoint);
        lineRenderer.startWidth = remoteTracerWidth;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 2;
        lineRenderer.material = GetRemoteTracerMaterial();
        lineRenderer.startColor = remoteTracerColor;
        lineRenderer.endColor = new Color(remoteTracerColor.r, remoteTracerColor.g, remoteTracerColor.b, 0f);
        Destroy(tracerObject, remoteTracerDuration);
    }
    #endregion

    private void SpawnRemoteMuzzleFlash(int weaponId, Vector3 position, Quaternion rotation) {
        ParticleSystem weaponMuzzleFlash = GetRemoteWeaponMuzzleFlashPrefab(weaponId);
        if(weaponMuzzleFlash != null) {
            ParticleSystem instance = Instantiate(weaponMuzzleFlash);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.Play(true);
            Destroy(instance.gameObject, remoteMuzzleFlashLifetime);
            return;
        }

        if(remoteMuzzleFlashPrefab == null) {
            return;
        }

        SpawnTimedPrefab(remoteMuzzleFlashPrefab, position, rotation, remoteMuzzleFlashLifetime);
    }

    private void SpawnRemoteImpactVfx(Vector3 position, Quaternion rotation) {
        if(remoteImpactVfxPrefab == null) {
            return;
        }

        SpawnTimedPrefab(remoteImpactVfxPrefab, position, rotation, remoteImpactVfxLifetime);
    }

    private static void SpawnTimedPrefab(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime) {
        GameObject instance = Instantiate(prefab, position, rotation);
        Destroy(instance, lifetime);
    }

    private ParticleSystem GetRemoteWeaponMuzzleFlashPrefab(int weaponId) {
        CacheRemoteWeaponShotFx();

        if(_remoteWeaponMuzzleFlashPrefabs == null || weaponId < 0 || weaponId >= _remoteWeaponMuzzleFlashPrefabs.Length) {
            return null;
        }

        return _remoteWeaponMuzzleFlashPrefabs[weaponId];
    }

    private void CacheRemoteWeaponShotFx() {
        if(_remoteWeaponMuzzleFlashPrefabs != null || activeWeapon == null) {
            return;
        }

        WeaponSO[] weaponList = activeWeapon.WeaponList;
        if(weaponList == null || weaponList.Length == 0) {
            return;
        }

        int maxWeaponId = -1;
        foreach(WeaponSO weaponSo in weaponList) {
            if(weaponSo != null) {
                maxWeaponId = Mathf.Max(maxWeaponId, weaponSo.ID);
            }
        }

        if(maxWeaponId < 0) {
            return;
        }

        _remoteWeaponMuzzleFlashPrefabs = new ParticleSystem[maxWeaponId + 1];

        foreach(WeaponSO weaponSo in weaponList) {
            if(weaponSo == null || weaponSo.weaponPrefab == null || weaponSo.ID < 0 || weaponSo.ID >= _remoteWeaponMuzzleFlashPrefabs.Length) {
                continue;
            }

            Weapon weaponPrefabComponent = weaponSo.weaponPrefab.GetComponent<Weapon>();
            if(weaponPrefabComponent == null || weaponPrefabComponent.MuzzleFlash == null) {
                continue;
            }

            _remoteWeaponMuzzleFlashPrefabs[weaponSo.ID] = weaponPrefabComponent.MuzzleFlash;
        }
    }

#region Helper method get shot property
    private Vector3 GetShotOrigin() {
        Transform target = cameraTarget != null ? cameraTarget : transform;
        return target.position + target.forward * 0.35f;
    }

    private Vector3 GetShotForward() {
        Transform target = cameraTarget != null ? cameraTarget : transform;
        return target.forward;
    }

    private static Material GetRemoteTracerMaterial() {
        if(_remoteTracerMaterial == null) {
            Shader shader = Shader.Find("Sprites/Default");
            if(shader == null) {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            _remoteTracerMaterial = new Material(shader);
        }

        return _remoteTracerMaterial;
    }
#endregion

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

        CacheRemoteWeaponShotFx();
        DisableLegacyControllerIfNeeded();
    }

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

#region Helper method help player find/set object in game scene
    private static CinemachineVirtualCamera FindSceneCamera(string cameraName) {
        var cameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(var camera in cameras) {
            if(camera.name == cameraName) {
                return camera;
            }
        }

        return null;
    }

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
#endregion
}

public static class PlayerNameStorage {
    public const string PlayerNamePrefsKey = "PLAYER_NAME";
    public const string DefaultPlayerName = "Player";
    public const int MaxPlayerNameLength = 16;

    public static string GetPlayerName() {
        return Sanitize(PlayerPrefs.GetString(PlayerNamePrefsKey, DefaultPlayerName));
    }

    public static void SavePlayerName(string playerName) {
        PlayerPrefs.SetString(PlayerNamePrefsKey, Sanitize(playerName));
        PlayerPrefs.Save();
    }

    public static string Sanitize(string playerName) {
        string resolvedName = string.IsNullOrWhiteSpace(playerName)
            ? DefaultPlayerName
            : playerName.Trim();

        if(resolvedName.Length > MaxPlayerNameLength) {
            resolvedName = resolvedName.Substring(0, MaxPlayerNameLength);
        }

        return resolvedName;
    }
}
