using TMPro;
using UnityEngine;
using VanGame.Data;

namespace VanGame.UI
{
  public class MapStatsTooltipView : MonoBehaviour
  {
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text cityNameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text parkingText;
    [SerializeField] TMP_Text costOfLivingText;
    [SerializeField] TMP_Text funThemeText;
    [SerializeField] TMP_Text moraleText;
    [SerializeField] TMP_Text stayDaysText;
    [SerializeField] TMP_Text drivingDaysText;

    [Header("Labels")]
    [SerializeField] string moralePositiveFormat = "+{0} Morale";
    [SerializeField] string moraleNegativeFormat = "{0} Morale";
    [SerializeField] string stayDaysFormat = "Your family will spend {0} days in {1}.";
    [SerializeField] string drivingDaysFormat = "It will take you {0} days driving to get there from your current location.";

    void Awake()
    {
      SetLegacyFieldVisible(parkingText, false);
      SetLegacyFieldVisible(costOfLivingText, false);
      SetLegacyFieldVisible(funThemeText, false);
      Hide(immediate: true);
    }

    static void SetLegacyFieldVisible(TMP_Text text, bool visible)
    {
      if (text == null)
        return;

      text.gameObject.SetActive(visible);
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

      if (descriptionText != null)
      {
        descriptionText.gameObject.SetActive(true);
        descriptionText.text = string.IsNullOrWhiteSpace(city.regionDescription)
          ? city.funTheme
          : city.regionDescription;
      }
      else if (funThemeText != null)
      {
        funThemeText.gameObject.SetActive(true);
        funThemeText.text = string.IsNullOrWhiteSpace(city.regionDescription)
          ? city.funTheme
          : city.regionDescription;
      }

      if (moraleText != null)
        moraleText.text = city.baseMoraleDelta >= 0
          ? string.Format(moralePositiveFormat, city.baseMoraleDelta)
          : string.Format(moraleNegativeFormat, city.baseMoraleDelta);

      if (stayDaysText != null)
      {
        bool showStayDays = city.stayDaysInCity > 0;
        stayDaysText.gameObject.SetActive(showStayDays);
        if (showStayDays)
          stayDaysText.text = string.Format(stayDaysFormat, city.stayDaysInCity, city.displayName);
      }

      if (drivingDaysText != null)
      {
        bool showDrivingDays = drivingDays > 0;
        drivingDaysText.gameObject.SetActive(showDrivingDays);
        if (showDrivingDays)
          drivingDaysText.text = string.Format(drivingDaysFormat, drivingDays);
      }

      if (canvasGroup != null)
      {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
      }
    }

    public void Hide(bool immediate)
    {
      if (canvasGroup != null && !immediate)
        canvasGroup.alpha = 0f;

      gameObject.SetActive(false);
    }
  }
}
