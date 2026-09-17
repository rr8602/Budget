namespace HouseholdBudget.Data.Entities;

public class FixedItem
{
    public int Id { get; set; }
    public string GroupType { get; set; } = "";     // 저축 / 보험 / 정기지출 / 유동지출
    public string Owner { get; set; } = "";         // 담당: 병진 / 현지 / 공통/병진 …
    public string Name { get; set; } = "";          // 항목명
    public string? PaymentNote { get; set; }        // 결제수단 메모 (신한, 우리, 토스 …)
    public decimal Amount { get; set; }             // 월 금액

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string? Memo { get; set; }
}
