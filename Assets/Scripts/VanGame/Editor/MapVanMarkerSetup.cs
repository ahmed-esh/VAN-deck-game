#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VanGame.UI;

namespace VanGame.Editor
{
  public static class MapVanMarkerSetup
  {
    const string VanSpritePath = "Assets/Visuals/vadfwdwadawn.png";
    const string MarkerName = "MapVanMarker";

    [MenuItem("Van Game/Add Map Van Marker To Scene")]
    public static void AddMapVanMarkerToScene()
    {
      MapController mapController = Object.FindFirstObjectByType<MapController>();
      if (mapController == null)
      {
        Debug.LogError("MapVanMarkerSetup: No MapController found in the open scene.");
        return;
      }

      Transform regionsParent = FindMapRegionsParent();
      if (regionsParent == null)
      {
        Debug.LogError("MapVanMarkerSetup: Could not find MapRegions under the map canvas.");
        return;
      }

      MapVanMarkerView existing = regionsParent.GetComponentInChildren<MapVanMarkerView>(true);
      MapVanMarkerView marker = existing != null ? existing : CreateMarker(regionsParent);

      SerializedObject mapSo = new SerializedObject(mapController);
      mapSo.FindProperty("vanMarker").objectReferenceValue = marker;
      mapSo.ApplyModifiedProperties();

      EditorSceneManager.MarkSceneDirty(mapController.gameObject.scene);
      Debug.Log("Map van marker added and wired to MapController.");
    }

    static Transform FindMapRegionsParent()
    {
      foreach (MapRegionView region in Object.FindObjectsByType<MapRegionView>(FindObjectsSortMode.None))
      {
        if (region != null && region.transform.parent != null)
          return region.transform.parent;
      }

      GameObject regionsGo = GameObject.Find("MapRegions");
      return regionsGo != null ? regionsGo.transform : null;
    }

    static MapVanMarkerView CreateMarker(Transform parent)
    {
      GameObject go = new GameObject(MarkerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapVanMarkerView));
      go.transform.SetParent(parent, false);

      RectTransform rt = go.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 0.5f);
      rt.anchorMax = new Vector2(0.5f, 0.5f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      rt.sizeDelta = new Vector2(64f, 64f);
      rt.anchoredPosition = Vector2.zero;
      rt.SetAsLastSibling();

      Image image = go.GetComponent<Image>();
      image.raycastTarget = false;
      image.preserveAspect = true;
      Sprite vanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VanSpritePath);
      if (vanSprite != null)
        image.sprite = vanSprite;
      else
        Debug.LogWarning("MapVanMarkerSetup: Van sprite not found at " + VanSpritePath + ". Assign a sprite on the MapVanMarker Image.");

      MapVanMarkerView marker = go.GetComponent<MapVanMarkerView>();
      SerializedObject markerSo = new SerializedObject(marker);
      markerSo.FindProperty("rectTransform").objectReferenceValue = rt;
      markerSo.FindProperty("vanImage").objectReferenceValue = image;
      markerSo.ApplyModifiedProperties();

      return marker;
    }
  }
}
#endif
