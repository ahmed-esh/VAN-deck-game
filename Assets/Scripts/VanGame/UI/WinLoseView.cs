using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class WinLoseView : MonoBehaviour
  {
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text summaryText;
    [SerializeField] Button restartButton;
    [SerializeField] GameConfig gameConfig;

    [SerializeField] string winTitle = "Grandpa's Dream Fulfilled!";
    [SerializeField] string loseTitle = "Trip Over";
    [SerializeField] string winSummaryFormat = "You reached {0} on day {1} with ${2} left.";
    [SerializeField] string loseSummaryFormat = "{0}\nDay {1}/{2} — Money ${3}, Morale {4:0}%, Fuel {5:0}%";

    GameFlowController _flow;
    StatResolver _statResolver;

    public void Initialize(GameFlowController flow, StatResolver statResolver, GameConfig config)
    {
      _flow = flow;
      _statResolver = statResolver;
      if (config != null)
        gameConfig = config;

      if (restartButton != null)
      {
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(HandleRestart);
      }

      Hide(immediate: true);
    }

    public void ShowWin(RunState runState)
    {
      if (runState == null)
        return;

      if (titleText != null)
        titleText.text = winTitle;

      if (summaryText != null)
      {
        string cityName = runState.DestinationCityAsset != null
          ? runState.DestinationCityAsset.displayName
          : "City B";
        summaryText.text = string.Format(
          winSummaryFormat,
          cityName,
          runState.TripDayCurrent,
          runState.Money);
      }

      FadeIn();
    }

    public void ShowLose(RunState runState)
    {
      if (runState == null)
        return;

      if (titleText != null)
        titleText.text = loseTitle;

      if (summaryText != null && _statResolver != null && gameConfig != null)
      {
        summaryText.text = string.Format(
          loseSummaryFormat,
          _statResolver.GetLoseReason(),
          runState.TripDayCurrent,
          gameConfig.maxTripDays,
          runState.Money,
          runState.MoralePercent,
          runState.FuelPercent);
      }

      FadeIn();
    }

    void FadeIn()
    {
      gameObject.SetActive(true);

      if (canvasGroup == null)
        return;

      canvasGroup.alpha = 0f;
      canvasGroup.DOFade(1f, gameConfig != null ? gameConfig.winLoseFadeDuration : 0.45f)
        .SetEase(gameConfig != null ? gameConfig.winLoseFadeEase : Ease.OutCubic);
    }

    void HandleRestart()
    {
      Hide(immediate: true);
      _flow?.StartNewRun();
    }

    public void Hide(bool immediate)
    {
      if (canvasGroup != null)
        canvasGroup.DOKill();

      if (immediate && canvasGroup != null)
        canvasGroup.alpha = 0f;

      gameObject.SetActive(false);
    }

    void OnDisable()
    {
      if (canvasGroup != null)
        canvasGroup.DOKill();
    }
  }
}
