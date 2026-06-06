namespace VanGame.Data
{
  public struct CardEffectPreview
  {
    public int MoneyBefore;
    public int MoneyAfter;
    public float FuelBefore;
    public float FuelAfter;
    public float MoraleBefore;
    public float MoraleAfter;
    public float VanBefore;
    public float VanAfter;
    public float TimerSectionsBefore;
    public float TimerSectionsAfter;

    public bool AffectsMoney;
    public bool AffectsFuel;
    public bool AffectsMorale;
    public bool AffectsVan;
    public bool AffectsTimer;
  }
}
