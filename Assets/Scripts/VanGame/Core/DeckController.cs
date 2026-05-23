using System;
using System.Collections.Generic;
using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class DeckController : MonoBehaviour
  {
    [SerializeField] DeckDefinition deckDefinition;

    readonly List<ActionCardDefinition> _hand = new List<ActionCardDefinition>();
    readonly List<ActionCardDefinition> _drawPool = new List<ActionCardDefinition>();
    readonly List<ActionCardDefinition> _discard = new List<ActionCardDefinition>();

    public event Action HandChanged;

    public IReadOnlyList<ActionCardDefinition> Hand => _hand;

    public void Initialize(DeckDefinition deck)
    {
      if (deck != null)
        deckDefinition = deck;

      _hand.Clear();
      _drawPool.Clear();
      _discard.Clear();

      if (deckDefinition == null)
        return;

      if (deckDefinition.startingHandCards != null)
        _hand.AddRange(deckDefinition.startingHandCards);

      if (deckDefinition.drawPoolCards != null)
        _drawPool.AddRange(deckDefinition.drawPoolCards);

      NotifyHandChanged();
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

      if (card == null || !_hand.Contains(card))
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
      if (_drawPool.Count == 0)
      {
        if (deckDefinition != null && deckDefinition.recycleDiscardWhenEmpty && _discard.Count > 0)
        {
          _drawPool.AddRange(_discard);
          _discard.Clear();
        }
        else
        {
          return;
        }
      }

      ActionCardDefinition next = _drawPool[0];
      _drawPool.RemoveAt(0);
      _hand.Add(next);
    }
  }
}
