using DG.Tweening;
using UnityEngine;

namespace VanGame.Visual
{
  /// <summary>
  /// Slight left-right shake with the bottom edge pinned in place.
  /// </summary>
  public class SouvenirVanShake2D : MonoBehaviour
  {
    [SerializeField] float shakeAmplitudeX = 4f;
    [SerializeField] float shakeHalfCycleDuration = 0.35f;
    [SerializeField] Ease shakeEase = Ease.InOutSine;
    [SerializeField] bool playOnEnable = true;

    RectTransform _rectTransform;
    Vector2 _restAnchoredPosition;
    Tween _shakeTween;

    void Awake()
    {
      _rectTransform = transform as RectTransform;
      CacheRestPosition();
    }

    void OnEnable()
    {
      CacheRestPosition();
      if (playOnEnable)
        StartShake();
    }

    void OnDisable()
    {
      StopShake();
      RestoreRestPosition();
    }

    public void StartShake()
    {
      StopShake();
      CacheRestPosition();

      if (_rectTransform == null || shakeAmplitudeX <= 0f || shakeHalfCycleDuration <= 0f)
        return;

      _rectTransform.anchoredPosition = _restAnchoredPosition;
      _shakeTween = _rectTransform
        .DOAnchorPosX(_restAnchoredPosition.x + shakeAmplitudeX, shakeHalfCycleDuration)
        .SetEase(shakeEase)
        .SetLoops(-1, LoopType.Yoyo)
        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void StopShake()
    {
      if (_shakeTween != null)
      {
        _shakeTween.Kill();
        _shakeTween = null;
      }

      if (_rectTransform != null)
        _rectTransform.DOKill();
    }

    void CacheRestPosition()
    {
      if (_rectTransform != null)
        _restAnchoredPosition = _rectTransform.anchoredPosition;
    }

    void RestoreRestPosition()
    {
      if (_rectTransform != null)
        _rectTransform.anchoredPosition = _restAnchoredPosition;
    }
  }
}
