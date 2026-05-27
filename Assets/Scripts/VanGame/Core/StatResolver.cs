using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class StatResolver : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;

    RunState _runState;

    public void Initialize(RunState runState, GameConfig config)
    {
      _runState = runState;
      if (config != null)
        gameConfig = config;
    }

    public void ApplyMoneyDelta(int delta)
    {
      if (_runState == null)
        return;

      int modified = Mathf.RoundToInt(ApplyModifier(ModifierTarget.MoneyCost, delta));
      _runState.Money = Mathf.Max(0, _runState.Money + modified);
      _runState.NotifyStatsChanged();
    }

    public void ApplyMoraleDelta(float deltaPercent)
    {
      if (_runState == null)
        return;

      float modified = ApplyModifier(ModifierTarget.MoraleGain, deltaPercent);
      _runState.MoralePercent = Mathf.Clamp(_runState.MoralePercent + modified, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    public void ApplyFuelDelta(float deltaPercent)
    {
      if (_runState == null)
        return;

      _runState.FuelPercent = Mathf.Clamp(_runState.FuelPercent + deltaPercent, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    public void ApplyVanDelta(float delta)
    {
      if (_runState == null)
        return;

      _runState.VanConditionPercent = Mathf.Clamp(_runState.VanConditionPercent + delta, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    public void ApplyTripDayDelta(int days)
    {
      if (_runState == null)
        return;

      _runState.TripDayCurrent += days;
      _runState.NotifyStatsChanged();
    }

    public void ApplyRandomEvent(RandomEventDefinition evt)
    {
      if (_runState == null || evt == null)
        return;

      ApplyMoneyDelta(evt.moneyDelta);
      ApplyMoraleDelta(evt.moraleDeltaPercent);
      ApplyFuelDelta(evt.fuelDeltaPercent);
      ApplyVanDelta(evt.vanConditionDelta);
      ApplyTripDayDelta(evt.extraDaysAdded);
    }

    public void ApplyCityArrival(CityDefinition city)
    {
      if (_runState == null || city == null)
        return;

      ApplyMoraleDelta(city.baseMoraleDelta);
      ApplyTripDayDelta(city.stayDaysInCity);
    }

    public bool CanAfford(int moneyCost)
    {
      if (_runState == null)
        return false;

      int adjustedCost = Mathf.RoundToInt(ApplyModifier(ModifierTarget.MoneyCost, moneyCost));
      return _runState.Money >= adjustedCost;
    }

    public float GetResolvedActionDuration(float baseRealTimeSeconds)
    {
      if (baseRealTimeSeconds <= 0f)
        return 0f;

      float modified = ApplyModifier(ModifierTarget.ActionDuration, baseRealTimeSeconds);
      return Mathf.Max(0f, modified);
    }

    public int GetResolvedMoneyCost(ActionCardDefinition card)
    {
      if (card == null)
        return 0;

      int cost = card.moneyCostMin;
      if (card.rollCostOnPlay && card.moneyCostMax > card.moneyCostMin)
        cost = Random.Range(card.moneyCostMin, card.moneyCostMax + 1);

      return Mathf.RoundToInt(ApplyModifier(ModifierTarget.MoneyCost, cost));
    }

    public void ApplyCardEffects(ActionCardDefinition card)
    {
      if (_runState == null || card == null)
        return;

      int moneyCost = GetResolvedMoneyCost(card);
      ApplyMoneyDelta(-moneyCost);

      if (card.HasAuthoredEffects)
        ApplyCardEffectList(card.effects);
      else
        ApplyLegacyCardEffects(card);

      if (card.countsAsFedToday)
        _runState.FedToday = true;
    }

    public float GetCardActionDuration(ActionCardDefinition card)
    {
      if (card == null)
        return 0f;

      if (card.HasAuthoredEffects)
        return ResolveActionDurationFromEffects(card.effects);

      return card.realTimeSeconds;
    }

    public int GetCardDayTimeSections(ActionCardDefinition card)
    {
      if (card == null)
        return 1;

      return Mathf.Clamp(card.GetDayTimeSections(), 1, 4);
    }

    void ApplyLegacyCardEffects(ActionCardDefinition card)
    {
      ApplyMoraleDelta(card.moraleDeltaPercent);
      ApplyFuelDelta(card.fuelDeltaPercent);
      ApplyVanDelta(card.vanConditionDelta);
    }

    void ApplyCardEffectList(CardEffect[] effects)
    {
      if (effects == null)
        return;

      foreach (CardEffect effect in effects)
        ApplyCardEffect(effect);
    }

    void ApplyCardEffect(CardEffect effect)
    {
      if (_runState == null)
        return;

      switch (effect.target)
      {
        case CardEffectTarget.Money:
          ApplyMoneyDelta(Mathf.RoundToInt(ResolveCardEffectValue(_runState.Money, effect)));
          break;
        case CardEffectTarget.Morale:
          ApplyMoralePercentEffect(effect);
          break;
        case CardEffectTarget.Fuel:
          ApplyFuelPercentEffect(effect);
          break;
        case CardEffectTarget.VanCondition:
          ApplyVanEffect(effect);
          break;
        case CardEffectTarget.ActionDuration:
          break;
      }
    }

    void ApplyMoralePercentEffect(CardEffect effect)
    {
      if (effect.operation == CardStatOperation.Add || effect.operation == CardStatOperation.Subtract)
      {
        float delta = effect.operation == CardStatOperation.Add ? effect.value : -effect.value;
        ApplyMoraleDelta(delta);
        return;
      }

      float next = CardEffectMath.Apply(_runState.MoralePercent, effect.operation, effect.value);
      _runState.MoralePercent = Mathf.Clamp(next, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    void ApplyFuelPercentEffect(CardEffect effect)
    {
      if (effect.operation == CardStatOperation.Add || effect.operation == CardStatOperation.Subtract)
      {
        float delta = effect.operation == CardStatOperation.Add ? effect.value : -effect.value;
        ApplyFuelDelta(delta);
        return;
      }

      float next = CardEffectMath.Apply(_runState.FuelPercent, effect.operation, effect.value);
      _runState.FuelPercent = Mathf.Clamp(next, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    void ApplyVanEffect(CardEffect effect)
    {
      if (effect.operation == CardStatOperation.Add || effect.operation == CardStatOperation.Subtract)
      {
        float delta = effect.operation == CardStatOperation.Add ? effect.value : -effect.value;
        ApplyVanDelta(delta);
        return;
      }

      float next = CardEffectMath.Apply(_runState.VanConditionPercent, effect.operation, effect.value);
      _runState.VanConditionPercent = Mathf.Clamp(next, 0f, 100f);
      _runState.NotifyStatsChanged();
    }

    static float ResolveCardEffectValue(float currentStat, CardEffect effect)
    {
      if (effect.operation == CardStatOperation.Add || effect.operation == CardStatOperation.Subtract)
        return effect.operation == CardStatOperation.Add ? effect.value : -effect.value;

      return CardEffectMath.Apply(currentStat, effect.operation, effect.value) - currentStat;
    }

    float ResolveActionDurationFromEffects(CardEffect[] effects)
    {
      float duration = 0f;

      foreach (CardEffect effect in effects)
      {
        if (effect.target != CardEffectTarget.ActionDuration)
          continue;

        duration = CardEffectMath.Apply(duration, effect.operation, effect.value);
      }

      return duration;
    }

    public float ApplyModifier(ModifierTarget target, float baseValue)
    {
      if (_runState == null)
        return baseValue;

      float result = baseValue;

      foreach (AbilityDefinition ability in _runState.OwnedAbilities)
      {
        if (ability?.modifiers == null)
          continue;

        foreach (AbilityModifier mod in ability.modifiers)
        {
          if (mod.target != target)
            continue;

          result = ApplyModifierOperation(result, mod.operation, mod.value);
        }
      }

      return result;
    }

    static float ApplyModifierOperation(float baseValue, ModifierOperation operation, float value)
    {
      switch (operation)
      {
        case ModifierOperation.Add:
          return baseValue + value;
        case ModifierOperation.Subtract:
          return baseValue - value;
        case ModifierOperation.Multiply:
          return baseValue * value;
        case ModifierOperation.Divide:
          return Mathf.Approximately(value, 0f) ? baseValue : baseValue / value;
        default:
          return baseValue;
      }
    }

    public string GetLoseReason()
    {
      if (_runState == null || gameConfig == null)
        return "You ran out of resources.";

      if (_runState.FuelPercent <= 0f)
        return "You ran out of fuel.";

      if (_runState.MoralePercent <= 0f)
        return "Family morale collapsed.";

      if (_runState.VanConditionPercent <= 0f)
        return "The van broke down.";

      if (_runState.TripDayCurrent > gameConfig.maxTripDays)
        return "You ran out of time.";

      return "The trip ended.";
    }

    public bool CheckLoseConditions()
    {
      if (_runState == null || gameConfig == null)
        return false;

      if (_runState.FuelPercent <= 0f)
        return true;

      if (_runState.MoralePercent <= 0f)
        return true;

      if (_runState.VanConditionPercent <= 0f)
        return true;

      if (_runState.TripDayCurrent > gameConfig.maxTripDays)
        return true;

      return false;
    }

    public bool CheckWinCondition()
    {
      if (_runState == null || _runState.DestinationCityAsset == null || gameConfig == null)
        return false;

      return _runState.CurrentCity == _runState.DestinationCityAsset
        && _runState.TripDayCurrent <= gameConfig.maxTripDays;
    }
  }
}
