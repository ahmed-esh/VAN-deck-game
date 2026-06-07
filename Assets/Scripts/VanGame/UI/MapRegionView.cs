using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VanGame.Data;

namespace VanGame.UI
{
  [RequireComponent(typeof(Image))]
  public class MapRegionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
  {
    [SerializeField] CityDefinition city;
    [SerializeField] RectTransform liftTarget;
    [SerializeField] float hoverLiftY = 2.4f;
    [SerializeField] float hoverDuration = 0.25f;
    [SerializeField] Ease hoverEase = Ease.OutCubic;
    [SerializeField] MapStatsTooltipView tooltip;
    [SerializeField] Image highlightImage;
    [SerializeField] Color reachableTint = Color.white;
    [SerializeField] Color visitedTint = new Color(0.55f, 0.55f, 0.55f, 0.65f);
    [SerializeField] Color unreachableTint = new Color(1f, 1f, 1f, 0.25f);

    [Header("Current location")]
    [SerializeField] Color currentTint = Color.white;
    [SerializeField] Color currentBlinkTint = new Color(1f, 0.95f, 0.55f, 1f);
    [SerializeField] float currentBlinkDuration = 0.55f;

    [Header("Final destination")]
    [SerializeField] Color destinationBaseTint = Color.white;
    [SerializeField] Image destinationBlinkFlag;
    [SerializeField] Color destinationFlagTint = new Color(1f, 0.72f, 0.72f, 1f);
    [SerializeField] Color destinationFlagBlinkTint = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] float destinationFlagBlinkDuration = 0.65f;

    Image _image;
    MapController _mapController;
    Vector2 _restAnchoredPosition;
    bool _isLifted;
    bool _isReachable;
    bool _isVisited;
    Tweener _regionBlinkTween;
    Tweener _flagBlinkTween;

    public CityDefinition City => city;
    public RectTransform LiftTarget => liftTarget != null ? liftTarget : (RectTransform)transform;
    public Vector2 MapAnchorPosition => LiftTarget.anchoredPosition;

    void Awake()
    {
      _image = GetComponent<Image>();
      if (liftTarget == null)
        liftTarget = (RectTransform)transform;

      _restAnchoredPosition = LiftTarget.anchoredPosition;

      if (highlightImage != null)
        highlightImage.enabled = false;

      if (destinationBlinkFlag != null)
        destinationBlinkFlag.enabled = false;
    }

    public void Initialize(MapController controller)
    {
      _mapController = controller;
    }

    public void SetInteractableState(
      bool reachable,
      bool visited,
      bool isDestination,
      bool isCurrent,
      bool allowSelection)
    {
      _isReachable = allowSelection && reachable && !visited && !isCurrent;
      _isVisited = visited;

      StopRegionBlink();

      if (_image == null)
        return;

      _image.raycastTarget = _isReachable;

      if (isCurrent)
      {
        StartRegionBlink(_image, currentTint, currentBlinkTint, currentBlinkDuration);
        return;
      }

      if (isDestination)
      {
        _image.color = destinationBaseTint;

        if (destinationBlinkFlag != null)
        {
          destinationBlinkFlag.enabled = true;
          StartRegionBlink(destinationBlinkFlag, destinationFlagTint, destinationFlagBlinkTint, destinationFlagBlinkDuration);
        }

        return;
      }

      _image.color = visited ? visitedTint : reachable ? reachableTint : unreachableTint;
    }

    void StartRegionBlink(Image target, Color baseTint, Color blinkTint, float duration)
    {
      if (target == null)
        return;

      target.color = baseTint;
      Tweener tween = target
        .DOColor(blinkTint, duration)
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);

      if (target == _image)
        _regionBlinkTween = tween;
      else
        _flagBlinkTween = tween;
    }

    void StopRegionBlink()
    {
      if (_regionBlinkTween != null)
      {
        _regionBlinkTween.Kill();
        _regionBlinkTween = null;
      }

      if (_flagBlinkTween != null)
      {
        _flagBlinkTween.Kill();
        _flagBlinkTween = null;
      }

      if (_image != null)
        _image.DOKill();

      if (destinationBlinkFlag != null)
        destinationBlinkFlag.DOKill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      if (!_isReachable || _mapController == null)
        return;

      LiftUp();
      _mapController.NotifyRegionHovered(this);

      if (highlightImage != null)
        highlightImage.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      LowerDown();

      if (highlightImage != null)
        highlightImage.enabled = false;

      _mapController?.NotifyRegionUnhovered(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_isReachable || _mapController == null)
        return;

      _mapController.NotifyRegionClicked(this);
    }

    void LiftUp()
    {
      if (_isLifted)
        return;

      _isLifted = true;
      LiftTarget.DOKill();
      LiftTarget.DOAnchorPosY(_restAnchoredPosition.y + hoverLiftY, hoverDuration).SetEase(hoverEase);
    }

    void LowerDown()
    {
      if (!_isLifted)
        return;

      _isLifted = false;
      LiftTarget.DOKill();
      LiftTarget.DOAnchorPosY(_restAnchoredPosition.y, hoverDuration).SetEase(hoverEase);
    }

    void OnDisable()
    {
      StopRegionBlink();
      LiftTarget.DOKill();
      _isLifted = false;
      LiftTarget.anchoredPosition = _restAnchoredPosition;
    }
  }
}
