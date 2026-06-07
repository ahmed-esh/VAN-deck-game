using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class SouvenirPickController : MonoBehaviour
  {
    [SerializeField] GameObject root;
    [SerializeField] Transform pickObjectsRoot;
    [SerializeField] TextMeshProUGUI lineText;
    [SerializeField] Image textLeft;
    [SerializeField] Image textCenter;
    [SerializeField] Image textRight;
    [SerializeField] float textFadeDuration = 0.2f;
    [SerializeField] float textActiveScale = 1.04f;

    readonly List<SouvenirPickItem> _activeItems = new List<SouvenirPickItem>();
    readonly Dictionary<string, GameObject> _pickObjectsByName = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    readonly Image[] _textSlots = new Image[3];

    Action<string> _onPicked;
    RunState _runState;
    int _hoveredSlotIndex = -1;
    bool _isInitialized;

    void Awake()
    {
      if (root == null)
        root = gameObject;
    }

    void EnsureInitialized()
    {
      if (_isInitialized)
        return;

      _isInitialized = true;
      ResolveReferences();
      CachePickObjects();
      _textSlots[0] = textLeft;
      _textSlots[1] = textCenter;
      _textSlots[2] = textRight;
    }

    void ResolveReferences()
    {
      if (root == null)
        root = gameObject;

      if (pickObjectsRoot == null && root != null)
      {
        Transform found = root.transform.Find(SouvenirCatalog.PickObjectsName);
        if (found != null)
          pickObjectsRoot = found;
      }

      if (lineText == null && root != null)
      {
        Transform line = root.transform.Find(SouvenirCatalog.LineTextName);
        if (line != null)
          lineText = line.GetComponent<TextMeshProUGUI>();
      }

      if (textLeft == null && root != null)
      {
        Transform found = root.transform.Find("text left");
        if (found != null)
          textLeft = found.GetComponent<Image>();
      }

      if (textCenter == null && root != null)
      {
        Transform found = root.transform.Find("text center");
        if (found != null)
          textCenter = found.GetComponent<Image>();
      }

      if (textRight == null && root != null)
      {
        Transform found = root.transform.Find("text right");
        if (found != null)
          textRight = found.GetComponent<Image>();
      }
    }

    void CachePickObjects()
    {
      _pickObjectsByName.Clear();

      Transform container = pickObjectsRoot;
      if (container == null && root != null)
      {
        Transform found = root.transform.Find(SouvenirCatalog.PickObjectsName);
        if (found != null)
          container = found;
      }

      if (container == null)
        return;

      for (int i = 0; i < container.childCount; i++)
      {
        Transform child = container.GetChild(i);
        if (child == null)
          continue;

        _pickObjectsByName[child.name] = child.gameObject;
        child.gameObject.SetActive(false);
      }
    }

    public void ShowOffer(RunState runState, CityDefinition city, Action<string> onPicked)
    {
      EnsureInitialized();

      _runState = runState;
      _onPicked = onPicked;
      ClearActiveItems();
      _hoveredSlotIndex = -1;

      if (root != null)
        root.SetActive(true);
      else
        gameObject.SetActive(true);

      string[] offerNames = SouvenirCatalog.GetSouvenirObjectNamesForCity(city);
      for (int i = 0; i < offerNames.Length; i++)
      {
        string objectName = offerNames[i];
        if (!_pickObjectsByName.TryGetValue(objectName, out GameObject source) || source == null)
          continue;

        source.SetActive(true);
        SouvenirPickItem item = EnsurePickItem(source, i);
        item.Configure(objectName, i);
        item.SetInteractable(true);
        item.Hovered -= OnItemHovered;
        item.Unhovered -= OnItemUnhovered;
        item.Clicked -= OnItemClicked;
        item.Hovered += OnItemHovered;
        item.Unhovered += OnItemUnhovered;
        item.Clicked += OnItemClicked;
        _activeItems.Add(item);
      }

      SetLineText(null);
      ResetTextSlots();
    }

    SouvenirPickItem EnsurePickItem(GameObject source, int slotIndex)
    {
      SouvenirPickItem existing = source.GetComponent<SouvenirPickItem>();
      if (existing != null)
        return existing;

      SouvenirPickItem created = source.AddComponent<SouvenirPickItem>();
      created.Configure(source.name, slotIndex);
      return created;
    }

    void OnItemHovered(SouvenirPickItem item)
    {
      if (item == null)
        return;

      _hoveredSlotIndex = item.SlotIndex;
      SouvenirRewardInfo info = SouvenirCatalog.GetInfo(_runState, item.SouvenirObjectName);
      SetLineText(info.GetDisplayText(includeFlavor: true));
      AnimateTextSlot(item.SlotIndex);
    }

    void OnItemUnhovered(SouvenirPickItem item)
    {
      if (item == null || item.SlotIndex != _hoveredSlotIndex)
        return;

      _hoveredSlotIndex = -1;
      SetLineText(null);
      ResetTextSlots();
    }

    void OnItemClicked(SouvenirPickItem item)
    {
      if (item == null)
        return;

      foreach (SouvenirPickItem active in _activeItems)
        active.SetInteractable(false);

      string picked = item.SouvenirObjectName;
      Hide(immediate: false);
      _onPicked?.Invoke(picked);
      _onPicked = null;
    }

    void SetLineText(string text)
    {
      if (lineText == null)
        return;

      lineText.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }

    void AnimateTextSlot(int slotIndex)
    {
      for (int i = 0; i < _textSlots.Length; i++)
      {
        Image slot = _textSlots[i];
        if (slot == null)
          continue;

        bool active = i == slotIndex;
        slot.gameObject.SetActive(active);
        slot.transform.DOKill();
        slot.transform.localScale = active ? Vector3.one * textActiveScale : Vector3.one;

        if (active)
        {
          slot.transform
            .DOScale(1f, textFadeDuration)
            .SetEase(Ease.OutBack)
            .SetLink(slot.gameObject, LinkBehaviour.KillOnDisable);
        }
      }
    }

    void ResetTextSlots()
    {
      for (int i = 0; i < _textSlots.Length; i++)
      {
        Image slot = _textSlots[i];
        if (slot == null)
          continue;

        slot.transform.DOKill();
        slot.gameObject.SetActive(false);
        slot.transform.localScale = Vector3.one;
      }
    }

    public void Hide(bool immediate)
    {
      if (root != null)
        root.SetActive(false);
      else
        gameObject.SetActive(false);

      ClearActiveItems();
      SetLineText(null);
      ResetTextSlots();
    }

    void ClearActiveItems()
    {
      foreach (SouvenirPickItem item in _activeItems)
      {
        if (item == null)
          continue;

        item.Hovered -= OnItemHovered;
        item.Unhovered -= OnItemUnhovered;
        item.Clicked -= OnItemClicked;
        item.ResetLift();

        if (_pickObjectsByName.TryGetValue(item.SouvenirObjectName, out GameObject source) && source != null)
          source.SetActive(false);
      }

      _activeItems.Clear();
    }

    void OnDisable()
    {
      foreach (Image slot in _textSlots)
      {
        if (slot != null)
          slot.transform.DOKill();
      }
    }
  }
}
