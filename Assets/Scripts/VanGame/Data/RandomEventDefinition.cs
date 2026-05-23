using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "RandomEvent", menuName = "Van Game/Random Event")]
    public class RandomEventDefinition : ScriptableObject
    {
        public string eventId;
        public string title;
        [TextArea(2, 5)] public string logText;

        [Header("Conditions (optional filters)")]
        public bool requireParkingMatch;
        public ParkingType requiredParking;
        public bool requireCostOfLivingMatch;
        public CostOfLiving requiredCostOfLiving;

        [Header("Effects")]
        public int moneyDelta;
        public float moraleDeltaPercent;
        public float fuelDeltaPercent;
        public float vanConditionDelta;
        public int extraDaysAdded;
    }
}
