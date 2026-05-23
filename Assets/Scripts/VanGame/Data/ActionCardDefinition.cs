using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "ActionCard", menuName = "Van Game/Action Card")]
    public class ActionCardDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string cardId;
        public string title;
        [TextArea(2, 5)] public string description;
        public CardCategory category = CardCategory.Food;
        public CardTier tier = CardTier.HumbleBeginning;

        [Header("Costs")]
        public int moneyCostMin;
        public int moneyCostMax;
        public bool rollCostOnPlay;

        [Header("Effects")]
        public float moraleDeltaPercent;
        public float fuelDeltaPercent;
        public float vanConditionDelta;
        public float realTimeSeconds;
        public bool countsAsFedToday;

        [Header("Deck building")]
        public bool includeInStartingHand;
        public int duplicateCount = 1;
    }
}
