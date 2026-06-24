using Asp.Versioning;
using FridgeManager.Api.Common;
using FridgeManager.Api.Data;
using FridgeManager.Api.Hubs;
using FridgeManager.Api.Jobs;
using FridgeManager.Api.Middleware;
using FridgeManager.Api.Repositories;
using FridgeManager.Api.Services;
using FridgeManager.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/fridge-.log", rollingInterval: RollingInterval.Day));

    // Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Hangfire
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions { CommandBatchMaxTimeout = TimeSpan.FromMinutes(5) }));
    builder.Services.AddHangfireServer();

    // SignalR
    builder.Services.AddSignalR();

    // HTTP client for recipe API
    builder.Services.AddHttpClient<IRecipeService, RecipeService>();

    // Repositories
    builder.Services.AddScoped<IFridgeItemRepository, FridgeItemRepository>();
    builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();

    // Services
    builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
    builder.Services.AddScoped<IFridgeItemService, FridgeItemService>();
    builder.Services.AddScoped<ExpiryCheckJob>();

    // Common
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

    // FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateFridgeItemValidator>();

    // CORS
    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // API versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "FridgeManager API", Version = "v1" });
    });

    var app = builder.Build();

    // Global exception handling (before anything else)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Migrate only in development; in production use CI/CD pipeline
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();
    app.UseCors("Frontend");
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health");
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthFilter()]
    });

    RecurringJob.AddOrUpdate<ExpiryCheckJob>(
        "expiry-check",
        job => job.RunAsync(CancellationToken.None),
        "0 8 * * *");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
