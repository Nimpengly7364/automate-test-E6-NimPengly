using System.Text;
using System.Text.Json;
using SmartPark.Core.Interfaces;

namespace SmartPark.Api.HttpClients;

/// <summary>
/// Calls an external payment service over HTTP.
/// In production, this hits a real payment provider.
/// In tests, WireMock stands in for the external service.
/// </summary>
public class HttpPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;

    public HttpPaymentGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ProcessPaymentAsync(string ticketId, decimal amount)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, amount });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/payments/charge", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RefundAsync(string ticketId, decimal amount)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, amount });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/payments/refund", content);
        return response.IsSuccessStatusCode;
    }
}
