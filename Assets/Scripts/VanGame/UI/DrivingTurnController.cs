using UnityEngine;
using VanGame;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class DrivingTurnController : MonoBehaviour
  {
    [SerializeField] GameFlowController gameFlow;
    [SerializeField] GameConfig gameConfig;
    [SerializeField] StatResolver statResolver;
    [SerializeField] DeckController deckController;
    [SerializeField] EndOfDayResolver endOfDayResolver;
    [SerializeField] CardHandController cardHand;
    [SerializeField] DrivingDayTimerView timerView;

    bool _isLegActive;
    bool _isEndingDay;

    public bool CanPlayCards => _isLegActive && gameFlow != null
      && gameFlow.RunState.Phase == GamePhase.Driving;

    void Awake()
    {
      if (gameFlow == null)
        gameFlow = FindObjectOfType<GameFlowController>();
    }

    void Start()
    {
      RunState run = gameFlow?.RunState;
      if (run != null)
        run.PhaseChanged += OnPhaseChanged;

      if (deckController != null)
        deckController.HandChanged += OnHandChanged;

      if (gameFlow != null && statResolver != null && endOfDayResolver != null)
      {
        endOfDayResolver.Initialize(gameFlow.RunState, statResolver, gameConfig);
        cardHand?.Initialize(deckController, statResolver, this, gameConfig);
      }

      if (timerView != null && gameFlow != null)
        timerView.Bind(gameFlow.RunState, gameConfig);

      RefreshTimerUi();
    }

    void OnDestroy()
    {
      RunState run = gameFlow?.RunState;
      if (run != null)
        run.PhaseChanged -= OnPhaseChanged;

      if (deckController != null)
        deckController.HandChanged -= OnHandChanged;
    }

    void Update()
    {
      if (!CanAdvanceTimer())
        return;

      float idleSections = gameConfig.IdleSectionsPerSecond * Time.deltaTime;
      AdvanceDrivingDaySections(idleSections);
    }

    bool CanAdvanceTimer()
    {
      return _isLegActive && gameConfig != null && gameFlow != null
        && gameFlow.RunState.Phase == GamePhase.Driving;
    }

    void OnPhaseChanged()
    {
      bool driving = gameFlow.RunState.Phase == GamePhase.Driving;
      cardHand?.SetHandInteractable(driving);
      RefreshTimerUi();
    }

    void OnHandChanged()
    {
      cardHand?.RefreshAffordability();
    }

    public void OnLegStarted()
    {
      _isLegActive = true;

      RunState run = gameFlow.RunState;
      run.DrivingDayTimer = 0f;
      run.FedToday = false;

      deckController?.SetCurrentRegion(run.CurrentCity);
      cardHand?.RebuildHand();
      cardHand?.SetHandInteractable(true);
      RefreshTimerUi();
    }

    public void OnLegEnded()
    {
      _isLegActive = false;
      cardHand?.ClearHandVisuals();
      cardHand?.SetHandInteractable(false);
    }

    public void TryPlayCard(CardView view)
    {
      if (!CanPlayCards || view?.Definition == null || statResolver == null || deckController == null)
        return;

      ActionCardDefinition card = view.Definition;

      if (!deckController.IsCardLegalInCurrentRegion(card))
        return;

      if (!statResolver.CanAfford(deckController.GetCardMoneyCost(card)))
        return;

      PlayCard(view, card);
    }

    void PlayCard(CardView view, ActionCardDefinition card)
    {
      cardHand?.AnimateCardPlay(view, null);

      statResolver.ApplyCardEffects(card);

      int sections = statResolver.GetCardDayTimeSections(card);
      AdvanceDrivingDaySections(sections);

      if (!deckController.TryPlayCard(card, out _))
        return;

      cardHand?.RefreshAffordability();

      if (CheckLoseAfterAction())
        return;

      if (ShouldEndDrivingDay())
        EndDrivingDay();

      RefreshTimerUi();
    }

    void AdvanceDrivingDaySections(float sectionDelta)
    {
      if (gameConfig == null || gameFlow == null || sectionDelta <= 0f)
        return;

      RunState run = gameFlow.RunState;
      float maxSections = GetDrivingDaySectionCount();
      run.DrivingDayTimer = Mathf.Min(run.DrivingDayTimer + sectionDelta, maxSections);
      RefreshTimerUi();

      if (ShouldEndDrivingDay())
        EndDrivingDay();
    }

    int GetDrivingDaySectionCount()
    {
      if (gameConfig == null)
        return 8;

      if (statResolver == null)
        return Mathf.Max(1, gameConfig.drivingDaySectionCount);

      return Mathf.Max(
        1,
        Mathf.RoundToInt(statResolver.ApplyModifier(ModifierTarget.DrivingDayBudget, gameConfig.drivingDaySectionCount)));
    }

    bool ShouldEndDrivingDay()
    {
      if (gameConfig == null || gameFlow == null)
        return false;

      return gameFlow.RunState.DrivingDayTimer >= GetDrivingDaySectionCount();
    }

    void EndDrivingDay()
    {
      if (!_isLegActive || gameFlow == null || _isEndingDay)
        return;

      _isEndingDay = true;

      RunState run = gameFlow.RunState;
      run.DrivingDayTimer = 0f;
      endOfDayResolver?.ApplyEndOfDrivingDay();

      run.DrivingDaysRemaining = Mathf.Max(0, run.DrivingDaysRemaining - 1);
      run.EventLog.Add($"Finished driving day. {run.DrivingDaysRemaining} day(s) left on this leg.");

      if (statResolver != null && statResolver.CheckLoseConditions())
      {
        _isEndingDay = false;
        gameFlow.EnterLoseFromDriving();
        OnLegEnded();
        return;
      }

      if (run.DrivingDaysRemaining <= 0)
      {
        _isEndingDay = false;
        OnLegEnded();
        gameFlow.CompleteDrivingLeg();
        return;
      }

      RefreshTimerUi();
      _isEndingDay = false;
    }

    bool CheckLoseAfterAction()
    {
      if (statResolver == null || !statResolver.CheckLoseConditions())
        return false;

      gameFlow.EnterLoseFromDriving();
      OnLegEnded();
      return true;
    }

    void RefreshTimerUi()
    {
      if (timerView == null || gameFlow == null)
        return;

      timerView.Refresh();
      timerView.RefreshTimer(gameFlow.RunState.DrivingDayTimer, GetDrivingDaySectionCount());
    }
  }
}
