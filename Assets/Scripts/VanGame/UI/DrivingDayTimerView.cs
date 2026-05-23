using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class DrivingDayTimerView : MonoBehaviour
  {
    [SerializeField] TMP_Text destinationText;
    [SerializeField] TMP_Text legDaysText;
    [SerializeField] TMP_Text dayTimerText;
    [SerializeField] Image dayTimerFill;

    [SerializeField] string destinationFormat = "Driving to {0}";
    [SerializeField] string legDaysFormat = "Leg: {0} day(s) left";
    [SerializeField] string timerFormat = "{0:0}s / {1:0}s";

    RunState _runState;
    GameConfig _config;

    public void Bind(RunState runState, GameConfig config)
    {
      _runState = runState;
      _config = config;

      if (_runState != null)
        _runState.StatsChanged += Refresh;

      Refresh();
    }

    void OnDestroy()
    {
      if (_runState != null)
        _runState.StatsChanged -= Refresh;
    }

    public void RefreshTimer(float currentSeconds, float budgetSeconds)
    {
      if (dayTimerText != null && _config != null)
        dayTimerText.text = string.Format(timerFormat, currentSeconds, budgetSeconds);

      if (dayTimerFill != null && budgetSeconds > 0f)
        dayTimerFill.fillAmount = Mathf.Clamp01(currentSeconds / budgetSeconds);
    }

    public void Refresh()
    {
      if (_runState == null)
        return;

      if (destinationText != null)
      {
        string destName = _runState.DestinationCity != null
          ? _runState.DestinationCity.displayName
          : "—";
        destinationText.text = string.Format(destinationFormat, destName);
      }

      if (legDaysText != null)
        legDaysText.text = string.Format(legDaysFormat, _runState.DrivingDaysRemaining);

      if (_config != null)
        RefreshTimer(_runState.DrivingDayTimer, _config.drivingDayRealTimeSeconds);
    }
  }
}
