namespace HouseholdBudget.Data.Entities;

public class Payer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";  // 남편 / 아내 / 공동

    public ICollection<Entry> Entries { get; set; } = [];
}
