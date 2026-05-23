using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "Ability", menuName = "Van Game/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        public string abilityId;
        public string title;
        [TextArea(2, 5)] public string description;
        public bool isFirstCityReward;
        public AbilityModifier[] modifiers = System.Array.Empty<AbilityModifier>();
    }
}
