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
    [SerializeField] MapVanMarkerView vanMarker;
    [SerializeField] GameObject closeMapButton;

    RunState _runState;
    GameFlowController _flow;
    MapRegionView _hoveredRegion;

    public void Initialize(GameFlowController flow, RunState runState)
    {
      _flow = flow;

      if (_runState != null)
        _runState.PhaseChanged -= OnRunStateChanged;

      _runState = runState;

      if (_runState != null)
        _runState.PhaseChanged += OnRunStateChanged;

      foreach (MapRegionView region in mapRegions)
      {
        if (region != null)
          region.Initialize(this);
      }

      RefreshRegionStates();
    }

    void OnDestroy()
    {
      if (_runState != null)
        _runState.PhaseChanged -= OnRunStateChanged;
    }

    void OnRunStateChanged()
    {
      RefreshVanMarker();
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

        region.SetInteractableState(isReachable, isVisited, isDestination, isCurrent);
      }

      RefreshVanMarker();
    }

    public void RefreshVanMarker()
    {
      if (vanMarker == null || _runState?.CurrentCity == null)
      {
        vanMarker?.SetVisible(false);
        return;
      }

      MapRegionView currentRegion = FindRegionForCity(_runState.CurrentCity);
      if (currentRegion == null)
      {
        vanMarker.SetVisible(false);
        return;
      }

      Vector2 position = currentRegion.MapAnchorPosition;

      if (_runState.DestinationCity != null)
      {
        MapRegionView destinationRegion = FindRegionForCity(_runState.DestinationCity);
        if (destinationRegion != null)
          position = (position + destinationRegion.MapAnchorPosition) * 0.5f;
      }

      vanMarker.SetAnchoredPosition(position);
      vanMarker.SetVisible(true);
    }

    MapRegionView FindRegionForCity(CityDefinition city)
    {
      if (city == null)
        return null;

      foreach (MapRegionView region in mapRegions)
      {
        if (region != null && region.City == city)
          return region;
      }

      return null;
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

      tooltip.Show(city, drivingDays);
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
