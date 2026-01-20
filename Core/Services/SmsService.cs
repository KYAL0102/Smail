using System;
using System.Security;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosedXML.Parser;
using Core.Models.ApiResponseClasses;
using Core.Models;
using System.Net;

namespace Core.Services;

public class SmsService
{
    private string _authToken = string.Empty;
    public string DeviceIP { get; private set; }
    public string Port { get; private set; }
    private readonly HttpClient _httpClient;
    private ConcurrentBag<Webhook> _webhooks = [];
    private readonly JsonSerializerOptions _jsonOptions;

    public SmsService(string ipAddress, string port, string? usr = null, string? pwd = null)
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

        UpdateToken(usr, pwd);

        DeviceIP = ipAddress;
        Port = port;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authToken);
    }

    public static async Task<SmsService> CreateNewInstance(string ipAddress, string port, string usr, string pwd)
    {
        await TestArguments(ipAddress, port, usr, pwd);
        return new SmsService(ipAddress, port, usr, pwd);
    }

    public async Task UpdateGatewayParameters(string? ipAddress = null, string? port = null, string? usr = null, string? pwd = null)
    {
        if (ipAddress == null && port == null && usr == null && pwd == null) return;
        
        ipAddress ??= DeviceIP;
        port      ??= Port;
        usr       ??= SecurityVault.Instance.GetUsername();
        if (pwd == null)
        {
            using var secret = SecurityVault.Instance.GetGatewayPassword();
            pwd = secret.Value ?? string.Empty;
        }

        await TestArguments(ipAddress, port, usr, pwd);

        DeviceIP = ipAddress;
        Port = port;
        UpdateToken(usr, pwd);
        SecurityVault.Instance.SetGateWayCredentials(usr, pwd);
    }

    public static async Task TestArguments(string ipAddress, string port, string usr, string pwd)
    {
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{usr}:{pwd}"));

        var response = await IsDeviceReachableAsync(ipAddress, port, token);

        if (response == null) 
        {
            Console.WriteLine("Response was null");
            return;
        }
        else if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{response.StatusCode} - {response.ReasonPhrase}");
    }

    private static async Task<HttpResponseMessage?> IsDeviceReachableAsync(string ip, string port, string? token = null)
    {
        var httpClient = new HttpClient();
        if (token != null) httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        var url = $"http://{ip}:{port}/";
        var response = await httpClient.GetAsync(url);
        return response;
    }

    private void UpdateToken(string? username = null, string? password = null)
    {
        var usr = username ?? SecurityVault.Instance.GetUsername();
        string? pwd;
        if (password != null) pwd = password;
        else
        {
            using var vaultPwd = SecurityVault.Instance.GetGatewayPassword();
            pwd = vaultPwd.Value;
        }

        if (usr == string.Empty || pwd == null) 
        {
            Console.WriteLine($"Either username was empty or password was null.");
            return;
        }

        _authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{usr}:{pwd}"));
    }

    public async Task DeregisterWebhooksAsync()
    {
        List<Task> tasks = [];
        foreach(var wh in _webhooks)
        {
            var task = Task.Run(async () => 
            {
                var url = $"http://{DeviceIP}:{Port}/webhooks/{wh.Id}";

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
        var phoneUrl = $"http://{DeviceIP}:{Port}/webhooks";

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
        var url = $"http://{DeviceIP}:{Port}/message";

        using var aesPassphraseAccessor = SecurityVault.Instance.GetAesPassphrase();
        var aesPassphrase = aesPassphraseAccessor.Value ?? string.Empty;

        var encryptor = new AesEncryptor(aesPassphrase);
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

}
