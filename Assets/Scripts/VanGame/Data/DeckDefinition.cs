using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "Deck", menuName = "Van Game/Deck Definition")]
    public class DeckDefinition : ScriptableObject
    {
        public string deckName;

        [Tooltip("Exact cards the player holds at run start, in order.")]
        public ActionCardDefinition[] startingHandCards = System.Array.Empty<ActionCardDefinition>();

        [Tooltip("Remaining cards drawn sequentially when a card is played.")]
        public ActionCardDefinition[] drawPoolCards = System.Array.Empty<ActionCardDefinition>();

        [Tooltip("When the draw pool is empty, shuffle discard back into draw.")]
        public bool recycleDiscardWhenEmpty;
    }
}
