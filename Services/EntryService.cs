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
        // FindAsync: EF Core 로컬 추적 캐시 우선 확인 → 없으면 DB 조회
        var entry = await db.Entries.FindAsync(id);
        if (entry is null) return null;

        // 내비게이션 프로퍼티가 아직 로드되지 않은 경우에만 조회
        if (entry.Category is null)
            await db.Entry(entry).Reference(e => e.Category).LoadAsync();
        if (entry.Payer is null)
            await db.Entry(entry).Reference(e => e.Payer).LoadAsync();
        if (entry.PaymentMethod is null)
            await db.Entry(entry).Reference(e => e.PaymentMethod).LoadAsync();

        return entry;
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

    public async Task<bool?> UpdateAsync(Entry entry)
    {
        try
        {
            db.Entries.Update(entry);
            entry.UpdatedAt = DateTime.UtcNow;  // Update() 호출 후 변경 → EF가 original/current 구분
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;  // 삭제됐거나 다른 사람이 먼저 수정함
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool?> DeleteAsync(Guid id)
    {
        try
        {
            var entry = await db.Entries.FindAsync(id);
            if (entry is null) return null;
            db.Entries.Remove(entry);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;  // FindAsync가 EF 캐시를 반환했으나 DB에서는 이미 삭제됨
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
