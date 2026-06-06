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
    [Tooltip("Preview text offset from the original stat label (X = right, Y = up).")]
    [SerializeField] Vector2 previewOffsetFromSource = new Vector2(10f, 8f);
    [Tooltip("Extra offset per stat, added on top of Preview Offset From Source.")]
    [SerializeField] Vector2 moneyPreviewOffset;
    [SerializeField] Vector2 fuelPreviewOffset;
    [SerializeField] Vector2 moralePreviewOffset;
    [SerializeField] Vector2 vanPreviewOffset;

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

      SetPreviewLabel(_moneyPreview, moneyText, preview.AffectsMoney, string.Format(moneyFormat, preview.MoneyAfter), moneyPreviewOffset);
      SetPreviewLabel(_fuelPreview, fuelText, preview.AffectsFuel, string.Format(percentFormat, preview.FuelAfter), fuelPreviewOffset);
      SetPreviewLabel(_moralePreview, moraleText, preview.AffectsMorale, string.Format(percentFormat, preview.MoraleAfter), moralePreviewOffset);
      SetPreviewLabel(_vanPreview, vanText, preview.AffectsVan, string.Format(percentFormat, preview.VanAfter), vanPreviewOffset);
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

    void EnsurePreviewLabel(TMP_Text source, ref TMP_Text preview, Vector2 extraOffset)
    {
      if (source == null)
        return;

      if (preview == null)
      {
        preview = Instantiate(source, source.rectTransform.parent);
        preview.name = source.name + "_Preview";
        preview.raycastTarget = false;
        preview.fontMaterial = Instantiate(source.fontSharedMaterial);
        ApplyHollowStyle(preview);
        preview.gameObject.SetActive(false);
      }

      ApplyPreviewLayout(preview, source, extraOffset);
    }

    void ApplyPreviewLayout(TMP_Text preview, TMP_Text source, Vector2 extraOffset)
    {
      if (preview == null || source == null)
        return;

      RectTransform sourceRect = source.rectTransform;
      RectTransform previewRect = preview.rectTransform;

      if (previewRect.parent != sourceRect.parent)
        previewRect.SetParent(sourceRect.parent, false);

      previewRect.anchorMin = sourceRect.anchorMin;
      previewRect.anchorMax = sourceRect.anchorMax;
      previewRect.pivot = sourceRect.pivot;
      previewRect.sizeDelta = sourceRect.sizeDelta;
      previewRect.localRotation = sourceRect.localRotation;
      previewRect.localScale = sourceRect.localScale;
      previewRect.anchoredPosition = sourceRect.anchoredPosition + previewOffsetFromSource + extraOffset;
      previewRect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);
    }

    void EnsurePreviewLabels()
    {
      EnsurePreviewLabel(moneyText, ref _moneyPreview, moneyPreviewOffset);
      EnsurePreviewLabel(fuelText, ref _fuelPreview, fuelPreviewOffset);
      EnsurePreviewLabel(moraleText, ref _moralePreview, moralePreviewOffset);
      EnsurePreviewLabel(vanText, ref _vanPreview, vanPreviewOffset);
    }

    void ApplyHollowStyle(TMP_Text text)
    {
      if (text.fontMaterial != null)
        text.fontMaterial.EnableKeyword("OUTLINE_ON");

      text.outlineWidth = previewOutlineWidth;
      text.outlineColor = previewOutlineColor;
      text.color = previewFaceColor;
    }

    void SetPreviewLabel(TMP_Text preview, TMP_Text source, bool active, string value, Vector2 extraOffset)
    {
      if (preview == null || source == null)
        return;

      preview.text = value;
      preview.fontSize = source.fontSize;
      preview.alignment = source.alignment;
      ApplyPreviewLayout(preview, source, extraOffset);
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
