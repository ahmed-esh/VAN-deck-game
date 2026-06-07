using UnityEngine;
using UnityEngine.UI;
using VanGame.Audio;
using VanGame.Core;
using VanGame.Data;
using VanGame.UI;

namespace VanGame
{
  public class GameFlowController : MonoBehaviour
  {
    [Header("Data")]
    [SerializeField] GameConfig gameConfig;
    [SerializeField] CityDefinition startCity;
    [SerializeField] CityDefinition destinationCity;
    [SerializeField] DeckDefinition deckDefinition;
    [SerializeField] AbilityCatalog abilityCatalog;

    [Header("Controllers")]
    [SerializeField] StatResolver statResolver;
    [SerializeField] DeckController deckController;
    [SerializeField] CanvasTransitionController canvasTransition;
    [SerializeField] MapController mapController;
    [SerializeField] StatsHudView statsHud;
    [SerializeField] DrivingTurnController drivingTurn;
    [SerializeField] CityArrivalController cityArrival;
    [SerializeField] CityRandomEventResolver randomEventResolver;

    [Header("UI buttons")]
    [SerializeField] Button openMapButton;
    [SerializeField] Button closeMapButton;

    [Header("Phase panels")]
    [SerializeField] GameObject drivingPanel;
    [SerializeField] GameObject cityArrivalPanel;
    [SerializeField] WinScrollPanelView winScrollPanel;
    [SerializeField] LoseScrollPanelView loseScrollPanel;

    [Header("Audio")]
    [SerializeField] RegionalMusicController regionalMusic;

    readonly RunState _runState = new RunState();

    public RunState RunState => _runState;
    public GameConfig Config => gameConfig;

    void Awake()
    {
      WireButtons();
      InitializeSystems();
      StartNewRun();
    }

    void Update()
    {
      if (!Input.GetKeyDown(KeyCode.M))
        return;

      if (IsMapVisible())
        OnCloseMapClicked();
      else
        OnOpenMapClicked();
    }

    void WireButtons()
    {
      if (openMapButton != null)
      {
        openMapButton.onClick.RemoveAllListeners();
        openMapButton.onClick.AddListener(OnOpenMapClicked);
      }

      if (closeMapButton != null)
      {
        closeMapButton.onClick.RemoveAllListeners();
        closeMapButton.onClick.AddListener(OnCloseMapClicked);
      }
    }

    void InitializeSystems()
    {
      statResolver?.Initialize(_runState, gameConfig);
      deckController?.Initialize(deckDefinition);
      canvasTransition?.Configure(gameConfig);
      mapController?.Initialize(this, _runState);
      statsHud?.Bind(_runState, gameConfig);
      randomEventResolver?.Configure(gameConfig);
      cityArrival?.Initialize(_runState, gameConfig, statResolver, randomEventResolver);
      winScrollPanel?.Initialize(this, statResolver, gameConfig);
      loseScrollPanel?.Initialize(this, statResolver, gameConfig);
    }

    public void StartNewRun()
    {
      _runState.ResetFromConfig(gameConfig, startCity, destinationCity);
      _runState.NotifyDestinationSelected(null);
      deckController?.Initialize(deckDefinition);
      drivingTurn?.OnLegEnded();
      statsHud?.Refresh();
      mapController?.RefreshRegionStates();
      deckController?.SetCurrentRegion(_runState.CurrentCity);

      SetPanel(drivingPanel, true);
      SetPanel(cityArrivalPanel, false);
      winScrollPanel?.Hide(immediate: true);
      loseScrollPanel?.Hide(immediate: true);
      SetOpenMapButtonVisible(true);

      regionalMusic?.OnRunStarted(_runState.CurrentCity);

      _runState.SetPhase(GamePhase.CardIdle);

      PromptMapForDestination(force: !_runState.HasPickedFirstDestination);
    }

    void PromptMapForDestination(bool force)
    {
      _runState.SetPhase(force ? GamePhase.MapSelectingDestination : GamePhase.MapOpen);
      canvasTransition?.OpenMap(force);
      mapController?.OnMapOpened(force);
    }

    public void OnOpenMapClicked()
    {
      if (canvasTransition != null && canvasTransition.IsTransitioning)
        return;

      if (!CanOpenMap())
        return;

      _runState.SetPhase(GamePhase.MapOpen);
      canvasTransition?.OpenMap(forceDestinationPick: false);
      mapController?.OnMapOpened(forceDestinationPick: false);
    }

