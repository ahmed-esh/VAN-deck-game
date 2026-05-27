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
    [SerializeField] string timerFormat = "{0:0}/{1}";

    RunState _runState;
    GameConfig _config;

    public void Bind(RunState runState, GameConfig config)
    {
      if (_runState != null)
      {
        _runState.StatsChanged -= Refresh;
        _runState.PhaseChanged -= Refresh;
      }

      _runState = runState;
      _config = config;

      if (_runState != null)
      {
        _runState.StatsChanged += Refresh;
        _runState.PhaseChanged += Refresh;
      }

      Refresh();
    }

    void OnDestroy()
    {
      if (_runState != null)
      {
        _runState.StatsChanged -= Refresh;
        _runState.PhaseChanged -= Refresh;
      }
    }

    public void RefreshTimer(float filledSections, int sectionCount)
    {
      int maxSections = Mathf.Max(1, sectionCount);
      float clamped = Mathf.Clamp(filledSections, 0f, maxSections);

      if (dayTimerText != null)
        dayTimerText.text = string.Format(timerFormat, clamped, maxSections);

      if (dayTimerFill != null)
        dayTimerFill.fillAmount = clamped / maxSections;
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

      int sectionCount = GetSectionCount();
      RefreshTimer(_runState.DrivingDayTimer, sectionCount);
    }

    int GetSectionCount()
    {
      if (_config == null)
        return 8;

      return Mathf.Max(1, _config.drivingDaySectionCount);
    }
  }
}
