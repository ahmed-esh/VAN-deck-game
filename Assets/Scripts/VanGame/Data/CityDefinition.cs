using System;
using System.Collections.Generic;
using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "City", menuName = "Van Game/City Definition")]
    public class CityDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string cityId;
        public string displayName;

        [Header("Map flags")]
        public bool isStartCity;
        public bool isDestinationCity;

        [Header("Connections (parallel lists — same length)")]
        public CityDefinition[] neighborCities = Array.Empty<CityDefinition>();
        public int[] drivingDaysToNeighbor = Array.Empty<int>();

        [Header("City profile")]
        public ParkingType parking = ParkingType.Available;
        public CostOfLiving costOfLiving = CostOfLiving.Low;
        [TextArea(1, 3)] public string funTheme;
        public int baseMoraleDelta;
        public int stayDaysInCity = 1;

        [Header("Random events (city arrival — later milestone)")]
        public RandomEventDefinition[] possibleEvents = Array.Empty<RandomEventDefinition>();
        public float[] eventWeights = Array.Empty<float>();

        public int GetDrivingDaysTo(CityDefinition destination)
        {
            if (destination == null || neighborCities == null)
                return 0;

            for (int i = 0; i < neighborCities.Length; i++)
            {
                if (neighborCities[i] == destination && i < drivingDaysToNeighbor.Length)
                    return drivingDaysToNeighbor[i];
            }

            return 0;
        }

        public IReadOnlyList<CityDefinition> GetNeighbors()
        {
            return neighborCities ?? Array.Empty<CityDefinition>();
        }
    }
}
