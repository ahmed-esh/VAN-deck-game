#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VanGame.Data;

namespace VanGame.Editor
{
  public static class ActionCardDefinitionEditor
  {
    [MenuItem("Van Game/Cards/Migrate Legacy Effects On Selected Cards")]
    public static void MigrateLegacyEffectsOnSelection()
    {
      int count = 0;

      foreach (Object obj in Selection.objects)
      {
        if (obj is not ActionCardDefinition card)
          continue;

        if (card.effects != null && card.effects.Length > 0)
          continue;

        card.MigrateLegacyEffectsToArray();
        EditorUtility.SetDirty(card);
        count++;
      }

      AssetDatabase.SaveAssets();
      Debug.Log($"Migrated legacy card effects on {count} ActionCardDefinition asset(s).");
    }
  }
}
#endif
