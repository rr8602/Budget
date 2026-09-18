using HouseholdBudget.Data;
using HouseholdBudget.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 실제 SQLite 파일 + EF 마이그레이션을 사용한 통합 테스트.
/// 테스트마다 고유한 임시 파일을 생성하고 종료 시 삭제한다.
/// </summary>
public class MigrationTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"hb_migration_{Guid.NewGuid()}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
            if (File.Exists(path)) File.Delete(path);
    }

    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource={_dbPath}")
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task Migrate_CompletesWithoutError()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync(); // 예외 없이 완료돼야 함
    }

    [Fact]
    public async Task Migrate_AllTablesAccessible()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        // 각 테이블에 COUNT 쿼리 → 테이블이 존재함을 확인
        Assert.Equal(0, await db.Entries.CountAsync());
        Assert.Equal(0, await db.Categories.CountAsync());
        Assert.Equal(0, await db.FixedItems.CountAsync());
        Assert.Equal(0, await db.Budgets.CountAsync());
        Assert.Equal(0, await db.Installments.CountAsync());
        Assert.Equal(0, await db.MonthlyFixedActuals.CountAsync());
    }

    [Fact]
    public async Task Migrate_IsIdempotent()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync(); // 두 번 실행해도 오류 없음
    }

    [Fact]
    public async Task Seed_CreatesRequiredLookupData()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        await DataSeeder.SeedAsync(db);

        Assert.True(await db.Categories.AnyAsync());
        Assert.True(await db.Payers.AnyAsync());
        Assert.True(await db.PaymentMethods.AnyAsync());
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        await DataSeeder.SeedAsync(db);

        var countAfterFirst = await db.Categories.CountAsync();

        await DataSeeder.SeedAsync(db); // 두 번 실행해도 중복 없음

        Assert.Equal(countAfterFirst, await db.Categories.CountAsync());
    }
}
