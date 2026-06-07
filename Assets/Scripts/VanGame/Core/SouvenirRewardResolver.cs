using System.Collections.Generic;
using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class SouvenirRewardResolver : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;

    RunState _runState;
    StatResolver _statResolver;
    DeckController _deckController;

    public void Initialize(RunState runState, StatResolver statResolver, DeckController deckController, GameConfig config)
    {
      _runState = runState;
      _statResolver = statResolver;
      _deckController = deckController;
      if (config != null)
        gameConfig = config;
    }

    public bool OwnsReward(SouvenirRewardType type)
    {
      if (_runState == null || type == SouvenirRewardType.None)
        return false;

      foreach (string souvenirId in _runState.OwnedSouvenirIds)
      {
        SouvenirRewardInfo info = SouvenirCatalog.GetInfo(_runState, souvenirId);
        if (info.Type == type)
          return true;
      }

      return false;
    }

    public int GetExtraHandSize()
    {
      return OwnsReward(SouvenirRewardType.ExtraHandCard) ? 1 : 0;
    }

    public float GetDailyFuelDrainPercent()
    {
      float fallback = gameConfig != null ? gameConfig.dailyFuelDrainPercent : 15f;
      return OwnsReward(SouvenirRewardType.ReducedFuelDrain) ? 12f : fallback;
    }

    public float GetDailyVanDrainPercent()
    {
      float fallback = gameConfig != null ? gameConfig.dailyVanConditionDrainPercent : 5f;
      return OwnsReward(SouvenirRewardType.ReducedVanDrain) ? 4f : fallback;
    }

    public void OnDrivingSectionAdvanced(int sections)
    {
      if (_statResolver == null || _runState == null || sections <= 0)
        return;

      if (!OwnsReward(SouvenirRewardType.MoneyPerSection))
        return;

      _statResolver.ApplyMoneyDelta(sections);
      _runState.EventLog.Add($"Souvenir bonus: +${sections} for driving sections.");
    }

    public void ApplyCityEndBonuses()
    {
      if (_runState == null || _statResolver == null)
        return;

      if (OwnsReward(SouvenirRewardType.HighMoraleCityBonus) && _runState.MoralePercent > 90f)
      {
        _statResolver.ApplyMoneyDelta(30);
        _runState.EventLog.Add('Souvenir bonus: +$30 for high morale in the city.');
      }

      if (OwnsReward(SouvenirRewardType.HighVanCityBonus) && _runState.VanConditionPercent > 90f)
      {
        _statResolver.ApplyFuelDelta(10f);
        _runState.EventLog.Add('Souvenir bonus: +10 fuel for great van condition in the city.');
      }
    }

    public bool TryRescueVanCondition()
    {
      if (_runState == null || _statResolver == null)
        return false;

      if (_runState.UsedVanConditionRescue || !OwnsReward(SouvenirRewardType.VanConditionRescue))
        return false;

      if (_runState.VanConditionPercent > 0f)
        return false;

      _runState.UsedVanConditionRescue = true;
      _statResolver.ApplyVanDelta(50f);
      _runState.EventLog.Add('Souvenir rescue: +50 van condition from the BBB coupon.');
      return true;
    }

    public bool TryBankruptcyShuffle()
    {
      if (_runState == null || _deckController == null)
        return false;

      if (_runState.UsedBankruptcyShuffle || !OwnsReward(SouvenirRewardType.BankruptcyShuffle))
        return false;

      if (!_deckController.HasLegalCardInHand() || _deckController.HasAffordableLegalCardInHand(_statResolver))
        return false;

      _runState.UsedBankruptcyShuffle = true;
      _deckController.ShuffleHandOnly();
      _runState.EventLog.Add('Souvenir bankruptcy shuffle: your hand was reshuffled.');
      return true;
    }

    public void BeginDrivingRound()
    {
      if (_runState == null)
        return;

      _runState.UsedDoubleCardThisRound = false;
      _runState.DoubleNextCardEffect = OwnsReward(SouvenirRewardType.DoubleNextCard);
    }

    public bool ShouldDoubleCardEffects()
    {
      return _runState != null
        && _runState.DoubleNextCardEffect
        && !_runState.UsedDoubleCardThisRound;
    }

    public void MarkDoubleCardUsed()
    {
      if (_runState == null)
        return;

      _runState.UsedDoubleCardThisRound = true;
      _runState.DoubleNextCardEffect = false;
    }
  }
}
