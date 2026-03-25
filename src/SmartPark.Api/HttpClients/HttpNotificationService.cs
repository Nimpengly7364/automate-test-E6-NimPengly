using System.Text;
using System.Text.Json;
using SmartPark.Core.Interfaces;

namespace SmartPark.Api.HttpClients;

/// <summary>
/// Calls an external notification/SMS service over HTTP.
/// In production, this hits a real SMS provider.
/// In tests, WireMock stands in for the external service.
/// </summary>
public class HttpNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;

    public HttpNotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendReceiptAsync(string phoneNumber, string receiptContent)
    {
        var payload = JsonSerializer.Serialize(new { phoneNumber, message = receiptContent });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/notifications/sms", content);
        response.EnsureSuccessStatusCode();
    }
}
