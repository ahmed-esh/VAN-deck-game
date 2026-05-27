#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VanGame.Data;
using VanGame.UI;

namespace VanGame.Editor
{
  public static class CardPrefabExampleSetup
  {
    const string ExamplePrefabPath = "Assets/Prefabs/VanGame/Cards/Example_Food_SnackCard.prefab";
    const string SourcePrefabPath = "Assets/Prefabs/VanGame/CardView.prefab";
    const string SnackCardPath = "Assets/Data/Cards/Food_Snack.asset";
    const string MainDeckPath = "Assets/Data/Decks/MainDeck.asset";
    const string SceneExampleName = "Example_Food_SnackCard";

    [MenuItem("Van Game/Cards/Add Example Card Prefab To Scene")]
    public static void AddExampleCardPrefabToScene()
    {
      ActionCardDefinition snack = AssetDatabase.LoadAssetAtPath<ActionCardDefinition>(SnackCardPath);
      if (snack == null)
      {
        Debug.LogError("CardPrefabExampleSetup: Missing " + SnackCardPath);
        return;
      }

      snack.MigrateLegacyEffectsToArray();
      EditorUtility.SetDirty(snack);

      GameObject prefabRoot = EnsureExamplePrefab(snack);
      if (prefabRoot == null)
        return;

      ActionCardPrefab cardPrefab = prefabRoot.GetComponent<ActionCardPrefab>();
      WireMainDeck(cardPrefab);
      AddSceneExample(prefabRoot);
      UpdateCardHandFallback();

      AssetDatabase.SaveAssets();
      EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
      Debug.Log("Example card prefab ready. See inactive '" + SceneExampleName + "' under CardHandArea and MainDeck.startingHandPrefabs[0].");
    }

    static GameObject EnsureExamplePrefab(ActionCardDefinition snack)
    {
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ExamplePrefabPath);
      if (existing != null)
        return existing;

      EnsureFolder("Assets/Prefabs/VanGame/Cards");

      GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
      if (source == null)
      {
        Debug.LogError("CardPrefabExampleSetup: Missing " + SourcePrefabPath);
        return null;
      }

      GameObject instance = Object.Instantiate(source);
      instance.name = "Example_Food_SnackCard";

      ActionCardPrefab linker = instance.GetComponent<ActionCardPrefab>();
      if (linker == null)
        linker = instance.AddComponent<ActionCardPrefab>();

      SerializedObject linkerSo = new SerializedObject(linker);
      linkerSo.FindProperty("definition").objectReferenceValue = snack;
      linkerSo.FindProperty("cardView").objectReferenceValue = instance.GetComponent<CardView>();
      linkerSo.ApplyModifiedProperties();

      PrefabUtility.SaveAsPrefabAsset(instance, ExamplePrefabPath);
      Object.DestroyImmediate(instance);

      return AssetDatabase.LoadAssetAtPath<GameObject>(ExamplePrefabPath);
    }

    static void WireMainDeck(ActionCardPrefab cardPrefab)
    {
      DeckDefinition deck = AssetDatabase.LoadAssetAtPath<DeckDefinition>(MainDeckPath);
      if (deck == null || cardPrefab == null)
        return;

      deck.startingHandPrefabs = new[] { cardPrefab };
      EditorUtility.SetDirty(deck);
    }

    static void AddSceneExample(GameObject prefabAsset)
    {
      CardHandController hand = Object.FindFirstObjectByType<CardHandController>();
      if (hand == null)
      {
        Debug.LogError("CardPrefabExampleSetup: No CardHandController in scene.");
        return;
      }

      Transform parent = hand.transform;

      Transform existing = parent.Find(SceneExampleName);
      if (existing != null)
        Object.DestroyImmediate(existing.gameObject);

      GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
      instance.name = SceneExampleName;
      instance.SetActive(false);

      RectTransform rt = instance.GetComponent<RectTransform>();
      if (rt != null)
      {
        rt.anchoredPosition = new Vector2(-300f, 120f);
        rt.localScale = Vector3.one;
      }
    }

    static void UpdateCardHandFallback()
    {
      CardHandController hand = Object.FindFirstObjectByType<CardHandController>();
      if (hand == null)
        return;

      CardView fallback = AssetDatabase.LoadAssetAtPath<CardView>(SourcePrefabPath);
      SerializedObject handSo = new SerializedObject(hand);
      handSo.FindProperty("fallbackCardPrefab").objectReferenceValue = fallback;
      handSo.ApplyModifiedProperties();
      EditorUtility.SetDirty(hand);
    }

    static void EnsureFolder(string path)
    {
      if (AssetDatabase.IsValidFolder(path))
        return;

      string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
      string name = System.IO.Path.GetFileName(path);
      if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        EnsureFolder(parent);

      AssetDatabase.CreateFolder(parent, name);
    }
  }
}
#endif
