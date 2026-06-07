namespace VanGame.Data
{
  public readonly struct SouvenirRewardInfo
  {
    public SouvenirRewardType Type { get; }
    public string FunctionText { get; }
    public string FlavorText { get; }
    public bool HasGameplayEffect { get; }

    public SouvenirRewardInfo(
      SouvenirRewardType type,
      string functionText,
      string flavorText,
      bool hasGameplayEffect = true)
    {
      Type = type;
      FunctionText = functionText;
      FlavorText = flavorText;
      HasGameplayEffect = hasGameplayEffect;
    }

    public string GetDisplayText(bool includeFlavor)
    {
      if (!includeFlavor || string.IsNullOrWhiteSpace(FlavorText))
        return FunctionText;

      return $"{FunctionText}\n{FlavorText}";
    }
  }
}
