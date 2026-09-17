using HouseholdBudget.Data;
using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

// 결제자·결제수단 목록 조회 (드롭다운용)
public class LookupService(AppDbContext db)
{
    public async Task<List<Payer>> GetPayersAsync()
    {
        return await db.Payers.OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
    {
        return await db.PaymentMethods.OrderBy(p => p.Id).ToListAsync();
    }
}
