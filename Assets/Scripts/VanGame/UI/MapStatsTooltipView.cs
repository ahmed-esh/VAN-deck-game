using TMPro;
using UnityEngine;
using VanGame.Data;

namespace VanGame.UI
{
  public class MapStatsTooltipView : MonoBehaviour
  {
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text cityNameText;
    [SerializeField] TMP_Text parkingText;
    [SerializeField] TMP_Text costOfLivingText;
    [SerializeField] TMP_Text funThemeText;
    [SerializeField] TMP_Text moraleText;
    [SerializeField] TMP_Text stayDaysText;
    [SerializeField] TMP_Text drivingDaysText;

    [Header("Labels")]
    [SerializeField] string moralePositiveFormat = "+{0} Morale";
    [SerializeField] string moraleNegativeFormat = "{0} Morale";
    [SerializeField] string drivingDaysFormat = "{0} days to drive";

    void Awake()
    {
      Hide(immediate: true);
    }

    public void Show(CityDefinition city, int drivingDays)
    {
      if (city == null)
      {
        Hide(immediate: false);
        return;
      }

      gameObject.SetActive(true);

      if (cityNameText != null)
        cityNameText.text = city.displayName;

      if (parkingText != null)
        parkingText.text = city.parking.ToString();

      if (costOfLivingText != null)
        costOfLivingText.text = city.costOfLiving.ToString();

      if (funThemeText != null)
        funThemeText.text = city.funTheme;

      if (moraleText != null)
        moraleText.text = city.baseMoraleDelta >= 0
          ? string.Format(moralePositiveFormat, city.baseMoraleDelta)
          : string.Format(moraleNegativeFormat, city.baseMoraleDelta);

      if (stayDaysText != null)
        stayDaysText.text = city.stayDaysInCity.ToString();

      if (drivingDaysText != null)
        drivingDaysText.text = string.Format(drivingDaysFormat, drivingDays);

      if (canvasGroup != null)
      {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
      }
    }

    public void Hide(bool immediate)
    {
      if (canvasGroup != null && !immediate)
      {
        canvasGroup.alpha = 0f;
      }

      gameObject.SetActive(false);
    }
  }
}
