using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;

public class CategoryServiceTests : IDisposable
{
    private readonly TestDatabase _tdb;
    private readonly CategoryService _svc;

    public CategoryServiceTests()
    {
        _tdb = new TestDatabase();
        _svc = new CategoryService(_tdb.Db);
    }

    public void Dispose() => _tdb.Dispose();

    private async Task<Category> AddCategoryAsync(string parent = "식비", string name = "외식")
    {
        var cat = new Category { ParentName = parent, Name = name };
        _tdb.Db.Categories.Add(cat);
        await _tdb.Db.SaveChangesAsync();
        return cat;
    }

    private AppDbContext MakeDb2()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_tdb.Connection).Options);

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var cat = await AddCategoryAsync();
        var result = await _svc.DeleteAsync(cat.Id);
        Assert.True(result);
        Assert.Equal(0, _tdb.Db.Categories.Count());
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNull()
    {
        var result = await _svc.DeleteAsync(99999);
        Assert.Null(result);
    }

    // ── 동시성 테스트 ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedByOther_ReturnsNull()
    {
        // 상대방이 먼저 삭제한 항목을 다시 삭제하면 null
        var cat = await AddCategoryAsync();

        using var db2 = MakeDb2();
        await new CategoryService(db2).DeleteAsync(cat.Id);  // 상대방 삭제

        var result = await _svc.DeleteAsync(cat.Id);         // 내 삭제 시도
        Assert.Null(result);
    }
}
