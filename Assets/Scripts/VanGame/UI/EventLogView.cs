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

    readonly List<TMP_Text> _spawnedLines = new List<TMP_Text>();
    Action _onContinue;

    void Awake()
    {
      if (continueButton != null)
      {
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(HandleContinue);
      }

      Hide(immediate: true);
    }

    public void Show(CityDefinition city, IReadOnlyList<string> lines, Action onContinue)
    {
      _onContinue = onContinue;

      if (root != null)
        root.SetActive(true);
      else
        gameObject.SetActive(true);

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
      Hide(immediate: false);
      _onContinue?.Invoke();
      _onContinue = null;
    }

    public void Hide(bool immediate)
    {
      if (root != null)
        root.SetActive(false);
      else
        gameObject.SetActive(false);

      ClearLines();

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
      foreach (TMP_Text line in _spawnedLines)
      {
        if (line != null)
          line.DOKill();
      }
    }
  }
}
