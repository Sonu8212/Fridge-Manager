using FridgeManager.Api.Data;
using FridgeManager.Api.Hubs;
using FridgeManager.Api.Jobs;
using FridgeManager.Api.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Services
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
builder.Services.AddScoped<IFridgeItemService, FridgeItemService>();
builder.Services.AddScoped<ExpiryCheckJob>();

// CORS for frontend
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FridgeManager API", Version = "v1" });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHangfireDashboard("/hangfire");

// Schedule daily expiry check at 8 AM
RecurringJob.AddOrUpdate<ExpiryCheckJob>(
    "expiry-check",
    job => job.RunAsync(),
    "0 8 * * *");

app.Run();
