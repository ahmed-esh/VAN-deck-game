using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class CardHandController : MonoBehaviour
  {
    [SerializeField] RectTransform handContainer;
    [SerializeField] RectTransform playAnimationRoot;
    [SerializeField] CardHandHoverFan hoverFan;
    [SerializeField] CardView fallbackCardPrefab;

    readonly List<CardView> _cardViews = new List<CardView>();

    bool _handRebuildSuspended;
    DeckController _deck;
    StatResolver _statResolver;
    DrivingTurnController _drivingTurn;
    GameConfig _config;

    public IReadOnlyList<CardView> CardViews => _cardViews;

    public void Initialize(
      DeckController deck,
      StatResolver statResolver,
      DrivingTurnController drivingTurn,
      GameConfig config)
    {
      _deck = deck;
      _statResolver = statResolver;
      _drivingTurn = drivingTurn;
      _config = config;

      if (_deck != null)
        _deck.HandChanged += OnDeckHandChanged;
    }

    void OnDestroy()
    {
      if (_deck != null)
        _deck.HandChanged -= OnDeckHandChanged;
    }

    public void SetHandRebuildSuspended(bool suspended)
    {
      _handRebuildSuspended = suspended;
    }

    void OnDeckHandChanged()
    {
      if (_handRebuildSuspended)
      {
        RefreshAffordability();
        return;
      }

      RebuildHand();
    }

    public void RebuildHand()
    {
      ClearHandViews();

      if (_deck == null || handContainer == null)
        return;

      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card == null || (_deck != null && !_deck.IsCardLegalInCurrentRegion(card)))
          continue;

        CardView view = SpawnCardView(card);
        if (view == null)
          continue;
        bool canAfford = _statResolver != null && _statResolver.CanAfford(_deck.GetCardMoneyCost(card));
        view.Setup(card, canAfford);
        view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
        view.Clicked += OnCardClicked;
        _cardViews.Add(view);

        if (_config != null && view.RectTransform != null)
        {
          RectTransform drawRt = view.RectTransform;
          drawRt.localScale = Vector3.one * _config.cardDrawStartScale;
          drawRt.DOScale(1f, _config.cardDrawInDuration)
            .SetEase(_config.cardDrawEase)
            .SetLink(drawRt.gameObject, LinkBehaviour.KillOnDestroy);
        }
      }

      hoverFan?.RefreshFromChildren();
      RefreshAffordability();
    }

    CardView SpawnCardView(ActionCardDefinition card)
    {
      ActionCardPrefab cardPrefab = _deck?.GetActionCardPrefab(card);
      if (cardPrefab != null)
        return Instantiate(cardPrefab.View, handContainer);

      if (fallbackCardPrefab != null)
        return Instantiate(fallbackCardPrefab, handContainer);

      Debug.LogWarning($"CardHandController: No prefab for card '{card.title}'. Assign it on the deck or set Fallback Card Prefab.");
      return null;
    }

    public void RefreshAffordability()
    {
      if (_deck == null || _statResolver == null)
        return;

      foreach (CardView view in _cardViews)
      {
        if (view?.Definition == null || view.IsPlaying)
          continue;

        view.SetAffordable(_statResolver.CanAfford(_deck.GetCardMoneyCost(view.Definition)));
        view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
      }
    }

    public void SetHandInteractable(bool interactable)
    {
      foreach (CardView view in _cardViews)
      {
        if (view != null && !view.IsPlaying)
          view.SetInteractable(interactable);
      }
    }

    public void AnimateCardPlay(CardView view, System.Action onComplete)
    {
      if (view == null)
      {
        onComplete?.Invoke();
        return;
      }

      view.Clicked -= OnCardClicked;
      view.SetPlaying(true);
      _cardViews.Remove(view);

      RectTransform rt = view.RectTransform;
      if (rt == null || _config == null)
      {
        DestroyPlayingCardView(view);
        onComplete?.Invoke();
        hoverFan?.RefreshFromChildren();
        return;
      }

      rt.DOKill(true);
      hoverFan?.RefreshFromChildren();

      RectTransform animationRoot = GetPlayAnimationRoot();
      rt.SetParent(animationRoot, true);
      rt.SetAsLastSibling();
      hoverFan?.RefreshFromChildren();
      CanvasGroup group = rt.GetComponent<CanvasGroup>();
      if (group == null)
        group = rt.gameObject.AddComponent<CanvasGroup>();

      group.alpha = 1f;
      group.DOKill(true);
      Vector2 center = Vector2.zero;
      float moveDuration = _config.cardPlayMoveToCenterDuration;
      float holdDuration = _config.cardPlayCenterHoldDuration;
      float vanishDuration = _config.cardPlayVanishDuration;
      float peakScale = _config.cardPlayCenterScale;
      float spin = _config.cardPlaySpinDegrees;

      bool[] finished = { false };
      Sequence seq = DOTween.Sequence();
      seq.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      seq.Append(rt.DOAnchorPos(center, moveDuration).SetEase(Ease.OutCubic));
      seq.Join(rt.DOScale(peakScale, moveDuration).SetEase(Ease.OutBack));
      seq.Join(rt.DORotate(new Vector3(0f, 0f, spin * 0.5f), moveDuration, RotateMode.Fast));
      seq.AppendInterval(holdDuration);
      seq.Append(rt.DOScale(0f, vanishDuration).SetEase(_config.cardPlayEase));
      seq.Join(group.DOFade(0f, vanishDuration));
      seq.Join(rt.DORotate(new Vector3(0f, 0f, spin), vanishDuration, RotateMode.FastBeyond360));
      seq.OnKill(() => CompleteCardPlayAnimation(view, finished, onComplete));
      seq.OnComplete(() => CompleteCardPlayAnimation(view, finished, onComplete));
    }

    void CompleteCardPlayAnimation(CardView view, bool[] finished, System.Action onComplete)
    {
      if (finished[0])
        return;

      finished[0] = true;

      if (view != null)
        DestroyPlayingCardView(view);

      hoverFan?.RefreshFromChildren();
      onComplete?.Invoke();
    }

    static void DestroyPlayingCardView(CardView view)
    {
      if (view == null)
        return;

      RectTransform rt = view.RectTransform;
      if (rt != null)
        rt.DOKill(true);

      if (view.gameObject != null)
      {
        view.gameObject.SetActive(false);
        Destroy(view.gameObject);
      }
    }

    RectTransform GetPlayAnimationRoot()
    {
      if (playAnimationRoot != null)
        return playAnimationRoot;

      if (handContainer != null)
      {
        Canvas canvas = handContainer.GetComponentInParent<Canvas>();
        if (canvas != null)
          return canvas.transform as RectTransform;
      }

      return handContainer;
    }

    void OnCardClicked(CardView view)
    {
      _drivingTurn?.TryPlayCard(view);
    }

    /// <summary>Clears spawned card views without changing deck state (e.g. when a leg ends).</summary>
    public void ClearHandVisuals()
    {
      ClearHandViews();
    }

    void ClearHandViews()
    {
      foreach (CardView view in _cardViews)
      {
        if (view == null)
          continue;

        view.Clicked -= OnCardClicked;
        DestroyPlayingCardView(view);
      }

      _cardViews.Clear();
      hoverFan?.RefreshFromChildren();
    }
  }
}
