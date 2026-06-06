using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  /// <summary>
  /// Shows hollow stat/timer previews while hovering a card, then animates values when the card is played.
  /// </summary>
  public class CardPlayPreviewController : MonoBehaviour
  {
    [SerializeField] GameFlowController gameFlow;
    [SerializeField] StatResolver statResolver;
    [SerializeField] CardHandHoverFan hoverFan;
    [SerializeField] StatsHudView statsHud;
    [SerializeField] DrivingDayTimerView timerView;

    CardView _lastHoveredCard;
    bool _hoverPreviewActive;

    void Awake()
    {
      if (gameFlow == null)
        gameFlow = FindFirstObjectByType<GameFlowController>();

      if (statResolver == null)
        statResolver = FindFirstObjectByType<StatResolver>();

      if (hoverFan == null)
        hoverFan = FindFirstObjectByType<CardHandHoverFan>();

      if (statsHud == null)
        statsHud = FindFirstObjectByType<StatsHudView>();

      if (timerView == null)
        timerView = FindFirstObjectByType<DrivingDayTimerView>();
    }

    void Update()
    {
      if (hoverFan == null || statResolver == null || statsHud == null)
        return;

      if (!hoverFan.CanFocusCards)
      {
        if (_hoverPreviewActive)
          ClearHoverPreview();
        return;
      }

      CardView focused = hoverFan.GetFocusedCardView();
      if (focused == _lastHoveredCard)
        return;

      _lastHoveredCard = focused;

      if (focused?.Definition == null)
      {
        ClearHoverPreview();
        return;
      }

      ShowHoverPreview(focused.Definition);
    }

    public void ShowHoverPreview(ActionCardDefinition card)
    {
      CardEffectPreview preview = statResolver.BuildCardEffectPreview(card);
      int sectionCount = GetSectionCount();
      statsHud.ShowCardPreview(preview);

      if (preview.AffectsTimer)
        timerView?.ShowSectionPreview(preview.TimerSectionsAfter, sectionCount);
      else
        timerView?.ClearSectionPreview();

      _hoverPreviewActive = true;
    }

    public void ClearHoverPreview()
    {
      _lastHoveredCard = null;
      _hoverPreviewActive = false;
      statsHud?.ClearCardPreview();
      timerView?.ClearSectionPreview();
    }

    public CardEffectPreview BuildPreview(ActionCardDefinition card)
    {
      if (statResolver == null || card == null)
        return default;

      return statResolver.BuildCardEffectPreview(card);
    }

    public void PlayApplyAnimation(CardEffectPreview preview)
    {
      ClearHoverPreview();
      statsHud?.AnimateApplyPreview(preview);
    }

    int GetSectionCount()
    {
      if (gameFlow?.Config != null)
        return Mathf.Max(1, gameFlow.Config.drivingDaySectionCount);

      return 8;
    }
  }
}
