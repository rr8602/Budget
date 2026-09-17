using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;

public class EntryServiceTests : IDisposable
{
    private readonly TestDatabase _tdb;
    private readonly EntryService _svc;

    public EntryServiceTests()
    {
        _tdb = new TestDatabase();
        _svc = new EntryService(_tdb.Db);
    }

    public void Dispose() => _tdb.Dispose();

    private static Entry MakeEntry(int catId, int payerId, int methodId, DateOnly date, decimal amount)
        => new() { CategoryId = catId, PayerId = payerId, PaymentMethodId = methodId,
                   Date = date, Amount = amount, Content = "테스트" };

    [Fact]
    public async Task CreateAsync_AddsEntryAndReturnsTrue()
    {
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();

        var ok = await _svc.CreateAsync(MakeEntry(catId, payerId, methodId,
            DateOnly.FromDateTime(DateTime.Today), 10_000));

        Assert.True(ok);
        Assert.Equal(1, _tdb.Db.Entries.Count());
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();
        var entry = MakeEntry(catId, payerId, methodId, DateOnly.FromDateTime(DateTime.Today), 5_000);
        await _svc.CreateAsync(entry);

        var ok = await _svc.DeleteAsync(entry.Id);

        Assert.True(ok);
        Assert.Equal(0, _tdb.Db.Entries.Count());
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var ok = await _svc.DeleteAsync(Guid.NewGuid());
        Assert.False(ok);
    }

    [Fact]
    public async Task UpdateAsync_ChangesAmountAndReturnsTrue()
    {
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();
        var entry = MakeEntry(catId, payerId, methodId, DateOnly.FromDateTime(DateTime.Today), 10_000);
        await _svc.CreateAsync(entry);

        entry.Amount = 20_000;
        var ok = await _svc.UpdateAsync(entry);

        Assert.True(ok);
        Assert.Equal(20_000, _tdb.Db.Entries.First().Amount);
    }

    [Fact]
    public async Task GetMonthlyAsync_ReturnsOnlySpecifiedMonth()
    {
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();

        await _svc.CreateAsync(MakeEntry(catId, payerId, methodId, new DateOnly(2026, 9, 1), 10_000));
        await _svc.CreateAsync(MakeEntry(catId, payerId, methodId, new DateOnly(2026, 8, 1), 20_000));

        var result = await _svc.GetMonthlyAsync(2026, 9);

        Assert.Single(result);
        Assert.Equal(10_000, result[0].Amount);
    }

    [Fact]
    public async Task GetMonthlyAsync_ReturnsEmptyList_WhenNoEntries()
    {
        var result = await _svc.GetMonthlyAsync(2026, 9);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDailyCumulativeAsync_CalculatesCumulativeSums()
    {
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();

        await _svc.CreateAsync(MakeEntry(catId, payerId, methodId, new DateOnly(2026, 9,  1), 10_000));
        await _svc.CreateAsync(MakeEntry(catId, payerId, methodId, new DateOnly(2026, 9,  3), 20_000));
        await _svc.CreateAsync(MakeEntry(catId, payerId, methodId, new DateOnly(2026, 9,  5), 15_000));

        var result = await _svc.GetDailyCumulativeAsync(2026, 9);

        Assert.Equal(10_000, result[1]);  // Day 1
        Assert.Equal(10_000, result[2]);  // Day 2 (지출 없음)
        Assert.Equal(30_000, result[3]);  // Day 3
        Assert.Equal(30_000, result[4]);  // Day 4 (지출 없음)
        Assert.Equal(45_000, result[5]);  // Day 5
        Assert.Equal(45_000, result[30]); // Day 30 (이후 동일)
    }

    [Fact]
    public async Task GetMonthlyTotalByCategoryAsync_GroupsByParentName()
    {
        var cat1   = new Category      { ParentName = "식비", Name = "외식" };
        var cat2   = new Category      { ParentName = "교통", Name = "버스" };
        var payer  = new Payer         { Name = "남편" };
        var method = new PaymentMethod { Name = "카드" };
        _tdb.Db.Categories.AddRange(cat1, cat2);
        _tdb.Db.Payers.Add(payer);
        _tdb.Db.PaymentMethods.Add(method);
        await _tdb.Db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 1);
        await _svc.CreateAsync(new Entry { CategoryId = cat1.Id, PayerId = payer.Id, PaymentMethodId = method.Id, Date = date, Amount = 30_000, Content = "A" });
        await _svc.CreateAsync(new Entry { CategoryId = cat1.Id, PayerId = payer.Id, PaymentMethodId = method.Id, Date = date, Amount = 20_000, Content = "B" });
        await _svc.CreateAsync(new Entry { CategoryId = cat2.Id, PayerId = payer.Id, PaymentMethodId = method.Id, Date = date, Amount = 10_000, Content = "C" });

        var result = await _svc.GetMonthlyTotalByCategoryAsync(2026, 9);

        Assert.Equal(50_000, result["식비"]);
        Assert.Equal(10_000, result["교통"]);
    }
}
