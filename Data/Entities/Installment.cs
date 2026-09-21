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
    // 같은 달이면 이번 달 납입이 남아 있으므로 +1 포함
    public int RemainingMonths
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (today > LastPayDate) return 0;
            return ((LastPayDate.Year - today.Year) * 12) + LastPayDate.Month - today.Month + 1;
        }
    }

    // 오늘 ~ 만기일까지 남은 일수 (마지막 달일 때 일 단위 표시용)
    public int RemainingDays
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today > LastPayDate ? 0 : LastPayDate.DayNumber - today.DayNumber;
        }
    }

    public bool IsCompleted => RemainingMonths <= 0;
}
