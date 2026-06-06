using DG.Tweening;
using UnityEngine;

namespace VanGame.Visual
{
  /// <summary>
  /// Gentle vertical bob on a 2D car (Transform or RectTransform) to suggest driving.
  /// </summary>
  public class DrivingBob2D : MonoBehaviour
  {
    [SerializeField] float bobAmplitude = 0.06f;
    [SerializeField] float bobHalfCycleDuration = 0.45f;
    [SerializeField] Ease bobEase = Ease.InOutSine;
    [SerializeField] bool playOnEnable = true;

    RectTransform _rectTransform;
    Vector2 _restAnchoredPosition;
    Vector3 _restLocalPosition;
    Tween _bobTween;

    void Awake()
    {
      _rectTransform = transform as RectTransform;
      CacheRestPosition();
    }

    void OnEnable()
    {
      CacheRestPosition();

      if (playOnEnable)
        StartBob();
    }

    void OnDisable()
    {
      StopBob();
      RestoreRestPosition();
    }

    public void StartBob()
    {
      StopBob();
      CacheRestPosition();

      float peak = bobAmplitude;
      if (peak <= 0f || bobHalfCycleDuration <= 0f)
        return;

      if (_rectTransform != null)
      {
        _rectTransform.anchoredPosition = _restAnchoredPosition;
        _bobTween = _rectTransform
          .DOAnchorPosY(_restAnchoredPosition.y + peak, bobHalfCycleDuration)
          .SetEase(bobEase)
          .SetLoops(-1, LoopType.Yoyo)
          .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        return;
      }

      transform.localPosition = _restLocalPosition;
      _bobTween = transform
        .DOLocalMoveY(_restLocalPosition.y + peak, bobHalfCycleDuration)
        .SetEase(bobEase)
        .SetLoops(-1, LoopType.Yoyo)
        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void StopBob()
    {
      if (_bobTween != null)
      {
        _bobTween.Kill();
        _bobTween = null;
      }

      if (_rectTransform != null)
        _rectTransform.DOKill();
      else
        transform.DOKill();
    }

    void CacheRestPosition()
    {
      if (_rectTransform != null)
        _restAnchoredPosition = _rectTransform.anchoredPosition;
      else
        _restLocalPosition = transform.localPosition;
    }

    void RestoreRestPosition()
    {
      if (_rectTransform != null)
        _rectTransform.anchoredPosition = _restAnchoredPosition;
      else
        transform.localPosition = _restLocalPosition;
    }
  }
}
