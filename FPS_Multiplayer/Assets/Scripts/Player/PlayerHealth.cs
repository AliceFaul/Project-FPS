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

    string lastAttacker = "";
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
        //  tên người chết
        string victimName = gameObject.name;

        //  KILL FEED
        if (KillFeedManager.Instance != null)
        {
            KillFeedManager.Instance.AddKill(lastAttacker, victimName);
        }

        // 🏆 LEADERBOARD
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddKill(lastAttacker, victimName);
        }

        //  tách camera
        if (weaponCamera != null)
        {
            weaponCamera.parent = null;
        }

        //  UI game over
        if (gameOverContainer != null)
        {
            gameOverContainer.SetActive(true);
        }

        //  mở chuột
        StarterAssetsInputs starterAssetsInputs = FindAnyObjectByType<StarterAssetsInputs>();
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.SetCursorState(false);
        }

        //  xoá player
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

    public void SetLastAttacker(string attacker)
    {
        lastAttacker = attacker;
    }
}