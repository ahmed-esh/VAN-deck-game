using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace VanGame.UI
{
  /// <summary>
  /// Two-state button: crossfades between state A and state B on each click.
  /// Attach to the primary visual (state A). Assign the alternate visual as state B.
  /// </summary>
  public class DualStateToggleButton : MonoBehaviour
  {
    [SerializeField] GameObject stateAVisual;
    [SerializeField] GameObject stateBVisual;
    [SerializeField] Button clickButton;

    [Header("Animation")]
    [SerializeField] float transitionDuration = 0.2f;
    [SerializeField] float activeScale = 1.06f;
    [SerializeField] float inactiveScale = 0.94f;
    [SerializeField] Ease transitionEase = Ease.OutCubic;
    [SerializeField] Ease activeScaleEase = Ease.OutBack;
    [SerializeField] bool useUnscaledTime = true;

    [Header("Start state")]
    [SerializeField] bool startOnStateA = true;

    CanvasGroup _stateAGroup;
    CanvasGroup _stateBGroup;
    Sequence _sequence;
    bool _isStateA;

    public bool IsStateA => _isStateA;
    public event Action<bool> StateChanged;

    void Awake()
    {
      if (stateAVisual == null)
        stateAVisual = gameObject;

      if (clickButton == null)
        clickButton = stateAVisual.GetComponent<Button>();

      _stateAGroup = EnsureCanvasGroup(stateAVisual, allowRaycasts: true);
      _stateBGroup = EnsureCanvasGroup(stateBVisual, allowRaycasts: false);

      DisableBuiltInButtonTransition();
      DisableRaycastsOnAlternateVisual();
      AlignAlternateVisual();
      SetState(startOnStateA, animate: false);

      if (clickButton != null)
        clickButton.onClick.AddListener(HandleClick);
    }

    void OnDestroy()
    {
      _sequence?.Kill();
    }

    void HandleClick()
    {
      SetState(!_isStateA, animate: true);
      StateChanged?.Invoke(_isStateA);
    }

    public void SetState(bool stateA, bool animate)
    {
      if (!animate)
      {
        _isStateA = stateA;
        ApplyStateImmediate(stateA);
        return;
      }

      if (_isStateA == stateA)
        return;

      _isStateA = stateA;
      AnimateState(stateA);
    }

    void DisableBuiltInButtonTransition()
    {
      if (clickButton == null)
        return;

      clickButton.transition = Selectable.Transition.None;

      if (clickButton.targetGraphic != null)
        clickButton.targetGraphic.raycastTarget = true;
    }

    void DisableRaycastsOnAlternateVisual()
    {
      if (stateBVisual == null)
        return;

      Graphic[] graphics = stateBVisual.GetComponentsInChildren<Graphic>(true);
      foreach (Graphic graphic in graphics)
        graphic.raycastTarget = false;

      Selectable[] selectables = stateBVisual.GetComponentsInChildren<Selectable>(true);
      foreach (Selectable selectable in selectables)
        selectable.interactable = false;
    }

    void AlignAlternateVisual()
    {
      if (stateAVisual == null || stateBVisual == null)
        return;

      RectTransform stateARect = stateAVisual.transform as RectTransform;
      RectTransform stateBRect = stateBVisual.transform as RectTransform;
      if (stateARect == null || stateBRect == null)
        return;

      stateBRect.SetParent(stateARect.parent, false);
      stateBRect.anchorMin = stateARect.anchorMin;
      stateBRect.anchorMax = stateARect.anchorMax;
      stateBRect.pivot = stateARect.pivot;
      stateBRect.anchoredPosition = stateARect.anchoredPosition;
      stateBRect.sizeDelta = stateARect.sizeDelta;
      stateBRect.localRotation = stateARect.localRotation;
      stateBRect.localScale = Vector3.one;
      stateBRect.SetSiblingIndex(stateARect.GetSiblingIndex() + 1);
    }

    void ApplyStateImmediate(bool stateA)
    {
      if (stateAVisual != null)
      {
        stateAVisual.SetActive(true);
        if (_stateAGroup != null)
          _stateAGroup.alpha = stateA ? 1f : 0f;
        stateAVisual.transform.localScale = Vector3.one;
      }

      if (stateBVisual == null)
        return;

      stateBVisual.SetActive(!stateA);
      if (_stateBGroup != null)
        _stateBGroup.alpha = stateA ? 0f : 1f;
      stateBVisual.transform.localScale = stateA ? Vector3.one : Vector3.one * activeScale;
    }

    void AnimateState(bool stateA)
    {
      _sequence?.Kill();

      if (stateBVisual != null)
      {
        stateBVisual.SetActive(true);
        DisableRaycastsOnAlternateVisual();
      }

      _sequence = DOTween.Sequence();
      if (useUnscaledTime)
        _sequence.SetUpdate(true);

      if (_stateAGroup != null && stateAVisual != null)
      {
        _sequence.Join(_stateAGroup.DOFade(stateA ? 1f : 0f, transitionDuration).SetEase(transitionEase));
        _sequence.Join(
          stateAVisual.transform
            .DOScale(stateA ? 1f : inactiveScale, transitionDuration)
            .SetEase(transitionEase));
      }

      if (_stateBGroup != null && stateBVisual != null)
      {
        _sequence.Join(_stateBGroup.DOFade(stateA ? 0f : 1f, transitionDuration).SetEase(transitionEase));
        _sequence.Join(
          stateBVisual.transform
            .DOScale(stateA ? 1f : activeScale, transitionDuration)
            .SetEase(activeScaleEase));
      }

      if (stateA && stateBVisual != null)
      {
        _sequence.OnComplete(() =>
        {
          if (_isStateA)
            stateBVisual.SetActive(false);
        });
      }
    }

    static CanvasGroup EnsureCanvasGroup(GameObject target, bool allowRaycasts)
    {
      if (target == null)
        return null;

      CanvasGroup group = target.GetComponent<CanvasGroup>();
      if (group == null)
        group = target.AddComponent<CanvasGroup>();

      group.blocksRaycasts = allowRaycasts;
      group.interactable = allowRaycasts;
      return group;
    }
  }
}
