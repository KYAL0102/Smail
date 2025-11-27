using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.Models;

namespace Core;

public class SmsService
{
    private string AUTH_TOKEN;
    private string DEVICE_IP;
    private const int PORT = 8080;
    private HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SmsService(string ipAddress, string username, string password)
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
        };
        _httpClient = new HttpClient(handler);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        DEVICE_IP = ipAddress;
        AUTH_TOKEN = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AUTH_TOKEN);
    }

    public async Task SendMessageAsync(string message, List<string> numbers)
    {
        var url = $"http://{DEVICE_IP}:{PORT}/message";

        var payload = new SendMessageSchema
        {
          textMessage = new TextMessage{ text = message },
          phoneNumbers = numbers.ToArray()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request
        var response = await _httpClient.PostAsync(url, content);

        // Output response
        string responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {responseString}");
    }
}
