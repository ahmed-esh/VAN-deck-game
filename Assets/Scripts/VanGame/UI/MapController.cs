using System.Collections.Generic;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class MapController : MonoBehaviour
  {
    [SerializeField] MapRegionView[] mapRegions = System.Array.Empty<MapRegionView>();
    [SerializeField] MapStatsTooltipView tooltip;
    [SerializeField] GameObject closeMapButton;

    RunState _runState;
    GameFlowController _flow;
    MapRegionView _hoveredRegion;

    public void Initialize(GameFlowController flow, RunState runState)
    {
      _flow = flow;
      _runState = runState;

      foreach (MapRegionView region in mapRegions)
      {
        if (region != null)
          region.Initialize(this);
      }

      RefreshRegionStates();
    }

    public void OnMapOpened(bool forceDestinationPick)
    {
      RefreshRegionStates();

      if (closeMapButton != null)
        closeMapButton.SetActive(!forceDestinationPick);

      tooltip?.Hide(immediate: true);
    }

    public void RefreshRegionStates()
    {
      if (_runState?.CurrentCity == null)
        return;

      HashSet<CityDefinition> reachable = BuildReachableSet();

      foreach (MapRegionView region in mapRegions)
      {
        if (region?.City == null)
          continue;

        CityDefinition city = region.City;
        bool isCurrent = city == _runState.CurrentCity;
        bool isVisited = _runState.IsCityVisited(city) && !isCurrent;
        bool isReachable = reachable.Contains(city);
        bool isDestination = _runState.DestinationCityAsset != null && city == _runState.DestinationCityAsset;

        region.SetInteractableState(isReachable, isVisited, isDestination);
      }
    }

    HashSet<CityDefinition> BuildReachableSet()
    {
      HashSet<CityDefinition> set = new HashSet<CityDefinition>();
      CityDefinition current = _runState.CurrentCity;

      if (current == null)
        return set;

      foreach (CityDefinition neighbor in current.GetNeighbors())
      {
        if (neighbor == null)
          continue;

        if (_runState.IsCityVisited(neighbor))
          continue;

        set.Add(neighbor);
      }

      return set;
    }

    public void NotifyRegionHovered(MapRegionView region)
    {
      _hoveredRegion = region;
      if (tooltip == null || _runState?.CurrentCity == null || region?.City == null)
        return;

      CityDefinition city = region.City;
      int drivingDays = _runState.CurrentCity.GetDrivingDaysTo(city);
      bool reachable = BuildReachableSet().Contains(city);
      bool visited = _runState.IsCityVisited(city) && city != _runState.CurrentCity;
      bool isDestination = _runState.DestinationCityAsset != null && city == _runState.DestinationCityAsset;

      tooltip.Show(city, drivingDays, reachable, visited, isDestination);
    }

    public void NotifyRegionUnhovered(MapRegionView region)
    {
      if (_hoveredRegion != region)
        return;

      _hoveredRegion = null;
      tooltip?.Hide(immediate: false);
    }

    public void NotifyRegionClicked(MapRegionView region)
    {
      if (_flow == null || region?.City == null || _runState?.CurrentCity == null)
        return;

      CityDefinition destination = region.City;
      if (!BuildReachableSet().Contains(destination))
        return;

      _flow.SelectDestination(region);
    }
  }
}
