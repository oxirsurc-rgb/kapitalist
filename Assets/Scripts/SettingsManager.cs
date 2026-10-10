using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// MainMenu (veya Gameplay) sahnesindeki bir "SettingsPanel" üzerine eklenir.
// Inspector'dan slider/dropdown/toggle referanslarını sürükleyin.
public class SettingsManager : MonoBehaviour
{
    [Header("Ses")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Görüntü")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Dil")]
    public TMP_Dropdown languageDropdown;

    private Resolution[] resolutions;

    void OnEnable()
    {
        LocalizationManager.LoadSavedLanguage();
        SetupResolutions();
        SetupVolume();
        SetupLanguage();
        SetupFullscreen();
    }

    private void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;
        var r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
    }

    private void SetupFullscreen()
    {
        if (fullscreenToggle == null) return;
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
    }

private void SetupVolume()
{
    if (musicVolumeSlider != null)
    {
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.AddListener(v => {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
            else PlayerPrefs.SetFloat("MusicVolume", v);   // Fallback
        });
    }
    if (sfxVolumeSlider != null)
    {
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.AddListener(v => {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
            else PlayerPrefs.SetFloat("SfxVolume", v);
        });
    }
}

    private void SetupLanguage()
    {
        if (languageDropdown == null) return;
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string> { "Türkçe", "English" });
        languageDropdown.value = (int)LocalizationManager.CurrentLanguage;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(i => LocalizationManager.SetLanguage((Language)i));
    }
}
