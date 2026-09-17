namespace HouseholdBudget.Data.Entities;

public class MonthlyFixedActual
{
    public int     Id           { get; set; }
    public int     FixedItemId  { get; set; }
    public FixedItem FixedItem  { get; set; } = null!;
    public int     Year         { get; set; }
    public int     Month        { get; set; }
    public decimal ActualAmount { get; set; }
}
