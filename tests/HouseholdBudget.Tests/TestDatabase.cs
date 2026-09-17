using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 각 테스트마다 독립된 SQLite in-memory DB를 제공한다.
/// Connection을 열어두는 동안만 in-memory DB가 유지되므로 Dispose 전까지 보관한다.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    public AppDbContext Db { get; }

    public TestDatabase()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .Options;

        Db = new AppDbContext(opts);
        Db.Database.EnsureCreated();
    }

    /// <summary>Category, Payer, PaymentMethod 시드 후 ID 반환</summary>
    public async Task<(int catId, int payerId, int methodId)> SeedLookupAsync(
        string parentName = "식비", string catName = "외식",
        string payerName = "남편", string methodName = "남편 카드")
    {
        var cat    = new Category      { ParentName = parentName, Name = catName };
        var payer  = new Payer         { Name = payerName };
        var method = new PaymentMethod { Name = methodName };
        Db.Categories.Add(cat);
        Db.Payers.Add(payer);
        Db.PaymentMethods.Add(method);
        await Db.SaveChangesAsync();
        return (cat.Id, payer.Id, method.Id);
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
