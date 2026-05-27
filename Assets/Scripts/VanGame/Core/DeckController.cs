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

      if (deckDefinition.UsesPrefabDeck)
      {
        RegisterPrefabs(deckDefinition.startingHandPrefabs);
        RegisterPrefabs(deckDefinition.drawPoolPrefabs);
        AddPrefabsToHand(deckDefinition.startingHandPrefabs);
        AddPrefabsToPool(deckDefinition.drawPoolPrefabs);
      }

      if (deckDefinition.startingHandCards != null)
      {
        foreach (ActionCardDefinition card in deckDefinition.startingHandCards)
        {
          if (card != null && !_hand.Contains(card))
            _hand.Add(card);
        }
      }

      if (_drawPool.Count == 0 && deckDefinition.drawPoolCards != null)
        _drawPool.AddRange(deckDefinition.drawPoolCards);

      if (deckDefinition.shuffleDrawPoolOnInit)
        ShuffleList(_drawPool);

      NotifyHandChanged();
    }

    public void SetCurrentRegion(CityDefinition region)
    {
      _currentRegion = region;
      ApplyRegionToHand();
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
        _drawPool.Insert(0, card);
      }
    }

    public bool IsCardLegalInCurrentRegion(ActionCardDefinition card)
    {
      return card != null && card.IsLegalInRegion(_currentRegion);
    }

    void RegisterPrefabs(ActionCardPrefab[] prefabs)
    {
      if (prefabs == null)
        return;

      foreach (ActionCardPrefab prefab in prefabs)
        RegisterCardPrefab(prefab);
    }

    void AddPrefabsToHand(ActionCardPrefab[] prefabs)
    {
      if (prefabs == null)
        return;

      foreach (ActionCardPrefab prefab in prefabs)
        AddCardFromPrefab(prefab, _hand);
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
      DrawNextCard();

      if (_hand.Count > 0)
        drawnCard = _hand[_hand.Count - 1];

      NotifyHandChanged();
      return true;
    }

    void NotifyHandChanged() => HandChanged?.Invoke();

    void DrawNextCard()
    {
      if (_drawPool.Count == 0 && !TryRecycleDiscard())
        return;

      int safety = _drawPool.Count;
      for (int attempt = 0; attempt < safety; attempt++)
      {
        if (_drawPool.Count == 0 && !TryRecycleDiscard())
          return;

        ActionCardDefinition next = _drawPool[0];
        _drawPool.RemoveAt(0);

        if (IsCardLegalInCurrentRegion(next))
        {
          _hand.Add(next);
          return;
        }

        _drawPool.Add(next);
      }
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
