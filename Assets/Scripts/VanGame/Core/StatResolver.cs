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

    public bool IsStuckWithUnaffordableHand(DeckController deck)
    {
      if (deck == null)
        return false;

      return deck.HasLegalCardInHand() && !deck.HasAffordableLegalCardInHand(this);
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

    public CardEffectPreview BuildCardEffectPreview(ActionCardDefinition card)
    {
      var preview = new CardEffectPreview();

      if (_runState == null || card == null)
        return preview;

      preview.MoneyBefore = _runState.Money;
      preview.FuelBefore = _runState.FuelPercent;
      preview.MoraleBefore = _runState.MoralePercent;
      preview.VanBefore = _runState.VanConditionPercent;
      preview.TimerSectionsBefore = _runState.DrivingDayTimer;

      int money = _runState.Money;
      float fuel = _runState.FuelPercent;
      float morale = _runState.MoralePercent;
      float van = _runState.VanConditionPercent;

      int cost = GetPreviewMoneyCost(card);
      if (cost != 0)
      {
        money = Mathf.Max(0, money - cost);
        preview.AffectsMoney = true;
      }

      if (card.HasAuthoredEffects)
        SimulateCardEffects(card.effects, ref money, ref fuel, ref morale, ref van, ref preview);
      else
        SimulateLegacyCardEffects(card, ref fuel, ref morale, ref van, ref preview);

      preview.MoneyAfter = money;
      preview.FuelAfter = Mathf.Clamp(fuel, 0f, 100f);
      preview.MoraleAfter = Mathf.Clamp(morale, 0f, 100f);
      preview.VanAfter = Mathf.Clamp(van, 0f, 100f);
      preview.TimerSectionsAfter = Mathf.Min(
        preview.TimerSectionsBefore + GetCardDayTimeSections(card),
        GetMaxTimerSections());
      preview.AffectsTimer = GetCardDayTimeSections(card) > 0;

      if (!preview.AffectsMoney && preview.MoneyAfter != preview.MoneyBefore)
        preview.AffectsMoney = true;

      return preview;
    }

    int GetPreviewMoneyCost(ActionCardDefinition card)
    {
      if (card == null)
        return 0;

      int cost = card.moneyCostMin;
      return Mathf.RoundToInt(ApplyModifier(ModifierTarget.MoneyCost, cost));
    }

    int GetMaxTimerSections()
    {
      if (gameConfig == null)
        return 8;

      return Mathf.Max(1, gameConfig.drivingDaySectionCount);
    }

    void SimulateLegacyCardEffects(
      ActionCardDefinition card,
      ref float fuel,
      ref float morale,
      ref float van,
      ref CardEffectPreview preview)
    {
      if (Mathf.Abs(card.moraleDeltaPercent) > 0.001f)
      {
        morale = ApplyPreviewMorale(morale, CardStatOperation.Add, card.moraleDeltaPercent);
        preview.AffectsMorale = true;
      }

      if (Mathf.Abs(card.fuelDeltaPercent) > 0.001f)
      {
        fuel = ApplyPreviewFuel(fuel, CardStatOperation.Add, card.fuelDeltaPercent);
        preview.AffectsFuel = true;
      }

      if (Mathf.Abs(card.vanConditionDelta) > 0.001f)
      {
        van = ApplyPreviewVan(van, CardStatOperation.Add, card.vanConditionDelta);
        preview.AffectsVan = true;
      }
    }

    void SimulateCardEffects(
      CardEffect[] effects,
      ref int money,
      ref float fuel,
      ref float morale,
      ref float van,
      ref CardEffectPreview preview)
    {
      if (effects == null)
        return;

      foreach (CardEffect effect in effects)
      {
        switch (effect.target)
        {
          case CardEffectTarget.Money:
            money = Mathf.Max(0, money + Mathf.RoundToInt(ResolveCardEffectValue(money, effect)));
            preview.AffectsMoney = true;
            break;
          case CardEffectTarget.Morale:
            morale = ApplyPreviewMorale(morale, effect.operation, effect.value);
            preview.AffectsMorale = true;
            break;
          case CardEffectTarget.Fuel:
            fuel = ApplyPreviewFuel(fuel, effect.operation, effect.value);
            preview.AffectsFuel = true;
            break;
          case CardEffectTarget.VanCondition:
            van = ApplyPreviewVan(van, effect.operation, effect.value);
            preview.AffectsVan = true;
            break;
        }
      }
    }

    static float ApplyPreviewMorale(float current, CardStatOperation operation, float value)
    {
      if (operation == CardStatOperation.Add || operation == CardStatOperation.Subtract)
      {
        float delta = operation == CardStatOperation.Add ? value : -value;
        return Mathf.Clamp(current + delta, 0f, 100f);
      }

      return Mathf.Clamp(CardEffectMath.Apply(current, operation, value), 0f, 100f);
    }

    static float ApplyPreviewFuel(float current, CardStatOperation operation, float value)
    {
      if (operation == CardStatOperation.Add || operation == CardStatOperation.Subtract)
      {
        float delta = operation == CardStatOperation.Add ? value : -value;
        return Mathf.Clamp(current + delta, 0f, 100f);
      }

      return Mathf.Clamp(CardEffectMath.Apply(current, operation, value), 0f, 100f);
    }

    static float ApplyPreviewVan(float current, CardStatOperation operation, float value)
    {
      if (operation == CardStatOperation.Add || operation == CardStatOperation.Subtract)
      {
        float delta = operation == CardStatOperation.Add ? value : -value;
        return Mathf.Clamp(current + delta, 0f, 100f);
      }

      return Mathf.Clamp(CardEffectMath.Apply(current, operation, value), 0f, 100f);
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

    public string GetLoseReason(DeckController deck = null)
    {
      if (_runState == null || gameConfig == null)
        return "You ran out of resources.";

      if (_runState.FuelPercent <= 0f)
        return "You ran out of fuel.";

      if (deck != null && IsStuckWithUnaffordableHand(deck))
        return "You couldn't afford any cards in your hand.";

      if (_runState.Money <= 0)
        return "You ran out of money.";

      if (_runState.MoralePercent <= 0f)
        return "Family morale collapsed.";

      if (_runState.VanConditionPercent <= 0f)
        return "The van broke down.";

      if (_runState.TripDayCurrent > gameConfig.maxTripDays)
        return "You ran out of time.";

      return "The trip ended.";
    }

    public bool CheckLoseConditions(DeckController deck = null)
    {
      if (_runState == null || gameConfig == null)
        return false;

      if (_runState.Money <= 0)
        return true;

      if (_runState.FuelPercent <= 0f)
        return true;

      if (_runState.MoralePercent <= 0f)
        return true;

      if (_runState.VanConditionPercent <= 0f)
        return true;

      if (_runState.TripDayCurrent > gameConfig.maxTripDays)
        return true;

      if (deck != null && IsStuckWithUnaffordableHand(deck))
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
