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
    [SerializeField] CardHandHoverFan hoverFan;
    [SerializeField] CardView cardPrefab;

    readonly List<CardView> _cardViews = new List<CardView>();

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
        _deck.HandChanged += RebuildHand;
    }

    void OnDestroy()
    {
      if (_deck != null)
        _deck.HandChanged -= RebuildHand;
    }

    public void RebuildHand()
    {
      ClearHandViews();

      if (_deck == null || cardPrefab == null || handContainer == null)
        return;

      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card == null)
          continue;

        CardView view = Instantiate(cardPrefab, handContainer);
        bool canAfford = _statResolver != null && _statResolver.CanAfford(_deck.GetCardMoneyCost(card));
        view.Setup(card, canAfford);
        view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
        view.Clicked += OnCardClicked;
        _cardViews.Add(view);

        if (_config != null && view.RectTransform != null)
        {
          view.RectTransform.localScale = Vector3.one * _config.cardDrawStartScale;
          view.RectTransform.DOScale(1f, _config.cardDrawInDuration).SetEase(_config.cardDrawEase);
        }
      }

      hoverFan?.RefreshFromChildren();
      RefreshAffordability();
    }

    public void RefreshAffordability()
    {
      if (_deck == null || _statResolver == null)
        return;

      foreach (CardView view in _cardViews)
      {
        if (view?.Definition == null)
          continue;

        view.SetAffordable(_statResolver.CanAfford(_deck.GetCardMoneyCost(view.Definition)));
        view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
      }
    }

    public void SetHandInteractable(bool interactable)
    {
      foreach (CardView view in _cardViews)
      {
        if (view != null)
          view.SetInteractable(interactable);
      }
    }

    public void AnimateCardOut(CardView view, System.Action onComplete)
    {
      if (view == null)
      {
        onComplete?.Invoke();
        return;
      }

      view.Clicked -= OnCardClicked;
      _cardViews.Remove(view);

      RectTransform rt = view.RectTransform;
      if (rt == null || _config == null)
      {
        Destroy(view.gameObject);
        onComplete?.Invoke();
        hoverFan?.RefreshFromChildren();
        return;
      }

      rt.DOKill();
      CanvasGroup group = rt.GetComponent<CanvasGroup>();
      if (group == null)
        group = rt.gameObject.AddComponent<CanvasGroup>();

      Sequence seq = DOTween.Sequence();
      seq.Append(rt.DOScale(0f, _config.cardPlayOutDuration).SetEase(_config.cardPlayEase));
      seq.Join(group.DOFade(0f, _config.cardPlayOutDuration));
      seq.OnComplete(() =>
      {
        Destroy(view.gameObject);
        hoverFan?.RefreshFromChildren();
        onComplete?.Invoke();
      });
    }

    void OnCardClicked(CardView view)
    {
      _drivingTurn?.TryPlayCard(view);
    }

    void ClearHandViews()
    {
      foreach (CardView view in _cardViews)
      {
        if (view == null)
          continue;

        view.Clicked -= OnCardClicked;
        if (view.RectTransform != null)
          view.RectTransform.DOKill();

        Destroy(view.gameObject);
      }

      _cardViews.Clear();
    }
  }
}
