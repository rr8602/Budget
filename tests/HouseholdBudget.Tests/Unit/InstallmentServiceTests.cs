using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;

public class InstallmentServiceTests : IDisposable
{
    private readonly TestDatabase _tdb;
    private readonly InstallmentService _svc;

    public InstallmentServiceTests()
    {
        _tdb = new TestDatabase();
        _svc = new InstallmentService(_tdb.Db);
    }

    public void Dispose() => _tdb.Dispose();

    private static Installment MakeInstallment()
        => new() { Name = "냉장고", Card = "신한카드", TotalAmount = 1_200_000, Months = 12,
                   LastPayDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(11)) };

    private AppDbContext MakeDb2()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_tdb.Connection).Options);

    [Fact]
    public async Task CreateAsync_AddsInstallmentAndReturnsTrue()
    {
        var ok = await _svc.CreateAsync(MakeInstallment());
        Assert.True(ok);
        Assert.Equal(1, _tdb.Db.Installments.Count());
    }

    [Fact]
    public async Task DeleteAsync_RemovesInstallment()
    {
        var item = MakeInstallment();
        await _svc.CreateAsync(item);
        var result = await _svc.DeleteAsync(item.Id);
        Assert.True(result);
        Assert.Equal(0, _tdb.Db.Installments.Count());
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNull()
    {
        var result = await _svc.DeleteAsync(99999);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndReturnsTrue()
    {
        var item = MakeInstallment();
        await _svc.CreateAsync(item);
        item.Name = "세탁기";
        var result = await _svc.UpdateAsync(item);
        Assert.True(result);
        Assert.Equal("세탁기", _tdb.Db.Installments.First().Name);
    }

    // ── 동시성 테스트 ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedByOther_ReturnsNull()
    {
        // 상대방이 먼저 삭제한 항목을 다시 삭제하면 null
        var item = MakeInstallment();
        await _svc.CreateAsync(item);

        using var db2 = MakeDb2();
        await new InstallmentService(db2).DeleteAsync(item.Id);  // 상대방 삭제

        var result = await _svc.DeleteAsync(item.Id);            // 내 삭제 시도
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ItemDeletedByOther_ReturnsNull()
    {
        // 상대방이 삭제한 항목을 수정하면 null
        var item = MakeInstallment();
        await _svc.CreateAsync(item);

        using var db2 = MakeDb2();
        await new InstallmentService(db2).DeleteAsync(item.Id);  // 상대방 삭제

        item.Name = "TV";
        var result = await _svc.UpdateAsync(item);               // 내 수정 시도
        Assert.Null(result);
    }
}
