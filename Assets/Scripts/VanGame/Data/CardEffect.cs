using System;
using UnityEngine;

namespace VanGame.Data
{
  public enum CardEffectTarget
  {
    Money,
    Morale,
    Fuel,
    VanCondition,
    ActionDuration
  }

  public enum CardStatOperation
  {
    Add,
    Subtract,
    Multiply,
    Divide
  }

  [Serializable]
  public struct CardEffect
  {
    public CardEffectTarget target;
    public CardStatOperation operation;
    public float value;
  }

  public static class CardEffectMath
  {
    public static float Apply(float baseValue, CardStatOperation operation, float value)
    {
      switch (operation)
      {
        case CardStatOperation.Add:
          return baseValue + value;
        case CardStatOperation.Subtract:
          return baseValue - value;
        case CardStatOperation.Multiply:
          return baseValue * value;
        case CardStatOperation.Divide:
          return Mathf.Approximately(value, 0f) ? baseValue : baseValue / value;
        default:
          return baseValue;
      }
    }
  }
}
