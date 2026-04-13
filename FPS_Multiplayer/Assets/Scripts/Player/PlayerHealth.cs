using Fusion;
using System.Collections.Generic;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public struct RendererFlashState {
    public Renderer Renderer;
    public string ColorProperty;
    public Color BaseColor;
}

public class PlayerHealth : NetworkBehaviour {
    [Range(2, 10)]
    [SerializeField] private int startHealth = 10;
    [SerializeField] private CinemachineVirtualCamera deathVirtualCamera;
    [SerializeField] private Transform weaponCamera;
    [SerializeField] private Image[] shieldBars;
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float respawnDelay = 2f;

    [Header("Damage Flash")]
    [SerializeField] private Renderer[] damageFlashRenderers;

    [Header("Debug")]
    [SerializeField] private int debugHealthStep = 1;
    [SerializeField] private Color damageFlashColor = new(1f, 0.25f, 0.25f, 1f);
    [SerializeField] [Range(0f, 1f)] private float damageFlashStrength = 0.85f;
    [SerializeField] private float damageFlashDuration = 0.12f;

    [Networked] private int NetworkHealth { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] private TickTimer DamageFlashTimer { get; set; }

    private static int loadedHealth;

    private StarterAssetsInputs _starterAssetsInputs;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private RendererFlashState[] _flashStates;
    private MaterialPropertyBlock _propertyBlock;
    private int _lastRenderedHealth = -1;
    private bool _isInitialized;
    private bool _localDeathStateApplied;

    public int CurrentHealth => GetDisplayHealth();
    public int StartHealth => startHealth;

    // Cache component & damage flash renderer
    // if object is StateAuthority and NetworkHealth isn't correct, will set NetworkHealth by startHealth
    public override void Spawned() {
        CacheComponents();
        CacheDamageFlashRenderers();
        if (Object.HasStateAuthority && NetworkHealth <= 0) {
            NetworkHealth = GetFallbackHealth();
        }
        _lastRenderedHealth = -1;
        ApplyDamageFlash(false);
    }

    // run core logic if object has StateAuthority (host)
    // clear timer, reset health, respawn player by NetworkRunnerManager
    public override void FixedUpdateNetwork() {
        // Check if player is the host to continue run core logic
        if (!Object.HasStateAuthority) {
            return;
        }

        if (RespawnTimer.Expired(Runner)) {
            RespawnTimer = TickTimer.None;
            DamageFlashTimer = TickTimer.None;
            NetworkHealth = startHealth;

            // Respawn player by NetworkRunnerManager
            NetworkRunnerManager runnerManager = FindFirstObjectByType<NetworkRunnerManager>();
            if (runnerManager != null) {
                runnerManager.RespawnPlayer(this);
            }
        }
    }

    // run display logic like update UI/chromatic
    // apply local death state, damage flash in body 
    public override void Render() {
        // get displayed health
        int displayedHealth = GetDisplayHealth();
        if (_lastRenderedHealth != displayedHealth) {
            ApplyHealthVisuals(displayedHealth); // update health UI
            _lastRenderedHealth = displayedHealth;
        }
        // check if player in waiting for respawn state
        bool isRespawning = Runner != null && RespawnTimer.IsRunning && !RespawnTimer.ExpiredOrNotRunning(Runner);
        ApplyLocalDeathState(Object != null && Object.HasInputAuthority && isRespawning && NetworkHealth <= 0);
        ApplyDamageFlash(Runner != null && DamageFlashTimer.IsRunning && !DamageFlashTimer.ExpiredOrNotRunning(Runner));
    }

