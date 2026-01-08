using System;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosedXML.Parser;
using Core.ApiResponseClasses;
using Core.Models;

namespace Core.Services;

public class SmsService
{
    private readonly string _authToken;
    private readonly string _deviceIP;
    private readonly int _port;
    private readonly HttpClient _httpClient;
    private ConcurrentBag<Webhook> _webhooks = [];
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

        _ = RegisterWebhooks();
    }

    public async Task DeregisterWebhooksAsync()
    {
        List<Task> tasks = [];
        foreach(var wh in _webhooks)
        {
            var task = Task.Run(async () => 
            {
                var url = $"http://{_deviceIP}:{_port}/webhooks/{wh.Id}";

                var response = await _httpClient.DeleteAsync(url);

                if(!response.IsSuccessStatusCode) Console.WriteLine($"{response.StatusCode} - Failed to deregister webhook ({wh.Id})!");
                //else Console.WriteLine($"Successfully deregistered webhook ({wh.Id})!");
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }

    public async Task RegisterWebhooks()
    {
        var serverUrl = $"https://{NetworkManager.GetLocalIPv4()}:5001/api/webhook"; //TODO: get port somehow else
        var phoneUrl = $"http://{_deviceIP}:{_port}/webhooks";

        string[] toRegisterEvents = [ "sms:failed", "sms:sent", "sms:delivered" ];

        //TODO: Complete Webhook registration
        List<Task> tasks = [];
        foreach(var evt in toRegisterEvents)
        {
            var task = Task.Run(async () =>
            {
                var obj = new 
                {
                    url = serverUrl,
                    @event = evt
                };

                var json = JsonSerializer.Serialize(obj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(phoneUrl, content);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to register webhook '{evt}': {response.StatusCode}");
                    Console.WriteLine(body);
                    return;
                }
                
                var wh = JsonSerializer.Deserialize<Webhook>(body);
                if(wh != null) 
                {
                    //Console.WriteLine($"Successfully created and received webhook info -> {wh.Id}");
                    _webhooks.Add(wh);
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async Task<List<Recipient>> SendMessageAsync(string message, List<string> numbers)
    {
        var url = $"http://{_deviceIP}:{_port}/message";

        var encryptor = new AesEncryptor(Globals.AesPassphrase);
        var encryptedMessage = encryptor.Encrypt(message);
        var encryptedNumbers = numbers.Select(n => encryptor.Encrypt(n)).ToArray();

        var payload = new SendMessageSchema
        {
            TextMessage = new TextMessage{ Text = encryptedMessage },
            PhoneNumbers = encryptedNumbers
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request
        var response = await _httpClient.PostAsync(url, content);

        // Output response
        string responseString = await response.Content.ReadAsStringAsync();
        var responseObj = JsonSerializer.Deserialize<SendMessageResponse>(responseString);

        var recipients = responseObj!.Recipients;
        foreach(var r in recipients)
        {
            var encryptedNumber = r.PhoneNumber;
            r.PhoneNumber = encryptor.Decrypt(r.PhoneNumber);
        }

        return recipients;
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
