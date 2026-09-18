using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public record FixedActualStatus(
    int FixedItemId, string GroupType, string Owner, string Name,
    decimal Planned, decimal? Actual)
{
    public decimal Shortfall     => Planned - (Actual ?? 0);
    public bool    IsShortfall   => Actual.HasValue && Actual.Value < Planned;
    public bool    IsUnconfirmed => !Actual.HasValue;
}

public class FixedItemService(AppDbContext db)
{
    public async Task<List<FixedItem>> GetAllAsync()
        => await db.FixedItems
            .Include(f => f.Category)
            .OrderBy(f => f.GroupType)
            .ThenBy(f => f.Name)
            .ToListAsync();

    public async Task<decimal> GetTotalAsync()
    {
        var amounts = await db.FixedItems.Select(f => f.Amount).ToListAsync();
        return amounts.Sum();
    }

    public async Task<bool> CreateAsync(FixedItem item)
    {
        try
        {
            db.FixedItems.Add(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<List<FixedActualStatus>> GetShortfallStatusAsync(int year, int month)
    {
        var items   = await GetAllAsync();
        var actuals = await db.MonthlyFixedActuals
            .Where(a => a.Year == year && a.Month == month)
            .ToListAsync();
        var map = actuals.ToDictionary(a => a.FixedItemId, a => a.ActualAmount);
        return items.Select(f => new FixedActualStatus(
            f.Id, f.GroupType, f.Owner, f.Name, f.Amount,
            map.TryGetValue(f.Id, out var v) ? v : null)).ToList();
    }

    public async Task<bool> UpsertActualAsync(int fixedItemId, int year, int month, decimal amount)
    {
        try
        {
            var existing = await db.MonthlyFixedActuals
                .FirstOrDefaultAsync(a => a.FixedItemId == fixedItemId
                                       && a.Year == year && a.Month == month);
            if (existing is null)
                db.MonthlyFixedActuals.Add(new MonthlyFixedActual
                    { FixedItemId = fixedItemId, Year = year, Month = month, ActualAmount = amount });
            else
                existing.ActualAmount = amount;
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool?> DeleteAsync(int id)
    {
        try
        {
            var item = await db.FixedItems.FindAsync(id);
            if (item is null) return null;
            db.FixedItems.Remove(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }
}
