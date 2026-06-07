using System;
using System.Collections.Generic;
using UnityEngine;
using VanGame.Core;

namespace VanGame.Data
{
  public static class SouvenirCatalog
  {
    public const string PickScreenRootName = "Souvenirs screen";
    public const string PickObjectsName = "objects Souvenirs";
    public const string VanObjectsName = "objects Souvenirs in the car ";
    public const string DescriptionTextOnVanName = "description text on the van";
    public const string LineTextName = "line";

    public const float VanSouvenirSpacingX = 50f;
    public const float PickHoverLiftY = 18f;

    static readonly SouvenirRewardInfo[] RandomizableRewards =
    {
      new SouvenirRewardInfo(SouvenirRewardType.MoneyPerSection, "Gains $1 per section", "Slowly, diligently providing help."),
      new SouvenirRewardInfo(
        SouvenirRewardType.ReducedFuelDrain,
        "-12 fuel per day instead of -15",
        "\"A free promotional hybrid engine does its job\", says the souvenir."),
      new SouvenirRewardInfo(
        SouvenirRewardType.ReducedVanDrain,
        "-4 van condition instead of -5",
        "Reminds you of a picture from a mechanic magazine you read."),
      new SouvenirRewardInfo(
        SouvenirRewardType.VanConditionRescue,
        "+50 van condition when van condition reaches 0 (once per game)",
        "Somehow there's a free BBB coupon inside."),
      new SouvenirRewardInfo(
        SouvenirRewardType.HighMoraleCityBonus,
        "If Morale > 90 at the end of the city, + $30",
        "Well, this time fortune favors happy people."),
      new SouvenirRewardInfo(
        SouvenirRewardType.HighVanCityBonus,
        "If Van Condition > 90 at the end of the city, + 10 fuel",
        "No idea why but if the souvenir says so."),
      new SouvenirRewardInfo(
        SouvenirRewardType.BankruptcyShuffle,
        "If cannot afford any of the cards in hand, shuffle hand (once per game) (has to be done before)",
        "Declare bankruptcy! Just don't always declare bankruptcy."),
      new SouvenirRewardInfo(SouvenirRewardType.ExtraHandCard, "+1 hand", "Be happy and grateful."),
      new SouvenirRewardInfo(
        SouvenirRewardType.DoubleNextCard,
        "The card played right after activation takes effect twice (once per round)",
        "Let's double the good and bad!")
    };

    static readonly Dictionary<string, SouvenirRewardInfo> FixedSpecialByObjectName =
      new Dictionary<string, SouvenirRewardInfo>(StringComparer.Ordinal)
      {
        {
          "South ridge 2 (special)",
          new SouvenirRewardInfo(SouvenirRewardType.SpecialCamel, "(camel)", "Just very cute and lovely.", false)
        },
        {
          "Oak Woods  1 (special)",
          new SouvenirRewardInfo(SouvenirRewardType.SpecialBird, "(bird)", "Somehow better and smarter-looking than the others.", false)
        },
        {
          "Red Willow 2 (special)",
          new SouvenirRewardInfo(SouvenirRewardType.SpecialCasino, "(casino)", "Makes people feel good (may not help if they lose too much).", false)
        }
      };

    static readonly Dictionary<string, string[]> RegionSouvenirObjectNames =
      new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
      {
        { "southridge", new[] { "South ridge 1", "South ridge 2 (special)", "South ridge 3 " } },
        { "oakwoods", new[] { "Oak Woods  1 (special)", "Oak Woods  2", "Oak Woods 3" } },
        { "redwillow", new[] { "Red Willow 1", "Red Willow 2 (special)", "Red Willow 3" } },
        { "argylle", new[] { "Argylle 1", "Argylle 2", "Argylle 3" } },
        { "foxcreek", new[] { "Fox Creek  1", "Fox Creek  2", "Fox Creek  3" } },
        { "carolinton", new[] { "Carolinton 1", "Carolinton 2", "Carolinton 3" } },
        { "louisville", new[] { "Louisville 1", "Louisville 2", "Louisville 3" } },
        { "dentone", new[] { "denote 1", "denote 2", "denote 3" } }
      };

