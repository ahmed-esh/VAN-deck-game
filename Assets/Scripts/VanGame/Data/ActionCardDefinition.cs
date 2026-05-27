using System.Collections.Generic;
using UnityEngine;

namespace VanGame.Data
{
  [CreateAssetMenu(fileName = "ActionCard", menuName = "Van Game/Action Card")]
  public class ActionCardDefinition : ScriptableObject
  {
    [Header("Identity")]
    public string cardId;
    public string title;
    [TextArea(2, 5)] public string description;
    public CardCategory category = CardCategory.Food;
    public CardTier tier = CardTier.HumbleBeginning;

    [Header("Costs")]
    public int moneyCostMin;
    public int moneyCostMax;
    public bool rollCostOnPlay;

    [Header("Effects")]
    [Tooltip("How this card changes stats when played. Add/Subtract change by value; Multiply/Divide apply to the current stat (or duration).")]
    public CardEffect[] effects = System.Array.Empty<CardEffect>();

    public bool countsAsFedToday;

    [Header("Driving day time")]
    [Tooltip("Each driving day is 8 bar sections. Playing this card fills that many sections immediately.")]
    public CardDayTimeCost dayTimeCost = CardDayTimeCost.OneSection;

    [Header("Legacy effects (auto-migrated to Effects array)")]
    [HideInInspector] public float moraleDeltaPercent;
    [HideInInspector] public float fuelDeltaPercent;
    [HideInInspector] public float vanConditionDelta;
    [HideInInspector] public float realTimeSeconds;

    [Header("Region availability")]
    [Tooltip("When off, this card can appear in the hand in any region.")]
    public bool restrictToSpecificRegions;

    [Tooltip("Only used when Restrict To Specific Regions is on. Card is drawn/shown only in these cities.")]
    public CityDefinition[] allowedRegions = System.Array.Empty<CityDefinition>();

    [Header("Deck building")]
    public bool includeInStartingHand;
    public int duplicateCount = 1;

    public bool HasAuthoredEffects => effects != null && effects.Length > 0;

    public int GetDayTimeSections()
    {
      int sections = (int)dayTimeCost;
      return Mathf.Clamp(sections, 1, 4);
    }

    public bool IsLegalInRegion(CityDefinition region)
    {
      if (!restrictToSpecificRegions || allowedRegions == null || allowedRegions.Length == 0)
        return true;

      if (region == null)
        return false;

      foreach (CityDefinition allowed in allowedRegions)
      {
        if (allowed == region)
          return true;
      }

      return false;
    }

    public void MigrateLegacyEffectsToArray()
    {
      if (HasAuthoredEffects)
        return;

      List<CardEffect> list = new List<CardEffect>();

      if (Mathf.Abs(moraleDeltaPercent) > 0.001f)
        list.Add(new CardEffect { target = CardEffectTarget.Morale, operation = CardStatOperation.Add, value = moraleDeltaPercent });

      if (Mathf.Abs(fuelDeltaPercent) > 0.001f)
        list.Add(new CardEffect { target = CardEffectTarget.Fuel, operation = CardStatOperation.Add, value = fuelDeltaPercent });

      if (Mathf.Abs(vanConditionDelta) > 0.001f)
        list.Add(new CardEffect { target = CardEffectTarget.VanCondition, operation = CardStatOperation.Add, value = vanConditionDelta });

      if (realTimeSeconds > 0.001f)
        list.Add(new CardEffect { target = CardEffectTarget.ActionDuration, operation = CardStatOperation.Add, value = realTimeSeconds });

      effects = list.ToArray();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      if (!HasAuthoredEffects && HasAnyLegacyEffect())
        MigrateLegacyEffectsToArray();
    }

    bool HasAnyLegacyEffect()
    {
      return Mathf.Abs(moraleDeltaPercent) > 0.001f
        || Mathf.Abs(fuelDeltaPercent) > 0.001f
        || Mathf.Abs(vanConditionDelta) > 0.001f
        || realTimeSeconds > 0.001f;
    }
#endif
  }
}
