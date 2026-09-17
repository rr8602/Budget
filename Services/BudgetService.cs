using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public record BudgetStatus(string CategoryParentName, decimal Budget, decimal Actual)
{
    public decimal Remaining     => Budget - Actual;
    public double  Percentage    => Budget > 0 ? Math.Min((double)(Actual / Budget * 100), 200) : 0;
    public bool    IsOverBudget  => Actual > Budget && Budget > 0;
    public bool    IsWarning     => Percentage >= 80 && !IsOverBudget;
}

public record BudgetEditRow(string CategoryParentName, decimal FixedTotal, decimal MonthlyExtra, decimal Actual)
{
    public decimal TotalBudget => FixedTotal + MonthlyExtra;
    public decimal Remaining   => TotalBudget - Actual;
}

public class BudgetService(AppDbContext db)
{
    // 대분류별 월 예산 = FixedItems 합산 + Budgets.MonthlyExtra
    public async Task<Dictionary<string, decimal>> GetMonthlyBudgetAsync()
    {
        var fixedItems = await db.FixedItems
            .Include(f => f.Category)
            .ToListAsync();

        var fromFixed = fixedItems
            .GroupBy(f => f.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount));

        var extras = await db.Budgets
            .ToDictionaryAsync(b => b.CategoryParentName, b => b.MonthlyExtra);

        return fromFixed.Keys.Union(extras.Keys).ToDictionary(
            p => p,
            p => fromFixed.GetValueOrDefault(p) + extras.GetValueOrDefault(p)
        );
    }

    public async Task<decimal> GetTotalMonthlyBudgetAsync()
        => (await GetMonthlyBudgetAsync()).Values.Sum();

    // 예산 편집 행 목록 (모든 대분류 + 이달 실지출)
    public async Task<List<BudgetEditRow>> GetBudgetEditRowsAsync(int year, int month)
    {
        var parentNames = await db.Categories
            .Select(c => c.ParentName).Distinct().OrderBy(n => n).ToListAsync();

        var fixedItems = await db.FixedItems.Include(f => f.Category).ToListAsync();
        var fixedTotals = fixedItems
            .GroupBy(f => f.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount));

        var extras = await db.Budgets
            .ToDictionaryAsync(b => b.CategoryParentName, b => b.MonthlyExtra);

        var entries = await db.Entries
            .Include(e => e.Category)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();
        var actuals = entries
            .GroupBy(e => e.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return parentNames.Select(p => new BudgetEditRow(
            p,
            fixedTotals.GetValueOrDefault(p),
            extras.GetValueOrDefault(p),
            actuals.GetValueOrDefault(p)
        )).ToList();
    }

    // 추가 예산 저장 (upsert)
    public async Task<bool> SaveMonthlyExtraAsync(string categoryParentName, decimal amount)
    {
        try
        {
            var existing = await db.Budgets
                .FirstOrDefaultAsync(b => b.CategoryParentName == categoryParentName);
            if (existing is null)
                db.Budgets.Add(new Budget { CategoryParentName = categoryParentName, MonthlyExtra = amount });
            else
                existing.MonthlyExtra = amount;
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    // 예산 vs 실제 현황 목록
    public async Task<List<BudgetStatus>> GetBudgetStatusAsync(int year, int month)
    {
        var budgets = await GetMonthlyBudgetAsync();

        var monthEntries = await db.Entries
            .Include(e => e.Category)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync();

        var actuals = monthEntries
            .GroupBy(e => e.Category.ParentName)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return budgets.Keys.Union(actuals.Keys)
            .Select(p => new BudgetStatus(p, budgets.GetValueOrDefault(p), actuals.GetValueOrDefault(p)))
            .Where(s => s.Budget > 0 || s.Actual > 0)
            .OrderByDescending(s => s.Actual)
            .ToList();
    }
}
