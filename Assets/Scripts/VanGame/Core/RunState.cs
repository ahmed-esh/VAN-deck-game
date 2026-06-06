using System;
using System.Collections.Generic;
using VanGame.Data;

namespace VanGame.Core
{
  public class RunState
  {
    public int Money { get; set; }
    public float FuelPercent { get; set; }
    public float MoralePercent { get; set; }
    public float VanConditionPercent { get; set; }
    public int TripDayCurrent { get; set; }
    public int DrivingDaysRemaining { get; set; }
    /// <summary>Sections filled on the current driving-day bar (0 … drivingDaySectionCount).</summary>
    public float DrivingDayTimer { get; set; }

    public CityDefinition CurrentCity { get; set; }
    public CityDefinition DestinationCity { get; set; }
    public CityDefinition StartCity { get; set; }
    public CityDefinition DestinationCityAsset { get; set; }

    public GamePhase Phase { get; set; } = GamePhase.CardIdle;

    public HashSet<CityDefinition> VisitedCities { get; } = new HashSet<CityDefinition>();
    public List<AbilityDefinition> OwnedAbilities { get; } = new List<AbilityDefinition>();
    public List<string> EventLog { get; } = new List<string>();

    public bool FedToday { get; set; }
    public bool HasPickedFirstDestination { get; set; }
    public bool HasReceivedFirstCityReward { get; set; }

    public event Action StatsChanged;
    public event Action PhaseChanged;
    public event Action<CityDefinition> DestinationSelected;

    public void NotifyStatsChanged() => StatsChanged?.Invoke();

    public void NotifyDestinationSelected(CityDefinition city) => DestinationSelected?.Invoke(city);

    public void SetPhase(GamePhase phase)
    {
      if (Phase == phase)
        return;

      Phase = phase;
      PhaseChanged?.Invoke();
    }

    public void MarkCityVisited(CityDefinition city)
    {
      if (city != null)
        VisitedCities.Add(city);
    }

    public bool IsCityVisited(CityDefinition city)
    {
      return city != null && VisitedCities.Contains(city);
    }

    public void ResetFromConfig(GameConfig config, CityDefinition startCity, CityDefinition destinationCity)
    {
      Money = config.startingMoney;
      FuelPercent = config.startingFuelPercent;
      MoralePercent = config.startingMoralePercent;
      VanConditionPercent = config.startingVanConditionPercent;
      TripDayCurrent = 0;
      DrivingDaysRemaining = 0;
      DrivingDayTimer = 0f;
      CurrentCity = startCity;
      DestinationCity = null;
      StartCity = startCity;
      DestinationCityAsset = destinationCity;
      FedToday = false;
      HasPickedFirstDestination = false;
      HasReceivedFirstCityReward = false;
      VisitedCities.Clear();
      OwnedAbilities.Clear();
      EventLog.Clear();

      if (startCity != null)
        VisitedCities.Add(startCity);

      SetPhase(GamePhase.CardIdle);
      NotifyStatsChanged();
    }
  }
}
