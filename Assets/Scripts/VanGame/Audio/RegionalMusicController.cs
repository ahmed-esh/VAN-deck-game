using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VanGame.Data;
using VanGame.UI;

namespace VanGame.Audio
{
  public class RegionalMusicController : MonoBehaviour
  {
    [Serializable]
    public class RegionMusicEntry
    {
      public CityDefinition city;
      public AudioClip musicClip;
    }

    [Header("Region music (one clip per map region / city)")]
    [SerializeField] RegionMusicEntry[] regionMusic = Array.Empty<RegionMusicEntry>();

    [Header("End-of-run stingers")]
    [SerializeField] AudioClip winStinger;
    [SerializeField] AudioClip loseStinger;

    [Header("Audio sources (auto-created if empty)")]
    [SerializeField] AudioSource musicSourceA;
    [SerializeField] AudioSource musicSourceB;
    [SerializeField] AudioSource stingerSource;

    [Header("Mix")]
    [SerializeField] float musicVolume = 0.65f;
    [SerializeField] float stingerVolume = 1f;
    [SerializeField] bool useSavedMasterVolume = true;

    [Header("Crossfade")]
    [SerializeField] float crossfadeDuration = 2f;
    [SerializeField] float runEndFadeDuration = 1.25f;
    [SerializeField] Ease crossfadeEase = Ease.InOutSine;

    [Header("Card overlay duck")]
    [SerializeField] float overlayDuckDuration = 0.45f;
    [SerializeField] float overlayRestoreDuration = 0.65f;
    [SerializeField] float overlayClipFadeOutDuration = 0.4f;
    [SerializeField] Ease overlayFadeEase = Ease.InOutSine;

    readonly Dictionary<CityDefinition, AudioClip> _clipsByCity = new Dictionary<CityDefinition, AudioClip>();

    AudioSource _activeMusicSource;
    AudioSource _inactiveMusicSource;
    CityDefinition _currentCity;
    Sequence _musicSequence;
    Sequence _overlaySequence;
    bool _runEnded;

    void Awake()
    {
      EnsureAudioSources();
      RebuildLookup();
    }

    void OnDestroy()
    {
      KillTweens();
    }

    public void RebuildLookup()
    {
      _clipsByCity.Clear();

      if (regionMusic == null)
        return;

      foreach (RegionMusicEntry entry in regionMusic)
      {
        if (entry?.city == null || entry.musicClip == null)
          continue;

        _clipsByCity[entry.city] = entry.musicClip;
      }
    }

    public void OnRunStarted(CityDefinition startCity)
    {
      _runEnded = false;
      _currentCity = null;
      PlayRegionMusic(startCity);
    }

    public void PlayRegionMusic(CityDefinition city)
    {
      if (_runEnded || city == null)
        return;

      if (_currentCity == city)
        return;

      if (!_clipsByCity.TryGetValue(city, out AudioClip clip) || clip == null)
        return;

      _currentCity = city;
      CrossfadeToLoop(clip);
    }

    public void PlayWin()
    {
      if (_runEnded)
        return;

      _runEnded = true;
      FadeOutMusicAndPlayStinger(winStinger);
    }

    public void PlayLose()
    {
      if (_runEnded)
        return;

      _runEnded = true;
      KillOverlaySequence();
      FadeOutMusicAndPlayStinger(loseStinger);
    }

    /// <summary>
    /// Ducks regional music, plays a one-shot clip, then fades music back in.
    /// Used by special cards such as Great Song on the Radio.
    /// </summary>
    public void PlayMusicOverlay(AudioClip clip, float clipDuration, float duckDuration, float restoreDuration, float clipVolume)
    {
      if (_runEnded || clip == null || clipDuration <= 0f)
        return;

      EnsureAudioSources();
      KillOverlaySequence();

      float targetVolume = GetEffectiveMusicVolume();
      float volume = Mathf.Clamp01(clipVolume);

      _overlaySequence = DOTween.Sequence().SetUpdate(true);

      if (_activeMusicSource != null && _activeMusicSource.isPlaying)
        _overlaySequence.Append(FadeAudioSource(_activeMusicSource, 0f, duckDuration).SetEase(overlayFadeEase));

      _overlaySequence.AppendCallback(() => PlayOverlayClip(clip, volume));

      float holdDuration = Mathf.Max(0f, clipDuration - overlayClipFadeOutDuration);
      if (holdDuration > 0f)
        _overlaySequence.AppendInterval(holdDuration);

      Sequence crossfade = DOTween.Sequence().SetUpdate(true);
      crossfade.Join(FadeAudioSource(stingerSource, 0f, overlayClipFadeOutDuration).SetEase(overlayFadeEase));

      if (_activeMusicSource != null && _activeMusicSource.isPlaying)
        crossfade.Join(FadeAudioSource(_activeMusicSource, targetVolume, restoreDuration).SetEase(overlayFadeEase));

      _overlaySequence.Append(crossfade);
      _overlaySequence.AppendCallback(StopOverlayClip);
    }

    public void PlayMusicOverlay(AudioClip clip, float clipDuration, float clipVolume)
    {
      PlayMusicOverlay(clip, clipDuration, overlayDuckDuration, overlayRestoreDuration, clipVolume);
    }

