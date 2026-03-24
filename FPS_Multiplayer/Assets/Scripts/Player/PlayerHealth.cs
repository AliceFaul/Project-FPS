using System;
using StarterAssets;
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

    static int loadedHealth;

    int currentHealth;
    // int gameOverVCPriority = 20;

    public int CurrentHealth => currentHealth;
    public int StartHealth => startHealth;

    void Awake()
    {
        if(loadedHealth == 0) currentHealth = startHealth;
        else currentHealth = loadedHealth;
        AdjustShieldUI();
    }

    public void AdjustHealth(int amount)
    {
        currentHealth += amount;
        AdjustShieldUI();
        globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration);
        float chromaticModifier = 1 - currentHealth / (float)startHealth;
        chromaticAberration.intensity.value = chromaticModifier;
        if (currentHealth <= 0)
        {
            PlayerGameOver();
        }
    }

    void PlayerGameOver()
    {
        weaponCamera.parent = null;
        //deathVirtualCamera.Priority = gameOverVCPriority;
        gameOverContainer.SetActive(true);
        StarterAssetsInputs starterAssetsInputs = FindAnyObjectByType<StarterAssetsInputs>();
        starterAssetsInputs.SetCursorState(false);
        Destroy(gameObject);
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