    public static IReadOnlyList<SouvenirRewardInfo> GetRandomizableRewards() => RandomizableRewards;

    public static bool IsFixedSpecial(string objectName)
    {
      return !string.IsNullOrWhiteSpace(objectName) && FixedSpecialByObjectName.ContainsKey(objectName);
    }

    public static SouvenirRewardInfo GetFixedSpecial(string objectName)
    {
      if (objectName != null && FixedSpecialByObjectName.TryGetValue(objectName, out SouvenirRewardInfo info))
        return info;

      return default;
    }

    public static SouvenirRewardInfo GetInfo(RunState runState, string objectName)
    {
      if (runState == null || string.IsNullOrWhiteSpace(objectName))
        return default;

      if (IsFixedSpecial(objectName))
        return GetFixedSpecial(objectName);

      if (runState.SouvenirRewardAssignments.TryGetValue(objectName, out SouvenirRewardType type))
        return GetInfoForType(type);

      return default;
    }

    public static SouvenirRewardInfo GetInfoForType(SouvenirRewardType type)
    {
      foreach (SouvenirRewardInfo reward in RandomizableRewards)
      {
        if (reward.Type == type)
          return reward;
      }

      if (type == SouvenirRewardType.SpecialCamel)
        return GetFixedSpecial("South ridge 2 (special)");
      if (type == SouvenirRewardType.SpecialBird)
        return GetFixedSpecial("Oak Woods  1 (special)");
      if (type == SouvenirRewardType.SpecialCasino)
        return GetFixedSpecial("Red Willow 2 (special)");

      return default;
    }

    public static string[] GetSouvenirObjectNamesForCity(CityDefinition city)
    {
      if (city == null)
        return Array.Empty<string>();

      string key = NormalizeCityKey(city.cityId);
      if (RegionSouvenirObjectNames.TryGetValue(key, out string[] names))
        return names;

      key = NormalizeCityKey(city.displayName);
      return RegionSouvenirObjectNames.TryGetValue(key, out names) ? names : Array.Empty<string>();
    }

    public static IReadOnlyList<string> GetAllAssignableObjectNames()
    {
      var names = new List<string>();
      foreach (string[] regionNames in RegionSouvenirObjectNames.Values)
      {
        foreach (string name in regionNames)
        {
          if (!IsFixedSpecial(name))
            names.Add(name);
        }
      }

      return names;
    }

    public static void AssignRandomRewards(RunState runState)
    {
      if (runState == null)
        return;

      runState.SouvenirRewardAssignments.Clear();

      IReadOnlyList<string> assignable = GetAllAssignableObjectNames();
      if (assignable.Count == 0)
        return;

      var rewardPool = new List<SouvenirRewardType>();
      foreach (SouvenirRewardInfo reward in RandomizableRewards)
        rewardPool.Add(reward.Type);

      while (rewardPool.Count < assignable.Count)
      {
        SouvenirRewardInfo pick = RandomizableRewards[UnityEngine.Random.Range(0, RandomizableRewards.Length)];
        rewardPool.Add(pick.Type);
      }

      for (int i = rewardPool.Count - 1; i > 0; i--)
      {
        int swapIndex = UnityEngine.Random.Range(0, i + 1);
        (rewardPool[i], rewardPool[swapIndex]) = (rewardPool[swapIndex], rewardPool[i]);
      }

      int count = Mathf.Min(assignable.Count, rewardPool.Count);
      for (int i = 0; i < count; i++)
        runState.SouvenirRewardAssignments[assignable[i]] = rewardPool[i];
    }

    static string NormalizeCityKey(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return value.Trim().ToLowerInvariant().Replace(" ", string.Empty);
    }
  }
}
