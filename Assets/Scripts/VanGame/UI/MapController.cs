using System.Collections.Generic;
using UnityEngine;
using VanGame.Audio;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public enum MapViewMode
  {
    DestinationSelection,
    DrivingOverview
  }

  public class MapController : MonoBehaviour
  {
    [SerializeField] MapRegionView[] mapRegions = System.Array.Empty<MapRegionView>();
    [SerializeField] MapStatsTooltipView tooltip;
    [SerializeField] MapVanMarkerView vanMarker;
    [SerializeField] MapRoadsController roadsController;
    [SerializeField] GameObject closeMapButton;

    CanvasTransitionController _canvasTransition;
    RunState _runState;
    GameFlowController _flow;
    MapRegionView _hoveredRegion;
    MapViewMode _viewMode = MapViewMode.DestinationSelection;

    public MapViewMode ViewMode => _viewMode;

    public void Initialize(GameFlowController flow, RunState runState)
    {
      _flow = flow;

      if (_canvasTransition != null)
        _canvasTransition.MapBecameVisible -= OnMapBecameVisible;

      _canvasTransition = flow != null
        ? flow.GetComponent<CanvasTransitionController>()
        : null;

      if (_canvasTransition == null)
        _canvasTransition = FindFirstObjectByType<CanvasTransitionController>();

      if (_canvasTransition != null)
        _canvasTransition.MapBecameVisible += OnMapBecameVisible;

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

      if (_canvasTransition != null)
        _canvasTransition.MapBecameVisible -= OnMapBecameVisible;
    }

    void OnMapBecameVisible()
    {
      RefreshMapVisuals();
    }

    public void RefreshMapVisuals()
    {
      RefreshRegionStates();
      RefreshRoadVisuals();
    }

    public void RequestMapVisualRefresh()
    {
      if (_viewMode == MapViewMode.DrivingOverview || _runState?.Phase == GamePhase.MapOpen
        || _runState?.Phase == GamePhase.MapSelectingDestination)
        RefreshMapVisuals();
    }

    void OnRunStateChanged()
    {
      RefreshVanMarker();

      if (_viewMode == MapViewMode.DrivingOverview)
        RefreshRoadVisuals();
    }

    public void OnMapOpened(bool forceDestinationPick)
    {
      _viewMode = forceDestinationPick || _runState?.DestinationCity == null
        ? MapViewMode.DestinationSelection
        : MapViewMode.DrivingOverview;

      RefreshMapVisuals();

      if (closeMapButton != null)
        closeMapButton.SetActive(!forceDestinationPick);

      tooltip?.Hide(immediate: true);
    }

    public void RefreshRegionStates()
    {
      if (_runState?.CurrentCity == null)
        return;

      HashSet<CityDefinition> reachable = BuildReachableSet();
      bool allowSelection = _viewMode == MapViewMode.DestinationSelection;

      MapRegionHighlightMode highlightMode = _viewMode == MapViewMode.DrivingOverview
        ? MapRegionHighlightMode.DrivingOverview
        : MapRegionHighlightMode.DestinationPick;

      foreach (MapRegionView region in mapRegions)
      {
        if (region?.City == null)
          continue;

        CityDefinition city = region.City;
        bool isCurrent = city == _runState.CurrentCity;
        bool isVisited = _runState.IsCityVisited(city) && !isCurrent;
        bool isReachable = reachable.Contains(city);
        bool isFinalDestination = _runState.DestinationCityAsset != null && city == _runState.DestinationCityAsset;
        bool isLegDestination = _viewMode == MapViewMode.DrivingOverview
          && _runState.DestinationCity != null
          && city == _runState.DestinationCity;
        bool isVisibleOnOverview = isVisited || isCurrent || isLegDestination || isFinalDestination;

        if (_viewMode == MapViewMode.DrivingOverview && !isVisibleOnOverview)
        {
          region.SetInteractableState(
            reachable: false,
            visited: false,
            isFinalDestination: false,
            isLegDestination: false,
            isCurrent: false,
            allowSelection: false,
            highlightMode);
          continue;
        }

        region.SetInteractableState(
          isReachable,
          isVisited,
          isFinalDestination,
          isLegDestination,
          isCurrent,
          allowSelection,
          highlightMode);
      }

      RefreshVanMarker();
    }

    void RefreshRoadVisuals()
    {
      if (roadsController == null)
        return;

      roadsController.HideAllRoads();

      if (_viewMode == MapViewMode.DestinationSelection)
        return;

      roadsController.ShowTraveledRoads(_runState);

      if (_runState?.CurrentCity != null && _runState.DestinationCity != null)
        roadsController.BlinkActiveLegRoad(_runState.CurrentCity, _runState.DestinationCity);
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

      if (_viewMode == MapViewMode.DestinationSelection)
      {
        GameSfxController.TryPlayMapHover();
        roadsController?.ShowHoverRoad(_runState.CurrentCity, city);
      }
    }

    public void NotifyRegionUnhovered(MapRegionView region)
    {
      if (_hoveredRegion != region)
        return;

      _hoveredRegion = null;
      tooltip?.Hide(immediate: false);
      roadsController?.ClearHoverRoad();
    }

    public void NotifyRegionClicked(MapRegionView region)
    {
      if (_viewMode != MapViewMode.DestinationSelection)
        return;

      if (_flow == null || region?.City == null || _runState?.CurrentCity == null)
        return;

      CityDefinition destination = region.City;
      if (!BuildReachableSet().Contains(destination))
        return;

      GameSfxController.TryPlayMapClick();
      roadsController?.ClearHoverRoad();
      _flow.SelectDestination(region);
    }
  }
}
