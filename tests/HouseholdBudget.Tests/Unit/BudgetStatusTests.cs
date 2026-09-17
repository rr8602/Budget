using HouseholdBudget.Services;

public class BudgetStatusTests
{
    // IsOverBudget: Actual > Budget && Budget > 0
    // IsWarning:    Percentage >= 80 && !IsOverBudget
    [Theory]
    [InlineData(100_000, 120_000, true,  false)]  // 120% → 초과
    [InlineData(100_000, 100_000, false, true)]   // 100% → 경고 (초과 아님, >가 아닌 >=이면 초과지만 코드는 >)
    [InlineData(100_000,  90_000, false, true)]   // 90% → 경고
    [InlineData(100_000,  80_000, false, true)]   // 80% → 경고 경계
    [InlineData(100_000,  79_999, false, false)]  // ~80% 미만 → 정상
    [InlineData(100_000,       0, false, false)]  // 0% → 정상
    [InlineData(      0,  10_000, false, false)]  // 예산 0 → Percentage=0
    public void ComputedProps_MatchExpected(
        decimal budget, decimal actual, bool expectOver, bool expectWarn)
    {
        var s = new BudgetStatus("테스트", budget, actual);
        Assert.Equal(expectOver, s.IsOverBudget);
        Assert.Equal(expectWarn, s.IsWarning);
    }

    [Fact]
    public void Percentage_CappedAt200()
    {
        var s = new BudgetStatus("테스트", 100_000, 500_000);
        Assert.Equal(200, s.Percentage);
    }

    [Fact]
    public void IsWarning_False_WhenIsOverBudget()
    {
        var s = new BudgetStatus("테스트", 100_000, 120_000);
        Assert.True(s.IsOverBudget);
        Assert.False(s.IsWarning);
    }

    [Fact]
    public void Remaining_IsNegative_WhenOverBudget()
    {
        var s = new BudgetStatus("테스트", 100_000, 130_000);
        Assert.Equal(-30_000, s.Remaining);
    }
}

public class FixedActualStatusTests
{
    [Fact]
    public void IsUnconfirmed_True_WhenActualIsNull()
    {
        var s = new FixedActualStatus(1, "저축", "남편", "청약", 100_000, null);
        Assert.True(s.IsUnconfirmed);
        Assert.False(s.IsShortfall);
        Assert.Equal(100_000, s.Shortfall); // Planned - 0
    }

    [Fact]
    public void IsShortfall_True_WhenActualLessThanPlanned()
    {
        var s = new FixedActualStatus(1, "저축", "남편", "청약", 100_000, 80_000);
        Assert.False(s.IsUnconfirmed);
        Assert.True(s.IsShortfall);
        Assert.Equal(20_000, s.Shortfall);
    }

    [Fact]
    public void IsShortfall_False_WhenActualMeetsPlanned()
    {
        var s = new FixedActualStatus(1, "저축", "남편", "청약", 100_000, 100_000);
        Assert.False(s.IsUnconfirmed);
        Assert.False(s.IsShortfall);
        Assert.Equal(0, s.Shortfall);
    }
}
