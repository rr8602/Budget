using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class CategoryService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Category>> GetAllAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Categories.OrderBy(c => c.ParentName).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<List<string>> GetParentNamesAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Categories.Select(c => c.ParentName).Distinct().OrderBy(x => x).ToListAsync();
    }

    public async Task<List<Category>> GetByParentAsync(string parentName)
    {
        await using var db = factory.CreateDbContext();
        return await db.Categories.Where(c => c.ParentName == parentName).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<string?> GetParentNameByIdAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.Categories.Where(c => c.Id == id).Select(c => c.ParentName).FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(string parentName, string name)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            var exists = await db.Categories.AnyAsync(c => c.ParentName == parentName && c.Name == name);
            if (exists) return false;
            db.Categories.Add(new Category { ParentName = parentName, Name = name });
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool?> DeleteAsync(int id)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            var cat = await db.Categories.FindAsync(id);
            if (cat is null) return null;
            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException) { return null; }
        catch { return false; }
    }
}
