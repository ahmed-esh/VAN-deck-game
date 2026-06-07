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

    /// <summary>How many slices of the 8-part driving-day bar this card advances when played.</summary>
    public enum CardDayTimeCost
    {
        OneSection = 1,
        TwoSections = 2,
        ThreeSections = 3,
        FourSections = 4
    }

    public enum GamePhase
    {
        CardIdle,
        MapOpen,
        MapSelectingDestination,
        Driving,
        CityArrival,
        AbilityPick,
        SouvenirPick,
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
        Subtract,
        Multiply,
        Divide
    }
}
