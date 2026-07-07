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
    private const int DefaultBatchSize = 25;
    private const int DefaultDelayBetweenBatchesMs = 250;

    private readonly SecurityVault _securityVault;
    private string _authToken = string.Empty;
    public string DeviceIP { get; private set; }
    public string Port { get; private set; }
    private readonly HttpClient _httpClient;
    private ConcurrentBag<Webhook> _webhooks = [];
    private readonly JsonSerializerOptions _jsonOptions;

    public SmsService(SecurityVault vault, string ipAddress, string port, string? usr = null, string? pwd = null)
    {
        _securityVault = vault;
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

    public static async Task<SmsService> CreateNewInstance(SecurityVault vault, string ipAddress, string port, string usr, string pwd)
    {
        await TestArguments(ipAddress, port, usr, pwd);
        return new SmsService(vault, ipAddress, port, usr, pwd);
    }
    
    public async Task UpdateGatewayParameters(string? ipAddress = null, string? port = null, string? usr = null, string? pwd = null)
    {
        if (ipAddress == null && port == null && usr == null && pwd == null) return;
        
        ipAddress ??= DeviceIP;
        port      ??= Port;
        usr       ??= _securityVault.SmsGatewayUsername;
        if (pwd == null)
        {
            using var secret = _securityVault.GetGatewayPassword();
            pwd = secret?.Value ?? string.Empty;
        }

        await TestArguments(ipAddress, port, usr, pwd);

        DeviceIP = ipAddress;
        Port = port;
        UpdateToken(usr, pwd);
        _securityVault.SetGateWayCredentials(usr, pwd);
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
        var usr = username ?? _securityVault.SmsGatewayUsername;
        string? pwd;
        if (password != null) pwd = password;
        else
        {
            using var vaultPwd = _securityVault.GetGatewayPassword();
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
                else Console.WriteLine($"Successfully deregistered webhook ({wh.Id})!");
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

    public static IReadOnlyList<string[]> SplitIntoBatches(string[] numbers, int batchSize)
    {
        if (numbers == null || numbers.Length == 0)
        {
            return [];
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        var batches = new List<string[]>();
        for (var index = 0; index < numbers.Length; index += batchSize)
        {
            var batch = numbers.Skip(index).Take(batchSize).ToArray();
            batches.Add(batch);
        }

        return batches;
    }

    public async Task<List<Recipient>> SendMessageAsync(
        string subject,
        string message,
        string[] numbers,
        int batchSize = DefaultBatchSize,
        int delayBetweenBatchesMs = DefaultDelayBetweenBatchesMs,
        Action<IReadOnlyList<Recipient>>? onBatchCompleted = null)
    {
        var url = $"http://{DeviceIP}:{Port}/message";
        if(!string.IsNullOrEmpty(subject))  message = $"{subject}{Environment.NewLine}{Environment.NewLine}{message}";

        using var aesPassphraseAccessor = _securityVault.GetAesPassphrase();

        var isEncrypted = !string.IsNullOrEmpty(aesPassphraseAccessor?.Value ?? string.Empty);
        var encryptor = new AesEncryptor(aesPassphraseAccessor?.Value ?? string.Empty);

        var batches = SplitIntoBatches(numbers, batchSize);
        var recipients = new List<Recipient>();

        for (var index = 0; index < batches.Count; index++)
        {
            var batchNumbers = batches[index];
            var payloadNumbers = isEncrypted
                ? [.. batchNumbers.Select(n => encryptor.EncryptSMS(n))]
                : batchNumbers;

            var payload = new SendMessageSchema
            {
                TextMessage = new TextMessage{ Text = message },
                PhoneNumbers = payloadNumbers,
                IsEncrypted = isEncrypted
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"SMS batch {index + 1}/{batches.Count} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseString);
                continue;
            }

            var responseObj = JsonSerializer.Deserialize<SendMessageResponse>(responseString);
            var batchRecipients = responseObj?.Recipients ?? [];
            if(isEncrypted)
            {
                foreach(var r in batchRecipients)
                {
                    r.PhoneNumber = encryptor.DecryptSMS(r.PhoneNumber);
                }
            }

            recipients.AddRange(batchRecipients);
            onBatchCompleted?.Invoke(batchRecipients);

            if (index < batches.Count - 1 && delayBetweenBatchesMs > 0)
            {
                await Task.Delay(delayBetweenBatchesMs);
            }
        }

        return recipients;
    }

}