    // initialize reference in sceneSetup object in gameplay scene
    // cache component and damageFlashRenderer
    public void Initialize(
        CinemachineVirtualCamera deathVirtualCamera,
        Transform weaponCamera,
        Image[] shieldBars,
        GameObject gameOverContainer,
        Volume globalVolume)
    {
        this.deathVirtualCamera = deathVirtualCamera;
        this.weaponCamera = weaponCamera;
        this.shieldBars = shieldBars;
        this.gameOverContainer = gameOverContainer;
        this.globalVolume = globalVolume;

        CacheComponents();
        CacheDamageFlashRenderers();

        _isInitialized = true;
        ApplyHealthVisuals(GetDisplayHealth());
    }

    // main function to change player health by get damaged or heal
    public void AdjustHealth(int amount) {
        if (amount == 0) {
            return;
        }
        if (Runner == null || Object == null) {
            return;
        }
        // if player is host, will directly change health
        if (Object.HasStateAuthority) {
            ApplyHealthChange(amount);
            return;
        }
        // if player isn't host, sent change health request to host
        RPC_RequestHealthChange(amount);
    }

    public void DebugTakeDamage() {
        AdjustHealth(-Mathf.Max(1, debugHealthStep));
    }

    public void DebugHeal() {
        AdjustHealth(Mathf.Max(1, debugHealthStep));
    }

    [ContextMenu("Debug/Take Damage")]
    private void DebugTakeDamageFromContextMenu() {
        DebugTakeDamage();
    }

    [ContextMenu("Debug/Heal")]
    private void DebugHealFromContextMenu() {
        DebugHeal();
    }

