using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HouseholdBudget.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 테스트용 WebApplicationFactory — 임시 SQLite 파일과 임시 업로드 경로를 사용한다.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"hb_waf_{Guid.NewGuid()}.db");
    private readonly string _uploadPath =
        Path.Combine(Path.GetTempPath(), $"hb_uploads_{Guid.NewGuid()}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // SDK 10 + .slnx 조합에서 MvcTestingAppManifest.json 경로 이중화 버그 우회.
        // 매니페스트 대신 콘텐츠 루트를 직접 지정한다.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HouseholdBudget.csproj")))
            dir = dir.Parent;
        if (dir != null)
            builder.UseContentRoot(dir.FullName);

        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UploadPath"] = _uploadPath,
                ["ConnectionStrings:DefaultConnection"] = $"DataSource={_dbPath}"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Program.cs 에서 등록된 DbContext를 임시 SQLite 파일로 교체
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlite($"DataSource={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
            if (File.Exists(path)) File.Delete(path);
        if (Directory.Exists(_uploadPath))
            Directory.Delete(_uploadPath, recursive: true);
    }
}

public class ReceiptUploadTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public ReceiptUploadTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidJpeg_Returns200AndCorrectUrl()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // JPEG 헤더
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "receipt.jpg");

        var response = await _client.PostAsync("/api/receipts/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(url);
        Assert.StartsWith("/uploads/receipts/", url);
        Assert.EndsWith(".jpg", url);
    }

    [Fact]
    public async Task Upload_ValidPng_Returns200AndCorrectUrl()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG 헤더
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "receipt.png");

        var response = await _client.PostAsync("/api/receipts/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(url);
        Assert.EndsWith(".png", url);
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "empty.jpg");

        var response = await _client.PostAsync("/api/receipts/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WrongExtension_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "document.pdf");

        var response = await _client.PostAsync("/api/receipts/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OversizedFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var oversizedBytes = new byte[5 * 1024 * 1024 + 1]; // 5MB + 1 byte
        var fileContent = new ByteArrayContent(oversizedBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "big.jpg");

        var response = await _client.PostAsync("/api/receipts/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
