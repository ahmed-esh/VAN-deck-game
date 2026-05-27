using UnityEngine;
using VanGame.Data;

namespace VanGame.UI
{
  /// <summary>
  /// Attach to each card prefab. Assign the ActionCardDefinition that describes gameplay.
  /// CardView on the same object handles display and clicks.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(CardView))]
  public class ActionCardPrefab : MonoBehaviour
  {
    [SerializeField] ActionCardDefinition definition;
    [SerializeField] CardView cardView;

    public ActionCardDefinition Definition => definition;
    public CardView View => cardView != null ? cardView : (cardView = GetComponent<CardView>());

    void Reset()
    {
      cardView = GetComponent<CardView>();
    }

    void OnValidate()
    {
      if (cardView == null)
        cardView = GetComponent<CardView>();
    }
  }
}
