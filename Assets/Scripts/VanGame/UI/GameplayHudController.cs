using UnityEngine;

namespace VanGame.UI
{
  /// <summary>
  /// Wires in-game sound mute and pause / continue toggles.
  /// </summary>
  public class GameplayHudController : MonoBehaviour
  {
    [SerializeField] DualStateToggleButton soundToggle;
    [SerializeField] DualStateToggleButton pauseToggle;

    float _volumeBeforeMute = 1f;
    bool _initialized;

    void Awake()
    {
      InitializeSoundState();
      InitializePauseState();

      if (soundToggle != null)
        soundToggle.StateChanged += OnSoundStateChanged;

      if (pauseToggle != null)
        pauseToggle.StateChanged += OnPauseStateChanged;

      _initialized = true;
    }

    void OnDestroy()
    {
      if (soundToggle != null)
        soundToggle.StateChanged -= OnSoundStateChanged;

      if (pauseToggle != null)
        pauseToggle.StateChanged -= OnPauseStateChanged;

      if (pauseToggle != null && !pauseToggle.IsStateA)
        Time.timeScale = 1f;
    }

    void InitializeSoundState()
    {
      if (soundToggle == null)
        return;

      float savedVolume = MainMenuSettings.MasterVolume;
      bool soundOn = savedVolume > 0.01f;

      if (soundOn)
        _volumeBeforeMute = savedVolume;
      else
        _volumeBeforeMute = 1f;

      soundToggle.SetState(soundOn, animate: false);
    }

    void InitializePauseState()
    {
      if (pauseToggle == null)
        return;

      Time.timeScale = 1f;
      pauseToggle.SetState(true, animate: false);
    }

    void OnSoundStateChanged(bool soundOn)
    {
      if (!_initialized)
        return;

      if (soundOn)
      {
        MainMenuSettings.MasterVolume = _volumeBeforeMute > 0.01f ? _volumeBeforeMute : 1f;
        return;
      }

      _volumeBeforeMute = MainMenuSettings.MasterVolume;
      if (_volumeBeforeMute <= 0.01f)
        _volumeBeforeMute = 1f;

      MainMenuSettings.MasterVolume = 0f;
    }

    void OnPauseStateChanged(bool notPaused)
    {
      if (!_initialized)
        return;

      Time.timeScale = notPaused ? 1f : 0f;
    }
  }
}
