using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class EndOfDayResolver : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;

    RunState _runState;
    StatResolver _statResolver;
    SouvenirRewardResolver _souvenirRewards;

    public void Initialize(
      RunState runState,
      StatResolver statResolver,
      GameConfig config,
      SouvenirRewardResolver souvenirRewards = null)
    {
      _runState = runState;
      _statResolver = statResolver;
      _souvenirRewards = souvenirRewards;
      if (config != null)
        gameConfig = config;
    }

    public void ConfigureSouvenirRewards(SouvenirRewardResolver souvenirRewards)
    {
      _souvenirRewards = souvenirRewards;
    }

    public void ApplyEndOfDrivingDay()
    {
      if (_runState == null || gameConfig == null || _statResolver == null)
        return;

      float fuelDrain = _souvenirRewards != null
        ? _souvenirRewards.GetDailyFuelDrainPercent()
        : gameConfig.dailyFuelDrainPercent;
      float vanDrain = _souvenirRewards != null
        ? _souvenirRewards.GetDailyVanDrainPercent()
        : gameConfig.dailyVanConditionDrainPercent;

      _statResolver.ApplyFuelDelta(-fuelDrain);
      _statResolver.ApplyVanDelta(-vanDrain);

      if (!_runState.FedToday)
      {
        float penalty = GetUnfedMoralePenalty();
        _statResolver.ApplyMoraleDelta(-penalty);
        _runState.EventLog.Add($"Family went hungry today (-{penalty:0} morale).");
      }

      _runState.FedToday = false;
      _statResolver.ApplyTripDayDelta(1);
      _runState.NotifyStatsChanged();
    }

    float GetUnfedMoralePenalty()
    {
      bool hasDietEr = false;
      foreach (AbilityDefinition ability in _runState.OwnedAbilities)
      {
        if (ability != null && ability.abilityId == "diet_er")
        {
          hasDietEr = true;
          break;
        }
      }

      return hasDietEr
        ? gameConfig.dietErUnfedMoralePenaltyPercent
        : gameConfig.unfedMoralePenaltyPercent;
    }
  }
}
