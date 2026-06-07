using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VanGame.Core;
using VanGame.Data;

namespace VanGame.UI
{
  public class MapRoadsController : MonoBehaviour
  {
    [Serializable]
    public struct Connection
    {
      public CityDefinition cityA;
      public CityDefinition cityB;
      public GameObject roadVisual;
    }

    [SerializeField] Connection[] connections = Array.Empty<Connection>();
    [SerializeField] float activeLegBlinkDuration = 0.55f;
    [SerializeField] Color activeLegBlinkColor = new Color(1f, 0.95f, 0.55f, 1f);

    readonly Dictionary<GameObject, Image> _roadImages = new Dictionary<GameObject, Image>();
    Connection? _hoverConnection;
    Connection? _blinkingConnection;
    Tweener _blinkTween;

    void Awake()
    {
      CacheRoadImages();
      HideAllRoads();
    }

    void CacheRoadImages()
    {
      _roadImages.Clear();

      foreach (Connection connection in connections)
      {
        if (connection.roadVisual == null)
          continue;

        Image image = connection.roadVisual.GetComponent<Image>();
        if (image != null)
          _roadImages[connection.roadVisual] = image;
      }
    }

    public void HideAllRoads()
    {
      StopActiveLegBlink();

      foreach (Connection connection in connections)
      {
        if (connection.roadVisual != null)
          connection.roadVisual.SetActive(false);
      }

      _hoverConnection = null;
    }

    public void ClearHoverRoad()
    {
      if (_hoverConnection == null)
        return;

      Connection hover = _hoverConnection.Value;
      _hoverConnection = null;

      if (_blinkingConnection.HasValue && SameConnection(hover, _blinkingConnection.Value))
        return;

      if (hover.roadVisual != null)
        hover.roadVisual.SetActive(false);
    }

    public void ShowHoverRoad(CityDefinition from, CityDefinition to)
    {
      ClearHoverRoad();

      if (!TryFindConnection(from, to, out Connection connection))
        return;

      _hoverConnection = connection;

      if (_blinkingConnection.HasValue && SameConnection(connection, _blinkingConnection.Value))
        return;

      if (connection.roadVisual != null)
        connection.roadVisual.SetActive(true);
    }

    public void ShowTraveledRoads(RunState runState)
    {
      if (runState == null)
        return;

      foreach (CityRoadPair pair in runState.TraveledRoads)
      {
        if (!TryFindConnection(pair.From, pair.To, out Connection connection))
          continue;

        if (connection.roadVisual != null)
          connection.roadVisual.SetActive(true);
      }
    }

    public void BlinkActiveLegRoad(CityDefinition from, CityDefinition to)
    {
      StopActiveLegBlink();

      if (!TryFindConnection(from, to, out Connection connection))
        return;

      _blinkingConnection = connection;

      if (connection.roadVisual != null)
        connection.roadVisual.SetActive(true);

      if (!_roadImages.TryGetValue(connection.roadVisual, out Image image) || image == null)
        return;

      Color baseColor = image.color;
      baseColor.a = 1f;
      image.color = baseColor;

      _blinkTween = image
        .DOColor(activeLegBlinkColor, activeLegBlinkDuration)
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);
    }

    void StopActiveLegBlink()
    {
      if (_blinkTween != null)
      {
        _blinkTween.Kill();
        _blinkTween = null;
      }

      if (_blinkingConnection.HasValue
        && _blinkingConnection.Value.roadVisual != null
        && _roadImages.TryGetValue(_blinkingConnection.Value.roadVisual, out Image image)
        && image != null)
      {
        image.DOKill();
      }

      _blinkingConnection = null;
    }

    bool TryFindConnection(CityDefinition a, CityDefinition b, out Connection connection)
    {
      foreach (Connection candidate in connections)
      {
        if (!Connects(candidate, a, b))
          continue;

        connection = candidate;
        return true;
      }

      connection = default;
      return false;
    }

    static bool Connects(Connection connection, CityDefinition a, CityDefinition b)
    {
      if (a == null || b == null)
        return false;

      return (connection.cityA == a && connection.cityB == b)
        || (connection.cityA == b && connection.cityB == a);
    }

    static bool SameConnection(Connection a, Connection b)
    {
      return a.roadVisual == b.roadVisual;
    }

    void OnDisable()
    {
      StopActiveLegBlink();
    }
  }
}
