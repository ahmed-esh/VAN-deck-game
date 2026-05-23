using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class StatsHudView : MonoBehaviour
  {
    [Header("Text fields")]
    [SerializeField] TMP_Text moneyText;
    [SerializeField] TMP_Text fuelText;
    [SerializeField] TMP_Text moraleText;
    [SerializeField] TMP_Text vanText;
    [SerializeField] TMP_Text dayText;

    [Header("Optional fill bars (Image type Filled)")]
    [SerializeField] Image fuelFill;
    [SerializeField] Image moraleFill;
    [SerializeField] Image vanFill;

    [Header("Format strings")]
    [SerializeField] string moneyFormat = "${0}";
    [SerializeField] string percentFormat = "{0:0}%";
    [SerializeField] string dayFormat = "Day {0}/{1}";

    RunState _runState;
    GameConfig _config;

    public void Bind(RunState runState, GameConfig config)
    {
      Unbind();

      _runState = runState;
      _config = config;

      if (_runState != null)
        _runState.StatsChanged += Refresh;

      Refresh();
    }

    void OnDestroy()
    {
      Unbind();
    }

    void Unbind()
    {
      if (_runState != null)
        _runState.StatsChanged -= Refresh;
    }

    public void Refresh()
    {
      if (_runState == null)
        return;

      if (moneyText != null)
        moneyText.text = string.Format(moneyFormat, _runState.Money);

      SetPercent(fuelText, fuelFill, _runState.FuelPercent);
      SetPercent(moraleText, moraleFill, _runState.MoralePercent);
      SetPercent(vanText, vanFill, _runState.VanConditionPercent);

      if (dayText != null && _config != null)
        dayText.text = string.Format(dayFormat, _runState.TripDayCurrent, _config.maxTripDays);
    }

    static void SetPercent(TMP_Text label, Image fill, float value)
    {
      if (label != null)
        label.text = string.Format("{0:0}%", value);

      if (fill != null)
        fill.fillAmount = value / 100f;
    }
  }
}
