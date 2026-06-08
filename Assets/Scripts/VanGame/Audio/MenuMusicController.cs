using DG.Tweening;
using UnityEngine;
using VanGame.UI;

namespace VanGame.Audio
{
  /// <summary>
  /// Title / menu loop that survives scene loads until the first map region is chosen.
  /// Attach to the title scene object that already has an AudioSource.
  /// </summary>
  public class MenuMusicController : MonoBehaviour
  {
    public static MenuMusicController Instance { get; private set; }

    [SerializeField] AudioSource musicSource;
    [SerializeField] float musicVolume = 0.711f;
    [SerializeField] bool useSavedMasterVolume = true;

    Tween _volumeTween;

    public bool IsPlaying => musicSource != null && musicSource.isPlaying;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);

      if (musicSource == null)
        musicSource = GetComponent<AudioSource>();

      if (musicSource == null)
        return;

      musicSource.loop = true;
      musicSource.playOnAwake = false;
      musicSource.volume = GetEffectiveVolume();

      if (!musicSource.isPlaying)
        musicSource.Play();
    }

    void OnDestroy()
    {
      KillVolumeTween();

      if (Instance == this)
        Instance = null;
    }

    public Tween FadeOut(float duration)
    {
      if (musicSource == null || !musicSource.isPlaying)
        return null;

      KillVolumeTween();

      _volumeTween = DOTween
        .To(() => musicSource.volume, value => musicSource.volume = value, 0f, duration)
        .SetEase(Ease.InOutSine)
        .SetTarget(musicSource);

      return _volumeTween;
    }

    public void StopImmediate()
    {
      KillVolumeTween();

      if (musicSource == null)
        return;

      musicSource.Stop();
      musicSource.volume = 0f;
    }

    float GetEffectiveVolume()
    {
      float volume = musicVolume;
      if (useSavedMasterVolume)
        volume *= MainMenuSettings.MasterVolume;

      return Mathf.Clamp01(volume);
    }

    void KillVolumeTween()
    {
      if (_volumeTween != null && _volumeTween.IsActive())
        _volumeTween.Kill();

      _volumeTween = null;

      if (musicSource != null)
        musicSource.DOKill();
    }
  }
}
