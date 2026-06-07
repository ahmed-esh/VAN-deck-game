using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class EventLogView : MonoBehaviour
  {
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text headerText;
    [SerializeField] RectTransform linesContainer;
    [SerializeField] TMP_Text linePrefab;
    [SerializeField] Button continueButton;
    [SerializeField] GameConfig gameConfig;

    [SerializeField] string headerFormat = "What happened in {0}";

    [Header("Curtain unfold")]
    [SerializeField] float curtainUnfoldDuration = 0.62f;
    [SerializeField] float curtainUnfoldOvershoot = 1.035f;
    [SerializeField] float curtainFoldDuration = 0.48f;

    readonly List<TMP_Text> _spawnedLines = new List<TMP_Text>();
    Action _onContinue;

    RectTransform _curtainRect;
    Image _curtainImage;
    Vector3 _curtainRestScale = Vector3.one;
    float _curtainRestAlpha = 1f;
    bool _curtainPivotPrepared;
    bool _isClosing;
    Tween _curtainTween;

    void Awake()
    {
      if (continueButton != null)
      {
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(HandleContinue);
      }

      CacheCurtainReferences();
      PrepareCurtainCollapsed();
      Hide(immediate: true);
    }

    void CacheCurtainReferences()
    {
      GameObject curtainRoot = root != null ? root : gameObject;
      _curtainRect = curtainRoot.transform as RectTransform;
      _curtainImage = curtainRoot.GetComponent<Image>();

      if (_curtainRect != null)
        _curtainRestScale = _curtainRect.localScale;

      if (_curtainImage != null)
        _curtainRestAlpha = _curtainImage.color.a;
    }

    static void SetPivotPreservingPosition(RectTransform rectTransform, Vector2 pivot)
    {
      Vector2 size = rectTransform.rect.size;
      Vector2 deltaPivot = rectTransform.pivot - pivot;
      Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);
      rectTransform.pivot = pivot;
      rectTransform.anchoredPosition -= deltaPosition;
    }

    void EnsureCurtainPivotAtTop()
    {
      if (_curtainRect == null || _curtainPivotPrepared)
        return;

      SetPivotPreservingPosition(_curtainRect, new Vector2(_curtainRect.pivot.x, 1f));
      _curtainPivotPrepared = true;
    }

    void PrepareCurtainCollapsed()
    {
      if (_curtainRect == null)
        return;

      KillCurtainTween();
      EnsureCurtainPivotAtTop();

      Vector3 collapsedScale = _curtainRestScale;
      collapsedScale.y = 0f;
      _curtainRect.localScale = collapsedScale;

      if (_curtainImage != null)
      {
        Color color = _curtainImage.color;
        color.a = 0f;
        _curtainImage.color = color;
      }
    }

    void PlayCurtainUnfold()
    {
      if (_curtainRect == null)
        return;

      KillCurtainTween();
      EnsureCurtainPivotAtTop();
      PrepareCurtainCollapsed();

      float overshootY = _curtainRestScale.y * curtainUnfoldOvershoot;
      float mainDuration = curtainUnfoldDuration * 0.82f;
      float settleDuration = curtainUnfoldDuration - mainDuration;

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(_curtainRect.gameObject, LinkBehaviour.KillOnDestroy);
      sequence.Append(
        _curtainRect.DOScaleY(overshootY, mainDuration)
          .SetEase(Ease.OutCubic));
      sequence.Append(
        _curtainRect.DOScaleY(_curtainRestScale.y, settleDuration)
          .SetEase(Ease.OutSine));

      if (_curtainImage != null)
      {
        sequence.Join(
          _curtainImage.DOFade(_curtainRestAlpha, curtainUnfoldDuration * 0.72f)
            .SetEase(Ease.OutQuad));
      }

      _curtainTween = sequence;
    }

    void PlayCurtainFoldUp(Action onComplete)
    {
      if (_curtainRect == null)
      {
        onComplete?.Invoke();
        return;
      }

      KillCurtainTween();
      EnsureCurtainPivotAtTop();

      float duration = Mathf.Max(0.05f, curtainFoldDuration);
      float fadeDuration = duration * 0.6f;

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(_curtainRect.gameObject, LinkBehaviour.KillOnDestroy);

      if (_curtainImage != null)
      {
        sequence.Join(
          _curtainImage.DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad));
      }

      sequence.Join(
        _curtainRect.DOScaleY(0f, duration)
          .SetEase(Ease.InCubic));
      sequence.OnComplete(() => onComplete?.Invoke());

      _curtainTween = sequence;
    }

    void DeactivateRoot()
    {
      if (root != null)
        root.SetActive(false);
      else
        gameObject.SetActive(false);
    }

    void KillCurtainTween()
    {
      if (_curtainTween != null && _curtainTween.IsActive())
        _curtainTween.Kill();

      _curtainTween = null;

      if (_curtainRect != null)
        _curtainRect.DOKill();

      if (_curtainImage != null)
        _curtainImage.DOKill();
    }

    public void Show(CityDefinition city, IReadOnlyList<string> lines, Action onContinue)
    {
      _isClosing = false;
      _onContinue = onContinue;

      if (root != null)
        root.SetActive(true);
      else
        gameObject.SetActive(true);

      PlayCurtainUnfold();

      if (headerText != null && city != null)
        headerText.text = string.Format(headerFormat, city.displayName);

      ClearLines();

      if (lines == null || lines.Count == 0)
      {
        if (continueButton != null)
          continueButton.interactable = true;
        return;
      }

      if (continueButton != null)
        continueButton.interactable = false;

      float stagger = gameConfig != null ? gameConfig.eventLogLineStagger : 0.12f;
      float duration = gameConfig != null ? gameConfig.eventLogLineFadeDuration : 0.35f;

      for (int i = 0; i < lines.Count; i++)
      {
        TMP_Text line = CreateLine(lines[i]);
        if (line == null)
          continue;

        Color c = line.color;
        c.a = 0f;
        line.color = c;
        line.DOFade(1f, duration).SetDelay(i * stagger).SetEase(Ease.OutCubic);
      }

      float totalDelay = lines.Count * stagger + duration;
      DOVirtual.DelayedCall(totalDelay, () =>
      {
        if (_isClosing)
          return;

        if (continueButton != null)
          continueButton.interactable = true;
      });
    }

    TMP_Text CreateLine(string text)
    {
      if (linesContainer == null || linePrefab == null)
        return null;

      TMP_Text line = Instantiate(linePrefab, linesContainer);
      line.text = text;
      line.gameObject.SetActive(true);
      _spawnedLines.Add(line);
      return line;
    }

    void HandleContinue()
    {
      if (_isClosing)
        return;

      _isClosing = true;

      if (continueButton != null)
        continueButton.interactable = false;

      Action callback = _onContinue;
      _onContinue = null;
      ClearLines();

      PlayCurtainFoldUp(() =>
      {
        PrepareCurtainCollapsed();
        DeactivateRoot();
        _isClosing = false;
        callback?.Invoke();
      });
    }

    public void Hide(bool immediate)
    {
      _isClosing = false;
      _onContinue = null;
      KillCurtainTween();
      ClearLines();
      PrepareCurtainCollapsed();
      DeactivateRoot();

      if (immediate && continueButton != null)
        continueButton.interactable = true;
    }

    void ClearLines()
    {
      foreach (TMP_Text line in _spawnedLines)
      {
        if (line == null)
          continue;

        line.DOKill();
        Destroy(line.gameObject);
      }

      _spawnedLines.Clear();
    }

    void OnDisable()
    {
      KillCurtainTween();

      foreach (TMP_Text line in _spawnedLines)
      {
        if (line != null)
          line.DOKill();
      }
    }
  }
}
