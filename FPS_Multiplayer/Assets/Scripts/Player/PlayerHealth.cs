using System;
using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.Cinemachine;

public class PlayerHealth : MonoBehaviour
{
    [Range(2, 10)]
    [SerializeField] int startHealth = 10;
    [SerializeField] CinemachineVirtualCamera deathVirtualCamera;
    [SerializeField] Transform weaponCamera;
    [SerializeField] Image[] shieldBars;
    [SerializeField] GameObject gameOverContainer;
    [SerializeField] Volume globalVolume;
    [SerializeField] float respawnDelay = 2f;

    static int loadedHealth;

    int currentHealth;
    // int gameOverVCPriority = 20;

    public int CurrentHealth => currentHealth;
    public int StartHealth => startHealth;

    private bool _isInitialized = false;
    private bool _isRespawning = false;

    // void Awake()
    // {
    //     if(loadedHealth == 0) currentHealth = startHealth;
    //     else currentHealth = loadedHealth;
    //     AdjustShieldUI();
    // }

    // Initialize is called from PlayerNetworkSetup when the player spawns in, 
    // to set up references to the UI and cameras, 
    // and to set the player's health based on loadedHealth or startHealth.
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
        if(loadedHealth == 0) currentHealth = startHealth;
        else currentHealth = loadedHealth;
        AdjustShieldUI();
        _isInitialized = true;
    }

    public void AdjustHealth(int amount)
    {
        if (!_isInitialized || _isRespawning) {
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, startHealth);
        AdjustShieldUI();
        globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration);
        float chromaticModifier = 1 - currentHealth / (float)startHealth;
        chromaticAberration.intensity.value = chromaticModifier;
        if (currentHealth <= 0)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    IEnumerator RespawnRoutine()
    {
        _isRespawning = true;

        FirstPersonController firstPersonController = GetComponent<FirstPersonController>();
        FusionPlayerController fusionPlayerController = GetComponent<FusionPlayerController>();
        CharacterController characterController = GetComponent<CharacterController>();
        StarterAssetsInputs starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        NetworkRunnerManager runnerManager = FindFirstObjectByType<NetworkRunnerManager>();

        if (fusionPlayerController != null) {
            fusionPlayerController.enabled = false;
        } else if (firstPersonController != null) {
            firstPersonController.enabled = false;
        }

        if (characterController != null) {
            characterController.enabled = false;
        }

        if (weaponCamera != null) {
            weaponCamera.gameObject.SetActive(false);
        }

        if (gameOverContainer != null) {
            gameOverContainer.SetActive(true);
        }

        if (starterAssetsInputs != null) {
            starterAssetsInputs.SetCursorState(false);
        }

        yield return new WaitForSeconds(respawnDelay);

        if (runnerManager != null) {
            runnerManager.RespawnPlayer(this);
        }

        currentHealth = startHealth;
        AdjustShieldUI();

        if (globalVolume != null && globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration)) {
            chromaticAberration.intensity.value = 0f;
        }

        if (gameOverContainer != null) {
            gameOverContainer.SetActive(false);
        }

        if (weaponCamera != null) {
            weaponCamera.gameObject.SetActive(true);
        }

        if (characterController != null) {
            characterController.enabled = true;
        }

        if (fusionPlayerController != null) {
            fusionPlayerController.enabled = true;
        } else if (firstPersonController != null) {
            firstPersonController.enabled = true;
        }

        if (starterAssetsInputs != null) {
            starterAssetsInputs.SetCursorState(true);
        }

        _isRespawning = false;
    }

    void AdjustShieldUI()
    {
        for (int i = 0; i < shieldBars.Length; i++)
        {
            if (i < currentHealth)
            {
                shieldBars[i].enabled = true;
            }
            else
            {
                shieldBars[i].enabled = false;
            }
        }
    }

    public void LoadHealth()
    {
        loadedHealth = currentHealth;
    }
}
