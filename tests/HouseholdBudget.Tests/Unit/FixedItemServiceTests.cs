using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;

public class FixedItemServiceTests : IDisposable
{
    private readonly TestDatabase _tdb;
    private readonly FixedItemService _svc;

    public FixedItemServiceTests()
    {
        _tdb = new TestDatabase();
        _svc = new FixedItemService(_tdb.Db);
    }

    public void Dispose() => _tdb.Dispose();

    private async Task<FixedItem> AddFixedItemAsync(decimal amount, string parentName = "저축")
    {
        var cat = new Category { ParentName = parentName, Name = "적금" };
        _tdb.Db.Categories.Add(cat);
        await _tdb.Db.SaveChangesAsync();

        var item = new FixedItem { Name = "테스트 항목", CategoryId = cat.Id, Amount = amount, GroupType = "저축", Owner = "남편" };
        _tdb.Db.FixedItems.Add(item);
        await _tdb.Db.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task GetTotalAsync_SumsAllAmounts()
    {
        await AddFixedItemAsync(300_000, "저축");
        await AddFixedItemAsync(200_000, "보험");

        var total = await _svc.GetTotalAsync();

        Assert.Equal(500_000, total);
    }

    [Fact]
    public async Task GetTotalAsync_ReturnsZero_WhenNoItems()
    {
        var total = await _svc.GetTotalAsync();
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task UpsertActualAsync_CreatesNewRecord()
    {
        var item = await AddFixedItemAsync(300_000);

        var ok = await _svc.UpsertActualAsync(item.Id, 2026, 9, 300_000);

        Assert.True(ok);
        Assert.Equal(1, _tdb.Db.MonthlyFixedActuals.Count());
        Assert.Equal(300_000, _tdb.Db.MonthlyFixedActuals.First().ActualAmount);
    }

    [Fact]
    public async Task UpsertActualAsync_UpdatesExistingRecord()
    {
        var item = await AddFixedItemAsync(300_000);
        await _svc.UpsertActualAsync(item.Id, 2026, 9, 300_000);

        var ok = await _svc.UpsertActualAsync(item.Id, 2026, 9, 250_000);

        Assert.True(ok);
        Assert.Equal(1, _tdb.Db.MonthlyFixedActuals.Count()); // upsert → 행 1개만
        Assert.Equal(250_000, _tdb.Db.MonthlyFixedActuals.First().ActualAmount);
    }

    [Fact]
    public async Task UpsertActualAsync_DifferentMonths_CreatesSeparateRecords()
    {
        var item = await AddFixedItemAsync(300_000);

        await _svc.UpsertActualAsync(item.Id, 2026, 8, 300_000);
        await _svc.UpsertActualAsync(item.Id, 2026, 9, 300_000);

        Assert.Equal(2, _tdb.Db.MonthlyFixedActuals.Count());
    }

    [Fact]
    public async Task GetShortfallStatusAsync_MarksUnconfirmed_WhenNoActualEntered()
    {
        await AddFixedItemAsync(300_000);

        var statuses = await _svc.GetShortfallStatusAsync(2026, 9);

        Assert.Single(statuses);
        Assert.True(statuses[0].IsUnconfirmed);
        Assert.False(statuses[0].IsShortfall);
    }

    [Fact]
    public async Task GetShortfallStatusAsync_MarksShortfall_WhenActualLessThanPlanned()
    {
        var item = await AddFixedItemAsync(300_000);
        await _svc.UpsertActualAsync(item.Id, 2026, 9, 250_000);

        var statuses = await _svc.GetShortfallStatusAsync(2026, 9);

        Assert.Single(statuses);
        Assert.False(statuses[0].IsUnconfirmed);
        Assert.True(statuses[0].IsShortfall);
        Assert.Equal(50_000, statuses[0].Shortfall);
    }

    [Fact]
    public async Task GetShortfallStatusAsync_NotShortfall_WhenActualMeetsPlanned()
    {
        var item = await AddFixedItemAsync(300_000);
        await _svc.UpsertActualAsync(item.Id, 2026, 9, 300_000);

        var statuses = await _svc.GetShortfallStatusAsync(2026, 9);

        Assert.False(statuses[0].IsShortfall);
        Assert.False(statuses[0].IsUnconfirmed);
    }

    // ── 동시성 테스트 ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedByOther_ReturnsNull()
    {
        // 상대방이 먼저 삭제한 항목을 다시 삭제하면 null
        var item = await AddFixedItemAsync(300_000);

        using var db2 = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_tdb.Connection).Options);
        await new FixedItemService(db2).DeleteAsync(item.Id);  // 상대방 삭제

        var result = await _svc.DeleteAsync(item.Id);          // 내 삭제 시도
        Assert.Null(result);
    }
}
