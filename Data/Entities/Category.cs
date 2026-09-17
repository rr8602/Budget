namespace HouseholdBudget.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string ParentName { get; set; } = "";   // 대분류 (식비, 주거/관리 …)
    public string Name { get; set; } = "";          // 소분류 (장보기/마트, 월세 …)

    public ICollection<Entry> Entries { get; set; } = [];
    public ICollection<FixedItem> FixedItems { get; set; } = [];
}
