using UnityEngine;
using UnityEngine.EventSystems;

namespace VanGame.UI
{
  /// <summary>
  /// Attach to each tutorial slide Image. Forwards clicks to TutorialSceneController.
  /// </summary>
  [RequireComponent(typeof(RectTransform))]
  public class TutorialSlideView : MonoBehaviour, IPointerClickHandler
  {
    TutorialSceneController _controller;

    public RectTransform RectTransform => transform as RectTransform;

    public void Bind(TutorialSceneController controller)
    {
      _controller = controller;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      _controller?.HandleSlideClicked(this);
    }
  }
}
