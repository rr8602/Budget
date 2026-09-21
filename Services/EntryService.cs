using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class EntryService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Entry>> GetMonthlyAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        return await db.Entries
            .Include(e => e.Category)
            .Include(e => e.Payer)
            .Include(e => e.PaymentMethod)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Entry>> GetRecentAsync(int count = 5)
    {
        await using var db = factory.CreateDbContext();
        return await db.Entries
            .Include(e => e.Category)
            .Include(e => e.Payer)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Entry?> GetByIdAsync(Guid id)
    {
        await using var db = factory.CreateDbContext();
        // SQLite TEXT 비교는 대소문자 구분 → 구 데이터에 대문자 GUID 있을 수 있어 UPPER() 사용
        return await db.Entries
            .FromSqlRaw("SELECT * FROM \"Entries\" WHERE UPPER(\"Id\") = UPPER({0})", id.ToString("D"))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(Entry entry)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            db.Entries.Add(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool?> UpdateAsync(Entry entry)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            // 네비게이션 객체를 null로 초기화 후 Update → EF가 Entry만 Modified로 처리
            entry.Category      = null!;
            entry.Payer         = null!;
            entry.PaymentMethod = null!;
            db.Entries.Update(entry);
            entry.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }

    public async Task<bool?> DeleteAsync(Guid id)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            var entry = await db.Entries.FindAsync(id);
            if (entry is null) return null;
            db.Entries.Remove(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyTotalByCategoryAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        var entries = await db.Entries
            .Include(e => e.Category)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        return entries
            .GroupBy(e => e.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyTotalByPayerAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        var entries = await db.Entries
            .Include(e => e.Payer)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        return entries
            .GroupBy(e => e.Payer.Name)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
    }

    public async Task<Dictionary<int, decimal>> GetDailyCumulativeAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        var entries = await db.Entries
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        var dailyTotals = entries
            .GroupBy(e => e.Date.Day)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var result = new Dictionary<int, decimal>();
        decimal cumulative = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            cumulative += dailyTotals.GetValueOrDefault(day, 0);
            result[day] = cumulative;
        }

        return result;
    }
}
