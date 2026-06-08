using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VanGame.Data;

namespace VanGame.UI
{
  public enum MapRegionHighlightMode
  {
    DestinationPick,
    DrivingOverview
  }

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

    [Header("Leg destination (driving overview)")]
    [SerializeField] Color legDestinationTint = Color.white;

    [Header("Final destination (City B win target)")]
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
      bool isFinalDestination,
      bool isLegDestination,
      bool isCurrent,
      bool allowSelection,
      MapRegionHighlightMode highlightMode)
    {
      _isReachable = allowSelection && reachable && !visited && !isCurrent;
      _isVisited = visited;

      StopRegionBlink();
      SetFinalDestinationFlag(isFinalDestination);

      if (_image == null)
        return;

      _image.raycastTarget = _isReachable;

      if (highlightMode == MapRegionHighlightMode.DrivingOverview)
      {
        ApplyDrivingOverviewVisuals(visited, isLegDestination, isCurrent, isFinalDestination);
        return;
      }

      if (isCurrent)
      {
        StartRegionBlink(_image, currentTint, currentBlinkTint, currentBlinkDuration);
        return;
      }

      if (isFinalDestination)
      {
        _image.color = destinationBaseTint;
        return;
      }

      _image.color = visited ? visitedTint : reachable ? reachableTint : unreachableTint;
    }

    void ApplyDrivingOverviewVisuals(
      bool visited,
      bool isLegDestination,
      bool isCurrent,
      bool isFinalDestination)
    {
      if (isCurrent)
      {
        _image.color = currentTint;
        return;
      }

      if (isLegDestination)
      {
        _image.color = legDestinationTint;
        return;
      }

      if (isFinalDestination)
      {
        _image.color = destinationBaseTint;
        return;
      }

      if (visited)
      {
        _image.color = visitedTint;
        return;
      }

      _image.color = unreachableTint;
    }

    void SetFinalDestinationFlag(bool show)
    {
      if (destinationBlinkFlag == null)
        return;

      if (!show)
      {
        destinationBlinkFlag.DOKill();
        destinationBlinkFlag.enabled = false;
        destinationBlinkFlag.gameObject.SetActive(false);
        return;
      }

      destinationBlinkFlag.gameObject.SetActive(true);
      destinationBlinkFlag.enabled = true;
      StartRegionBlink(
        destinationBlinkFlag,
        destinationFlagTint,
        destinationFlagBlinkTint,
        destinationFlagBlinkDuration);
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

    void OnEnable()
    {
      _mapController?.RequestMapVisualRefresh();
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
