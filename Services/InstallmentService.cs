using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class InstallmentService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Installment>> GetAllAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Installments.OrderBy(i => i.LastPayDate).ToListAsync();
    }

    public async Task<bool> CreateAsync(Installment item)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            db.Installments.Add(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool?> UpdateAsync(Installment item)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            db.Installments.Update(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }

    public async Task<bool?> DeleteAsync(int id)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            var item = await db.Installments.FindAsync(id);
            if (item is null) return null;
            db.Installments.Remove(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }
}
