using HouseholdBudget.Components;
using HouseholdBudget.Data;
using HouseholdBudget.Data.Seed;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();

// MudBlazor
builder.Services.AddMudServices();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<EntryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<InstallmentService>();
builder.Services.AddScoped<FixedItemService>();

var app = builder.Build();

// Production: 기존 DB를 /data 볼륨으로 이전 (최초 1회)
if (app.Environment.IsProduction())
{
    const string newPath = "/data/household.db";
    const string oldPath = "/app/household_budget.db";
    if (!File.Exists(newPath) && File.Exists(oldPath))
    {
        File.Copy(oldPath, newPath);
        foreach (var ext in new[] { "-wal", "-shm" })
            if (File.Exists(oldPath + ext))
                File.Copy(oldPath + ext, newPath + ext);
    }
}

// DB 마이그레이션 및 초기 데이터 적용
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Railway는 프록시에서 HTTPS 처리 → 컨테이너 내부는 HTTP만 사용
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();

// 업로드 파일 서빙: UploadPath 환경변수가 있으면 Persistent Volume 경로 사용
var uploadBasePath = app.Configuration["UploadPath"];
if (!string.IsNullOrEmpty(uploadBasePath))
{
    Directory.CreateDirectory(uploadBasePath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadBasePath),
        RequestPath = "/uploads"
    });
}
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 영수증 이미지 업로드 엔드포인트
app.MapPost("/api/receipts/upload", async (IFormFile file, IWebHostEnvironment env, IConfiguration cfg) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest("파일이 없습니다.");

    const long maxSize = 5 * 1024 * 1024;
    if (file.Length > maxSize)
        return Results.BadRequest("파일 크기는 5MB 이하만 가능합니다.");

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        return Results.BadRequest("이미지 파일만 가능합니다.");

    var basePath = cfg["UploadPath"] ?? Path.Combine(env.WebRootPath, "uploads");
    var uploadDir = Path.Combine(basePath, "receipts");
    Directory.CreateDirectory(uploadDir);

    var fileName = $"{Guid.NewGuid()}{ext}";
    var filePath = Path.Combine(uploadDir, fileName);

    await using var stream = File.Create(filePath);
    await file.CopyToAsync(stream);

    return Results.Ok($"/uploads/receipts/{fileName}");
})
.DisableAntiforgery();

app.Run();

public partial class Program { }
