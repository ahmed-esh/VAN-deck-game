using UnityEngine;
using VanGame.UI;

namespace VanGame.Data
{
  [CreateAssetMenu(fileName = "Deck", menuName = "Van Game/Deck Definition")]
  public class DeckDefinition : ScriptableObject
  {
    public string deckName;

    [Header("Card prefabs (recommended)")]
    [Tooltip("One prefab per card type. Each prefab needs ActionCardPrefab + CardView and a linked ActionCardDefinition.")]
    public ActionCardPrefab[] startingHandPrefabs = System.Array.Empty<ActionCardPrefab>();

    [Tooltip("Draw pile order. Same card can appear more than once.")]
    public ActionCardPrefab[] drawPoolPrefabs = System.Array.Empty<ActionCardPrefab>();

    [Header("Fallback (definitions only)")]
    [Tooltip("Used when prefab lists are empty. CardHandController must have a fallback CardView prefab.")]
    public ActionCardDefinition[] startingHandCards = System.Array.Empty<ActionCardDefinition>();

    public ActionCardDefinition[] drawPoolCards = System.Array.Empty<ActionCardDefinition>();

    [Tooltip("When the draw pool is empty, shuffle discard back into draw.")]
    public bool recycleDiscardWhenEmpty;

    [Header("Draw order")]
    [Tooltip("Randomize draw pile order when the run starts.")]
    public bool shuffleDrawPoolOnInit = true;

    [Tooltip("Randomize discard when it is shuffled back into the draw pile.")]
    public bool shuffleDiscardOnRecycle = true;

    public bool UsesPrefabDeck =>
      (startingHandPrefabs != null && startingHandPrefabs.Length > 0)
      || (drawPoolPrefabs != null && drawPoolPrefabs.Length > 0);
  }
}
