using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VanGame.UI
{
  public class SouvenirPickItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
  {
    [SerializeField] string souvenirObjectName;
    [SerializeField] int slotIndex;
    [SerializeField] float hoverLiftY = 18f;

    RectTransform _rectTransform;
    Vector2 _restAnchoredPosition;
    bool _isInteractable = true;

    public string SouvenirObjectName => souvenirObjectName;
    public int SlotIndex => slotIndex;

    public event Action<SouvenirPickItem> Hovered;
    public event Action<SouvenirPickItem> Unhovered;
    public event Action<SouvenirPickItem> Clicked;

    public void Configure(string objectName, int slot)
    {
      souvenirObjectName = objectName;
      slotIndex = slot;
      gameObject.name = objectName;
    }

    public void SetInteractable(bool interactable)
    {
      _isInteractable = interactable;
      Image image = GetComponent<Image>();
      if (image != null)
        image.raycastTarget = interactable;
    }

    void Awake()
    {
      _rectTransform = transform as RectTransform;
      if (_rectTransform != null)
        _restAnchoredPosition = _rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      if (!_isInteractable || _rectTransform == null)
        return;

      _rectTransform.anchoredPosition = _restAnchoredPosition + new Vector2(0f, hoverLiftY);
      Hovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      ResetLift();
      Unhovered?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_isInteractable)
        return;

      Clicked?.Invoke(this);
    }

    public void ResetLift()
    {
      if (_rectTransform != null)
        _rectTransform.anchoredPosition = _restAnchoredPosition;
    }
  }
}