    // network function to sent change health request
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] // all users can sent a request, host will get and execute change heath
    private void RPC_RequestHealthChange(int amount) {
        ApplyHealthChange(amount);
    }

    // save current health in static field
    // can set health by this field in new scene
    public void LoadHealth() {
        loadedHealth = CurrentHealth <= 0 ? startHealth : CurrentHealth;
    }

    // use in function AdjustHealth, update current health
    // if amount is positive = heal, negative = damaged
    private void ApplyHealthChange(int amount) {
        // check if player in waiting for respawn state
        if (RespawnTimer.IsRunning) {
            return;
        }
        // get new health 
        int nextHealth = Mathf.Clamp(NetworkHealth + amount, 0, startHealth);

        if (nextHealth == NetworkHealth) {
            return;
        }

        NetworkHealth = nextHealth;
        // if player get damaged, active damageFlashTimer
        if (amount < 0) {
            DamageFlashTimer = TickTimer.CreateFromSeconds(Runner, damageFlashDuration);
        }
        // if player die, active respawnTimer
        if (NetworkHealth <= 0) {
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
        }
    }
    
    // return health use to display in UI
    private int GetDisplayHealth() {
        // if not have runner/object, get fallback
        if (Runner == null || Object == null) {
            return GetFallbackHealth();
        }
        // if networkHealth isn't correct & not in waiting to respawn state, get fallback
        if (NetworkHealth <= 0 && !RespawnTimer.IsRunning) {
            return GetFallbackHealth();
        }

        return NetworkHealth;
    }

    // get safe value until network state stable
    private int GetFallbackHealth() {
        // if not have loadedHealth, will return startHealth
        if (loadedHealth <= 0) {
            return startHealth;
        }
        // clamp loadedHealth to return
        return Mathf.Clamp(loadedHealth, 0, startHealth);
    }

    // function to cache component, need for local death
    private void CacheComponents() {
        if (_starterAssetsInputs == null) {
            _starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        }

#if ENABLE_INPUT_SYSTEM
        if (_playerInput == null) {
            _playerInput = GetComponent<PlayerInput>();
        }
#endif
    }

    // function to update shield bars UI, update chromatic aberration
    // use only for local player, only read currentHealth synced and update
    private void ApplyHealthVisuals(int currentHealth) {
        // check if player is local player
        if (!_isInitialized || !Object.HasInputAuthority) {
            return;
        }
        // update shield UI
        AdjustShieldUI(currentHealth);
        // update chromatic aberration
        if (globalVolume != null &&
            globalVolume.profile != null &&
            globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
        {
            float chromaticModifier = 1f - currentHealth / (float)startHealth;
            chromaticAberration.intensity.value = chromaticModifier;
        }
    }

    // active local death state when player die 
    private void ApplyLocalDeathState(bool shouldApply) {
        if (!_isInitialized || Object == null || !Object.HasInputAuthority || _localDeathStateApplied == shouldApply) {
            return;
        }
        // clear input when die
        if (shouldApply) {
            ClearLocalInputState();
        }
        // unactive playerInput, weapon camera
        // active gameOver UI and unlock cursor
#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null) {
            _playerInput.enabled = !shouldApply;
        }
#endif
        if (weaponCamera != null) {
            weaponCamera.gameObject.SetActive(!shouldApply);
        }
        if (gameOverContainer != null) {
            gameOverContainer.SetActive(shouldApply);
        }
        if (_starterAssetsInputs != null) {
            _starterAssetsInputs.SetCursorState(!shouldApply);
        }
        _localDeathStateApplied = shouldApply;
    }

    // helper function to clear local input
    private void ClearLocalInputState() {
        if (_starterAssetsInputs == null) {
            return;
        }

        _starterAssetsInputs.MoveInput(Vector2.zero);
        _starterAssetsInputs.LookInput(Vector2.zero);
        _starterAssetsInputs.JumpInput(false);
        _starterAssetsInputs.SprintInput(false);
        _starterAssetsInputs.ShootInput(false);
        _starterAssetsInputs.ZoomInput(false);
    }

    // update shield bars UI by currentHealth
    private void AdjustShieldUI(int currentHealth) {
        if (shieldBars == null) {
            return;
        }

        for (int i = 0; i < shieldBars.Length; i++) {
            if (shieldBars[i] == null) {
                continue;
            }

            shieldBars[i].enabled = i < currentHealth;
        }
    }

    // auto cache damage flash renderer
    // set data to tint red body
    private void CacheDamageFlashRenderers() {
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0) {
            Transform firstPersonRoot = transform.Find("PlayerCameraRoot");
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            List<Renderer> rendererList = new();
            // Add to rendererList
            foreach (Renderer candidate in allRenderers) {
                // check null
                if (candidate == null) {
                    continue;
                }
                // check continue if is first-person root
                if (firstPersonRoot != null && candidate.transform.IsChildOf(firstPersonRoot)) {
                    continue;
                }

                rendererList.Add(candidate);
            }
            // add to damageFlashRenderers
            damageFlashRenderers = rendererList.ToArray();
        }
        // create a new RendererFlashState
        _flashStates = new RendererFlashState[damageFlashRenderers.Length];
        // create a new materialPropertyBlock to reduce draw call when change body color
        _propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < damageFlashRenderers.Length; i++) {
            Renderer renderer = damageFlashRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null) {
                continue;
            }
            string colorProperty = renderer.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Color baseColor = renderer.sharedMaterial.HasProperty(colorProperty)
                ? renderer.sharedMaterial.GetColor(colorProperty)
                : Color.white;

            _flashStates[i] = new RendererFlashState {
                Renderer = renderer,
                ColorProperty = colorProperty,
                BaseColor = baseColor
            };
        }
    }

    // function apply damage flash
    private void ApplyDamageFlash(bool flashActive) {
        // check null
        if (_flashStates == null || _propertyBlock == null) {
            return;
        }
        // change body color to damage color
        for (int i = 0; i < _flashStates.Length; i++) {
            RendererFlashState state = _flashStates[i];
            if (state.Renderer == null || string.IsNullOrEmpty(state.ColorProperty)) {
                continue;
            }
            // if active, lerp base color to damage color
            // else hold base color
            Color targetColor = flashActive
                ? Color.Lerp(state.BaseColor, damageFlashColor, damageFlashStrength)
                : state.BaseColor;
            // use materialPropertyBlock set body color
            // materialPropertyBlock reduce draw call
            state.Renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(state.ColorProperty, targetColor);
            state.Renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

