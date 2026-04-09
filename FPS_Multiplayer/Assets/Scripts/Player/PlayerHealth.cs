using Fusion;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    private struct RendererFlashState
    {
        public Renderer Renderer;
        public string ColorProperty;
        public Color BaseColor;
    }

    public override void Spawned()
    {
        CacheComponents();
        CacheDamageFlashRenderers();

        if (Object.HasStateAuthority && NetworkHealth <= 0)
        {
            NetworkHealth = GetFallbackHealth();
        }

        _lastRenderedHealth = -1;
        ApplyDamageFlash(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (RespawnTimer.Expired(Runner))
        {
            RespawnTimer = TickTimer.None;
            DamageFlashTimer = TickTimer.None;
            NetworkHealth = startHealth;

            NetworkRunnerManager runnerManager = FindFirstObjectByType<NetworkRunnerManager>();
            if (runnerManager != null)
            {
                runnerManager.RespawnPlayer(this);
            }
        }
    }

    public override void Render()
    {
        int displayedHealth = GetDisplayHealth();

        if (_lastRenderedHealth != displayedHealth)
        {
            ApplyHealthVisuals(displayedHealth);
            _lastRenderedHealth = displayedHealth;
        }

        bool isRespawning = Runner != null && RespawnTimer.IsRunning && !RespawnTimer.ExpiredOrNotRunning(Runner);
        ApplyLocalDeathState(Object != null && Object.HasInputAuthority && isRespawning && NetworkHealth <= 0);
        ApplyDamageFlash(Runner != null && DamageFlashTimer.IsRunning && !DamageFlashTimer.ExpiredOrNotRunning(Runner));
    }

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

    public void AdjustHealth(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        if (Runner == null || Object == null)
        {
            return;
        }

        if (Object.HasStateAuthority)
        {
            ApplyHealthChange(amount);
            return;
        }

        RPC_RequestHealthChange(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestHealthChange(int amount)
    {
        ApplyHealthChange(amount);
    }

    public void LoadHealth()
    {
        loadedHealth = CurrentHealth <= 0 ? startHealth : CurrentHealth;
    }

    private void ApplyHealthChange(int amount)
    {
        if (RespawnTimer.IsRunning)
        {
            return;
        }

        int nextHealth = Mathf.Clamp(NetworkHealth + amount, 0, startHealth);

        if (nextHealth == NetworkHealth)
        {
            return;
        }

        NetworkHealth = nextHealth;

        if (amount < 0)
        {
            DamageFlashTimer = TickTimer.CreateFromSeconds(Runner, damageFlashDuration);
        }

        if (NetworkHealth <= 0)
        {
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
        }
    }

    private int GetDisplayHealth()
    {
        if (Runner == null || Object == null)
        {
            return GetFallbackHealth();
        }

        if (NetworkHealth <= 0 && !RespawnTimer.IsRunning)
        {
            return GetFallbackHealth();
        }

        return NetworkHealth;
    }

    private int GetFallbackHealth()
    {
        if (loadedHealth <= 0)
        {
            return startHealth;
        }

        return Mathf.Clamp(loadedHealth, 0, startHealth);
    }

    private void CacheComponents()
    {
        if (_starterAssetsInputs == null)
        {
            _starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        }

#if ENABLE_INPUT_SYSTEM
        if (_playerInput == null)
        {
            _playerInput = GetComponent<PlayerInput>();
        }
#endif
    }

    private void ApplyHealthVisuals(int currentHealth)
    {
        if (!_isInitialized || !Object.HasInputAuthority)
        {
            return;
        }

        AdjustShieldUI(currentHealth);

        if (globalVolume != null &&
            globalVolume.profile != null &&
            globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
        {
            float chromaticModifier = 1f - currentHealth / (float)startHealth;
            chromaticAberration.intensity.value = chromaticModifier;
        }
    }

    private void ApplyLocalDeathState(bool shouldApply)
    {
        if (!_isInitialized || Object == null || !Object.HasInputAuthority || _localDeathStateApplied == shouldApply)
        {
            return;
        }

        if (shouldApply)
        {
            ClearLocalInputState();
        }

#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            _playerInput.enabled = !shouldApply;
        }
#endif

        if (weaponCamera != null)
        {
            weaponCamera.gameObject.SetActive(!shouldApply);
        }

        if (gameOverContainer != null)
        {
            gameOverContainer.SetActive(shouldApply);
        }

        if (_starterAssetsInputs != null)
        {
            _starterAssetsInputs.SetCursorState(!shouldApply);
        }

        _localDeathStateApplied = shouldApply;
    }

    private void ClearLocalInputState()
    {
        if (_starterAssetsInputs == null)
        {
            return;
        }

        _starterAssetsInputs.MoveInput(Vector2.zero);
        _starterAssetsInputs.LookInput(Vector2.zero);
        _starterAssetsInputs.JumpInput(false);
        _starterAssetsInputs.SprintInput(false);
        _starterAssetsInputs.ShootInput(false);
        _starterAssetsInputs.ZoomInput(false);
    }

    private void AdjustShieldUI(int currentHealth)
    {
        if (shieldBars == null)
        {
            return;
        }

        for (int i = 0; i < shieldBars.Length; i++)
        {
            if (shieldBars[i] == null)
            {
                continue;
            }

            shieldBars[i].enabled = i < currentHealth;
        }
    }

    private void CacheDamageFlashRenderers()
    {
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0)
        {
            Transform firstPersonRoot = transform.Find("PlayerCameraRoot");
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Renderer> rendererList = new();

            foreach (Renderer candidate in allRenderers)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (firstPersonRoot != null && candidate.transform.IsChildOf(firstPersonRoot))
                {
                    continue;
                }

                rendererList.Add(candidate);
            }

            damageFlashRenderers = rendererList.ToArray();
        }

        _flashStates = new RendererFlashState[damageFlashRenderers.Length];
        _propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            Renderer renderer = damageFlashRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            string colorProperty = renderer.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Color baseColor = renderer.sharedMaterial.HasProperty(colorProperty)
                ? renderer.sharedMaterial.GetColor(colorProperty)
                : Color.white;

            _flashStates[i] = new RendererFlashState
            {
                Renderer = renderer,
                ColorProperty = colorProperty,
                BaseColor = baseColor
            };
        }
    }

    private void ApplyDamageFlash(bool flashActive)
    {
        if (_flashStates == null || _propertyBlock == null)
        {
            return;
        }

        for (int i = 0; i < _flashStates.Length; i++)
        {
            RendererFlashState state = _flashStates[i];
            if (state.Renderer == null || string.IsNullOrEmpty(state.ColorProperty))
            {
                continue;
            }

            Color targetColor = flashActive
                ? Color.Lerp(state.BaseColor, damageFlashColor, damageFlashStrength)
                : state.BaseColor;

            state.Renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(state.ColorProperty, targetColor);
            state.Renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
