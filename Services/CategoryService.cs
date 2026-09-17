using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public class CategoryService(AppDbContext db)
{
    public async Task<List<Category>> GetAllAsync()
    {
        return await db.Categories.OrderBy(c => c.ParentName).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<List<string>> GetParentNamesAsync()
    {
        return await db.Categories.Select(c => c.ParentName).Distinct().OrderBy(x => x).ToListAsync();
    }

    public async Task<List<Category>> GetByParentAsync(string parentName)
    {
        return await db.Categories.Where(c => c.ParentName == parentName).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<bool> CreateAsync(string parentName, string name)
    {
        try
        {
            var exists = await db.Categories.AnyAsync(c => c.ParentName == parentName && c.Name == name);
            if (exists) return false;
            db.Categories.Add(new Category { ParentName = parentName, Name = name });
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat is null) return false;
            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }
}
