using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VanGame.UI
{
  public class MainMenuController : MonoBehaviour
  {
    [Header("Menu buttons")]
    [SerializeField] MainMenuButtonVisual startButton;
    [SerializeField] MainMenuButtonVisual settingsButton;
    [SerializeField] MainMenuButtonVisual quitButton;

    [Header("Settings panel")]
    [SerializeField] GameObject settingsPanelRoot;
    [SerializeField] Slider soundSlider;
    [Tooltip("When checked, Start skips the tutorial scene and loads the main game directly.")]
    [SerializeField] Toggle skipTutorialToggle;
    [SerializeField] Button settingsCloseButton;

    [Header("Scenes")]
    [SerializeField] string tutorialSceneName = "TOT";
    [SerializeField] string gameSceneName = "Main game";

    bool _settingsOpen;

    void Awake()
    {
      ResolveReferencesIfNeeded();
      MainMenuSettings.ApplySavedAudio();

      if (settingsPanelRoot != null)
        settingsPanelRoot.SetActive(false);

      if (soundSlider != null)
      {
        soundSlider.SetValueWithoutNotify(MainMenuSettings.MasterVolume);
        soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
      }

      if (skipTutorialToggle != null)
      {
        skipTutorialToggle.SetIsOnWithoutNotify(MainMenuSettings.SkipTutorial);
        skipTutorialToggle.onValueChanged.AddListener(OnSkipTutorialChanged);
      }
    }

    void Start()
    {
      WireButton(startButton, OnStartClicked);
      WireButton(settingsButton, OnSettingsClicked);
      WireButton(quitButton, OnQuitClicked);

      if (settingsCloseButton != null)
        settingsCloseButton.onClick.AddListener(CloseSettings);
    }

    void WireButton(MainMenuButtonVisual visual, UnityAction action)
    {
      if (visual == null || action == null)
        return;

      Button button = visual.ClickButton;
      if (button != null)
        button.onClick.AddListener(action);
    }

    void OnStartClicked()
    {
      string nextScene = MainMenuSettings.SkipTutorial ? gameSceneName : tutorialSceneName;
      SceneManager.LoadScene(nextScene);
    }

    void OnSettingsClicked()
    {
      if (settingsPanelRoot == null)
        return;

      _settingsOpen = !_settingsOpen;
      settingsPanelRoot.SetActive(_settingsOpen);
    }

    public void CloseSettings()
    {
      _settingsOpen = false;
      if (settingsPanelRoot != null)
        settingsPanelRoot.SetActive(false);
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    void OnSoundVolumeChanged(float value)
    {
      MainMenuSettings.MasterVolume = value;
    }

    void OnSkipTutorialChanged(bool skipTutorial)
    {
      MainMenuSettings.SkipTutorial = skipTutorial;
    }

    void ResolveReferencesIfNeeded()
    {
      MainMenuButtonVisual[] buttonVisuals = GetComponentsInChildren<MainMenuButtonVisual>(true);
      foreach (MainMenuButtonVisual visual in buttonVisuals)
      {
        string objectName = visual.NormalVisual != null
          ? visual.NormalVisual.name.ToLowerInvariant()
          : visual.name.ToLowerInvariant();

        if (startButton == null && objectName == "start")
          startButton = visual;
        else if (settingsButton == null && objectName == "settings")
          settingsButton = visual;
        else if (quitButton == null && objectName == "quit")
          quitButton = visual;
      }

      if (settingsPanelRoot != null)
        return;

      Transform canvasRoot = transform.parent;
      if (canvasRoot == null)
        return;

      foreach (Transform child in canvasRoot)
      {
        if (child == transform)
          continue;

        if (child.name.ToLowerInvariant().Contains("settings"))
        {
          settingsPanelRoot = child.gameObject;
          break;
        }
      }
    }
  }
}
