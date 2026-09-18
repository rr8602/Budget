using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;

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
    public async Task DeleteAsync_NonExistentId_ReturnsNull()
    {
        var ok = await _svc.DeleteAsync(Guid.NewGuid());
        Assert.Null(ok);
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

    // ── 동시성 테스트 ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedByOther_ReturnsNull()
    {
        // 상대방이 먼저 삭제한 항목을 다시 삭제하면 null
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();
        var entry = MakeEntry(catId, payerId, methodId, DateOnly.FromDateTime(DateTime.Today), 5_000);
        await _svc.CreateAsync(entry);

        using var db2 = MakeDb2();
        await new EntryService(db2).DeleteAsync(entry.Id);  // 상대방 삭제

        var result = await _svc.DeleteAsync(entry.Id);      // 내 삭제 시도
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_EntryDeletedByOther_ReturnsNull()
    {
        // 상대방이 삭제한 항목을 수정하면 null
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();
        var entry = MakeEntry(catId, payerId, methodId, DateOnly.FromDateTime(DateTime.Today), 10_000);
        await _svc.CreateAsync(entry);

        var loaded = await _svc.GetByIdAsync(entry.Id);     // 내가 편집 화면 진입

        using var db2 = MakeDb2();
        await new EntryService(db2).DeleteAsync(entry.Id);  // 상대방 삭제

        loaded!.Amount = 99_999;
        var result = await _svc.UpdateAsync(loaded);        // 내가 저장 시도
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentEdit_ReturnsNull()
    {
        // 상대방이 먼저 저장한 항목을 (stale 데이터로) 수정하면 null
        var (catId, payerId, methodId) = await _tdb.SeedLookupAsync();
        var entry = MakeEntry(catId, payerId, methodId, DateOnly.FromDateTime(DateTime.Today), 10_000);
        await _svc.CreateAsync(entry);

        var entryA = await _svc.GetByIdAsync(entry.Id);    // 내가 편집 화면 진입

        using var db2 = MakeDb2();
        var svc2   = new EntryService(db2);
        var entryB = await svc2.GetByIdAsync(entry.Id);
        entryB!.Amount = 99_000;
        var resultB = await svc2.UpdateAsync(entryB);       // 상대방이 먼저 저장
        Assert.True(resultB);

        entryA!.Amount = 55_000;
        var resultA = await _svc.UpdateAsync(entryA);       // 내가 저장 시도 (UpdatedAt 충돌)
        Assert.Null(resultA);
    }

    private AppDbContext MakeDb2()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_tdb.Connection).Options);

    // ────────────────────────────────────────────────────────────────

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
