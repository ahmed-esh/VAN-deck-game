using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VanGame.Data;

namespace VanGame.UI
{
  public class AbilityCardView : MonoBehaviour, IPointerClickHandler
  {
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image backgroundImage;
    [SerializeField] Color normalColor = new Color(0.85f, 0.92f, 1f, 1f);
    [SerializeField] Color hoverColor = new Color(0.75f, 0.88f, 1f, 1f);

    AbilityDefinition _definition;
    bool _interactable = true;

    public AbilityDefinition Definition => _definition;
    public RectTransform RectTransform => transform as RectTransform;

    public event Action<AbilityCardView> Clicked;

    public void Setup(AbilityDefinition definition)
    {
      _definition = definition;

      if (definition == null)
        return;

      if (titleText != null)
        titleText.text = definition.title;

      if (descriptionText != null)
        descriptionText.text = definition.description;

      if (backgroundImage != null)
        backgroundImage.color = normalColor;
    }

    public void SetInteractable(bool interactable)
    {
      _interactable = interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_interactable || _definition == null)
        return;

      Clicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      if (backgroundImage != null && _interactable)
        backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      if (backgroundImage != null)
        backgroundImage.color = normalColor;
    }
  }
}
