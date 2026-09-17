namespace HouseholdBudget.Data.Entities;

public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = "";  // 공동 체크카드, 남편 카드 …

    public ICollection<Entry> Entries { get; set; } = [];
}
