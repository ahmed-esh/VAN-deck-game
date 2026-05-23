using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class EndOfDayResolver : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;

    RunState _runState;
    StatResolver _statResolver;

    public void Initialize(RunState runState, StatResolver statResolver, GameConfig config)
    {
      _runState = runState;
      _statResolver = statResolver;
      if (config != null)
        gameConfig = config;
    }

    public void ApplyEndOfDrivingDay()
    {
      if (_runState == null || gameConfig == null || _statResolver == null)
        return;

      _statResolver.ApplyFuelDelta(-gameConfig.dailyFuelDrainPercent);

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
