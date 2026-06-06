using System;
using System.Collections.Generic;
using UnityEngine;
using VanGame.Data;
using VanGame.UI;

namespace VanGame.Core
{
  public class DeckController : MonoBehaviour
  {
    [SerializeField] DeckDefinition deckDefinition;

    readonly List<ActionCardDefinition> _hand = new List<ActionCardDefinition>();
    readonly List<ActionCardDefinition> _drawPool = new List<ActionCardDefinition>();
    readonly List<ActionCardDefinition> _discard = new List<ActionCardDefinition>();
    readonly Dictionary<ActionCardDefinition, ActionCardPrefab> _prefabByDefinition = new Dictionary<ActionCardDefinition, ActionCardPrefab>();

    CityDefinition _currentRegion;

    public event Action HandChanged;

    public IReadOnlyList<ActionCardDefinition> Hand => _hand;
    public CityDefinition CurrentRegion => _currentRegion;
    public int HandSize => deckDefinition != null ? Mathf.Max(1, deckDefinition.handSize) : 8;

    public void Initialize(DeckDefinition deck)
    {
      if (deck != null)
        deckDefinition = deck;

      _hand.Clear();
      _drawPool.Clear();
      _discard.Clear();
      _prefabByDefinition.Clear();

      if (deckDefinition == null)
        return;

      BuildDrawPoolFromDefinition();
      NotifyHandChanged();
    }

    /// <summary>
    /// Shuffles hand + discard back into draw, then deals until the hand is full.
    /// Call at the start of each driving leg.
    /// </summary>
    public void BeginRound()
    {
      if (deckDefinition == null)
        return;

      ReturnHandAndDiscardToDrawPool();

      if (_drawPool.Count == 0)
        BuildDrawPoolFromDefinition();

      if (deckDefinition.shuffleDrawPoolOnInit)
        ShuffleList(_drawPool);

      RefillHandToSize();
      NotifyHandChanged();
    }

    public void SetCurrentRegion(CityDefinition region)
    {
      _currentRegion = region;
      ApplyRegionToHand();
      RefillHandToSize();
      NotifyHandChanged();
    }

    void ApplyRegionToHand()
    {
      for (int i = _hand.Count - 1; i >= 0; i--)
      {
        ActionCardDefinition card = _hand[i];
        if (card == null || IsCardLegalInCurrentRegion(card))
          continue;

        _hand.RemoveAt(i);
        _drawPool.Add(card);
      }
    }

    public bool IsCardLegalInCurrentRegion(ActionCardDefinition card)
    {
      return card != null && card.IsLegalInRegion(_currentRegion);
    }

    void BuildDrawPoolFromDefinition()
    {
      _drawPool.Clear();

      if (deckDefinition.UsesPrefabDeck)
      {
        RegisterPrefabs(deckDefinition.drawPoolPrefabs);
        AddPrefabsToPool(deckDefinition.drawPoolPrefabs);
        return;
      }

      if (deckDefinition.drawPoolCards != null)
        _drawPool.AddRange(deckDefinition.drawPoolCards);
    }

    void ReturnHandAndDiscardToDrawPool()
    {
      _drawPool.AddRange(_hand);
      _drawPool.AddRange(_discard);
      _hand.Clear();
      _discard.Clear();
    }

    void RegisterPrefabs(ActionCardPrefab[] prefabs)
    {
      if (prefabs == null)
        return;

      foreach (ActionCardPrefab prefab in prefabs)
        RegisterCardPrefab(prefab);
    }

    void AddPrefabsToPool(ActionCardPrefab[] prefabs)
    {
      if (prefabs == null)
        return;

      foreach (ActionCardPrefab prefab in prefabs)
        AddCardFromPrefab(prefab, _drawPool);
    }

    static void AddCardFromPrefab(ActionCardPrefab prefab, List<ActionCardDefinition> list)
    {
      if (prefab?.Definition == null)
        return;

      list.Add(prefab.Definition);
    }

    public void RegisterCardPrefab(ActionCardPrefab prefab)
    {
      if (prefab?.Definition == null)
        return;

      _prefabByDefinition[prefab.Definition] = prefab;
    }

    public ActionCardPrefab GetActionCardPrefab(ActionCardDefinition card)
    {
      if (card != null && _prefabByDefinition.TryGetValue(card, out ActionCardPrefab registered) && registered != null)
        return registered;

      return null;
    }

    public CardView GetCardViewPrefab(ActionCardDefinition card)
    {
      return GetActionCardPrefab(card)?.View;
    }

    public int GetCardMoneyCost(ActionCardDefinition card)
    {
      if (card == null)
        return 0;

      return card.moneyCostMax > card.moneyCostMin && card.rollCostOnPlay
        ? card.moneyCostMax
        : card.moneyCostMin;
    }

    public bool TryPlayCard(ActionCardDefinition card, out ActionCardDefinition drawnCard)
    {
      drawnCard = null;

      if (card == null || !_hand.Contains(card) || !IsCardLegalInCurrentRegion(card))
        return false;

      _hand.Remove(card);
      _discard.Add(card);
      RefillHandToSize();

      if (_hand.Count > 0)
        drawnCard = _hand[_hand.Count - 1];

      NotifyHandChanged();
      return true;
    }

    void RefillHandToSize()
    {
      int target = HandSize;
      int safety = Mathf.Max(_drawPool.Count + _discard.Count + target, target) + 8;

      while (_hand.Count < target && safety-- > 0)
      {
        if (!TryDrawOneCardIntoHand())
          break;
      }
    }

    bool TryDrawOneCardIntoHand()
    {
      if (_drawPool.Count == 0 && !TryRecycleDiscard())
        return false;

      int safety = Mathf.Max(_drawPool.Count, 1);
      for (int attempt = 0; attempt < safety; attempt++)
      {
        if (_drawPool.Count == 0 && !TryRecycleDiscard())
          return false;

        ActionCardDefinition next = _drawPool[0];
        _drawPool.RemoveAt(0);

        if (IsCardLegalInCurrentRegion(next))
        {
          _hand.Add(next);
          return true;
        }

        _drawPool.Add(next);
      }

      return false;
    }

    bool TryRecycleDiscard()
    {
      if (deckDefinition == null || !deckDefinition.recycleDiscardWhenEmpty || _discard.Count == 0)
        return false;

      _drawPool.AddRange(_discard);
      _discard.Clear();

      if (deckDefinition.shuffleDiscardOnRecycle)
        ShuffleList(_drawPool);

      return _drawPool.Count > 0;
    }

    void NotifyHandChanged() => HandChanged?.Invoke();

    static void ShuffleList(List<ActionCardDefinition> list)
    {
      for (int i = list.Count - 1; i > 0; i--)
      {
        int swapIndex = UnityEngine.Random.Range(0, i + 1);
        (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
      }
    }
  }
}
