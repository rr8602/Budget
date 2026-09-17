using HouseholdBudget.Data.Entities;

public class InstallmentTests
{
    [Fact]
    public void MonthlyAmount_DividesTotalByMonths()
    {
        var i = new Installment { TotalAmount = 1_200_000, Months = 12 };
        Assert.Equal(100_000, i.MonthlyAmount);
    }

    [Fact]
    public void MonthlyAmount_ReturnsZero_WhenMonthsIsZero()
    {
        var i = new Installment { TotalAmount = 500_000, Months = 0 };
        Assert.Equal(0, i.MonthlyAmount);
    }

    [Fact]
    public void RemainingMonths_IsZero_WhenPastLastPayDate()
    {
        var i = new Installment
        {
            LastPayDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1))
        };
        Assert.Equal(0, i.RemainingMonths);
    }

    [Fact]
    public void RemainingMonths_CalculatesCorrectly_ForFutureDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var i = new Installment { LastPayDate = today.AddMonths(6) };
        Assert.Equal(6, i.RemainingMonths);
    }

    [Fact]
    public void IsCompleted_True_WhenPastLastPayDate()
    {
        var i = new Installment
        {
            LastPayDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1))
        };
        Assert.True(i.IsCompleted);
    }

    [Fact]
    public void IsCompleted_False_WhenFutureLastPayDate()
    {
        var i = new Installment
        {
            LastPayDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3))
        };
        Assert.False(i.IsCompleted);
    }
}
