using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using Scalar.AspNetCore;
using SimpleAutomaticStorageSystem.Server.Backgrounds;
using SimpleAutomaticStorageSystem.Server.Infrastructures;
using SimpleAutomaticStorageSystem.Server.Repositories;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// NLog設定
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// DB接続設定
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));

// JOB状態の監視時間設定
builder.Services.Configure<TimeoutSettings>(
    builder.Configuration.GetSection("TimeoutSettings"));

// HTTP通信設定
builder.Services.Configure<HttpSettings>(
    builder.Configuration.GetSection("HttpSettings"));

// HTTP通信設定
builder.Services.Configure<EquipmentSettings>(
    builder.Configuration.GetSection("EquipmentSettings"));

// UseCases
builder.Services.AddScoped<JobIssuer>();
builder.Services.AddScoped<JobAssigner>();
builder.Services.AddScoped<JobManager>();
builder.Services.AddScoped<JobViewer>();
builder.Services.AddScoped<InventoryViewer>();

// Repositories
builder.Services.AddScoped<IJobsRepository, JobsRepository>();
builder.Services.AddScoped<IItemsRepository, ItemsRepository>();
builder.Services.AddScoped<IEquipmentsRepository, EquipmentsRepository>();

// Shared
builder.Services.AddScoped<ClientValidator>();

// Infrastructures
builder.Services.AddHttpClient<IJobDispatcher, JobDispatcher>((sp, client) =>
{
    // Http設定
    HttpSettings httpSettings =
        sp.GetRequiredService<IOptions<HttpSettings>>().Value;

    client.Timeout = TimeSpan.FromSeconds(httpSettings.PushTimeoutSeconds);
});

// Background Services
builder.Services.AddHostedService<TimeoutMonitorService>();

// Razor Pages
builder.Services.AddRazorPages();

// Controllers
builder.Services.AddControllers();


// Json設定
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enumを文字列に変換する
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

    // シリアライズ時のプロパティ名をキャメルケースにする
    options.SerializerOptions.PropertyNamingPolicy =
        JsonNamingPolicy.CamelCase;

    // デシリアライズ時のプロパティ名は大文字・小文字を区別しない
    options.SerializerOptions.PropertyNameCaseInsensitive = true;

});

// OpenApi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

app.UseStaticFiles();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Controllers
app.MapControllers();

// OpenApi
app.MapOpenApi();
app.MapScalarApiReference(
    endpointPrefix: "/docs",
    configureOptions: options =>
    {
        options
            .WithTitle("簡易自動倉庫管理システムAPI")
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient);
    });

// ロギング設定
ILogger<Program> logger =
    app.Services.GetRequiredService<ILogger<Program>>();

app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("サーバー起動");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("サーバー停止");
});

try
{
    // アプリ起動
    await app.RunAsync();
}
finally
{
    // バッファを書き込んで解放
    LogManager.Shutdown();
}
