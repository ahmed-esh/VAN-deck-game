using System.Collections;
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
    bool _cardActionLocked;
    bool _isEndingDay;

    public bool CanPlayCards => _isLegActive && !_cardActionLocked && gameFlow != null
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

      float drift = Time.deltaTime * gameConfig.idleTimeMultiplier;
      AdvanceDrivingTimer(drift);
    }

    bool CanAdvanceTimer()
    {
      return _isLegActive && !_cardActionLocked && gameConfig != null && gameFlow != null
        && gameFlow.RunState.Phase == GamePhase.Driving;
    }

    void OnPhaseChanged()
    {
      bool driving = gameFlow.RunState.Phase == GamePhase.Driving;
      cardHand?.SetHandInteractable(driving && !_cardActionLocked);
      RefreshTimerUi();
    }

    void OnHandChanged()
    {
      cardHand?.RefreshAffordability();
    }

    public void OnLegStarted()
    {
      _isLegActive = true;
      _cardActionLocked = false;

      RunState run = gameFlow.RunState;
      run.DrivingDayTimer = 0f;
      run.FedToday = false;

      cardHand?.RebuildHand();
      cardHand?.SetHandInteractable(true);
      RefreshTimerUi();
    }

    public void OnLegEnded()
    {
      _isLegActive = false;
      _cardActionLocked = false;
      cardHand?.SetHandInteractable(false);
    }

    public void TryPlayCard(CardView view)
    {
      if (!CanPlayCards || view?.Definition == null || statResolver == null || deckController == null)
        return;

      ActionCardDefinition card = view.Definition;

      if (!statResolver.CanAfford(deckController.GetCardMoneyCost(card)))
        return;

      StartCoroutine(PlayCardRoutine(view, card));
    }

    IEnumerator PlayCardRoutine(CardView view, ActionCardDefinition card)
    {
      _cardActionLocked = true;
      cardHand?.SetHandInteractable(false);

      statResolver.ApplyCardEffects(card);

      float actionDuration = statResolver.GetResolvedActionDuration(card.realTimeSeconds);
      AdvanceDrivingTimer(actionDuration);

      bool played = deckController.TryPlayCard(card, out _);

      if (played)
      {
        bool removed = false;
        cardHand?.AnimateCardOut(view, () => removed = true);

        while (!removed)
          yield return null;

        cardHand?.RebuildHand();
      }

      if (actionDuration > 0f)
        yield return new WaitForSeconds(actionDuration);
      else
        yield return null;

      _cardActionLocked = false;

      if (CheckLoseAfterAction())
        yield break;

      if (ShouldEndDrivingDay())
        EndDrivingDay();

      if (_isLegActive)
        cardHand?.SetHandInteractable(true);

      RefreshTimerUi();
    }

    void AdvanceDrivingTimer(float delta)
    {
      if (gameConfig == null || gameFlow == null)
        return;

      RunState run = gameFlow.RunState;
      run.DrivingDayTimer += delta;
      timerView?.RefreshTimer(run.DrivingDayTimer, GetDrivingDayBudget());

      if (ShouldEndDrivingDay())
        EndDrivingDay();
    }

    float GetDrivingDayBudget()
    {
      if (gameConfig == null)
        return 60f;

      if (statResolver == null)
        return gameConfig.drivingDayRealTimeSeconds;

      return statResolver.ApplyModifier(ModifierTarget.DrivingDayBudget, gameConfig.drivingDayRealTimeSeconds);
    }

    bool ShouldEndDrivingDay()
    {
      if (gameConfig == null || gameFlow == null)
        return false;

      return gameFlow.RunState.DrivingDayTimer >= GetDrivingDayBudget();
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
      timerView?.Refresh();

      if (gameConfig != null && gameFlow != null)
        timerView?.RefreshTimer(gameFlow.RunState.DrivingDayTimer, GetDrivingDayBudget());
    }
  }
}
