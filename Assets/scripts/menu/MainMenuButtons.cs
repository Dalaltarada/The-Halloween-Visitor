using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    public const string VolumeKey = "THV_MasterVolume";
    public const string FullscreenKey = "THV_Fullscreen";
    public const string WidthKey = "THV_ResolutionWidth";
    public const string HeightKey = "THV_ResolutionHeight";
    public const string SensitivityKey = "THV_MouseSensitivity";

    [Header("Gameplay Scene")]
    public string firstSceneName = "House";

    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("Start Fade")]
    public CanvasGroup blackFade;
    [Min(0f)] public float fadeDuration = 1.2f;

    [Header("Sound - All Audio")]
    public Slider volumeSlider;

    [Header("Screen")]
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    [Header("Mouse Sensitivity")]
    public Slider sensitivitySlider;
    [Range(0.1f, 10f)] public float defaultSensitivity = 2f;

    private readonly List<Resolution> availableResolutions = new List<Resolution>();
    private bool startingGame;

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        if (blackFade != null)
        {
            blackFade.alpha = 0f;
            blackFade.interactable = false;
            blackFade.blocksRaycasts = false;
        }

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        SetupResolutions();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 10f;
            sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SensitivityKey, defaultSensitivity));
        }
    }

    private void SetupResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;
        foreach (Resolution r in resolutions)
        {
            // Show unique width/height combinations (ignore duplicate refresh rates).
            if (!availableResolutions.Exists(x => x.width == r.width && x.height == r.height))
                availableResolutions.Add(r);
        }

        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        List<string> labels = new List<string>();
        foreach (Resolution r in availableResolutions)
            labels.Add(r.width + " x " + r.height);
        resolutionDropdown.AddOptions(labels);

        int desiredWidth = PlayerPrefs.GetInt(WidthKey, Screen.width);
        int desiredHeight = PlayerPrefs.GetInt(HeightKey, Screen.height);
        int selected = availableResolutions.FindIndex(r => r.width == desiredWidth && r.height == desiredHeight);
        if (selected < 0) selected = availableResolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
        if (selected < 0) selected = Mathf.Max(0, availableResolutions.Count - 1);
        resolutionDropdown.SetValueWithoutNotify(selected);
        resolutionDropdown.RefreshShownValue();
    }

    public void StartGame()
    {
        if (!startingGame) StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        startingGame = true;
        if (string.IsNullOrWhiteSpace(firstSceneName))
        {
            Debug.LogError("[Main Menu] Assign First Scene Name.");
            startingGame = false;
            yield break;
        }

        if (blackFade != null)
        {
            blackFade.blocksRaycasts = true;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                blackFade.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            blackFade.alpha = 1f;
        }
        SceneManager.LoadScene(firstSceneName);
    }

    public void OpenSettings()
    {
        if (startingGame) return;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        if (startingGame) return;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        if (startingGame) return;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Count) return;
        Resolution r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt(WidthKey, r.width);
        PlayerPrefs.SetInt(HeightKey, r.height);
        PlayerPrefs.Save();
    }

    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.1f, 10f));
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[Main Menu] Quit works in a built game.");
#else
        Application.Quit();
#endif
    }
}