    public void StopAllImmediate()
    {
      KillTweens();
      _runEnded = true;
      _currentCity = null;

      if (musicSourceA != null)
      {
        musicSourceA.Stop();
        musicSourceA.volume = 0f;
      }

      if (musicSourceB != null)
      {
        musicSourceB.Stop();
        musicSourceB.volume = 0f;
      }

      if (stingerSource != null)
      {
        stingerSource.Stop();
        stingerSource.volume = 0f;
      }

      _activeMusicSource = null;
      _inactiveMusicSource = null;
    }

    void CrossfadeToLoop(AudioClip clip)
    {
      EnsureAudioSources();
      KillMusicTweens();

      AudioSource incoming = _activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;
      AudioSource outgoing = _activeMusicSource;

      incoming.clip = clip;
      incoming.loop = true;
      incoming.volume = 0f;
      incoming.Play();

      float targetVolume = GetEffectiveMusicVolume();
      _musicSequence = DOTween.Sequence();

      if (outgoing != null && outgoing.isPlaying)
        _musicSequence.Join(FadeAudioSource(outgoing, 0f, crossfadeDuration));

      _musicSequence.Join(FadeAudioSource(incoming, targetVolume, crossfadeDuration));
      _musicSequence.SetEase(crossfadeEase);
      _musicSequence.OnComplete(() =>
      {
        if (outgoing != null)
        {
          outgoing.Stop();
          outgoing.volume = 0f;
        }
      });

      _activeMusicSource = incoming;
      _inactiveMusicSource = outgoing;
    }

    void FadeOutMusicAndPlayStinger(AudioClip stinger)
    {
      EnsureAudioSources();
      KillMusicTweens();

      _musicSequence = DOTween.Sequence();

      if (musicSourceA != null && musicSourceA.isPlaying)
        _musicSequence.Join(FadeAudioSource(musicSourceA, 0f, runEndFadeDuration));

      if (musicSourceB != null && musicSourceB.isPlaying)
        _musicSequence.Join(FadeAudioSource(musicSourceB, 0f, runEndFadeDuration));

      _musicSequence.OnComplete(() =>
      {
        if (musicSourceA != null)
        {
          musicSourceA.Stop();
          musicSourceA.volume = 0f;
        }

        if (musicSourceB != null)
        {
          musicSourceB.Stop();
          musicSourceB.volume = 0f;
        }

        _activeMusicSource = null;
        _inactiveMusicSource = null;
        PlayStingerOnce(stinger);
      });
    }

    void StopOverlayClip()
    {
      if (stingerSource == null)
        return;

      stingerSource.Stop();
      stingerSource.clip = null;
      stingerSource.volume = 0f;
    }

    void PlayStingerOnce(AudioClip clip)
    {
      if (clip == null || stingerSource == null)
        return;

      stingerSource.Stop();
      stingerSource.clip = clip;
      stingerSource.loop = false;
      stingerSource.volume = GetEffectiveStingerVolume();
      stingerSource.Play();
    }

    void PlayOverlayClip(AudioClip clip, float volume)
    {
      if (clip == null || stingerSource == null)
        return;

      stingerSource.Stop();
      stingerSource.clip = clip;
      stingerSource.loop = false;
      stingerSource.volume = volume;
      stingerSource.Play();
    }

    float GetEffectiveMusicVolume()
    {
      float volume = musicVolume;
      if (useSavedMasterVolume)
        volume *= MainMenuSettings.MasterVolume;

      return Mathf.Clamp01(volume);
    }

    float GetEffectiveStingerVolume()
    {
      float volume = stingerVolume;
      if (useSavedMasterVolume)
        volume *= MainMenuSettings.MasterVolume;

      return Mathf.Clamp01(volume);
    }

    static Tween FadeAudioSource(AudioSource source, float endVolume, float duration)
    {
      if (source == null)
        return null;

      return DOTween.To(() => source.volume, value => source.volume = value, endVolume, duration)
        .SetEase(Ease.InOutSine)
        .SetTarget(source);
    }

    void EnsureAudioSources()
    {
      if (musicSourceA == null)
        musicSourceA = CreateConfiguredSource("RegionalMusic_A");

      if (musicSourceB == null)
        musicSourceB = CreateConfiguredSource("RegionalMusic_B");

      if (stingerSource == null)
        stingerSource = CreateConfiguredSource("RegionalMusic_Stinger");

      ConfigureMusicSource(musicSourceA);
      ConfigureMusicSource(musicSourceB);
      ConfigureStingerSource(stingerSource);
    }

    AudioSource CreateConfiguredSource(string sourceName)
    {
      GameObject child = new GameObject(sourceName);
      child.transform.SetParent(transform, false);
      return child.AddComponent<AudioSource>();
    }

    static void ConfigureMusicSource(AudioSource source)
    {
      if (source == null)
        return;

      source.playOnAwake = false;
      source.loop = true;
      source.volume = 0f;
    }

    static void ConfigureStingerSource(AudioSource source)
    {
      if (source == null)
        return;

      source.playOnAwake = false;
      source.loop = false;
      source.volume = 0f;
    }

    void KillTweens()
    {
      KillMusicTweens();
      KillOverlaySequence();

      if (musicSourceA != null)
        musicSourceA.DOKill();

      if (musicSourceB != null)
        musicSourceB.DOKill();

      if (stingerSource != null)
        stingerSource.DOKill();
    }

    void KillOverlaySequence()
    {
      if (_overlaySequence != null)
      {
        _overlaySequence.Kill();
        _overlaySequence = null;
      }
    }

    void KillMusicTweens()
    {
      if (_musicSequence != null)
      {
        _musicSequence.Kill();
        _musicSequence = null;
      }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      RebuildLookup();
    }
#endif
  }
}
