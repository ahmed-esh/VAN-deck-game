using UnityEngine;
using UnityEngine.UI;

namespace VanGame.UI
{
  public class MapVanMarkerView : MonoBehaviour
  {
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image vanImage;
    [SerializeField] Vector2 markerSize = new Vector2(64f, 64f);

    void Awake()
    {
      if (rectTransform == null)
        rectTransform = transform as RectTransform;

      if (vanImage != null)
      {
        vanImage.raycastTarget = false;
        vanImage.preserveAspect = true;
      }

      ApplySize();
    }

    public void SetAnchoredPosition(Vector2 position)
    {
      if (rectTransform != null)
        rectTransform.anchoredPosition = position;
    }

    public void SetVisible(bool visible)
    {
      gameObject.SetActive(visible);
    }

    void ApplySize()
    {
      if (rectTransform != null)
        rectTransform.sizeDelta = markerSize;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      ApplySize();
    }
#endif
  }
}
