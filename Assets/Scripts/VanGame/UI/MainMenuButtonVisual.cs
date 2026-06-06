using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VanGame.UI
{
  /// <summary>
  /// Swaps between a normal and hover menu button visual with DOTween.
  /// Attach to the normal button GameObject (the one with the Button component).
  /// </summary>
  public class MainMenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    [SerializeField] GameObject normalVisual;
    [SerializeField] GameObject hoverVisual;

    [Header("Animation")]
    [SerializeField] float transitionDuration = 0.18f;
    [SerializeField] float hoverScale = 1.06f;
    [SerializeField] float normalHoverOutScale = 0.94f;
    [SerializeField] Ease transitionEase = Ease.OutCubic;
    [SerializeField] Ease hoverScaleEase = Ease.OutBack;

    CanvasGroup _normalGroup;
    CanvasGroup _hoverGroup;
    Sequence _sequence;
    bool _isHovered;

    public GameObject NormalVisual => normalVisual;
    public Button ClickButton => normalVisual != null ? normalVisual.GetComponent<Button>() : null;

    void Awake()
    {
      if (normalVisual == null)
        normalVisual = gameObject;

      _normalGroup = EnsureCanvasGroup(normalVisual, allowRaycasts: true);
      _hoverGroup = EnsureCanvasGroup(hoverVisual, allowRaycasts: false);

      DisableBuiltInButtonTransition();
      DisableRaycastsOnHoverSubtree();
      AlignHoverVisual();
      SetHoverImmediate(false);
    }

    void OnEnable()
    {
      DisableRaycastsOnHoverSubtree();
      SetHoverImmediate(_isHovered);
    }

    void OnDisable()
    {
      _sequence?.Kill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
      if (_isHovered == hovered)
        return;

      _isHovered = hovered;
      AnimateHover(hovered);
    }

    void DisableBuiltInButtonTransition()
    {
      Button button = GetComponent<Button>();
      if (button == null && normalVisual != null)
        button = normalVisual.GetComponent<Button>();

      if (button == null)
        return;

      button.transition = Selectable.Transition.None;

      Graphic targetGraphic = button.targetGraphic;
      if (targetGraphic != null)
        targetGraphic.raycastTarget = true;
    }

    void DisableRaycastsOnHoverSubtree()
    {
      if (hoverVisual == null)
        return;

      Graphic[] graphics = hoverVisual.GetComponentsInChildren<Graphic>(true);
      foreach (Graphic graphic in graphics)
        graphic.raycastTarget = false;

      Selectable[] selectables = hoverVisual.GetComponentsInChildren<Selectable>(true);
      foreach (Selectable selectable in selectables)
        selectable.interactable = false;
    }

    void AlignHoverVisual()
    {
      if (normalVisual == null || hoverVisual == null)
        return;

      RectTransform normalRect = normalVisual.transform as RectTransform;
      RectTransform hoverRect = hoverVisual.transform as RectTransform;
      if (normalRect == null || hoverRect == null)
        return;

      hoverRect.SetParent(normalRect.parent, false);
      hoverRect.anchorMin = normalRect.anchorMin;
      hoverRect.anchorMax = normalRect.anchorMax;
      hoverRect.pivot = normalRect.pivot;
      hoverRect.anchoredPosition = normalRect.anchoredPosition;
      hoverRect.sizeDelta = normalRect.sizeDelta;
      hoverRect.localRotation = normalRect.localRotation;
      hoverRect.localScale = Vector3.one;
      hoverRect.SetSiblingIndex(normalRect.GetSiblingIndex() + 1);
    }

    void SetHoverImmediate(bool hovered)
    {
      _isHovered = hovered;

      if (normalVisual != null)
      {
        normalVisual.SetActive(true);
        if (_normalGroup != null)
          _normalGroup.alpha = hovered ? 0f : 1f;
        normalVisual.transform.localScale = Vector3.one;
      }

      if (hoverVisual == null)
        return;

      hoverVisual.SetActive(hovered);
      if (_hoverGroup != null)
        _hoverGroup.alpha = hovered ? 1f : 0f;
      hoverVisual.transform.localScale = hovered ? Vector3.one * hoverScale : Vector3.one;
    }

    void AnimateHover(bool hovered)
    {
      _sequence?.Kill();

      if (hoverVisual != null)
      {
        hoverVisual.SetActive(true);
        DisableRaycastsOnHoverSubtree();
      }

      _sequence = DOTween.Sequence().SetUpdate(true);

      if (_normalGroup != null)
      {
        _sequence.Join(_normalGroup.DOFade(hovered ? 0f : 1f, transitionDuration).SetEase(transitionEase));
        _sequence.Join(
          normalVisual.transform
            .DOScale(hovered ? normalHoverOutScale : 1f, transitionDuration)
            .SetEase(transitionEase));
      }

      if (_hoverGroup != null && hoverVisual != null)
      {
        _sequence.Join(_hoverGroup.DOFade(hovered ? 1f : 0f, transitionDuration).SetEase(transitionEase));
        _sequence.Join(
          hoverVisual.transform
            .DOScale(hovered ? hoverScale : 1f, transitionDuration)
            .SetEase(hoverScaleEase));
      }

      if (!hovered && hoverVisual != null)
      {
        _sequence.OnComplete(() =>
        {
          if (!_isHovered)
            hoverVisual.SetActive(false);
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
