using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public enum TimerBarAnimateMode
  {
    Immediate,
    Smooth,
    CardPlay
  }

  public class DrivingDayTimerView : MonoBehaviour
  {
    [SerializeField] TMP_Text destinationText;
    [SerializeField] TMP_Text legDaysText;
    [SerializeField] TMP_Text dayTimerText;
    [SerializeField] Image dayTimerFill;
    [Tooltip("Track rect used to align the fill to the left edge (e.g. timer frame). Not the fill itself.")]
    [SerializeField] RectTransform barTrack;

    [SerializeField] string destinationFormat = "Driving to {0}";
    [SerializeField] string legDaysFormat = "Leg: {0} day(s) left";
    [SerializeField] string timerFormat = "{0:0}/{1}";

    [Header("Bar fill (8 sections)")]
    [Tooltip("Full bar width in pixels. Fill Image keeps this width; progress uses scale.x.")]
    [SerializeField] float fullBarWidth = 414f;
    [Tooltip("Scale.x when the bar is full (8/8 sections). Usually 1.")]
    [SerializeField] float fillMaxScaleX = 1f;
    [SerializeField] bool growFillFromLeft = true;
    [SerializeField] float cardPlaySectionDuration = 0.24f;
    [SerializeField] float cardPlaySectionStagger = 0.07f;
    [SerializeField] float cardPlayOvershootScale = 1.04f;
    [SerializeField] float cardPlaySettleDuration = 0.12f;
    [SerializeField] Ease cardPlayEase = Ease.OutBack;
    [SerializeField] Ease cardPlaySettleEase = Ease.OutCubic;

    [Header("Preview")]
    [SerializeField] Color previewFillColor = new Color(0.45f, 1f, 0.95f, 0.35f);
    [SerializeField] Color previewTextOutlineColor = new Color(0.45f, 1f, 0.95f, 1f);
    [SerializeField] Color previewTextFaceColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] float previewTextOutlineWidth = 0.28f;

    RunState _runState;
    GameConfig _config;
    RectTransform _fillRect;
    RectTransform _previewFillRect;
    Image _previewFillImage;
    TMP_Text _timerPreviewText;
    float _fullFillWidth;
    float _fullFillHeight;
    float _maxScaleX = 1f;
    float _displayedSections;
    Vector3 _fillBaseScale = Vector3.one;
    Vector2 _fillRestAnchoredPosition;
    Tween _fillTween;
    Sequence _fillSequence;

    public bool IsFillAnimating =>
      (_fillSequence != null && _fillSequence.IsActive())
      || (_fillTween != null && _fillTween.IsActive());

    public void Bind(RunState runState, GameConfig config)
    {
      if (_runState != null)
      {
        _runState.StatsChanged -= RefreshHeaderLabels;
        _runState.PhaseChanged -= RefreshHeaderLabels;
      }

      _runState = runState;
      _config = config;

      if (_runState != null)
      {
        _runState.StatsChanged += RefreshHeaderLabels;
        _runState.PhaseChanged += RefreshHeaderLabels;
      }

      _displayedSections = _runState != null ? _runState.DrivingDayTimer : 0f;
      CacheFillMetrics();
      RefreshHeaderLabels();
      RefreshTimer(_displayedSections, GetSectionCount(), TimerBarAnimateMode.Immediate);
    }

    void Awake()
    {
      CacheFillMetrics();
    }

    void OnDestroy()
    {
      KillFillTweens();

      if (_runState != null)
      {
        _runState.StatsChanged -= RefreshHeaderLabels;
        _runState.PhaseChanged -= RefreshHeaderLabels;
      }
    }

    public void RefreshHeaderLabels()
    {
      if (_runState == null)
        return;

      if (destinationText != null)
      {
        string destName = _runState.DestinationCity != null
          ? _runState.DestinationCity.displayName
          : "—";
        destinationText.text = string.Format(destinationFormat, destName);
      }

      if (legDaysText != null)
        legDaysText.text = string.Format(legDaysFormat, _runState.DrivingDaysRemaining);
    }

    public void Refresh()
    {
      RefreshHeaderLabels();
      if (_runState == null)
        return;

      RefreshTimer(_runState.DrivingDayTimer, GetSectionCount(), TimerBarAnimateMode.Immediate);
    }

    public void RefreshTimer(float filledSections, int sectionCount, TimerBarAnimateMode animateMode = TimerBarAnimateMode.Immediate)
    {
      if (_fillRect == null)
        CacheFillMetrics();

      int maxSections = Mathf.Max(1, sectionCount);
      float clamped = Mathf.Clamp(filledSections, 0f, maxSections);

      if (dayTimerText != null)
        dayTimerText.text = string.Format(timerFormat, clamped, maxSections);

      UpdateFillBar(clamped, maxSections, animateMode);
    }

    public void RefreshHeaderOnly()
    {
      RefreshHeaderLabels();
    }

    public void ShowSectionPreview(float targetSections, int maxSections)
    {
      if (_fillRect == null)
        CacheFillMetrics();

      EnsurePreviewVisuals();

      int clampedMax = Mathf.Max(1, maxSections);
      float clampedTarget = Mathf.Clamp(targetSections, 0f, clampedMax);

      if (_previewFillRect != null)
      {
        float scaleX = SectionsToScaleX(clampedTarget, clampedMax);
        _previewFillRect.localScale = new Vector3(scaleX, _fillBaseScale.y, _fillBaseScale.z);
        _previewFillRect.anchoredPosition = _fillRect.anchoredPosition;
        _previewFillRect.gameObject.SetActive(true);
      }

      if (_timerPreviewText != null)
      {
        _timerPreviewText.text = string.Format(timerFormat, clampedTarget, clampedMax);
        _timerPreviewText.gameObject.SetActive(true);
      }
    }

    public void ClearSectionPreview()
    {
      if (_previewFillRect != null)
        _previewFillRect.gameObject.SetActive(false);

      if (_timerPreviewText != null)
        _timerPreviewText.gameObject.SetActive(false);
    }

    void EnsurePreviewVisuals()
    {
      if (_fillRect == null || dayTimerFill == null)
        return;

      if (_previewFillRect == null)
      {
        Image previewImage = Instantiate(dayTimerFill, dayTimerFill.transform.parent);
        previewImage.name = dayTimerFill.name + "_Preview";
        previewImage.raycastTarget = false;
        previewImage.color = previewFillColor;
        _previewFillImage = previewImage;
        _previewFillRect = previewImage.rectTransform;
        _previewFillRect.SetSiblingIndex(dayTimerFill.transform.GetSiblingIndex());
        _previewFillRect.pivot = _fillRect.pivot;
        _previewFillRect.anchorMin = _fillRect.anchorMin;
        _previewFillRect.anchorMax = _fillRect.anchorMax;
        _previewFillRect.sizeDelta = _fillRect.sizeDelta;
        _previewFillRect.gameObject.SetActive(false);
      }

      if (_timerPreviewText == null && dayTimerText != null)
      {
        _timerPreviewText = Instantiate(dayTimerText, dayTimerText.transform.parent);
        _timerPreviewText.name = dayTimerText.name + "_Preview";
        _timerPreviewText.raycastTarget = false;
        _timerPreviewText.fontMaterial = Instantiate(dayTimerText.fontSharedMaterial);
        if (_timerPreviewText.fontMaterial != null)
          _timerPreviewText.fontMaterial.EnableKeyword("OUTLINE_ON");
        _timerPreviewText.outlineWidth = previewTextOutlineWidth;
        _timerPreviewText.outlineColor = previewTextOutlineColor;
        _timerPreviewText.color = previewTextFaceColor;

        RectTransform previewRect = _timerPreviewText.rectTransform;
        RectTransform sourceRect = dayTimerText.rectTransform;
        previewRect.anchorMin = sourceRect.anchorMin;
        previewRect.anchorMax = sourceRect.anchorMax;
        previewRect.anchoredPosition = sourceRect.anchoredPosition;
        previewRect.sizeDelta = sourceRect.sizeDelta;
        _timerPreviewText.gameObject.SetActive(false);
      }
    }

    void CacheFillMetrics()
    {
      _fillRect = dayTimerFill != null ? dayTimerFill.rectTransform : null;
      if (_fillRect == null)
        return;

      Canvas.ForceUpdateCanvases();

      _fillRestAnchoredPosition = _fillRect.anchoredPosition;
      _fullFillWidth = fullBarWidth > 0f ? fullBarWidth : Mathf.Max(1f, _fillRect.sizeDelta.x);
      _fullFillHeight = _fillRect.sizeDelta.y > 0f ? _fillRect.sizeDelta.y : Mathf.Max(1f, _fillRect.rect.height);

      Vector3 currentScale = _fillRect.localScale;
      _maxScaleX = fillMaxScaleX > 0f ? fillMaxScaleX : 1f;
      _fillBaseScale = new Vector3(
        _maxScaleX,
        Mathf.Approximately(currentScale.y, 0f) ? 1f : currentScale.y,
        Mathf.Approximately(currentScale.z, 0f) ? 1f : currentScale.z);

      _fillRect.sizeDelta = new Vector2(_fullFillWidth, _fullFillHeight);

      if (growFillFromLeft)
        ConfigureFillForLeftGrowth();
    }

    void ConfigureFillForLeftGrowth()
    {
      if (_fillRect == null)
        return;

      _fillRect.pivot = new Vector2(0f, 0.5f);
      _fillRect.anchorMin = new Vector2(0f, 0.5f);
      _fillRect.anchorMax = new Vector2(0f, 0.5f);
      _fillRect.anchoredPosition = new Vector2(0f, _fillRestAnchoredPosition.y);
    }

    void UpdateFillBar(float targetSections, int maxSections, TimerBarAnimateMode animateMode)
    {
      if (_fillRect == null)
      {
        if (dayTimerFill != null)
          dayTimerFill.fillAmount = targetSections / maxSections;
        return;
      }

      switch (animateMode)
      {
        case TimerBarAnimateMode.CardPlay:
          AnimateFillBySections(_displayedSections, targetSections, maxSections);
          break;
        case TimerBarAnimateMode.Smooth:
          AnimateFillToSections(targetSections, maxSections, 0.18f, Ease.OutCubic);
          break;
        default:
          if (IsFillAnimating)
            return;

          KillFillTweens();
          _displayedSections = targetSections;
          ApplyFillVisual(_displayedSections, maxSections);
          break;
      }
    }

    void AnimateFillBySections(float fromSections, float toSections, int maxSections)
    {
      KillFillTweens();

      int startStep = Mathf.FloorToInt(fromSections + 0.0001f);
      int endStep = Mathf.RoundToInt(toSections);
      if (endStep < startStep)
        endStep = startStep;

      if (endStep <= startStep)
      {
        AnimateFillToSections(toSections, maxSections, cardPlaySectionDuration, cardPlayEase);
        return;
      }

      _fillSequence = DOTween.Sequence();
      _fillSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

      for (int step = startStep + 1; step <= endStep; step++)
      {
        float sectionTarget = Mathf.Min(step, toSections);
        float targetScaleX = SectionsToScaleX(sectionTarget, maxSections);
        float overshootScaleX = Mathf.Min(targetScaleX * cardPlayOvershootScale, _maxScaleX);

        _fillSequence.Append(
          _fillRect.DOScaleX(overshootScaleX, cardPlaySectionDuration * 0.72f)
            .SetEase(cardPlayEase));

        _fillSequence.Append(
          _fillRect.DOScaleX(targetScaleX, cardPlaySettleDuration)
            .SetEase(cardPlaySettleEase));

        if (step < endStep)
          _fillSequence.AppendInterval(cardPlaySectionStagger);

        int capturedStep = step;
        _fillSequence.AppendCallback(() => _displayedSections = capturedStep);
      }

      _fillSequence.OnComplete(() =>
      {
        _displayedSections = toSections;
        ApplyFillVisual(_displayedSections, maxSections);
        _fillSequence = null;
      });
    }

    void AnimateFillToSections(float targetSections, int maxSections, float duration, Ease ease)
    {
      KillFillTweens();

      float targetScaleX = SectionsToScaleX(targetSections, maxSections);
      _fillTween = _fillRect
        .DOScaleX(targetScaleX, duration)
        .SetEase(ease)
        .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
        .OnComplete(() =>
        {
          _displayedSections = targetSections;
          _fillTween = null;
        });
    }

    float SectionsToScaleX(float sections, int maxSections)
    {
      float normalized = Mathf.Clamp01(sections / Mathf.Max(1, maxSections));
      return _maxScaleX * normalized;
    }

    void ApplyFillVisual(float sections, int maxSections)
    {
      if (_fillRect == null)
        return;

      float scaleX = SectionsToScaleX(sections, maxSections);
      _fillRect.localScale = new Vector3(scaleX, _fillBaseScale.y, _fillBaseScale.z);
    }

    void KillFillTweens()
    {
      if (_fillSequence != null)
      {
        _fillSequence.Kill();
        _fillSequence = null;
      }

      if (_fillTween != null)
      {
        _fillTween.Kill();
        _fillTween = null;
      }

      if (_fillRect != null)
        _fillRect.DOKill();
    }

    int GetSectionCount()
    {
      if (_config == null)
        return 8;

      return Mathf.Max(1, _config.drivingDaySectionCount);
    }
  }
}
