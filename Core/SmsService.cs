using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.Models;

namespace Core;

public class SmsService
{
    private const string USERNAME = "admin";
    private const string PASSWORD = "admin123";
    private const string DEVICE_IP = "192.168.100.93";
    private const int PORT = 8080;
    private HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private static SmsService? _smsService = null;
    public static SmsService Instance => _smsService == null ? new() : _smsService;

    public SmsService()
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

        // Set up basic authentication
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{USERNAME}:{PASSWORD}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
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
