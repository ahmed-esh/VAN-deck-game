using System;
using DG.Tweening;
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

    [Header("Card preview")]
    [SerializeField] Color previewOutlineColor = new Color(0.45f, 1f, 0.95f, 1f);
    [SerializeField] Color previewFaceColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] float previewOutlineWidth = 0.32f;
    [SerializeField] float applyAnimDuration = 0.42f;
    [SerializeField] float applyPunchStrength = 0.05f;
    [SerializeField] Ease applyAnimEase = Ease.OutCubic;

    RunState _runState;
    GameConfig _config;
    bool _suppressRefresh;
    Sequence _applySequence;

    TMP_Text _moneyPreview;
    TMP_Text _fuelPreview;
    TMP_Text _moralePreview;
    TMP_Text _vanPreview;

    public void Bind(RunState runState, GameConfig config)
    {
      Unbind();

      _runState = runState;
      _config = config;

      if (_runState != null)
        _runState.StatsChanged += Refresh;

      EnsurePreviewLabels();
      Refresh();
    }

    void Awake()
    {
      EnsurePreviewLabels();
    }

    void OnDestroy()
    {
      KillApplySequence();
      Unbind();
    }

    void Unbind()
    {
      if (_runState != null)
        _runState.StatsChanged -= Refresh;
    }

    public void Refresh()
    {
      if (_runState == null || _suppressRefresh)
        return;

      if (moneyText != null)
        moneyText.text = string.Format(moneyFormat, _runState.Money);

      SetPercent(fuelText, fuelFill, _runState.FuelPercent);
      SetPercent(moraleText, moraleFill, _runState.MoralePercent);
      SetPercent(vanText, vanFill, _runState.VanConditionPercent);

      if (dayText != null && _config != null)
        dayText.text = string.Format(dayFormat, _runState.TripDayCurrent, _config.maxTripDays);
    }

    public void ShowCardPreview(CardEffectPreview preview)
    {
      EnsurePreviewLabels();

      SetPreviewLabel(_moneyPreview, moneyText, preview.AffectsMoney, string.Format(moneyFormat, preview.MoneyAfter));
      SetPreviewLabel(_fuelPreview, fuelText, preview.AffectsFuel, string.Format(percentFormat, preview.FuelAfter));
      SetPreviewLabel(_moralePreview, moraleText, preview.AffectsMorale, string.Format(percentFormat, preview.MoraleAfter));
      SetPreviewLabel(_vanPreview, vanText, preview.AffectsVan, string.Format(percentFormat, preview.VanAfter));
    }

    public void ClearCardPreview()
    {
      SetPreviewActive(_moneyPreview, false);
      SetPreviewActive(_fuelPreview, false);
      SetPreviewActive(_moralePreview, false);
      SetPreviewActive(_vanPreview, false);
    }

    public void AnimateApplyPreview(CardEffectPreview preview)
    {
      KillApplySequence();
      ClearCardPreview();
      _suppressRefresh = true;

      _applySequence = DOTween.Sequence();
      _applySequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

      if (preview.AffectsMoney && moneyText != null)
        AppendStatTween(_applySequence, preview.MoneyBefore, preview.MoneyAfter, v => moneyText.text = string.Format(moneyFormat, Mathf.RoundToInt(v)), moneyText.transform);

      if (preview.AffectsFuel && fuelText != null)
        AppendStatTween(_applySequence, preview.FuelBefore, preview.FuelAfter, v => SetPercent(fuelText, fuelFill, v), fuelText.transform);

      if (preview.AffectsMorale && moraleText != null)
        AppendStatTween(_applySequence, preview.MoraleBefore, preview.MoraleAfter, v => SetPercent(moraleText, moraleFill, v), moraleText.transform);

      if (preview.AffectsVan && vanText != null)
        AppendStatTween(_applySequence, preview.VanBefore, preview.VanAfter, v => SetPercent(vanText, vanFill, v), vanText.transform);

      if (_applySequence.Duration() <= 0f)
      {
        FinishApplyAnimation();
        return;
      }

      _applySequence.OnComplete(FinishApplyAnimation);
    }

    void AppendStatTween(Sequence sequence, float from, float to, Action<float> apply, Transform punchTarget)
    {
      float value = from;
      sequence.Join(DOTween.To(() => value, v =>
      {
        value = v;
        apply(v);
      }, to, applyAnimDuration).SetEase(applyAnimEase));

      if (punchTarget != null)
      {
        Vector3 punch = new Vector3(applyPunchStrength, applyPunchStrength, 0f);
        sequence.Join(punchTarget.DOPunchScale(punch, applyAnimDuration, 1, 0.55f));
      }
    }

    void FinishApplyAnimation()
    {
      _suppressRefresh = false;
      _applySequence = null;
      Refresh();
    }

    void KillApplySequence()
    {
      if (_applySequence == null)
        return;

      _applySequence.Kill();
      _applySequence = null;
      _suppressRefresh = false;
    }

    void EnsurePreviewLabels()
    {
      EnsurePreviewLabel(moneyText, ref _moneyPreview);
      EnsurePreviewLabel(fuelText, ref _fuelPreview);
      EnsurePreviewLabel(moraleText, ref _moralePreview);
      EnsurePreviewLabel(vanText, ref _vanPreview);
    }

    void EnsurePreviewLabel(TMP_Text source, ref TMP_Text preview)
    {
      if (source == null || preview != null)
        return;

      preview = Instantiate(source, source.transform);
      preview.name = source.name + "_Preview";
      preview.raycastTarget = false;
      preview.fontMaterial = Instantiate(source.fontSharedMaterial);
      ApplyHollowStyle(preview);

      RectTransform previewRect = preview.rectTransform;
      previewRect.anchorMin = Vector2.zero;
      previewRect.anchorMax = Vector2.one;
      previewRect.offsetMin = Vector2.zero;
      previewRect.offsetMax = Vector2.zero;
      preview.gameObject.SetActive(false);
    }

    void ApplyHollowStyle(TMP_Text text)
    {
      if (text.fontMaterial != null)
        text.fontMaterial.EnableKeyword("OUTLINE_ON");

      text.outlineWidth = previewOutlineWidth;
      text.outlineColor = previewOutlineColor;
      text.color = previewFaceColor;
    }

    static void SetPreviewLabel(TMP_Text preview, TMP_Text source, bool active, string value)
    {
      if (preview == null || source == null)
        return;

      preview.text = value;
      preview.fontSize = source.fontSize;
      preview.alignment = source.alignment;
      preview.gameObject.SetActive(active);
    }

    static void SetPreviewActive(TMP_Text preview, bool active)
    {
      if (preview != null)
        preview.gameObject.SetActive(active);
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
