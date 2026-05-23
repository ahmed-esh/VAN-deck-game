using System.Collections.Generic;
using UnityEngine;
using VanGame.Data;

namespace VanGame.Core
{
  public class CityRandomEventResolver : MonoBehaviour
  {
    [SerializeField] GameConfig gameConfig;

    public void Configure(GameConfig config)
    {
      if (config != null)
        gameConfig = config;
    }

    public List<RandomEventDefinition> RollEvents(CityDefinition city)
    {
      var results = new List<RandomEventDefinition>();

      if (city == null || gameConfig == null)
        return results;

      List<RandomEventDefinition> eligible = BuildEligibleEvents(city);
      if (eligible.Count == 0)
        return results;

      int count = Mathf.Clamp(
        Random.Range(gameConfig.minRandomEventsPerCity, gameConfig.maxRandomEventsPerCity + 1),
        0,
        eligible.Count);

      var pool = new List<RandomEventDefinition>(eligible);
      var weights = BuildWeights(city, pool);

      for (int i = 0; i < count && pool.Count > 0; i++)
      {
        int index = PickWeightedIndex(pool, weights);
        results.Add(pool[index]);
        pool.RemoveAt(index);
        weights.RemoveAt(index);
      }

      return results;
    }

    static List<RandomEventDefinition> BuildEligibleEvents(CityDefinition city)
    {
      var eligible = new List<RandomEventDefinition>();

      if (city.possibleEvents == null)
        return eligible;

      foreach (RandomEventDefinition evt in city.possibleEvents)
      {
        if (evt == null)
          continue;

        if (evt.requireParkingMatch && evt.requiredParking != city.parking)
          continue;

        if (evt.requireCostOfLivingMatch && evt.requiredCostOfLiving != city.costOfLiving)
          continue;

        eligible.Add(evt);
      }

      return eligible;
    }

    static List<float> BuildWeights(CityDefinition city, List<RandomEventDefinition> pool)
    {
      var weights = new List<float>(pool.Count);

      for (int i = 0; i < pool.Count; i++)
      {
        float weight = 1f;
        RandomEventDefinition evt = pool[i];

        if (city.possibleEvents != null && city.eventWeights != null)
        {
          for (int j = 0; j < city.possibleEvents.Length; j++)
          {
            if (city.possibleEvents[j] == evt && j < city.eventWeights.Length)
            {
              weight = Mathf.Max(0.01f, city.eventWeights[j]);
              break;
            }
          }
        }

        weights.Add(weight);
      }

      return weights;
    }

    static int PickWeightedIndex(List<RandomEventDefinition> pool, List<float> weights)
    {
      float total = 0f;
      for (int i = 0; i < weights.Count; i++)
        total += weights[i];

      float roll = Random.Range(0f, total);
      float cumulative = 0f;

      for (int i = 0; i < pool.Count; i++)
      {
        cumulative += weights[i];
        if (roll <= cumulative)
          return i;
      }

      return pool.Count - 1;
    }
  }
}
