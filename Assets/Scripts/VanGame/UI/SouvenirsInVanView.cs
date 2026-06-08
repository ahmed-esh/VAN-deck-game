using System;
using System.Collections.Generic;
using DG.Tweening;
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
    [SerializeField] Image descriptionBackdropImage;
    [Header("Van souvenir slots")]
    [SerializeField] RectTransform[] souvenirSlotPositions = Array.Empty<RectTransform>();
    [SerializeField] Vector2 vanBaseAnchoredPosition = new Vector2(-120f, 40f);
    [Header("Description backdrop animation")]
    [SerializeField] float descriptionShowDuration = 0.34f;
    [SerializeField] float descriptionHideDuration = 0.24f;
    [SerializeField] float descriptionShowStartScale = 0.86f;
    [SerializeField] float descriptionShowYOffset = -8f;
    [SerializeField] Ease descriptionShowEase = Ease.OutBack;
    [SerializeField] Ease descriptionHideEase = Ease.InCubic;

    readonly Dictionary<string, GameObject> _vanObjectsByName = new Dictionary<string, GameObject>(StringComparer.Ordinal);

    RunState _runState;
    GameFlowController _gameFlow;
    SouvenirRewardResolver _souvenirRewards;
    RectTransform _descriptionBackdropRect;
    CanvasGroup _descriptionBackdropGroup;
    Vector3 _descriptionBackdropRestScale = Vector3.one;
    Vector2 _descriptionBackdropRestPosition;
    float _descriptionBackdropRestAlpha = 1f;
    Tween _descriptionTween;
    SouvenirVanItem _hoveredVanItem;
    bool _descriptionVisible;
    int _pendingHideDescriptionFrame = -1;

    void Awake()
    {
      if (_gameFlow == null)
        _gameFlow = FindFirstObjectByType<GameFlowController>();

      if (_souvenirRewards == null)
        _souvenirRewards = FindFirstObjectByType<SouvenirRewardResolver>();

      EnsureVanObjectsRoot();
      EnsureDescriptionText();
      CacheDescriptionBackdropTargets();
      RegisterPreplacedVanObjects();
      HideDescription(immediate: true);
    }

    void OnDisable()
    {
      if (_runState != null)
        _runState.PhaseChanged -= OnRunPhaseChanged;

      KillDescriptionTween();
      _hoveredVanItem = null;
      _descriptionVisible = false;
      _pendingHideDescriptionFrame = -1;
    }

    void LateUpdate()
    {
      if (_pendingHideDescriptionFrame < 0)
        return;

      if (Time.frameCount <= _pendingHideDescriptionFrame)
        return;

      _pendingHideDescriptionFrame = -1;

      if (_hoveredVanItem != null)
        return;

      _descriptionVisible = false;
      HideDescription(immediate: false);
    }

    public void Initialize(RunState runState)
    {
      if (_runState != null)
        _runState.PhaseChanged -= OnRunPhaseChanged;

      _runState = runState;

      if (_runState != null)
        _runState.PhaseChanged += OnRunPhaseChanged;

      RefreshOwnedSouvenirs();
    }

    void OnRunPhaseChanged()
    {
      RefreshVanSelectionVisuals();
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
      Transform found = FindVanObjectsRoot(canvasCards);
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

    static Transform FindVanObjectsRoot(Transform canvasCards)
    {
      if (canvasCards == null)
        return null;

      Transform found = canvasCards.Find(SouvenirCatalog.VanObjectsName);
      if (found != null)
        return found;

      for (int i = 0; i < canvasCards.childCount; i++)
      {
        Transform child = canvasCards.GetChild(i);
        if (child.name.Trim() == SouvenirCatalog.VanObjectsName.Trim())
          return child;
      }

      return canvasCards.FindDeepChild(SouvenirCatalog.VanObjectsName.Trim());
    }

    void EnsureDescriptionText()
    {
      if (descriptionText != null)
        return;

      if (vanObjectsRoot != null)
      {
        Transform found = vanObjectsRoot.Find(SouvenirCatalog.DescriptionTextOnVanName);
        if (found != null)
        {
          descriptionText = found.GetComponent<TextMeshProUGUI>();
          return;
        }
      }

      Transform searchRoot = FindCanvasCards() ?? transform;
      Transform foundInCanvas = searchRoot.Find(SouvenirCatalog.DescriptionTextOnVanName);
      if (foundInCanvas == null)
        foundInCanvas = searchRoot.FindDeepChild(SouvenirCatalog.DescriptionTextOnVanName);

      if (foundInCanvas != null)
        descriptionText = foundInCanvas.GetComponent<TextMeshProUGUI>();
    }

    void EnsureDescriptionBackdrop()
    {
      if (descriptionBackdropImage != null)
        return;

      if (vanObjectsRoot != null)
      {
        Transform found = vanObjectsRoot.Find(SouvenirCatalog.TextHolderOnVanName);
        if (found == null)
          found = vanObjectsRoot.FindDeepChild(SouvenirCatalog.TextHolderOnVanName.Trim());

        if (found != null)
          descriptionBackdropImage = found.GetComponent<Image>();
      }
    }

    void CacheDescriptionBackdropTargets()
    {
      EnsureDescriptionBackdrop();

      if (descriptionBackdropImage == null)
        return;

      _descriptionBackdropRect = descriptionBackdropImage.rectTransform;
      _descriptionBackdropGroup = descriptionBackdropImage.GetComponent<CanvasGroup>();
      if (_descriptionBackdropGroup == null)
        _descriptionBackdropGroup = descriptionBackdropImage.gameObject.AddComponent<CanvasGroup>();

      _descriptionBackdropRestScale = _descriptionBackdropRect != null
        ? _descriptionBackdropRect.localScale
        : Vector3.one;
      _descriptionBackdropRestPosition = _descriptionBackdropRect != null
        ? _descriptionBackdropRect.anchoredPosition
        : Vector2.zero;
      _descriptionBackdropRestAlpha = _descriptionBackdropGroup.alpha > 0.01f
        ? _descriptionBackdropGroup.alpha
        : 1f;
    }

    Transform FindCanvasCards()
    {
      GameObject canvas = GameObject.Find("Canvas_Cards");
      return canvas != null ? canvas.transform : null;
    }

    void RegisterPreplacedVanObjects()
    {
      if (vanObjectsRoot == null)
        return;

      for (int i = 0; i < vanObjectsRoot.childCount; i++)
      {
        Transform child = vanObjectsRoot.GetChild(i);
        if (child == null
          || IsDescriptionTextObject(child)
          || IsDescriptionBackdropObject(child)
          || IsTextHolderObject(child)
          || IsSlotPositionObject(child))
          continue;

        RegisterVanObject(child.gameObject);
      }
    }

    bool IsDescriptionBackdropObject(Transform child)
    {
      return descriptionBackdropImage != null && child.gameObject == descriptionBackdropImage.gameObject;
    }

    bool IsSlotPositionObject(Transform child)
    {
      if (child == null || souvenirSlotPositions == null)
        return false;

      for (int i = 0; i < souvenirSlotPositions.Length; i++)
      {
        RectTransform slot = souvenirSlotPositions[i];
        if (slot != null && child == slot)
          return true;
      }

      return false;
    }

    static bool IsDescriptionTextObject(Transform child)
    {
      return child.name == SouvenirCatalog.DescriptionTextOnVanName;
    }

    bool IsTextHolderObject(Transform child)
    {
      if (child == null)
        return false;

      if (child.name == SouvenirCatalog.TextHolderOnVanName
        || child.name.Trim() == SouvenirCatalog.TextHolderOnVanName.Trim())
        return true;

      return descriptionBackdropImage != null && child.gameObject == descriptionBackdropImage.gameObject;
    }

    void RegisterVanObject(GameObject vanObject)
    {
      if (vanObject == null)
        return;

      string objectName = vanObject.name;
      _vanObjectsByName[objectName] = vanObject;
      vanObject.SetActive(false);

      Image image = vanObject.GetComponent<Image>();
      if (image != null)
        image.raycastTarget = true;

      SouvenirVanItem item = vanObject.GetComponent<SouvenirVanItem>();
      if (item == null)
        item = vanObject.AddComponent<SouvenirVanItem>();

      item.Configure(objectName);
      item.Hovered -= OnVanItemHovered;
      item.Unhovered -= OnVanItemUnhovered;
      item.Clicked -= OnVanItemClicked;
      item.Hovered += OnVanItemHovered;
      item.Unhovered += OnVanItemUnhovered;
      item.Clicked += OnVanItemClicked;

      if (vanObject.GetComponent<SouvenirVanShake2D>() == null)
        vanObject.AddComponent<SouvenirVanShake2D>();
    }

    void EnsureVanObject(string objectName)
    {
      if (string.IsNullOrWhiteSpace(objectName) || vanObjectsRoot == null)
        return;

      if (_vanObjectsByName.TryGetValue(objectName, out GameObject existing) && existing != null)
        return;

      Transform preplaced = vanObjectsRoot.Find(objectName);
      if (preplaced != null)
      {
        RegisterVanObject(preplaced.gameObject);
        return;
      }

      Transform template = FindPickTemplate(objectName);
      if (template == null)
        return;

      GameObject clone = Instantiate(template.gameObject, vanObjectsRoot);
      clone.name = objectName;
      RegisterVanObject(clone);
    }

    Transform FindPickTemplate(string objectName)
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

      if (pickObjectsTemplateRoot == null)
        return null;

      return pickObjectsTemplateRoot.Find(objectName);
    }

    public void RefreshOwnedSouvenirs()
    {
      if (_runState == null || vanObjectsRoot == null)
        return;

      foreach (KeyValuePair<string, GameObject> entry in _vanObjectsByName)
      {
        if (entry.Value != null)
          entry.Value.SetActive(false);
      }

      int slot = 0;
      foreach (string objectName in _runState.OwnedSouvenirIds)
      {
        EnsureVanObject(objectName);
        if (!_vanObjectsByName.TryGetValue(objectName, out GameObject vanObject) || vanObject == null)
          continue;

        vanObject.SetActive(true);
        PlaceSouvenirAtSlot(vanObject.transform as RectTransform, slot);

        SouvenirVanShake2D shake = vanObject.GetComponent<SouvenirVanShake2D>();
        shake?.StartShake();
        slot++;
      }

      EnsureActiveSouvenirSelection();
      RefreshVanSelectionVisuals();
    }

    void EnsureActiveSouvenirSelection()
    {
      if (_runState == null || _runState.OwnedSouvenirIds.Count == 0)
      {
        _runState.ActiveSouvenirId = null;
        return;
      }

      if (_runState.OwnedSouvenirIds.Count == 1)
      {
        _runState.ActiveSouvenirId = _runState.OwnedSouvenirIds[0];
        return;
      }

      if (string.IsNullOrWhiteSpace(_runState.ActiveSouvenirId)
        || !_runState.OwnedSouvenirIds.Contains(_runState.ActiveSouvenirId))
        _runState.ActiveSouvenirId = null;
    }

    void RefreshVanSelectionVisuals()
    {
      bool canSelect = CanSelectSouvenirsDuringDriving();

      foreach (KeyValuePair<string, GameObject> entry in _vanObjectsByName)
      {
        GameObject vanObject = entry.Value;
        if (vanObject == null || !vanObject.activeSelf)
          continue;

        SouvenirVanItem item = vanObject.GetComponent<SouvenirVanItem>();
        if (item == null)
          continue;

        bool isOwned = _runState != null && _runState.OwnedSouvenirIds.Contains(entry.Key);
        bool isSelected = isOwned
          && _runState != null
          && entry.Key == _runState.ActiveSouvenirId;

        item.SetSelectionEnabled(canSelect && isOwned);
        item.SetSelected(isSelected);
      }
    }

    bool CanSelectSouvenirsDuringDriving()
    {
      if (_gameFlow?.RunState == null)
        return false;

      return _gameFlow.RunState.Phase == GamePhase.Driving;
    }

    void PlaceSouvenirAtSlot(RectTransform souvenirRect, int slotIndex)
    {
      if (souvenirRect == null)
        return;

      RectTransform slotRect = GetSlotRect(slotIndex);
      if (slotRect == null)
        return;

      if (souvenirRect.parent == slotRect.parent)
        souvenirRect.anchoredPosition = slotRect.anchoredPosition;
      else
        souvenirRect.position = slotRect.position;
    }

    RectTransform GetSlotRect(int slotIndex)
    {
      if (souvenirSlotPositions == null || slotIndex < 0 || slotIndex >= souvenirSlotPositions.Length)
        return null;

      return souvenirSlotPositions[slotIndex];
    }

    void OnVanItemHovered(SouvenirVanItem item)
    {
      if (item == null || _runState == null)
        return;

      _pendingHideDescriptionFrame = -1;
      _hoveredVanItem = item;

      SouvenirRewardInfo info = SouvenirCatalog.GetInfo(_runState, item.SouvenirObjectName);

      if (descriptionText != null)
      {
        descriptionText.text = info.FunctionText;
        descriptionText.gameObject.SetActive(true);
      }

      if (_descriptionVisible)
        return;

      _descriptionVisible = true;
      ShowDescriptionBackdrop();
    }

    void OnVanItemUnhovered(SouvenirVanItem item)
    {
      if (item == null || _hoveredVanItem != item)
        return;

      _hoveredVanItem = null;
      _pendingHideDescriptionFrame = Time.frameCount;
    }

    void OnVanItemClicked(SouvenirVanItem item)
    {
      if (item == null || _runState == null || !CanSelectSouvenirsDuringDriving())
        return;

      if (!_runState.OwnedSouvenirIds.Contains(item.SouvenirObjectName))
        return;

      _runState.ActiveSouvenirId = item.SouvenirObjectName;
      RefreshVanSelectionVisuals();
      _souvenirRewards?.RefreshActiveSouvenirState();
    }

    void ShowDescriptionBackdrop()
    {
      if (descriptionBackdropImage == null)
        return;

      CacheDescriptionBackdropTargets();
      KillDescriptionTween();

      descriptionBackdropImage.gameObject.SetActive(true);
      _descriptionBackdropGroup.alpha = 0f;
      _descriptionBackdropRect.localScale = _descriptionBackdropRestScale * descriptionShowStartScale;
      _descriptionBackdropRect.anchoredPosition = _descriptionBackdropRestPosition + new Vector2(0f, descriptionShowYOffset);

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(descriptionBackdropImage.gameObject, LinkBehaviour.KillOnDisable);
      sequence.Join(
        _descriptionBackdropGroup.DOFade(_descriptionBackdropRestAlpha, descriptionShowDuration)
          .SetEase(descriptionShowEase));
      sequence.Join(
        _descriptionBackdropRect.DOScale(_descriptionBackdropRestScale, descriptionShowDuration)
          .SetEase(descriptionShowEase));
      sequence.Join(
        _descriptionBackdropRect.DOAnchorPos(_descriptionBackdropRestPosition, descriptionShowDuration)
          .SetEase(descriptionShowEase));

      _descriptionTween = sequence;
    }

    void HideDescription(bool immediate)
    {
      KillDescriptionTween();

      if (descriptionText != null)
      {
        descriptionText.text = string.Empty;
        descriptionText.gameObject.SetActive(false);
      }

      if (descriptionBackdropImage == null)
        return;

      if (immediate || !descriptionBackdropImage.gameObject.activeInHierarchy)
      {
        ResetDescriptionBackdropInstant();
        return;
      }

      CacheDescriptionBackdropTargets();

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(descriptionBackdropImage.gameObject, LinkBehaviour.KillOnDisable);
      sequence.Join(
        _descriptionBackdropGroup.DOFade(0f, descriptionHideDuration)
          .SetEase(descriptionHideEase));
      sequence.Join(
        _descriptionBackdropRect.DOScale(_descriptionBackdropRestScale * descriptionShowStartScale, descriptionHideDuration)
          .SetEase(descriptionHideEase));
      sequence.Join(
        _descriptionBackdropRect.DOAnchorPos(
          _descriptionBackdropRestPosition + new Vector2(0f, descriptionShowYOffset),
          descriptionHideDuration)
          .SetEase(descriptionHideEase));
      sequence.OnComplete(ResetDescriptionBackdropInstant);

      _descriptionTween = sequence;
    }

    void ResetDescriptionBackdropInstant()
    {
      _descriptionVisible = false;

      if (descriptionBackdropImage == null)
        return;

      descriptionBackdropImage.gameObject.SetActive(false);

      if (_descriptionBackdropGroup != null)
        _descriptionBackdropGroup.alpha = 0f;

      if (_descriptionBackdropRect != null)
      {
        _descriptionBackdropRect.localScale = _descriptionBackdropRestScale;
        _descriptionBackdropRect.anchoredPosition = _descriptionBackdropRestPosition;
      }
    }

    void KillDescriptionTween()
    {
      if (_descriptionTween != null && _descriptionTween.IsActive())
        _descriptionTween.Kill();

      _descriptionTween = null;

      if (_descriptionBackdropGroup != null)
        _descriptionBackdropGroup.DOKill();

      if (_descriptionBackdropRect != null)
        _descriptionBackdropRect.DOKill();
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
        if (child.name == childName || child.name.Trim() == childName.Trim())
          return child;

        Transform nested = child.FindDeepChild(childName);
        if (nested != null)
          return nested;
      }

      return null;
    }
  }
}
