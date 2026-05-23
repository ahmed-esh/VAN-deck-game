using System;
using System.Collections.Generic;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class CityArrivalController : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;
    [SerializeField] StatResolver statResolver;
    [SerializeField] CityRandomEventResolver eventResolver;
    [SerializeField] EventLogView eventLogView;
    [SerializeField] AbilityPickController abilityPick;
    [SerializeField] GameObject drivingPanel;

    RunState _runState;
    Action _onArrivalComplete;
    CityDefinition _arrivedCity;

    public void Initialize(RunState runState, GameConfig config, StatResolver stats, CityRandomEventResolver events)
    {
      _runState = runState;
      if (config != null)
        gameConfig = config;
      statResolver = stats;
      eventResolver = events;
      eventResolver?.Configure(config);
    }

    public void ProcessArrival(CityDefinition city, Action onComplete)
    {
      if (city == null || _runState == null)
      {
        onComplete?.Invoke();
        return;
      }

      _arrivedCity = city;
      _onArrivalComplete = onComplete;

      if (drivingPanel != null)
        drivingPanel.SetActive(false);

      _runState.SetPhase(GamePhase.CityArrival);

      var logLines = new List<string>();
      logLines.Add($"The family arrived in {city.displayName}.");

      statResolver?.ApplyCityArrival(city);

      if (city.baseMoraleDelta != 0)
      {
        string sign = city.baseMoraleDelta > 0 ? "+" : string.Empty;
        logLines.Add($"City vibe ({city.funTheme}): {sign}{city.baseMoraleDelta} morale.");
      }

      if (city.stayDaysInCity > 0)
        logLines.Add($"Staying {city.stayDaysInCity} day(s) in the city.");

      _runState.EventLog.Add($"Arrived in {city.displayName}.");

      List<RandomEventDefinition> rolledEvents = eventResolver != null
        ? eventResolver.RollEvents(city)
        : new List<RandomEventDefinition>();

      foreach (RandomEventDefinition evt in rolledEvents)
      {
        statResolver?.ApplyRandomEvent(evt);
        string line = string.IsNullOrWhiteSpace(evt.logText) ? evt.title : evt.logText;
        logLines.Add(line);
        _runState.EventLog.Add(line);
      }

      _runState.NotifyStatsChanged();

      if (statResolver != null && statResolver.CheckLoseConditions())
      {
        _onArrivalComplete?.Invoke();
        _onArrivalComplete = null;
        return;
      }

      eventLogView?.Show(city, logLines, OnEventLogFinished);
    }

    void OnEventLogFinished()
    {
      if (_runState == null || _arrivedCity == null)
      {
        FinishArrival();
        return;
      }

      bool isFinalDestination = _runState.DestinationCityAsset != null
        && _arrivedCity == _runState.DestinationCityAsset;

      if (isFinalDestination)
      {
        FinishArrival();
        return;
      }

      _runState.SetPhase(GamePhase.AbilityPick);
      abilityPick?.ShowOffer(_runState, OnAbilityPicked);
    }

    void OnAbilityPicked(AbilityDefinition ability)
    {
      if (ability != null && _runState != null)
      {
        _runState.OwnedAbilities.Add(ability);
        _runState.HasReceivedFirstCityReward = true;
        _runState.EventLog.Add($"Learned: {ability.title}");
        _runState.NotifyStatsChanged();
      }

      abilityPick?.Hide(immediate: true);
      FinishArrival();
    }

    void FinishArrival()
    {
      eventLogView?.Hide(immediate: true);
      abilityPick?.Hide(immediate: true);

      if (drivingPanel != null)
        drivingPanel.SetActive(true);

      Action callback = _onArrivalComplete;
      _onArrivalComplete = null;
      _arrivedCity = null;
      callback?.Invoke();
    }
  }
}