    public void OnCloseMapClicked()
    {
      if (canvasTransition != null && canvasTransition.IsTransitioning)
        return;

      if (_runState.Phase == GamePhase.MapSelectingDestination)
        return;

      GamePhase resume = _runState.DestinationCity != null ? GamePhase.Driving : GamePhase.CardIdle;
      _runState.SetPhase(resume);
      canvasTransition?.CloseMap();
    }

    bool IsMapVisible()
    {
      return _runState.Phase == GamePhase.MapOpen
        || _runState.Phase == GamePhase.MapSelectingDestination;
    }

    bool CanOpenMap()
    {
      if (_runState.Phase == GamePhase.Win || _runState.Phase == GamePhase.Lose)
        return false;

      if (_runState.Phase == GamePhase.CityArrival || _runState.Phase == GamePhase.AbilityPick)
        return false;

      return true;
    }

    public void SelectDestination(MapRegionView region)
    {
      if (region?.City == null || _runState.CurrentCity == null)
        return;

      CityDefinition destination = region.City;
      int drivingDays = _runState.CurrentCity.GetDrivingDaysTo(destination);

      if (drivingDays <= 0)
        return;

      _runState.DestinationCity = destination;
      _runState.DrivingDaysRemaining = drivingDays;
      _runState.DrivingDayTimer = 0f;
      _runState.HasPickedFirstDestination = true;
      _runState.NotifyDestinationSelected(destination);

      regionalMusic?.PlayRegionMusic(destination);

      canvasTransition?.ConfirmCitySelection(region, BeginDrivingLeg);
    }

    void BeginDrivingLeg()
    {
      _runState.SetPhase(GamePhase.Driving);
      SetPanel(drivingPanel, true);
      SetPanel(cityArrivalPanel, false);
      canvasTransition?.SetCardCanvasInteractable(true);

      _runState.EventLog.Add($"Driving to {_runState.DestinationCity.displayName} ({_runState.DrivingDaysRemaining} days).");
      _runState.NotifyStatsChanged();
      mapController?.RefreshVanMarker();
      drivingTurn?.OnLegStarted();
    }

    public void CompleteDrivingLeg()
    {
      if (_runState.DestinationCity == null)
        return;

      drivingTurn?.OnLegEnded();

      CityDefinition arrivedCity = _runState.DestinationCity;
      _runState.CurrentCity = arrivedCity;
      _runState.MarkCityVisited(arrivedCity);
      _runState.DestinationCity = null;
      _runState.NotifyDestinationSelected(null);
      mapController?.RefreshRegionStates();
      deckController?.SetCurrentRegion(arrivedCity);

      SetPanel(cityArrivalPanel, true);
      SetOpenMapButtonVisible(false);

      cityArrival?.ProcessArrival(arrivedCity, OnCityArrivalFinished);
    }

    void OnCityArrivalFinished()
    {
      if (statResolver != null && statResolver.CheckLoseConditions())
      {
        EnterLose();
        return;
      }

      if (statResolver != null && statResolver.CheckWinCondition())
      {
        EnterWin();
        return;
      }

      SetPanel(cityArrivalPanel, false);
      SetOpenMapButtonVisible(true);
      PromptMapForDestination(force: true);
    }

    void EnterWin()
    {
      _runState.SetPhase(GamePhase.Win);
      regionalMusic?.PlayWin();
      SetPanel(drivingPanel, false);
      SetPanel(cityArrivalPanel, false);
      SetOpenMapButtonVisible(false);

      winScrollPanel?.ShowWin(_runState);
    }

    public void EnterLoseFromDriving()
    {
      drivingTurn?.OnLegEnded();
      EnterLose();
    }

    void EnterLose()
    {
      _runState.SetPhase(GamePhase.Lose);
      regionalMusic?.PlayLose();
      SetPanel(drivingPanel, false);
      SetPanel(cityArrivalPanel, false);
      SetOpenMapButtonVisible(false);
      loseScrollPanel?.ShowLose(_runState);
    }

    void SetOpenMapButtonVisible(bool visible)
    {
      if (openMapButton != null)
        openMapButton.gameObject.SetActive(visible);
    }

    static void SetPanel(GameObject panel, bool active)
    {
      if (panel != null)
        panel.SetActive(active);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Complete Driving Leg")]
    void DebugCompleteDrivingLeg()
    {
      drivingTurn?.OnLegEnded();
      CompleteDrivingLeg();
    }
#endif
  }
}
