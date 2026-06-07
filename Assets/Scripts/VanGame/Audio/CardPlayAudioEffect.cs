using UnityEngine;
using VanGame.UI;

namespace VanGame.Audio
{
  /// <summary>
  /// Attach to a card prefab (same GameObject as CardView).
  /// Ducks regional music, plays a one-shot clip, then restores music.
  /// </summary>
  public class CardPlayAudioEffect : MonoBehaviour
  {
    [SerializeField] AudioClip clip;
    [SerializeField] float playDuration = 4f;
    [SerializeField] float volume = 1f;
    [SerializeField] bool useSavedMasterVolume = true;

    [Header("Music duck")]
    [SerializeField] float musicDuckDuration = 0.45f;
    [SerializeField] float musicRestoreDuration = 0.65f;

    public void PlayOnCardPlayed()
    {
      if (clip == null)
        return;

      float effectiveVolume = volume;
      if (useSavedMasterVolume)
        effectiveVolume *= MainMenuSettings.MasterVolume;

      RegionalMusicController musicController = FindFirstObjectByType<RegionalMusicController>();
      if (musicController != null)
      {
        musicController.PlayMusicOverlay(
          clip,
          playDuration,
          musicDuckDuration,
          musicRestoreDuration,
          effectiveVolume);
        return;
      }

      CardPlayAudioRunner.Play(clip, playDuration, effectiveVolume);
    }
  }

  static class CardPlayAudioRunner
  {
    public static void Play(AudioClip clip, float duration, float volume)
    {
      if (clip == null)
        return;

      GameObject host = new GameObject("CardPlayAudio");

      AudioSource source = host.AddComponent<AudioSource>();
      source.clip = clip;
      source.volume = Mathf.Clamp01(volume);
      source.loop = false;
      source.playOnAwake = false;
      source.Play();

      Object.Destroy(host, Mathf.Max(0.01f, duration));
    }
  }
}
