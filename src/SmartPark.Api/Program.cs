using SmartPark.Api.HttpClients;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers & OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Core services ---
builder.Services.AddSingleton<ParkingFeeCalculator>();
builder.Services.AddSingleton<InMemoryParkingRepository>();
builder.Services.AddSingleton<IParkingRepository>(sp => sp.GetRequiredService<InMemoryParkingRepository>());

// --- External service clients (HTTP-based, configurable via appsettings) ---
builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:PaymentGatewayUrl"]
        ?? "http://localhost:9091");
});

builder.Services.AddHttpClient<INotificationService, HttpNotificationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:NotificationServiceUrl"]
        ?? "http://localhost:9092");
});

// --- Membership service (simple dictionary for demo) ---
builder.Services.AddSingleton<IMembershipService>(new DemoMembershipService());

// --- DateTime provider ---
builder.Services.AddSingleton<IDateTimeProvider>(new SystemDateTimeProvider());

// --- Session manager (depends on all above) ---
builder.Services.AddScoped<ParkingSessionManager>(sp => new ParkingSessionManager(
    sp.GetRequiredService<ParkingFeeCalculator>(),
    sp.GetRequiredService<IPaymentGateway>(),
    sp.GetRequiredService<INotificationService>(),
    sp.GetRequiredService<IMembershipService>(),
    sp.GetRequiredService<IParkingRepository>(),
    sp.GetRequiredService<IDateTimeProvider>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();

// --- Simple implementations for the API demo ---

class DemoMembershipService : IMembershipService
{
    private static readonly Dictionary<string, MembershipTier> Members = new()
    {
        ["PP-1234"] = MembershipTier.Gold,
        ["BTB-9999"] = MembershipTier.Platinum,
        ["KT-0042"] = MembershipTier.Silver,
    };

    public MembershipTier GetMembershipTier(string licensePlate)
        => Members.GetValueOrDefault(licensePlate.ToUpper(), MembershipTier.Guest);
}

class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
}

// Required for WebApplicationFactory in integration tests
public partial class Program { }
