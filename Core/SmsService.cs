using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosedXML.Parser;
using Core.ApiResponseClasses;
using Core.Models;

namespace Core;

public class SmsService
{
    private string _authToken;
    private string _deviceIP;
    private int _port;
    private HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SmsService(string ipAddress, int port, string username, string password)
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

        _deviceIP = ipAddress;
        _port = port;
        _authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authToken);
    }

    public async Task<List<Recipient>> SendMessageAsync(string message, List<string> numbers)
    {
        var url = $"http://{_deviceIP}:{_port}/message";

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
        var responseObj = JsonSerializer.Deserialize<SendMessageResponse>(responseString);

        return responseObj!.Recipients;
    }

    public async Task<bool> IsDeviceReachableAsync()
    {
        try
        {
            var url = $"http://{_deviceIP}:{_port}/";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

}
