using DG.Tweening;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class CanvasTransitionController : MonoBehaviour
  {
    [Header("Canvases")]
    [SerializeField] Canvas cardCanvas;
    [SerializeField] Canvas mapCanvas;

    [Header("Map animation targets")]
    [SerializeField] CanvasGroup mapCanvasGroup;
    [SerializeField] RectTransform mapRoot;
    [SerializeField] CanvasGroup mapShadeOverlay;

    [Header("Config")]
    [SerializeField] GameConfig gameConfig;

    [Header("Map root defaults (restored after city select)")]
    [SerializeField] Vector3 mapRootDefaultScale = Vector3.one;
    [SerializeField] Vector2 mapRootDefaultAnchoredPosition = Vector2.zero;

    Vector2 _mapRootDefaultPos;
    bool _isTransitioning;

    public bool IsTransitioning => _isTransitioning;

    void Awake()
    {
      if (mapRoot != null)
      {
        mapRootDefaultScale = mapRoot.localScale;
        _mapRootDefaultPos = mapRoot.anchoredPosition;
        mapRootDefaultAnchoredPosition = _mapRootDefaultPos;
      }

      SetMapVisible(false, immediate: true);
    }

    public void Configure(GameConfig config)
    {
      if (config != null)
        gameConfig = config;
    }

    public void OpenMap(bool forceDestinationPick = false)
    {
      if (_isTransitioning)
        return;

      SetMapVisible(true, immediate: false);
    }

    public void CloseMap()
    {
      if (_isTransitioning)
        return;

      SetMapVisible(false, immediate: false);
    }

    public void ConfirmCitySelection(MapRegionView selectedRegion, System.Action onComplete)
    {
      if (_isTransitioning || selectedRegion == null || gameConfig == null)
      {
        onComplete?.Invoke();
        return;
      }

      _isTransitioning = true;
      Sequence sequence = DOTween.Sequence();

      if (mapShadeOverlay != null)
      {
        mapShadeOverlay.gameObject.SetActive(true);
        mapShadeOverlay.alpha = 0f;
        sequence.Append(mapShadeOverlay.DOFade(gameConfig.citySelectShadeAlpha, gameConfig.citySelectShadeDuration)
          .SetEase(gameConfig.citySelectEase));
      }

      RectTransform regionRect = selectedRegion.LiftTarget;
      if (mapRoot != null && regionRect != null)
      {
        Vector2 focusOffset = -regionRect.anchoredPosition * (gameConfig.citySelectZoomScale - 1f);
        sequence.Join(mapRoot.DOScale(mapRootDefaultScale * gameConfig.citySelectZoomScale, gameConfig.citySelectZoomDuration)
          .SetEase(gameConfig.citySelectEase));
        sequence.Join(mapRoot.DOAnchorPos(mapRootDefaultAnchoredPosition + focusOffset, gameConfig.citySelectZoomDuration)
          .SetEase(gameConfig.citySelectEase));
      }

      sequence.AppendInterval(gameConfig.citySelectHoldDuration);

      if (mapCanvasGroup != null)
        sequence.Append(mapCanvasGroup.DOFade(0f, gameConfig.mapCloseFadeDuration).SetEase(gameConfig.canvasTransitionEase));

      if (mapShadeOverlay != null)
        sequence.Join(mapShadeOverlay.DOFade(0f, gameConfig.mapCloseFadeDuration));

      sequence.OnComplete(() =>
      {
        ResetMapTransform(immediate: true);
        if (mapCanvas != null)
          mapCanvas.gameObject.SetActive(false);

        if (mapShadeOverlay != null)
          mapShadeOverlay.gameObject.SetActive(false);

        if (cardCanvas != null)
        {
          cardCanvas.gameObject.SetActive(true);
          CanvasGroup cardGroup = cardCanvas.GetComponent<CanvasGroup>();
          if (cardGroup != null)
          {
            cardGroup.alpha = 0f;
            cardGroup.DOFade(1f, gameConfig.canvasTransitionDuration).SetEase(gameConfig.canvasTransitionEase);
          }
        }

        _isTransitioning = false;
        onComplete?.Invoke();
      });
    }

    void SetMapVisible(bool visible, bool immediate)
    {
      if (mapCanvas == null || cardCanvas == null || gameConfig == null)
        return;

      mapCanvas.gameObject.SetActive(true);
      cardCanvas.gameObject.SetActive(true);

      CanvasGroup cardGroup = GetOrAddCanvasGroup(cardCanvas);
      if (mapCanvasGroup == null)
        mapCanvasGroup = GetOrAddCanvasGroup(mapCanvas);

      KillTweens(cardGroup, mapCanvasGroup, mapShadeOverlay);

      if (immediate)
      {
        mapCanvasGroup.alpha = visible ? 1f : 0f;
        mapCanvasGroup.interactable = visible;
        mapCanvasGroup.blocksRaycasts = visible;
        mapCanvas.gameObject.SetActive(visible);

        cardGroup.alpha = visible ? 0f : 1f;
        cardGroup.interactable = !visible;
        cardGroup.blocksRaycasts = !visible;
        return;
      }

      _isTransitioning = true;

      if (visible)
      {
        mapCanvas.gameObject.SetActive(true);
        mapCanvasGroup.alpha = 0f;
        mapCanvasGroup.interactable = true;
        mapCanvasGroup.blocksRaycasts = true;

        Sequence openSeq = DOTween.Sequence();
        openSeq.Append(mapCanvasGroup.DOFade(1f, gameConfig.mapOpenFadeDuration).SetEase(gameConfig.canvasTransitionEase));
        openSeq.Join(cardGroup.DOFade(0f, gameConfig.mapOpenFadeDuration).SetEase(gameConfig.canvasTransitionEase));
        openSeq.OnComplete(() =>
        {
          cardGroup.interactable = false;
          cardGroup.blocksRaycasts = false;
          _isTransitioning = false;
        });
      }
      else
      {
        cardGroup.interactable = true;
        cardGroup.blocksRaycasts = true;

        Sequence closeSeq = DOTween.Sequence();
        closeSeq.Append(mapCanvasGroup.DOFade(0f, gameConfig.mapCloseFadeDuration).SetEase(gameConfig.canvasTransitionEase));
        closeSeq.Join(cardGroup.DOFade(1f, gameConfig.mapCloseFadeDuration).SetEase(gameConfig.canvasTransitionEase));
        closeSeq.OnComplete(() =>
        {
          mapCanvas.gameObject.SetActive(false);
          mapCanvasGroup.interactable = false;
          mapCanvasGroup.blocksRaycasts = false;
          ResetMapTransform(immediate: true);
          _isTransitioning = false;
        });
      }
    }

    void ResetMapTransform(bool immediate)
    {
      if (mapRoot == null)
        return;

      mapRoot.DOKill();
      if (immediate)
      {
        mapRoot.localScale = mapRootDefaultScale;
        mapRoot.anchoredPosition = mapRootDefaultAnchoredPosition;
      }
      else
      {
        mapRoot.localScale = mapRootDefaultScale;
        mapRoot.anchoredPosition = mapRootDefaultAnchoredPosition;
      }
    }

    static CanvasGroup GetOrAddCanvasGroup(Canvas canvas)
    {
      CanvasGroup group = canvas.GetComponent<CanvasGroup>();
      if (group == null)
        group = canvas.gameObject.AddComponent<CanvasGroup>();

      return group;
    }

    static void KillTweens(params CanvasGroup[] groups)
    {
      foreach (CanvasGroup group in groups)
      {
        if (group != null)
          group.DOKill();
      }
    }

    void OnDisable()
    {
      if (mapRoot != null)
        mapRoot.DOKill();

      if (mapCanvasGroup != null)
        mapCanvasGroup.DOKill();

      if (mapShadeOverlay != null)
        mapShadeOverlay.DOKill();
    }
  }
}
