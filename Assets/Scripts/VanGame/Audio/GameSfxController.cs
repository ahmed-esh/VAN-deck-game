using UnityEngine;
using VanGame.UI;

namespace VanGame.Audio
{
  /// <summary>
  /// Central one-shot SFX player. Attach to a GameObject, assign clips, and call the Play methods from gameplay UI.
  /// </summary>
  public class GameSfxController : MonoBehaviour
  {
    public static GameSfxController Instance { get; private set; }

    [Header("Map")]
    [SerializeField] AudioClip MAP_Hover;
    [SerializeField] AudioClip mapClick;

    [Header("Cards")]
    [SerializeField] AudioClip cardShuffle;
    [SerializeField] AudioClip cardClick;

    [Header("UI")]
    [SerializeField] AudioClip windowPopup;
    [SerializeField] AudioClip souvenirsPopup;

    [Header("Playback")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] float volume = 1f;
    [SerializeField] bool useSavedMasterVolume = true;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;

      if (sfxSource == null)
      {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
          sfxSource = gameObject.AddComponent<AudioSource>();
      }

      sfxSource.playOnAwake = false;
      sfxSource.loop = false;
    }

    void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    public void PlayMapHover() => Play(MAP_Hover);

    public void PlayMapClick() => Play(mapClick);

    public void PlayCardShuffle() => Play(cardShuffle);

    public void PlayCardClick() => Play(cardClick);

    public void PlayWindowPopup() => Play(windowPopup);

    public void PlaySouvenirsPopup() => Play(souvenirsPopup);

    public static void TryPlayMapHover() => Resolve()?.PlayMapHover();

    public static void TryPlayMapClick() => Resolve()?.PlayMapClick();

    public static void TryPlayCardShuffle() => Resolve()?.PlayCardShuffle();

    public static void TryPlayCardClick() => Resolve()?.PlayCardClick();

    public static void TryPlayWindowPopup() => Resolve()?.PlayWindowPopup();

    public static void TryPlaySouvenirsPopup() => Resolve()?.PlaySouvenirsPopup();

    static GameSfxController Resolve()
    {
      if (Instance != null)
        return Instance;

      return FindFirstObjectByType<GameSfxController>();
    }

    void Play(AudioClip clip)
    {
      if (clip == null || sfxSource == null)
        return;

      float effectiveVolume = volume;
      if (useSavedMasterVolume)
        effectiveVolume *= MainMenuSettings.MasterVolume;

      sfxSource.PlayOneShot(clip, Mathf.Clamp01(effectiveVolume));
    }
  }
}
