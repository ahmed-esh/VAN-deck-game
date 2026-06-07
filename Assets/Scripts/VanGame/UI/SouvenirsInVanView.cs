using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;
using VanGame.Visual;

namespace VanGame.UI
{
  public class SouvenirsInVanView : MonoBehaviour
  {
    [SerializeField] Transform vanObjectsRoot;
    [SerializeField] Transform pickObjectsTemplateRoot;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] float vanScale = 0.42f;
    [SerializeField] float slotSpacingX = 50f;
    [SerializeField] Vector2 vanBaseAnchoredPosition = new Vector2(-120f, 40f);

    readonly Dictionary<string, GameObject> _vanObjectsByName = new Dictionary<string, GameObject>(StringComparer.Ordinal);

    RunState _runState;

    void Awake()
    {
      EnsureVanObjectsRoot();
      EnsureDescriptionText();
      BuildVanObjectsIfNeeded();
      HideDescription();
    }

    public void Initialize(RunState runState)
    {
      _runState = runState;
      RefreshOwnedSouvenirs();
    }

    public void OnSouvenirPicked(string objectName)
    {
      if (string.IsNullOrWhiteSpace(objectName))
        return;

      EnsureVanObject(objectName);
      RefreshOwnedSouvenirs();
    }

    void EnsureVanObjectsRoot()
    {
      if (vanObjectsRoot != null)
        return;

      Transform canvasCards = FindCanvasCards();
      Transform found = canvasCards != null ? canvasCards.Find(SouvenirCatalog.VanObjectsName) : null;
      if (found != null)
      {
        vanObjectsRoot = found;
        return;
      }

      GameObject created = new GameObject(SouvenirCatalog.VanObjectsName, typeof(RectTransform));
      RectTransform rect = created.GetComponent<RectTransform>();
      rect.SetParent(canvasCards != null ? canvasCards : transform, false);
      rect.anchorMin = new Vector2(0.5f, 0.5f);
      rect.anchorMax = new Vector2(0.5f, 0.5f);
      rect.pivot = new Vector2(0f, 0f);
      rect.anchoredPosition = vanBaseAnchoredPosition;
      rect.sizeDelta = new Vector2(400f, 120f);
      vanObjectsRoot = rect;
    }

    void EnsureDescriptionText()
    {
      if (descriptionText != null)
        return;

      Transform searchRoot = FindCanvasCards() ?? transform;
      Transform found = searchRoot.Find(SouvenirCatalog.DescriptionTextOnVanName);
      if (found == null)
        found = searchRoot.FindDeepChild(SouvenirCatalog.DescriptionTextOnVanName);

      if (found != null)
        descriptionText = found.GetComponent<TextMeshProUGUI>();
    }

    Transform FindCanvasCards()
    {
      GameObject canvas = GameObject.Find('Canvas_Cards');
      return canvas != null ? canvas.transform : null;
    }

    void BuildVanObjectsIfNeeded()
    {
      if (pickObjectsTemplateRoot == null)
      {
        Transform canvasCards = FindCanvasCards();
        Transform pickScreen = canvasCards != null
          ? canvasCards.Find(SouvenirCatalog.PickScreenRootName)
          : null;
        if (pickScreen != null)
          pickObjectsTemplateRoot = pickScreen.Find(SouvenirCatalog.PickObjectsName);
      }

      if (pickObjectsTemplateRoot == null || vanObjectsRoot == null)
        return;

      for (int i = 0; i < pickObjectsTemplateRoot.childCount; i++)
      {
        Transform template = pickObjectsTemplateRoot.GetChild(i);
        if (template == null || _vanObjectsByName.ContainsKey(template.name))
          continue;

        EnsureVanObject(template.name);
      }
    }

    void EnsureVanObject(string objectName)
    {
      if (string.IsNullOrWhiteSpace(objectName) || vanObjectsRoot == null)
        return;

      if (_vanObjectsByName.TryGetValue(objectName, out GameObject existing) && existing != null)
        return;

      Transform template = FindPickTemplate(objectName);
      if (template == null)
        return;

      GameObject clone = Instantiate(template.gameObject, vanObjectsRoot);
      clone.name = objectName;
      clone.SetActive(false);

      RectTransform rect = clone.transform as RectTransform;
      if (rect != null)
      {
        rect.localScale = Vector3.one * vanScale;
        rect.pivot = new Vector2(0.5f, 0f);
      }

      Image image = clone.GetComponent<Image>();
      if (image != null)
        image.raycastTarget = true;

      SouvenirVanItem item = clone.GetComponent<SouvenirVanItem>();
      if (item == null)
        item = clone.AddComponent<SouvenirVanItem>();

      item.Configure(objectName);
      item.Hovered -= OnVanItemHovered;
      item.Unhovered -= OnVanItemUnhovered;
      item.Hovered += OnVanItemHovered;
      item.Unhovered += OnVanItemUnhovered;

      if (clone.GetComponent<SouvenirVanShake2D>() == null)
        clone.AddComponent<SouvenirVanShake2D>();

      _vanObjectsByName[objectName] = clone;
    }

    Transform FindPickTemplate(string objectName)
    {
      if (pickObjectsTemplateRoot == null)
        return null;

      Transform found = pickObjectsTemplateRoot.Find(objectName);
      return found;
    }

    public void RefreshOwnedSouvenirs()
    {
      if (_runState == null || vanObjectsRoot == null)
        return;

      int slot = 0;
      foreach (string objectName in _runState.OwnedSouvenirIds)
      {
        EnsureVanObject(objectName);
        if (!_vanObjectsByName.TryGetValue(objectName, out GameObject vanObject) || vanObject == null)
          continue;

        vanObject.SetActive(true);
        RectTransform rect = vanObject.transform as RectTransform;
        if (rect != null)
          rect.anchoredPosition = new Vector2(slot * slotSpacingX, 0f);

        SouvenirVanShake2D shake = vanObject.GetComponent<SouvenirVanShake2D>();
        shake?.StartShake();
        slot++;
      }
    }

    void OnVanItemHovered(SouvenirVanItem item)
    {
      if (item == null || _runState == null || descriptionText == null)
        return;

      SouvenirRewardInfo info = SouvenirCatalog.GetInfo(_runState, item.SouvenirObjectName);
      descriptionText.text = info.FunctionText;
      descriptionText.gameObject.SetActive(true);
    }

    void OnVanItemUnhovered(SouvenirVanItem item) => HideDescription();

    void HideDescription()
    {
      if (descriptionText == null)
        return;

      descriptionText.text = string.Empty;
      descriptionText.gameObject.SetActive(false);
    }
  }

  static class TransformExtensions
  {
    public static Transform FindDeepChild(this Transform parent, string childName)
    {
      if (parent == null)
        return null;

      for (int i = 0; i < parent.childCount; i++)
      {
        Transform child = parent.GetChild(i);
        if (child.name == childName)
          return child;

        Transform nested = child.FindDeepChild(childName);
        if (nested != null)
          return nested;
      }

      return null;
    }
  }
}
