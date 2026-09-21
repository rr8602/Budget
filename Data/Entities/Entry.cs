using System.ComponentModel.DataAnnotations;

namespace HouseholdBudget.Data.Entities;

public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }             // 향후 로그인 대비, 현재 미사용

    public DateOnly Date { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int PayerId { get; set; }
    public Payer Payer { get; set; } = null!;

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    public string Content { get; set; } = "";       // 내용/상호명
    public string? Memo { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ConcurrencyCheck]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
