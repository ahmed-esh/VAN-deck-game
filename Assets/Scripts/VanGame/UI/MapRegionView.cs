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
    [SerializeField] float hoverLiftY = 24f;
    [SerializeField] float hoverDuration = 0.25f;
    [SerializeField] Ease hoverEase = Ease.OutCubic;
    [SerializeField] MapStatsTooltipView tooltip;
    [SerializeField] Image highlightImage;
    [SerializeField] Color reachableTint = Color.white;
    [SerializeField] Color visitedTint = new Color(0.55f, 0.55f, 0.55f, 0.65f);
    [SerializeField] Color unreachableTint = new Color(1f, 1f, 1f, 0.25f);

    Image _image;
    MapController _mapController;
    Vector2 _restAnchoredPosition;
    bool _isLifted;
    bool _isReachable;
    bool _isVisited;

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
    }

    public void Initialize(MapController controller)
    {
      _mapController = controller;
    }

    public void SetInteractableState(bool reachable, bool visited, bool isDestination)
    {
      _isReachable = reachable && !visited;
      _isVisited = visited;

      if (_image != null)
      {
        _image.raycastTarget = _isReachable;
        _image.color = visited ? visitedTint : reachable ? reachableTint : unreachableTint;
      }
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
      LiftTarget.DOKill();
      _isLifted = false;
      LiftTarget.anchoredPosition = _restAnchoredPosition;
    }
  }
}
