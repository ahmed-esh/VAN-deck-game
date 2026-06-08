using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VanGame.UI
{
  /// <summary>
  /// Drives the tutorial slideshow: section-one slides, van-background shrink transition, section-two slides, skip, and load main game.
  /// </summary>
  public class TutorialSceneController : MonoBehaviour
  {
    [Header("Section slides")]
    [SerializeField] TutorialSlideView[] sectionOneSlides = Array.Empty<TutorialSlideView>();
    [SerializeField] TutorialSlideView[] sectionTwoSlides = Array.Empty<TutorialSlideView>();

    [Header("Van backgrounds")]
    [SerializeField] RawImage vanBackgroundLarge;
    [SerializeField] RawImage vanBackgroundSmall;

    [Header("Skip")]
    [SerializeField] Button skipTutorialButton;

    [Header("Scene")]
    [SerializeField] string gameSceneName = "Main game";

    [Header("Slide animation")]
    [SerializeField] float slideOutDuration = 0.28f;
    [SerializeField] float slideInDuration = 0.42f;
    [SerializeField] float slideOutScale = 0.94f;
    [SerializeField] float slideInStartScale = 1.06f;
    [SerializeField] float slideOutYOffset = -18f;
    [SerializeField] float slideInStartYOffset = 24f;
    [SerializeField] Ease slideOutEase = Ease.InCubic;
    [SerializeField] Ease slideInEase = Ease.OutBack;

    [Header("Section transition")]
    [SerializeField] float sectionTransitionDuration = 0.85f;
    [SerializeField] Ease sectionTransitionEase = Ease.InOutCubic;
    [SerializeField] float sectionSmallVanFadeDelay = 0.22f;
    [SerializeField] float sectionLargeVanFadeDelay = 0.12f;

    SlideVisualState[] _allSlideStates = Array.Empty<SlideVisualState>();

    CanvasGroup _largeVanGroup;
    CanvasGroup _smallVanGroup;
    RectTransform _largeVanRect;
    RectTransform _smallVanRect;

    Vector3 _largeVanRestLocalScale;
    Vector2 _largeVanRestAnchoredPosition;
    Vector3 _largeVanRestWorldCenter;
    Vector2 _largeVanRestWorldSize;
    Vector3 _smallVanWorldCenter;
    Vector2 _smallVanWorldSize;

    int _currentSlideIndex;
    bool _isAnimating;
    bool _isSectionTwo;
    Tween _activeTween;

    struct SlideVisualState
    {
      public TutorialSlideView Slide;
      public CanvasGroup Group;
      public Vector3 RestLocalScale;
      public Vector2 RestAnchoredPosition;
    }

    void Awake()
    {
      BuildSlideStates();
      CacheVanBackgroundTargets();

      if (skipTutorialButton != null)
        skipTutorialButton.onClick.AddListener(SkipTutorial);
    }

    void Start()
    {
      PrepareVanBackgroundsForSectionOne();
      HideAllSlides(immediate: true);

      if (_allSlideStates.Length == 0)
        return;

      ShowSlide(0, immediate: false, playEntrance: true);
    }

    void OnDestroy()
    {
      KillActiveTween();
    }

    public void HandleSlideClicked(TutorialSlideView slide)
    {
      if (_isAnimating || slide == null)
        return;

      int index = FindSlideIndex(slide);
      if (index < 0 || index != _currentSlideIndex)
        return;

      AdvanceFromCurrentSlide();
    }

    public void SkipTutorial()
    {
      if (_isAnimating)
        return;

      LoadMainGame();
    }

    public void LoadMainGame()
    {
      KillActiveTween();
      SceneManager.LoadScene(gameSceneName);
    }

    void BuildSlideStates()
    {
      int totalCount = sectionOneSlides.Length + sectionTwoSlides.Length;
      _allSlideStates = new SlideVisualState[totalCount];

      int writeIndex = 0;
      writeIndex = RegisterSlides(sectionOneSlides, writeIndex);
      RegisterSlides(sectionTwoSlides, writeIndex);

      for (int i = 0; i < _allSlideStates.Length; i++)
      {
        TutorialSlideView slide = _allSlideStates[i].Slide;
        if (slide != null)
          slide.Bind(this);
      }
    }

    int RegisterSlides(TutorialSlideView[] slides, int startIndex)
    {
      if (slides == null)
        return startIndex;

      for (int i = 0; i < slides.Length; i++)
      {
        TutorialSlideView slide = slides[i];
        if (slide == null)
        {
          _allSlideStates[startIndex] = default;
          startIndex++;
          continue;
        }

        RectTransform rect = slide.RectTransform;
        CanvasGroup group = slide.GetComponent<CanvasGroup>();
        if (group == null)
          group = slide.gameObject.AddComponent<CanvasGroup>();

        _allSlideStates[startIndex] = new SlideVisualState
        {
          Slide = slide,
          Group = group,
          RestLocalScale = rect != null ? rect.localScale : Vector3.one,
          RestAnchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero
        };

        startIndex++;
      }

      return startIndex;
    }

    void CacheVanBackgroundTargets()
    {
      if (vanBackgroundLarge != null)
      {
        _largeVanRect = vanBackgroundLarge.rectTransform;
        _largeVanGroup = EnsureCanvasGroup(vanBackgroundLarge.gameObject);
        _largeVanRestLocalScale = _largeVanRect.localScale;
        _largeVanRestAnchoredPosition = _largeVanRect.anchoredPosition;
      }

      if (vanBackgroundSmall != null)
      {
        _smallVanRect = vanBackgroundSmall.rectTransform;
        _smallVanGroup = EnsureCanvasGroup(vanBackgroundSmall.gameObject);
      }
    }

    void PrepareVanBackgroundTargetsAtRuntime()
    {
      if (_largeVanRect != null)
      {
        _largeVanRestWorldCenter = _largeVanRect.TransformPoint(_largeVanRect.rect.center);
        _largeVanRestWorldSize = GetWorldSize(_largeVanRect);
      }

      if (_smallVanRect == null)
        return;

      bool smallWasActive = _smallVanRect.gameObject.activeSelf;
      _smallVanRect.gameObject.SetActive(true);

      _smallVanWorldCenter = _smallVanRect.TransformPoint(_smallVanRect.rect.center);
      _smallVanWorldSize = GetWorldSize(_smallVanRect);

      if (!smallWasActive)
        _smallVanRect.gameObject.SetActive(false);
    }

    void PrepareVanBackgroundsForSectionOne()
    {
      PrepareVanBackgroundTargetsAtRuntime();

      if (vanBackgroundLarge != null)
      {
        vanBackgroundLarge.gameObject.SetActive(true);
        ResetLargeVanTransform();
        if (_largeVanGroup != null)
          _largeVanGroup.alpha = 1f;
      }

      if (vanBackgroundSmall != null)
      {
        vanBackgroundSmall.gameObject.SetActive(false);
        if (_smallVanGroup != null)
          _smallVanGroup.alpha = 0f;
      }

      _isSectionTwo = false;
    }

    void PrepareVanBackgroundsForSectionTwo()
    {
      if (vanBackgroundLarge != null)
        vanBackgroundLarge.gameObject.SetActive(false);

      if (vanBackgroundSmall != null)
      {
        vanBackgroundSmall.gameObject.SetActive(true);
        if (_smallVanGroup != null)
          _smallVanGroup.alpha = 1f;
      }

      _isSectionTwo = true;
    }

    void AdvanceFromCurrentSlide()
    {
      if (_currentSlideIndex >= _allSlideStates.Length - 1)
      {
        LoadMainGame();
        return;
      }

      int nextIndex = _currentSlideIndex + 1;
      bool crossingSections = !_isSectionTwo && nextIndex >= sectionOneSlides.Length;

      if (crossingSections)
        PlaySectionTransition(nextIndex);
      else
        TransitionToSlide(nextIndex);
    }

    void TransitionToSlide(int nextIndex, Action onComplete = null)
    {
      if (nextIndex < 0 || nextIndex >= _allSlideStates.Length)
        return;

      _isAnimating = true;
      KillActiveTween();

      SlideVisualState current = _allSlideStates[_currentSlideIndex];
      SlideVisualState next = _allSlideStates[nextIndex];

      PrepareSlideVisible(next, visible: true, immediate: false, entranceValues: true);

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

      AnimateSlideOut(sequence, current);
      AnimateSlideIn(sequence, next);

      sequence.OnComplete(() =>
      {
        HideSlide(current, immediate: true);
        _currentSlideIndex = nextIndex;
        _isAnimating = false;
        onComplete?.Invoke();
      });

      _activeTween = sequence;
    }

    void PlaySectionTransition(int firstSectionTwoSlideIndex)
    {
      if (_largeVanRect == null || _smallVanRect == null)
      {
        PrepareVanBackgroundsForSectionTwo();
        TransitionToSlide(firstSectionTwoSlideIndex);
        return;
      }

      _isAnimating = true;
      KillActiveTween();

      SlideVisualState current = _allSlideStates[_currentSlideIndex];
      HideSlide(current, immediate: true);

      _smallVanRect.gameObject.SetActive(true);
      if (_smallVanGroup != null)
        _smallVanGroup.alpha = 0f;

      ResetLargeVanTransform();
      if (_largeVanGroup != null)
        _largeVanGroup.alpha = 1f;

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

      sequence.Append(
        DOTween.To(() => 0f, ApplyLargeVanMorph, 1f, sectionTransitionDuration)
          .SetEase(sectionTransitionEase));

      if (_largeVanGroup != null)
      {
        sequence.Join(
          _largeVanGroup.DOFade(0f, sectionTransitionDuration * 0.55f)
            .SetDelay(sectionLargeVanFadeDelay)
            .SetEase(Ease.InQuad));
      }

      if (_smallVanGroup != null)
      {
        sequence.Join(
          _smallVanGroup.DOFade(1f, sectionTransitionDuration * 0.65f)
            .SetDelay(sectionSmallVanFadeDelay)
            .SetEase(Ease.OutQuad));
      }

      sequence.OnComplete(() =>
      {
        PrepareVanBackgroundsForSectionTwo();
        ResetLargeVanTransform();
        _currentSlideIndex = firstSectionTwoSlideIndex - 1;
        TransitionToSlide(firstSectionTwoSlideIndex);
      });

      _activeTween = sequence;
    }

    void ApplyLargeVanMorph(float t)
    {
      if (_largeVanRect == null)
        return;

      ResetLargeVanTransform();

      Vector3 targetCenter = Vector3.Lerp(_largeVanRestWorldCenter, _smallVanWorldCenter, t);
      float targetWidth = Mathf.Lerp(_largeVanRestWorldSize.x, _smallVanWorldSize.x, t);
      Vector2 currentSize = GetWorldSize(_largeVanRect);

      if (currentSize.x > 0.001f)
      {
        float scaleMultiplier = targetWidth / currentSize.x;
        _largeVanRect.localScale = _largeVanRestLocalScale * scaleMultiplier;
      }

      _largeVanRect.position = targetCenter;
    }

    void AnimateSlideOut(Sequence sequence, SlideVisualState slide)
    {
      if (slide.Slide == null || slide.Group == null)
        return;

      RectTransform rect = slide.Slide.RectTransform;
      if (rect == null)
        return;

      sequence.Join(
        rect.DOScale(slide.RestLocalScale * slideOutScale, slideOutDuration)
          .SetEase(slideOutEase));
      sequence.Join(
        rect.DOAnchorPos(slide.RestAnchoredPosition + new Vector2(0f, slideOutYOffset), slideOutDuration)
          .SetEase(slideOutEase));
      sequence.Join(
        slide.Group.DOFade(0f, slideOutDuration)
          .SetEase(slideOutEase));
    }

    void AnimateSlideIn(Sequence sequence, SlideVisualState slide)
    {
      if (slide.Slide == null || slide.Group == null)
        return;

      RectTransform rect = slide.Slide.RectTransform;
      if (rect == null)
        return;

      sequence.Append(
        rect.DOScale(slide.RestLocalScale, slideInDuration)
          .SetEase(slideInEase));
      sequence.Join(
        rect.DOAnchorPos(slide.RestAnchoredPosition, slideInDuration)
          .SetEase(slideInEase));
      sequence.Join(
        slide.Group.DOFade(1f, slideInDuration)
          .SetEase(Ease.OutCubic));
    }

    void ShowSlide(int index, bool immediate, bool playEntrance)
    {
      if (index < 0 || index >= _allSlideStates.Length)
        return;

      HideAllSlides(immediate: true);

      SlideVisualState slide = _allSlideStates[index];
      PrepareSlideVisible(slide, visible: true, immediate: immediate, entranceValues: playEntrance && !immediate);

      if (playEntrance && !immediate)
      {
        _isAnimating = true;
        KillActiveTween();

        Sequence sequence = DOTween.Sequence();
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        AnimateSlideIn(sequence, slide);
        sequence.OnComplete(() => _isAnimating = false);
        _activeTween = sequence;
      }

      _currentSlideIndex = index;
    }

    void PrepareSlideVisible(SlideVisualState slide, bool visible, bool immediate, bool entranceValues)
    {
      if (slide.Slide == null)
        return;

      slide.Slide.gameObject.SetActive(visible);

      RectTransform rect = slide.Slide.RectTransform;
      if (rect == null || slide.Group == null)
        return;

      if (immediate)
      {
        rect.localScale = slide.RestLocalScale;
        rect.anchoredPosition = slide.RestAnchoredPosition;
        slide.Group.alpha = visible ? 1f : 0f;
        return;
      }

      if (entranceValues)
      {
        rect.localScale = slide.RestLocalScale * slideInStartScale;
        rect.anchoredPosition = slide.RestAnchoredPosition + new Vector2(0f, slideInStartYOffset);
        slide.Group.alpha = 0f;
        return;
      }

      rect.localScale = slide.RestLocalScale;
      rect.anchoredPosition = slide.RestAnchoredPosition;
      slide.Group.alpha = visible ? 1f : 0f;
    }

    void HideSlide(SlideVisualState slide, bool immediate)
    {
      if (slide.Slide == null)
        return;

      if (immediate)
      {
        slide.Slide.gameObject.SetActive(false);
        if (slide.Group != null)
          slide.Group.alpha = 0f;
        return;
      }

      if (slide.Group != null)
      {
        slide.Group.DOFade(0f, slideOutDuration)
          .SetEase(slideOutEase)
          .OnComplete(() => slide.Slide.gameObject.SetActive(false));
        return;
      }

      slide.Slide.gameObject.SetActive(false);
    }

    void HideAllSlides(bool immediate)
    {
      for (int i = 0; i < _allSlideStates.Length; i++)
        HideSlide(_allSlideStates[i], immediate);
    }

    int FindSlideIndex(TutorialSlideView slide)
    {
      for (int i = 0; i < _allSlideStates.Length; i++)
      {
        if (_allSlideStates[i].Slide == slide)
          return i;
      }

      return -1;
    }

    void ResetLargeVanTransform()
    {
      if (_largeVanRect == null)
        return;

      _largeVanRect.localScale = _largeVanRestLocalScale;
      _largeVanRect.anchoredPosition = _largeVanRestAnchoredPosition;
    }

    static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
      CanvasGroup group = target.GetComponent<CanvasGroup>();
      if (group == null)
        group = target.AddComponent<CanvasGroup>();

      return group;
    }

    static Vector2 GetWorldSize(RectTransform rectTransform)
    {
      Vector3[] corners = new Vector3[4];
      rectTransform.GetWorldCorners(corners);
      return new Vector2(
        Vector3.Distance(corners[0], corners[3]),
        Vector3.Distance(corners[0], corners[1]));
    }

    void KillActiveTween()
    {
      if (_activeTween != null && _activeTween.IsActive())
        _activeTween.Kill();

      _activeTween = null;

      for (int i = 0; i < _allSlideStates.Length; i++)
      {
        SlideVisualState slide = _allSlideStates[i];
        if (slide.Group != null)
          slide.Group.DOKill();

        if (slide.Slide != null && slide.Slide.RectTransform != null)
          slide.Slide.RectTransform.DOKill();
      }

      if (_largeVanGroup != null)
        _largeVanGroup.DOKill();

      if (_smallVanGroup != null)
        _smallVanGroup.DOKill();

      if (_largeVanRect != null)
        _largeVanRect.DOKill();
    }
  }
}
