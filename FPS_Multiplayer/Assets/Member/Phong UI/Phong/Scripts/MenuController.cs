using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("New Game Level")]
    public string _newgamelevel;

    private string LevelToLoad;

    [Header("UI")]
    [SerializeField] private GameObject mainMenu;          // menu chính
    [SerializeField] private GameObject newGameDialog;     // dialog New Game
    [SerializeField] private GameObject loadGameDialog;    // dialog Load Game
    [SerializeField] private GameObject noSaveGameDialog;  // dialog No Save

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text VolumeTextValue = null;
    [SerializeField] private Slider VolumeSlider = null;
    [SerializeField] private float defaultVolume = 1f;

    [SerializeField] private GameObject comfirmationPrompt = null;
    // ===== NEW GAME =====
    public void OpenNewGameDialog()
    {
        mainMenu.SetActive(false);
        newGameDialog.SetActive(true);
    }

    public void NewGameDialogYes()
    {
        SceneManager.LoadScene(_newgamelevel);
    }

    // ===== LOAD GAME =====
    public void OpenLoadGameDialog()
    {
        mainMenu.SetActive(false);
        loadGameDialog.SetActive(true);
    }

    public void LoadGameDialogYes()
    {
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            LevelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneManager.LoadScene(LevelToLoad);
        }
        else
        {
            loadGameDialog.SetActive(false);
            noSaveGameDialog.SetActive(true);
        }
    }

    // ===== NO / BACK =====
    public void DialogNo()
    {
        newGameDialog.SetActive(false);
        loadGameDialog.SetActive(false);
        noSaveGameDialog.SetActive(false);
        mainMenu.SetActive(true);
    }

    // ===== EXIT =====
    public void ExitButton()
    {
        Application.Quit();
    }
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        VolumeTextValue.text = volume.ToString("0.0");
    }
    public void VolumeApplyButton()
    {
        PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        StartCoroutine(ConfirmationBox());
    }
    public void ResetButton(string MenuType)
    {
        if (MenuType == "Volume")
        {
            AudioListener.volume = defaultVolume;
            VolumeSlider.value = defaultVolume;
            VolumeTextValue.text = defaultVolume.ToString("0.0");
            VolumeApplyButton();    
        }
    }
    private IEnumerator ConfirmationBox()
    {
        comfirmationPrompt .SetActive(true);
        yield return new WaitForSeconds(2);
        comfirmationPrompt .SetActive(false);
    }
}

