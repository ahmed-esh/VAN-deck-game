using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VanGame.Data;

namespace VanGame.UI
{
  public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
  {
    [SerializeField] GameObject descriptionRoot;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image backgroundImage;
    [SerializeField] Color affordableColor = Color.white;
    [SerializeField] Color unaffordableColor = new Color(0.75f, 0.45f, 0.45f, 1f);

    ActionCardDefinition _definition;
    bool _interactable = true;
    bool _isPlaying;

    public ActionCardDefinition Definition => _definition;
    public RectTransform RectTransform => transform as RectTransform;
    public bool IsPlaying => _isPlaying;

    public event Action<CardView> Clicked;

    void Awake()
    {
      SetDescriptionVisible(false);
    }

    public void Setup(ActionCardDefinition definition, bool canAfford)
    {
      _definition = definition;
      _isPlaying = false;
      SetDescriptionVisible(false);

      if (definition == null)
        return;

      if (descriptionText != null)
        descriptionText.text = string.IsNullOrWhiteSpace(definition.description)
          ? definition.title
          : definition.description;

      SetAffordable(canAfford);
    }

    public void SetInteractable(bool interactable)
    {
      _interactable = interactable && !_isPlaying;
    }

    public void SetPlaying(bool playing)
    {
      _isPlaying = playing;
      _interactable = !playing;

      if (playing)
        SetDescriptionVisible(false);
    }

    public void SetAffordable(bool canAfford)
    {
      if (backgroundImage != null)
        backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      if (!_interactable || _isPlaying || _definition == null)
        return;

      SetDescriptionVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      SetDescriptionVisible(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_interactable || _isPlaying || _definition == null)
        return;

      Clicked?.Invoke(this);
    }

    void SetDescriptionVisible(bool visible)
    {
      if (descriptionRoot != null)
      {
        descriptionRoot.SetActive(visible);
        return;
      }

      if (descriptionText != null)
        descriptionText.gameObject.SetActive(visible);
    }
  }
}
