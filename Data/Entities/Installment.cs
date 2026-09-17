namespace HouseholdBudget.Data.Entities;

public class Installment
{
    public int Id { get; set; }
    public string Name { get; set; } = "";      // 항목명
    public string Card { get; set; } = "";      // 결제카드
    public decimal TotalAmount { get; set; }    // 총 금액
    public int Months { get; set; }             // 할부 개월 수
    public DateOnly LastPayDate { get; set; }   // 마지막 납입일

    // 월 납입액 = TotalAmount / Months (항상 계산, DB 저장 안 함)
    public decimal MonthlyAmount => Months > 0 ? TotalAmount / Months : 0;

    // 남은 개월 (오늘 기준 자동 계산)
    public int RemainingMonths
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (today > LastPayDate) return 0;
            var months = ((LastPayDate.Year - today.Year) * 12) + LastPayDate.Month - today.Month;
            return Math.Max(0, months);
        }
    }

    public bool IsCompleted => RemainingMonths <= 0;
}
