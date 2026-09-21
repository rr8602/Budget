using HouseholdBudget.Components;
using HouseholdBudget.Data;
using HouseholdBudget.Data.Seed;
using HouseholdBudget.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(o =>
        o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30));

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass      = "mud-snackbar-location-top-center";
    config.SnackbarConfiguration.VisibleStateDuration   = 2000;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
});

// EF Core + SQLite (Factory: 메서드마다 단수명 DbContext 생성·폐기 → Blazor Server 추적 목록 문제 없음)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<EntryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<InstallmentService>();
builder.Services.AddScoped<FixedItemService>();

var warmedUp = false;
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
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = dbFactory.CreateDbContext();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(db);
}

// 전체 JIT 워밍업: 앱 시작 후 자신에게 HTTP 요청 → Blazor 렌더링·EF Core·MudBlazor 전체 JIT
// ApplicationStarted 이후 백그라운드 실행 → 서버 기동을 막지 않음
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(1000); // 서버 완전 바인딩 대기
        var rawUrl  = app.Urls.FirstOrDefault() ?? "http://localhost:8080";
        var baseUrl = rawUrl
            .Replace("://+:",       "://localhost:")
            .Replace("://0.0.0.0:", "://localhost:")
            .Replace("://[::]:",    "://localhost:");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        await http.GetAsync(baseUrl); // 대시보드 전체 SSR 렌더링 → 모든 JIT 완료
    }
    catch { /* 워밍업 실패는 무시 */ }
    finally { warmedUp = true; } // 성공·실패 무관 트래픽 수신 허용
}));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Railway는 프록시에서 HTTPS 처리 → 컨테이너 내부는 HTTP만 사용
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 워밍업 완료 여부를 Fly.io health check에 노출
app.MapGet("/health", () => warmedUp ? Results.Ok("ok") : Results.StatusCode(503));

app.Run();

public partial class Program { }
