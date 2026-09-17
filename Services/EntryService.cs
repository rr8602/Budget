using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class EntryService(AppDbContext db)
{
    public async Task<List<Entry>> GetMonthlyAsync(int year, int month)
    {
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
        return await db.Entries
            .Include(e => e.Category)
            .Include(e => e.Payer)
            .Include(e => e.PaymentMethod)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> CreateAsync(Entry entry)
    {
        try
        {
            db.Entries.Add(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Entry entry)
    {
        try
        {
            db.Entries.Update(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entry = await db.Entries.FindAsync(id);
            if (entry is null) return false;
            db.Entries.Remove(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 월별 카테고리 합계 (대시보드·예산 비교용)
    public async Task<Dictionary<string, decimal>> GetMonthlyTotalByCategoryAsync(int year, int month)
    {
        var entries = await db.Entries
            .Include(e => e.Category)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        return entries
            .GroupBy(e => e.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
    }

    // 결제자별 합계
    public async Task<Dictionary<string, decimal>> GetMonthlyTotalByPayerAsync(int year, int month)
    {
        var entries = await db.Entries
            .Include(e => e.Payer)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        return entries
            .GroupBy(e => e.Payer.Name)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
    }

    // 일별 누적 합계 (라인 차트용)
    public async Task<Dictionary<int, decimal>> GetDailyCumulativeAsync(int year, int month)
    {
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
