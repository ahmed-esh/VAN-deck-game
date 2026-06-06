using UnityEngine;

namespace VanGame.UI
{
  public static class MainMenuSettings
  {
    public const string SoundVolumeKey = "VanGame_MasterVolume";
    public const string SkipTutorialKey = "VanGame_SkipTutorial";

    public static float MasterVolume
    {
      get => PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
      set
      {
        PlayerPrefs.SetFloat(SoundVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        AudioListener.volume = MasterVolume;
      }
    }

    public static bool SkipTutorial
    {
      get => PlayerPrefs.GetInt(SkipTutorialKey, 0) == 1;
      set
      {
        PlayerPrefs.SetInt(SkipTutorialKey, value ? 1 : 0);
        PlayerPrefs.Save();
      }
    }

    public static void ApplySavedAudio()
    {
      AudioListener.volume = MasterVolume;
    }
  }
}
