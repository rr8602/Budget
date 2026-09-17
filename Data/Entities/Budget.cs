namespace HouseholdBudget.Data.Entities;

// 대분류별 추가 예산 (기본 예산은 FixedItems.Amount 합산으로 계산)
public class Budget
{
    public int Id { get; set; }
    public string CategoryParentName { get; set; } = "";  // 대분류
    public decimal MonthlyExtra { get; set; }              // 직접 입력하는 추가 예산
}
