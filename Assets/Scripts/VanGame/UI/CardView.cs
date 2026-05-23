using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VanGame.Data;

namespace VanGame.UI
{
  public class CardView : MonoBehaviour, IPointerClickHandler
  {
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text categoryText;
    [SerializeField] TMP_Text costText;
    [SerializeField] TMP_Text statsText;
    [SerializeField] Image backgroundImage;
    [SerializeField] Color affordableColor = Color.white;
    [SerializeField] Color unaffordableColor = new Color(0.75f, 0.45f, 0.45f, 1f);

    ActionCardDefinition _definition;
    bool _interactable = true;

    public ActionCardDefinition Definition => _definition;
    public RectTransform RectTransform => transform as RectTransform;

    public event Action<CardView> Clicked;

    public void Setup(ActionCardDefinition definition, bool canAfford)
    {
      _definition = definition;

      if (definition == null)
        return;

      if (titleText != null)
        titleText.text = definition.title;

      if (categoryText != null)
        categoryText.text = definition.category.ToString();

      if (costText != null)
      {
        if (definition.moneyCostMin == definition.moneyCostMax)
          costText.text = $"${definition.moneyCostMin}";
        else
          costText.text = $"${definition.moneyCostMin}-${definition.moneyCostMax}";
      }

      if (statsText != null)
        statsText.text = BuildStatsLine(definition);

      SetAffordable(canAfford);
    }

    public void SetInteractable(bool interactable)
    {
      _interactable = interactable;
    }

    public void SetAffordable(bool canAfford)
    {
      if (backgroundImage != null)
        backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      if (!_interactable || _definition == null)
        return;

      Clicked?.Invoke(this);
    }

    static string BuildStatsLine(ActionCardDefinition card)
    {
      string line = string.Empty;

      if (Mathf.Abs(card.moraleDeltaPercent) > 0.01f)
        line += $"M {card.moraleDeltaPercent:+0;-0}%  ";

      if (Mathf.Abs(card.fuelDeltaPercent) > 0.01f)
        line += $"F {card.fuelDeltaPercent:+0;-0}%  ";

      if (Mathf.Abs(card.vanConditionDelta) > 0.01f)
        line += $"V {card.vanConditionDelta:+0;-0}  ";

      if (card.realTimeSeconds > 0f)
        line += $"{card.realTimeSeconds:0}s";

      return string.IsNullOrWhiteSpace(line) ? "Instant" : line.Trim();
    }
  }
}
