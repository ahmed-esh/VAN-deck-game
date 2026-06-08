using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VanGame.Audio;
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
    CardView _inspectedCard;
    InspectState _inspectState;
    DeckController _deck;
    StatResolver _statResolver;
    DrivingTurnController _drivingTurn;
    GameConfig _config;

    struct InspectState
    {
      public Transform parent;
      public int siblingIndex;
      public Vector2 anchoredPosition;
      public Vector3 localEulerAngles;
      public Vector3 localScale;
    }

    public IReadOnlyList<CardView> CardViews => _cardViews;
    public bool IsInspectingCard => _inspectedCard != null;

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

    void Update()
    {
      if (_inspectedCard != null)
      {
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
          CloseInspect();
        return;
      }

      if (!Input.GetKeyDown(KeyCode.X) || hoverFan == null || _drivingTurn == null || !_drivingTurn.CanPlayCards)
        return;

      CardView focused = hoverFan.GetFocusedCardView();
      if (focused != null && focused.IsInteractable && hoverFan.CanFocusCards)
        OpenInspect(focused);
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

      RefreshHandViews(dealerDealAll: false);
    }

    int _pendingDrawSlot = -1;

    public void AddDrawnCardToSlot(int slot)
    {
      ActionCardDefinition card = FindNewCardInDeckHand();
      if (card == null || handContainer == null || slot < 0)
      {
        hoverFan?.SetAwaitingDrawnCard(false);
        return;
      }

      CardView view = SpawnAndSetupCardView(card);
      if (view == null)
      {
        hoverFan?.SetAwaitingDrawnCard(false);
        return;
      }

      view.HandSlot = slot;
      view.transform.SetSiblingIndex(slot);
      _cardViews.Add(view);
      AnimateDrawFromLeft(view);
      RefreshAffordability();
    }

    void AssignRoundSlots()
    {
      int maxSlots = _deck != null ? _deck.HandSize : 8;
      for (int i = 0; i < _cardViews.Count; i++)
      {
        CardView view = _cardViews[i];
        if (view == null)
          continue;

        view.HandSlot = i;
        view.transform.SetSiblingIndex(i);
      }

      hoverFan?.BeginRound(maxSlots);
    }

    public ActionCardDefinition FindNewCardInDeckHand()
    {
      if (_deck == null)
        return null;

      var existing = new HashSet<ActionCardDefinition>();
      foreach (CardView view in _cardViews)
      {
        if (view?.Definition != null)
          existing.Add(view.Definition);
      }

      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card != null && !existing.Contains(card) && _deck.IsCardLegalInCurrentRegion(card))
          return card;
      }

      return null;
    }

    float AnimDur(float seconds)
    {
      if (_config == null)
        return seconds;

      return seconds * _config.cardAnimationDurationScale;
    }

    /// <summary>Full deal animation at the start of a driving leg.</summary>
    public void DealHandFromDealer()
    {
      CloseInspectImmediate();
      ClearHandViews();
      RefreshHandViews(dealerDealAll: true);
    }

    public void RebuildHand()
    {
      RefreshHandViews(dealerDealAll: false);
    }

    void RefreshHandViews(bool dealerDealAll)
    {
      if (_deck == null || handContainer == null)
        return;

      if (dealerDealAll || _cardViews.Count == 0)
      {
        ClearHandViews();
        SpawnAllHandCards(dealerDealAll);
        AssignRoundSlots();
        hoverFan?.RefreshFromChildren();
        RefreshAffordability();
        return;
      }

      var handSet = new HashSet<ActionCardDefinition>();
      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card != null)
          handSet.Add(card);
      }

      for (int i = _cardViews.Count - 1; i >= 0; i--)
      {
        CardView view = _cardViews[i];
        if (view == null || view.IsPlaying)
          continue;

        if (view.Definition == null || !handSet.Contains(view.Definition))
        {
          view.Clicked -= OnCardClicked;
          DestroyPlayingCardView(view);
          _cardViews.RemoveAt(i);
        }
      }

      var existing = new HashSet<ActionCardDefinition>();
      foreach (CardView view in _cardViews)
      {
        if (view?.Definition != null)
          existing.Add(view.Definition);
      }

      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card == null || existing.Contains(card) || !_deck.IsCardLegalInCurrentRegion(card))
          continue;

        CardView view = SpawnAndSetupCardView(card);
        if (view == null)
          continue;

        _cardViews.Add(view);
        AnimateDealerThrow(view.RectTransform, 0f);
      }

      hoverFan?.SyncCardListOnly();
      RefreshAffordability();
    }

    void SpawnAllHandCards(bool dealerDealAll)
    {
      int dealIndex = 0;
      foreach (ActionCardDefinition card in _deck.Hand)
      {
        if (card == null || !_deck.IsCardLegalInCurrentRegion(card))
          continue;

        CardView view = SpawnAndSetupCardView(card);
        if (view == null)
          continue;

        _cardViews.Add(view);

        if (dealerDealAll && _config != null)
          AnimateDealerThrow(view.RectTransform, dealIndex * AnimDur(_config.cardDealStagger));
        else if (_config != null && view.RectTransform != null)
          AnimateSimpleDrawIn(view.RectTransform);

        dealIndex++;
      }
    }

    CardView SpawnAndSetupCardView(ActionCardDefinition card)
    {
      CardView view = SpawnCardView(card);
      if (view == null)
        return null;

      bool canAfford = _statResolver != null && _statResolver.CanAfford(_deck.GetCardMoneyCost(card));
      view.Setup(card, canAfford);
      view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
      view.Clicked += OnCardClicked;
      return view;
    }

    void AnimateDealerThrow(RectTransform rt, float delay)
    {
      if (rt == null || _config == null)
        return;

      rt.DOKill(true);

      Vector2 restPos = new Vector2(0f, hoverFan != null ? GetHiddenY() : 0f);
      float startRot = Random.Range(-_config.cardDealStartRotation, _config.cardDealStartRotation);
      Vector2 startPos = restPos + _config.cardDealStartOffset;

      rt.anchoredPosition = startPos;
      rt.localRotation = Quaternion.Euler(0f, 0f, startRot);
      rt.localScale = Vector3.one * _config.cardDealStartScale;

      Sequence seq = DOTween.Sequence();
      seq.SetDelay(delay);
      seq.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      float duration = AnimDur(_config.cardDealDuration);
      seq.Append(rt.DOAnchorPos(restPos, duration).SetEase(_config.cardDealEase));
      seq.Join(rt.DOLocalRotate(Vector3.zero, duration, RotateMode.Fast).SetEase(_config.cardDealEase));
      seq.Join(rt.DOScale(1f, duration).SetEase(Ease.OutBack));
    }

    void AnimateDrawFromLeft(CardView view)
    {
      RectTransform rt = view?.RectTransform;
      if (rt == null || _config == null)
      {
        hoverFan?.SetAwaitingDrawnCard(false);
        return;
      }

      rt.DOKill(true);

      if (hoverFan == null || !hoverFan.TryGetLayoutTargetForCard(rt, out Vector2 targetPos, out Vector3 targetRot))
      {
        targetPos = new Vector2(0f, GetHiddenY());
        targetRot = Vector3.zero;
      }

      Vector2 startPos = targetPos + new Vector2(-_config.cardDrawFromLeftOffset, 0f);
      rt.anchoredPosition = startPos;
      rt.localRotation = Quaternion.Euler(0f, 0f, targetRot.z + 12f);
      rt.localScale = Vector3.one * _config.cardDrawFromLeftStartScale;

      float duration = AnimDur(_config.cardDrawFromLeftDuration);
      Sequence seq = DOTween.Sequence();
      seq.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      seq.Append(rt.DOAnchorPos(targetPos, duration).SetEase(_config.cardDrawFromLeftEase));
      seq.Join(rt.DOLocalRotate(targetRot, duration, RotateMode.Fast).SetEase(_config.cardDrawFromLeftEase));
      seq.Join(rt.DOScale(1f, duration).SetEase(Ease.OutBack));
      seq.OnComplete(() =>
      {
        if (hoverFan != null && rt != null)
        {
          hoverFan.EnjoinCardToCurrentLayout(rt, immediate: false, () => hoverFan.SetAwaitingDrawnCard(false));
          return;
        }

        hoverFan?.SetAwaitingDrawnCard(false);
      });
    }

    float GetHorizontalSpacing()
    {
      return hoverFan != null ? hoverFan.HorizontalSpacing : 70f;
    }

    float GetHiddenY()
    {
      return hoverFan != null ? hoverFan.HiddenAnchoredY : -100f;
    }

    void AnimateSimpleDrawIn(RectTransform rt)
    {
      if (rt == null || _config == null)
        return;

      rt.localScale = Vector3.one * _config.cardDrawStartScale;
      rt.DOScale(1f, AnimDur(_config.cardDrawInDuration))
        .SetEase(_config.cardDrawEase)
        .SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
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

    void OpenInspect(CardView view)
    {
      if (view == null || view.RectTransform == null || _config == null)
        return;

      CloseInspectImmediate();

      _inspectedCard = view;
      hoverFan?.SetInspectMode(true);

      RectTransform rt = view.RectTransform;
      _inspectState = new InspectState
      {
        parent = rt.parent,
        siblingIndex = rt.GetSiblingIndex(),
        anchoredPosition = rt.anchoredPosition,
        localEulerAngles = rt.localEulerAngles,
        localScale = rt.localScale
      };

      view.SetDescriptionVisible(true);
      view.SetInteractable(false);

      RectTransform animationRoot = GetPlayAnimationRoot();
      rt.SetParent(animationRoot, true);
      rt.SetAsLastSibling();
      rt.DOKill(true);

      float duration = AnimDur(_config.cardInspectDuration);
      rt.DOAnchorPos(Vector2.zero, duration).SetEase(_config.cardInspectEase).SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      rt.DOScale(_config.cardInspectScale, duration).SetEase(_config.cardInspectEase).SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      rt.DOLocalRotate(Vector3.zero, duration, RotateMode.Fast).SetEase(_config.cardInspectEase).SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
    }

    void CloseInspect()
    {
      if (_inspectedCard == null || _inspectedCard.RectTransform == null)
      {
        CloseInspectImmediate();
        return;
      }

      CardView view = _inspectedCard;
      RectTransform rt = view.RectTransform;
      float duration = _config != null ? AnimDur(_config.cardInspectDuration * 0.85f) : 0.38f;
      Ease ease = _config != null ? _config.cardInspectEase : Ease.InOutCubic;

      rt.DOKill(true);
      rt.DOAnchorPos(_inspectState.anchoredPosition, duration).SetEase(ease).SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      rt.DOScale(_inspectState.localScale, duration).SetEase(ease).SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
      rt.DOLocalRotate(_inspectState.localEulerAngles, duration, RotateMode.Fast).SetEase(ease)
        .SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy)
        .OnComplete(() => FinishCloseInspect(view));
    }

    void FinishCloseInspect(CardView view)
    {
      if (view == null)
      {
        CloseInspectImmediate();
        return;
      }

      RectTransform rt = view.RectTransform;
      if (rt != null && _inspectState.parent != null)
      {
        rt.SetParent(_inspectState.parent, false);
        rt.SetSiblingIndex(_inspectState.siblingIndex);
        rt.anchoredPosition = _inspectState.anchoredPosition;
        rt.localEulerAngles = _inspectState.localEulerAngles;
        rt.localScale = _inspectState.localScale;
      }

      view.SetDescriptionVisible(false);
      view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
      _inspectedCard = null;
      hoverFan?.SetInspectMode(false);
      hoverFan?.RefreshFromChildren();
    }

    void CloseInspectImmediate()
    {
      if (_inspectedCard == null)
        return;

      CardView view = _inspectedCard;
      RectTransform rt = view.RectTransform;
      if (rt != null)
      {
        rt.DOKill(true);
        if (_inspectState.parent != null)
        {
          rt.SetParent(_inspectState.parent, false);
          rt.SetSiblingIndex(_inspectState.siblingIndex);
          rt.anchoredPosition = _inspectState.anchoredPosition;
          rt.localEulerAngles = _inspectState.localEulerAngles;
          rt.localScale = _inspectState.localScale;
        }
      }

      view.SetDescriptionVisible(false);
      view.SetInteractable(_drivingTurn != null && _drivingTurn.CanPlayCards);
      _inspectedCard = null;
      hoverFan?.SetInspectMode(false);
    }

    public void ClearAwaitingDrawnCard()
    {
      hoverFan?.SetAwaitingDrawnCard(false);
    }

    public int ConsumePendingDrawSlot()
    {
      int slot = _pendingDrawSlot;
      _pendingDrawSlot = -1;
      return slot;
    }

    public void AnimateCardPlay(CardView view, System.Action onComplete)
    {
      CloseInspectImmediate();

      if (view == null)
      {
        onComplete?.Invoke();
        return;
      }

      view.Clicked -= OnCardClicked;
      view.SetPlaying(true);
      TriggerCardPlayEffects(view);
      _pendingDrawSlot = view.HandSlot;
      _cardViews.Remove(view);
      hoverFan?.SetAwaitingDrawnCard(true);

      RectTransform rt = view.RectTransform;
      if (rt == null || _config == null)
      {
        hoverFan?.SetAwaitingDrawnCard(false);
        DestroyPlayingCardView(view);
        onComplete?.Invoke();
        hoverFan?.RefreshFromChildren();
        return;
      }

      rt.DOKill(true);

      RectTransform animationRoot = GetPlayAnimationRoot();
      rt.SetParent(animationRoot, true);
      rt.SetAsLastSibling();
      hoverFan?.SyncSlotsAfterRemoval();

      CanvasGroup group = rt.GetComponent<CanvasGroup>();
      if (group == null)
        group = rt.gameObject.AddComponent<CanvasGroup>();

      group.alpha = 1f;
      group.DOKill(true);
      Vector2 center = Vector2.zero;
      float moveDuration = AnimDur(_config.cardPlayMoveToCenterDuration);
      float holdDuration = AnimDur(_config.cardPlayCenterHoldDuration);
      float vanishDuration = AnimDur(_config.cardPlayVanishDuration);
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

    static void TriggerCardPlayEffects(CardView view)
    {
      if (view == null)
        return;

      CardPlayAudioEffect[] audioEffects = view.GetComponents<CardPlayAudioEffect>();
      foreach (CardPlayAudioEffect effect in audioEffects)
        effect.PlayOnCardPlayed();
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
      if (_inspectedCard != null)
        return;

      GameSfxController.TryPlayCardClick();
      _drivingTurn?.TryPlayCard(view);
    }

    public void ClearHandVisuals()
    {
      CloseInspectImmediate();
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
