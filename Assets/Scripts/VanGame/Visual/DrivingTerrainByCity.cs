using System;
using System.Collections.Generic;
using UnityEngine;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.Visual
{
  /// <summary>
  /// Activates driving terrain / road animation roots for the selected destination city.
  /// Assign each city's terrain GameObjects in the inspector; all others stay disabled.
  /// </summary>
  public class DrivingTerrainByCity : MonoBehaviour
  {
    [Serializable]
    public class CityTerrainEntry
    {
      public CityDefinition city;
      [Tooltip("Terrain roots (parallax rigs, animators, roads) enabled when driving to this city.")]
      public GameObject[] terrainRoots = Array.Empty<GameObject>();
    }

    [SerializeField] GameFlowController gameFlow;
    [SerializeField] GameConfig gameConfig;
    [SerializeField] CityTerrainEntry[] cityTerrains = Array.Empty<CityTerrainEntry>();
    [SerializeField] GameObject[] defaultTerrainRoots = Array.Empty<GameObject>();
    [SerializeField] bool hideAllWhenNoDestination = true;
    [SerializeField] bool restartAnimationsOnActivate = true;
    [SerializeField] string animatorStateName = string.Empty;

    readonly HashSet<GameObject> _managedRoots = new HashSet<GameObject>();
    readonly List<ParallaxBackground2D> _activeParallax = new List<ParallaxBackground2D>();
    CityDefinition _activeCity;
    CityDefinition _heldTerrainCity;

    void Awake()
    {
      if (gameFlow == null)
        gameFlow = FindFirstObjectByType<GameFlowController>();

      CacheManagedRoots();
      DeactivateAllManagedRoots();
    }

    void OnEnable()
    {
      Subscribe(true);
      RefreshTerrain();
    }

    void OnDisable()
    {
      Subscribe(false);
    }

    void Subscribe(bool subscribe)
    {
      RunState run = gameFlow?.RunState;
      if (run == null)
        return;

      if (subscribe)
      {
        run.DestinationSelected += OnDestinationSelected;
        run.PhaseChanged += OnPhaseChanged;
      }
      else
      {
        run.DestinationSelected -= OnDestinationSelected;
        run.PhaseChanged -= OnPhaseChanged;
      }
    }

    void OnDestinationSelected(CityDefinition city)
    {
      if (city != null)
        _heldTerrainCity = city;

      RefreshTerrain();
    }

    void OnPhaseChanged()
    {
      RefreshTerrain();
    }

    void RefreshTerrain()
    {
      ApplyForCity(ResolveDisplayCity());
    }

    CityDefinition ResolveDisplayCity()
    {
      RunState run = gameFlow?.RunState;
      if (run == null)
        return null;

      if (run.Phase == GamePhase.Win || run.Phase == GamePhase.Lose)
      {
        if (_heldTerrainCity != null)
          return _heldTerrainCity;

        return run.CurrentCity;
      }

      if (run.DestinationCity != null)
      {
        _heldTerrainCity = run.DestinationCity;
        return run.DestinationCity;
      }

      if (IsMapPhase(run.Phase))
      {
        _heldTerrainCity = null;
        return null;
      }

      if (_heldTerrainCity != null && run.CurrentCity == _heldTerrainCity)
        return _heldTerrainCity;

      if (IsArrivalFlowPhase(run.Phase) && _heldTerrainCity != null)
        return _heldTerrainCity;

      return null;
    }

    static bool IsMapPhase(GamePhase phase)
    {
      return phase == GamePhase.MapOpen || phase == GamePhase.MapSelectingDestination;
    }

    static bool IsArrivalFlowPhase(GamePhase phase)
    {
      return phase == GamePhase.CityArrival
        || phase == GamePhase.SouvenirPick
        || phase == GamePhase.AbilityPick;
    }

    public void ApplyForCity(CityDefinition city)
    {
      if (city == _activeCity)
        return;

      _activeCity = city;

      if (city == null && hideAllWhenNoDestination)
      {
        DeactivateAllManagedRoots();
        return;
      }

      DeactivateAllManagedRoots();

      GameObject[] roots = ResolveRootsForCity(city);
      if (roots == null || roots.Length == 0)
        return;

      for (int i = 0; i < roots.Length; i++)
        ActivateRoot(roots[i]);

      CacheActiveParallax(roots);
      ResetActiveParallaxSpeed();
    }

    void CacheActiveParallax(GameObject[] roots)
    {
      _activeParallax.Clear();

      if (roots == null)
        return;

      for (int i = 0; i < roots.Length; i++)
      {
        GameObject root = roots[i];
        if (root == null)
          continue;

        ParallaxBackground2D[] parallaxComponents = root.GetComponentsInChildren<ParallaxBackground2D>(true);
        for (int p = 0; p < parallaxComponents.Length; p++)
        {
          ParallaxBackground2D parallax = parallaxComponents[p];
          if (parallax != null && !_activeParallax.Contains(parallax))
            _activeParallax.Add(parallax);
        }
      }
    }

    public void ResetActiveParallaxSpeed()
    {
      float decay = gameConfig != null ? gameConfig.parallaxSpeedDecayPerSecond : 1.5f;

      for (int i = 0; i < _activeParallax.Count; i++)
      {
        ParallaxBackground2D parallax = _activeParallax[i];
        if (parallax == null)
          continue;

        parallax.SetSpeedDecayPerSecond(decay);
        parallax.ResetScrollSpeed();
      }
    }

    public void BoostActiveParallaxSpeed()
    {
      float boost = gameConfig != null ? gameConfig.parallaxCardPlaySpeedBoost : 3f;

      for (int i = 0; i < _activeParallax.Count; i++)
      {
        ParallaxBackground2D parallax = _activeParallax[i];
        if (parallax == null || !parallax.isActiveAndEnabled)
          continue;

        parallax.BoostScrollSpeed(boost);
      }
    }

    GameObject[] ResolveRootsForCity(CityDefinition city)
    {
      if (city == null)
      {
        if (hideAllWhenNoDestination)
          return Array.Empty<GameObject>();

        return defaultTerrainRoots ?? Array.Empty<GameObject>();
      }

      for (int i = 0; i < cityTerrains.Length; i++)
      {
        CityTerrainEntry entry = cityTerrains[i];
        if (entry?.city == city && entry.terrainRoots != null && entry.terrainRoots.Length > 0)
          return entry.terrainRoots;
      }

      return defaultTerrainRoots ?? Array.Empty<GameObject>();
    }

    void ActivateRoot(GameObject root)
    {
      if (root == null)
        return;

      root.SetActive(true);

      if (restartAnimationsOnActivate)
        RestartAnimations(root);
    }

    void DeactivateAllManagedRoots()
    {
      foreach (GameObject root in _managedRoots)
      {
        if (root != null)
          root.SetActive(false);
      }

      _activeParallax.Clear();
    }

    void CacheManagedRoots()
    {
      _managedRoots.Clear();

      if (defaultTerrainRoots != null)
      {
        for (int i = 0; i < defaultTerrainRoots.Length; i++)
          TryAddManagedRoot(defaultTerrainRoots[i]);
      }

      for (int i = 0; i < cityTerrains.Length; i++)
      {
        CityTerrainEntry entry = cityTerrains[i];
        if (entry?.terrainRoots == null)
          continue;

        for (int r = 0; r < entry.terrainRoots.Length; r++)
          TryAddManagedRoot(entry.terrainRoots[r]);
      }
    }

    void TryAddManagedRoot(GameObject root)
    {
      if (root != null)
        _managedRoots.Add(root);
    }

    void RestartAnimations(GameObject root)
    {
      Animator[] animators = root.GetComponentsInChildren<Animator>(true);
      for (int i = 0; i < animators.Length; i++)
      {
        Animator animator = animators[i];
        if (animator == null)
          continue;

        animator.Rebind();
        animator.Update(0f);

        if (!string.IsNullOrEmpty(animatorStateName))
          animator.Play(animatorStateName, 0, 0f);
        else
          animator.Play(0, 0, 0f);
      }

      Animation[] legacyAnimations = root.GetComponentsInChildren<Animation>(true);
      for (int i = 0; i < legacyAnimations.Length; i++)
      {
        Animation animation = legacyAnimations[i];
        if (animation == null || animation.clip == null)
          continue;

        animation.Stop();
        animation.Play();
      }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      CacheManagedRoots();
    }

    [ContextMenu("Refresh Managed Roots Cache")]
    void EditorRefreshCache()
    {
      CacheManagedRoots();
    }
#endif

    public CityDefinition ActiveCity => _activeCity;
  }
}
