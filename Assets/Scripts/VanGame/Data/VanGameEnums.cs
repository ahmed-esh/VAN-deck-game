namespace VanGame.Data
{
    public enum ParkingType
    {
        Available,
        FreeTrailer,
        Limited,
        None
    }

    public enum CostOfLiving
    {
        Low,
        Medium,
        High
    }

    public enum CardCategory
    {
        Food,
        Fuel,
        Van,
        Special
    }

    public enum CardTier
    {
        HumbleBeginning,
        SmallSatisfaction
    }

    public enum GamePhase
    {
        CardIdle,
        MapOpen,
        MapSelectingDestination,
        Driving,
        CityArrival,
        AbilityPick,
        Win,
        Lose
    }

    public enum ModifierTarget
    {
        MoneyCost,
        MoraleGain,
        ActionDuration,
        DrivingDayBudget,
        ProbabilityWin,
        FuelCost,
        VanRepairDuration
    }

    public enum ModifierOperation
    {
        Add,
        Multiply
    }
}
