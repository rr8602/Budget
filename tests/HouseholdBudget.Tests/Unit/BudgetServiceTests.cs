using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;

public class BudgetServiceTests : IDisposable
{
    private readonly TestDatabase _tdb;
    private readonly BudgetService _svc;

    public BudgetServiceTests()
    {
        _tdb = new TestDatabase();
        _svc = new BudgetService(_tdb.Db);
    }

    public void Dispose() => _tdb.Dispose();

    private async Task<int> AddCategoryAsync(string parentName, string name = "기본")
    {
        var cat = new Category { ParentName = parentName, Name = name };
        _tdb.Db.Categories.Add(cat);
        await _tdb.Db.SaveChangesAsync();
        return cat.Id;
    }

    [Fact]
    public async Task GetMonthlyBudgetAsync_SumsFixedItemsAndMonthlyExtra()
    {
        var catId = await AddCategoryAsync("식비");
        _tdb.Db.FixedItems.Add(new FixedItem { Name = "고정식비", CategoryId = catId, Amount = 300_000, GroupType = "정기", Owner = "공통" });
        _tdb.Db.Budgets.Add(new Budget { CategoryParentName = "식비", MonthlyExtra = 100_000 });
        await _tdb.Db.SaveChangesAsync();

        var result = await _svc.GetMonthlyBudgetAsync();

        Assert.Equal(400_000, result["식비"]);
    }

    [Fact]
    public async Task GetMonthlyBudgetAsync_ReturnsEmpty_WhenNoData()
    {
        var result = await _svc.GetMonthlyBudgetAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveMonthlyExtraAsync_CreatesNewRecord()
    {
        var ok = await _svc.SaveMonthlyExtraAsync("식비", 200_000);

        Assert.True(ok);
        Assert.Equal(1, _tdb.Db.Budgets.Count());
        Assert.Equal(200_000, _tdb.Db.Budgets.First().MonthlyExtra);
    }

    [Fact]
    public async Task SaveMonthlyExtraAsync_UpdatesExistingRecord()
    {
        await _svc.SaveMonthlyExtraAsync("식비", 200_000);
        await _svc.SaveMonthlyExtraAsync("식비", 350_000);

        Assert.Equal(1, _tdb.Db.Budgets.Count()); // upsert → 행 1개만 존재
        Assert.Equal(350_000, _tdb.Db.Budgets.First().MonthlyExtra);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_ReturnsCorrectPercentage()
    {
        var catId  = await AddCategoryAsync("식비");
        var payer  = new Payer         { Name = "남편" };
        var method = new PaymentMethod { Name = "카드" };
        _tdb.Db.Payers.Add(payer);
        _tdb.Db.PaymentMethods.Add(method);
        _tdb.Db.FixedItems.Add(new FixedItem { Name = "고정식비", CategoryId = catId, Amount = 200_000, GroupType = "정기", Owner = "공통" });
        await _tdb.Db.SaveChangesAsync();

        _tdb.Db.Entries.Add(new Entry
        {
            CategoryId = catId, PayerId = payer.Id, PaymentMethodId = method.Id,
            Date = new DateOnly(2026, 9, 1), Amount = 100_000, Content = "테스트"
        });
        await _tdb.Db.SaveChangesAsync();

        var statuses = await _svc.GetBudgetStatusAsync(2026, 9);
        var s = statuses.Single(x => x.CategoryParentName == "식비");

        Assert.Equal(200_000, s.Budget);
        Assert.Equal(100_000, s.Actual);
        Assert.Equal(50.0, s.Percentage, precision: 1);
        Assert.False(s.IsOverBudget);
        Assert.False(s.IsWarning);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_MarksOverBudget_WhenActualExceedsBudget()
    {
        var catId  = await AddCategoryAsync("식비");
        var payer  = new Payer         { Name = "남편" };
        var method = new PaymentMethod { Name = "카드" };
        _tdb.Db.Payers.Add(payer);
        _tdb.Db.PaymentMethods.Add(method);
        _tdb.Db.FixedItems.Add(new FixedItem { Name = "고정식비", CategoryId = catId, Amount = 100_000, GroupType = "정기", Owner = "공통" });
        await _tdb.Db.SaveChangesAsync();

        _tdb.Db.Entries.Add(new Entry
        {
            CategoryId = catId, PayerId = payer.Id, PaymentMethodId = method.Id,
            Date = new DateOnly(2026, 9, 1), Amount = 150_000, Content = "과지출"
        });
        await _tdb.Db.SaveChangesAsync();

        var statuses = await _svc.GetBudgetStatusAsync(2026, 9);
        var s = statuses.Single(x => x.CategoryParentName == "식비");

        Assert.True(s.IsOverBudget);
        Assert.False(s.IsWarning);
    }
}
