using UnityEngine;

namespace VanGame.Data
{
  [CreateAssetMenu(fileName = "AbilityCatalog", menuName = "Van Game/Ability Catalog")]
  public class AbilityCatalog : ScriptableObject
  {
    [Tooltip("Offered on the first city arrival (Selfless / Bargainer / Practitioner).")]
    public AbilityDefinition[] firstCityRewards = System.Array.Empty<AbilityDefinition>();

    [Tooltip("Offered on later city arrivals (excludes abilities already owned).")]
    public AbilityDefinition[] generalPool = System.Array.Empty<AbilityDefinition>();
  }
}
