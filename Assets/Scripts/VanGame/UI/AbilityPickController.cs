using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class AbilityPickController : MonoBehaviour
  {
    [SerializeField] GameObject root;
    [SerializeField] RectTransform choicesContainer;
    [SerializeField] AbilityCardView abilityCardPrefab;
    [SerializeField] CardHandHoverFan hoverFan;
    [SerializeField] AbilityCatalog abilityCatalog;
    [SerializeField] GameConfig gameConfig;

    readonly List<AbilityCardView> _views = new List<AbilityCardView>();
    Action<AbilityDefinition> _onPicked;

    public void Configure(AbilityCatalog catalog, GameConfig config)
    {
      if (catalog != null)
        abilityCatalog = catalog;
      if (config != null)
        gameConfig = config;
    }

    void Awake()
    {
      Hide(immediate: true);
    }

    public void ShowOffer(RunState runState, Action<AbilityDefinition> onPicked)
    {
      _onPicked = onPicked;
      ClearViews();

      if (root != null)
        root.SetActive(true);
      else
        gameObject.SetActive(true);

      List<AbilityDefinition> offers = BuildOffers(runState);
      float startScale = gameConfig != null ? gameConfig.abilityCardInStartScale : 0.15f;
      float duration = gameConfig != null ? gameConfig.abilityCardInDuration : 0.4f;
      Ease ease = gameConfig != null ? gameConfig.abilityCardInEase : Ease.OutBack;

      foreach (AbilityDefinition ability in offers)
      {
        if (ability == null || abilityCardPrefab == null || choicesContainer == null)
          continue;

        AbilityCardView view = Instantiate(abilityCardPrefab, choicesContainer);
        view.Setup(ability);
        view.SetInteractable(true);
        view.Clicked += OnAbilityClicked;
        _views.Add(view);

        if (view.RectTransform != null)
        {
          view.RectTransform.localScale = Vector3.one * startScale;
          view.RectTransform.DOScale(1f, duration).SetEase(ease);
        }
      }

      hoverFan?.RefreshFromChildren();
    }

    List<AbilityDefinition> BuildOffers(RunState runState)
    {
      var offers = new List<AbilityDefinition>();
      if (abilityCatalog == null || runState == null || gameConfig == null)
        return offers;

      AbilityDefinition[] source = !runState.HasReceivedFirstCityReward
        ? abilityCatalog.firstCityRewards
        : abilityCatalog.generalPool;

      var eligible = new List<AbilityDefinition>();
      if (source != null)
      {
        foreach (AbilityDefinition ability in source)
        {
          if (ability == null || runState.OwnedAbilities.Contains(ability))
            continue;

          eligible.Add(ability);
        }
      }

      int count = Mathf.Min(gameConfig.abilityChoicesOffered, eligible.Count);
      for (int i = 0; i < count; i++)
      {
        int index = UnityEngine.Random.Range(0, eligible.Count);
        offers.Add(eligible[index]);
        eligible.RemoveAt(index);
      }

      return offers;
    }

    void OnAbilityClicked(AbilityCardView view)
    {
      if (view?.Definition == null)
        return;

      foreach (AbilityCardView v in _views)
        v.SetInteractable(false);

      AbilityDefinition picked = view.Definition;
      Hide(immediate: false);
      _onPicked?.Invoke(picked);
      _onPicked = null;
    }

    public void Hide(bool immediate)
    {
      if (root != null)
        root.SetActive(false);
      else
        gameObject.SetActive(false);

      ClearViews();
    }

    void ClearViews()
    {
      foreach (AbilityCardView view in _views)
      {
        if (view == null)
          continue;

        view.Clicked -= OnAbilityClicked;
        if (view.RectTransform != null)
          view.RectTransform.DOKill();

        Destroy(view.gameObject);
      }

      _views.Clear();
      hoverFan?.RefreshFromChildren();
    }

    void OnDisable()
    {
      foreach (AbilityCardView view in _views)
      {
        if (view?.RectTransform != null)
          view.RectTransform.DOKill();
      }
    }
  }
}
