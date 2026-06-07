using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VanGame;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  /// <summary>
  /// Scrollable lose screen: auto-scrolls through tall content, shows final stats, restart at the bottom.
  /// </summary>
  public class LoseScrollPanelView : MonoBehaviour
  {
    [Header("Scroll")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform scrollContent;

    [Header("Stats")]
    [SerializeField] TMP_Text dayText;
    [SerializeField] TMP_Text moneyText;
    [SerializeField] TMP_Text vanConditionText;
    [SerializeField] TMP_Text fuelText;
    [SerializeField] TMP_Text moraleText;

    [Header("Actions")]
    [SerializeField] Button restartButton;

    [Header("Optional fade")]
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Formats")]
    [SerializeField] string dayFormat = "{0}";
    [SerializeField] string moneyFormat = "${0}";
    [SerializeField] string percentFormat = "{0:0}%";

    [Header("Auto scroll")]
    [SerializeField] float autoScrollDuration = 10f;
    [SerializeField] Ease autoScrollEase = Ease.InOutSine;
    [SerializeField] float fadeInDuration = 0.35f;
    [SerializeField] Ease fadeInEase = Ease.OutCubic;
    [SerializeField] float layoutSettleDelay = 0.15f;

    Tween _autoScrollTween;
    bool _autoScrolling;
    Coroutine _showRoutine;

    public void Initialize(GameFlowController flow, StatResolver statResolver, GameConfig config)
    {
      ResolveScrollReferences();

      if (restartButton != null)
      {
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(HandleRestart);
      }

      Hide(immediate: true);
    }

    void Update()
    {
      if (!_autoScrolling || _autoScrollTween == null || !_autoScrollTween.IsActive())
        return;

      if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
        StopAutoScroll();
    }

    public void ShowLose(RunState runState)
    {
      if (runState == null)
        return;

      ResolveScrollReferences();

      if (_showRoutine != null)
        StopCoroutine(_showRoutine);

      PopulateStats(runState);
      gameObject.SetActive(true);
      PrepareFadeIn();
      _showRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
      yield return null;
      Canvas.ForceUpdateCanvases();

      if (scrollContent != null)
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

      yield return null;
      Canvas.ForceUpdateCanvases();

      ResetScrollToTop();

      if (layoutSettleDelay > 0f)
        yield return new WaitForSecondsRealtime(layoutSettleDelay);

      FadeIn();
      StartAutoScroll();
      _showRoutine = null;
    }

    public void Hide(bool immediate)
    {
      if (_showRoutine != null)
      {
        StopCoroutine(_showRoutine);
        _showRoutine = null;
      }

      StopAutoScroll();

      if (canvasGroup != null)
        canvasGroup.DOKill();

      if (immediate && canvasGroup != null)
        canvasGroup.alpha = 0f;

      gameObject.SetActive(false);
    }

    void ResolveScrollReferences()
    {
      if (scrollRect == null)
        scrollRect = GetComponentInChildren<ScrollRect>(true);

      if (scrollContent == null && scrollRect != null)
        scrollContent = scrollRect.content;
    }

    void PrepareFadeIn()
    {
      if (canvasGroup == null)
        return;

      canvasGroup.DOKill();
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = true;
      canvasGroup.blocksRaycasts = true;
    }

    void PopulateStats(RunState runState)
    {
      if (dayText != null)
        dayText.text = string.Format(dayFormat, runState.TripDayCurrent);

      if (moneyText != null)
        moneyText.text = string.Format(moneyFormat, runState.Money);

      if (vanConditionText != null)
        vanConditionText.text = string.Format(percentFormat, runState.VanConditionPercent);

      if (fuelText != null)
        fuelText.text = string.Format(percentFormat, runState.FuelPercent);

      if (moraleText != null)
        moraleText.text = string.Format(percentFormat, runState.MoralePercent);
    }

    void ResetScrollToTop()
    {
      if (scrollRect == null || scrollContent == null)
        return;

      scrollRect.StopMovement();
      scrollRect.velocity = Vector2.zero;
      scrollRect.verticalNormalizedPosition = 1f;
      Canvas.ForceUpdateCanvases();
    }

    void StartAutoScroll()
    {
      if (scrollRect == null || scrollContent == null)
        return;

      if (!TryGetScrollRange(out _, out _))
        return;

      StopAutoScroll();
      _autoScrolling = true;

      scrollRect.StopMovement();
      scrollRect.velocity = Vector2.zero;
      scrollRect.verticalNormalizedPosition = 1f;

      _autoScrollTween = DOTween.To(
          () => scrollRect.verticalNormalizedPosition,
          value => scrollRect.verticalNormalizedPosition = value,
          0f,
          autoScrollDuration)
        .SetEase(autoScrollEase)
        .SetUpdate(true)
        .OnComplete(() => _autoScrolling = false);
    }

    bool TryGetScrollRange(out float topY, out float bottomY)
    {
      topY = 0f;
      bottomY = 0f;

      RectTransform viewport = scrollRect.viewport != null
        ? scrollRect.viewport
        : scrollRect.transform as RectTransform;

      if (viewport == null)
        return false;

      Canvas.ForceUpdateCanvases();

      float hiddenHeight = scrollContent.rect.height - viewport.rect.height;
      if (hiddenHeight <= 1f)
        return false;

      // Top-anchored scroll content moves down (positive Y) to reveal lower content.
      topY = 0f;
      bottomY = hiddenHeight;
      return true;
    }

    void StopAutoScroll()
    {
      _autoScrolling = false;

      if (_autoScrollTween != null)
      {
        _autoScrollTween.Kill();
        _autoScrollTween = null;
      }

      if (scrollRect != null)
      {
        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
      }
    }

    void FadeIn()
    {
      if (canvasGroup == null)
        return;

      canvasGroup.DOKill();
      canvasGroup.alpha = 0f;
      canvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeInEase).SetUpdate(true);
    }

    void HandleRestart()
    {
      Time.timeScale = 1f;
      Hide(immediate: true);
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDisable()
    {
      if (_showRoutine != null)
      {
        StopCoroutine(_showRoutine);
        _showRoutine = null;
      }

      StopAutoScroll();

      if (canvasGroup != null)
        canvasGroup.DOKill();
    }
  }
}
