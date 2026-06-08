using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Audio;
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
    [SerializeField] float showDuration = 0.45f;
    [SerializeField] float hideDuration = 0.32f;
    [SerializeField] float showStartScale = 0.9f;
    [SerializeField] float souvenirInDuration = 0.38f;
    [SerializeField] float souvenirInStagger = 0.1f;
    [SerializeField] float souvenirInStartScale = 0.65f;
    [SerializeField] Ease showEase = Ease.OutCubic;
    [SerializeField] Ease hideEase = Ease.InCubic;
    [SerializeField] Ease souvenirInEase = Ease.OutBack;

    readonly List<SouvenirPickItem> _activeItems = new List<SouvenirPickItem>();
    readonly Dictionary<string, GameObject> _pickObjectsByName = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    readonly Image[] _textSlots = new Image[3];

    Action<string> _onPicked;
    RunState _runState;
    int _hoveredSlotIndex = -1;
    bool _isInitialized;
    CanvasGroup _canvasGroup;
    RectTransform _rootRect;
    Vector3 _restScale = Vector3.one;
    Tween _showHideTween;

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

      CacheRootAnimationTargets();
    }

    void CacheRootAnimationTargets()
    {
      if (root == null)
        return;

      _rootRect = root.transform as RectTransform;
      _canvasGroup = root.GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = root.AddComponent<CanvasGroup>();

      _restScale = _rootRect != null ? _rootRect.localScale : Vector3.one;
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

      SouvenirCatalog.BuildOfferForCity(_runState, city);

      string[] offerNames = SouvenirCatalog.GetSouvenirObjectNamesForCity(city);
      for (int i = 0; i < offerNames.Length; i++)
      {
        string objectName = offerNames[i];
        if (!_pickObjectsByName.TryGetValue(objectName, out GameObject source) || source == null)
          continue;

        source.SetActive(true);
        SouvenirPickItem item = EnsurePickItem(source, i);
        item.Configure(objectName, i);
        item.SetInteractable(false);
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
      GameSfxController.TryPlaySouvenirsPopup();
      PlayShowAnimation();
    }

    void PlayShowAnimation()
    {
      KillShowHideTween();
      CacheRootAnimationTargets();

      if (_canvasGroup != null)
      {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
      }

      if (_rootRect != null)
        _rootRect.localScale = _restScale * showStartScale;

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(root, LinkBehaviour.KillOnDisable);

      if (_canvasGroup != null)
      {
        sequence.Join(
          _canvasGroup.DOFade(1f, showDuration)
            .SetEase(showEase));
      }

      if (_rootRect != null)
      {
        sequence.Join(
          _rootRect.DOScale(_restScale, showDuration)
            .SetEase(Ease.OutBack));
      }

      for (int i = 0; i < _activeItems.Count; i++)
      {
        SouvenirPickItem item = _activeItems[i];
        if (item == null)
          continue;

        RectTransform itemRect = item.transform as RectTransform;
        if (itemRect == null)
          continue;

        Vector3 itemRestScale = itemRect.localScale;
        itemRect.localScale = itemRestScale * souvenirInStartScale;

        float delay = showDuration * 0.35f + i * souvenirInStagger;
        sequence.Insert(
          delay,
          itemRect.DOScale(itemRestScale, souvenirInDuration)
            .SetEase(souvenirInEase)
            .OnComplete(() => item.SetInteractable(true)));
      }

      sequence.OnComplete(EnableInteraction);
      _showHideTween = sequence;
    }

    void EnableInteraction()
    {
      if (_canvasGroup == null)
        return;

      _canvasGroup.interactable = true;
      _canvasGroup.blocksRaycasts = true;
    }

    void PlayHideAnimation(Action onComplete)
    {
      KillShowHideTween();
      CacheRootAnimationTargets();

      if (_canvasGroup == null && _rootRect == null)
      {
        DeactivateRoot();
        onComplete?.Invoke();
        return;
      }

      if (_canvasGroup != null)
      {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
      }

      Sequence sequence = DOTween.Sequence();
      sequence.SetLink(root, LinkBehaviour.KillOnDisable);

      if (_canvasGroup != null)
      {
        sequence.Join(
          _canvasGroup.DOFade(0f, hideDuration)
            .SetEase(hideEase));
      }

      if (_rootRect != null)
      {
        sequence.Join(
          _rootRect.DOScale(_restScale * showStartScale, hideDuration)
            .SetEase(hideEase));
      }

      sequence.OnComplete(() =>
      {
        DeactivateRoot();
        onComplete?.Invoke();
      });

      _showHideTween = sequence;
    }

    void KillShowHideTween()
    {
      if (_showHideTween != null && _showHideTween.IsActive())
        _showHideTween.Kill();

      _showHideTween = null;

      if (_canvasGroup != null)
        _canvasGroup.DOKill();

      if (_rootRect != null)
        _rootRect.DOKill();
    }

    void DeactivateRoot()
    {
      if (root != null)
        root.SetActive(false);
      else
        gameObject.SetActive(false);
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

      GameSfxController.TryPlayCardClick();

      string picked = item.SouvenirObjectName;
      Hide(immediate: false, () =>
      {
        _onPicked?.Invoke(picked);
        _onPicked = null;
      });
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
      Hide(immediate, null);
    }

    public void Hide(bool immediate, Action onComplete)
    {
      KillShowHideTween();
      SetLineText(null);
      ResetTextSlots();

      if (immediate || root == null || !root.activeInHierarchy)
      {
        ClearActiveItems();
        DeactivateRoot();
        onComplete?.Invoke();
        return;
      }

      PlayHideAnimation(() =>
      {
        ClearActiveItems();
        onComplete?.Invoke();
      });
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
      KillShowHideTween();

      foreach (Image slot in _textSlots)
      {
        if (slot != null)
          slot.transform.DOKill();
      }

      foreach (SouvenirPickItem item in _activeItems)
      {
        if (item?.transform != null)
          item.transform.DOKill();
      }
    }
  }
}
