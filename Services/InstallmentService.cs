using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class InstallmentService(AppDbContext db)
{
    public async Task<List<Installment>> GetAllAsync()
        => await db.Installments.OrderBy(i => i.LastPayDate).ToListAsync();

    public async Task<bool> CreateAsync(Installment item)
    {
        try
        {
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
            db.Installments.Update(item);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;  // 이미 삭제된 항목
        }
        catch { return false; }
    }

    public async Task<bool?> DeleteAsync(int id)
    {
        try
        {
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
