using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VanGame.UI
{
  public class SouvenirVanItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
  {
    [SerializeField] string souvenirObjectName;
    [SerializeField] float inactiveAlpha = 0.42f;
    [SerializeField] Color selectedOutlineColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] Vector2 selectedOutlineDistance = new Vector2(3f, -3f);

    Image _image;
    Outline _outline;
    CanvasGroup _canvasGroup;
    bool _isSelected;
    bool _selectionEnabled = true;

    public string SouvenirObjectName => souvenirObjectName;
    public bool IsSelected => _isSelected;

    public event Action<SouvenirVanItem> Hovered;
    public event Action<SouvenirVanItem> Unhovered;
    public event Action<SouvenirVanItem> Clicked;

    void Awake()
    {
      _image = GetComponent<Image>();
      _outline = GetComponent<Outline>();
      if (_outline == null)
        _outline = gameObject.AddComponent<Outline>();

      _outline.effectColor = selectedOutlineColor;
      _outline.effectDistance = selectedOutlineDistance;
      _outline.enabled = false;

      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Configure(string objectName)
    {
      souvenirObjectName = objectName;
      gameObject.name = objectName;
    }

    public void SetSelectionEnabled(bool enabled)
    {
      _selectionEnabled = enabled;
    }

    public void SetSelected(bool selected)
    {
      _isSelected = selected;

      if (_outline != null)
        _outline.enabled = selected;

      ApplyVisualAlpha();
    }

    void ApplyVisualAlpha()
    {
      if (_canvasGroup == null)
        return;

      _canvasGroup.alpha = _isSelected ? 1f : inactiveAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);

    public void OnPointerExit(PointerEventData eventData) => Unhovered?.Invoke(this);

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_selectionEnabled)
        return;

      Clicked?.Invoke(this);
    }
  }
}
