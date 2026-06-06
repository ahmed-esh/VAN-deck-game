using UnityEngine;
using VanGame.UI;

namespace VanGame.Data
{
  [CreateAssetMenu(fileName = "Deck", menuName = "Van Game/Deck Definition")]
  public class DeckDefinition : ScriptableObject
  {
    public string deckName;

    [Header("Hand")]
    [Min(1)] public int handSize = 8;

    [Header("Card prefabs (recommended)")]
    [Tooltip("Full deck. Same card can appear more than once. Each prefab needs ActionCardPrefab + CardView.")]
    public ActionCardPrefab[] drawPoolPrefabs = System.Array.Empty<ActionCardPrefab>();

    [Header("Fallback (definitions only)")]
    [Tooltip("Used when Draw Pool Prefabs is empty. CardHandController must have a fallback CardView prefab.")]
    public ActionCardDefinition[] drawPoolCards = System.Array.Empty<ActionCardDefinition>();

    [Tooltip("When the draw pool is empty, shuffle discard back into draw.")]
    public bool recycleDiscardWhenEmpty = true;

    [Header("Draw order")]
    [Tooltip("Randomize the full deck when a driving leg (round) begins.")]
    public bool shuffleDrawPoolOnInit = true;

    [Tooltip("Randomize discard when it is shuffled back into the draw pile.")]
    public bool shuffleDiscardOnRecycle = true;

    public bool UsesPrefabDeck => drawPoolPrefabs != null && drawPoolPrefabs.Length > 0;
  }
}
