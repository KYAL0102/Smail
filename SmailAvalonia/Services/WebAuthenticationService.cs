using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Core.Models;
using Core.Models.EmailAuthentication;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;

namespace SmailAvalonia.Services;

public static class WebAuthenticationService
{
    private static TaskCompletionSource<string>? _manualUrlTaskSource;

    public static void SetManualUrl(string url) 
        => _manualUrlTaskSource?.TrySetResult(url);

    public static async Task<LoginResult> GetTokenFromUserWebPermissionAsync(Provider provider, CancellationToken ct, string email = "")
    {
        var secrets = LoadSecretsFromJson(provider.SecretsPath);
        string? secret = null;
        if (provider.Name == "Google") secret = secrets.ClientSecret;

        var port = 45454;
        var options = new OidcClientOptions
        {
            Authority = $"https://{provider.AuthorityUrl}",
            ClientId = secrets.ClientId,
            ClientSecret = secret,
            Scope = provider.Scope,
            RedirectUri = $"http://localhost:{port}",
            Browser = new SystemBrowser(port),//new SystemBrowser(port: 45454), 
            Policy = new Policy 
            { 
                Discovery = new DiscoveryPolicy
                {
                    ValidateEndpoints = false,
                    ValidateIssuerName = false
                },
                //RequireAccessTokenHash = true 
            },
            LoadProfile = true
        };

        var client = new OidcClient(options);

        var loginParams = new Parameters { { "prompt", "consent" } };
        if(!string.IsNullOrEmpty(email)) loginParams.Add("login_hint", email);
        if(provider.Name == "Google") loginParams.Add("access_type", "offline");
        
        var state = await client.PrepareLoginAsync(loginParams, ct);
        _manualUrlTaskSource = new TaskCompletionSource<string>();

        using (ct.Register(() => _manualUrlTaskSource.TrySetCanceled()))
        {
            var browserTask = options.Browser.InvokeAsync(new BrowserOptions(state.StartUrl, options.RedirectUri), ct);
            var manualTask = _manualUrlTaskSource.Task;

            try 
            {
                var completedTask = await Task.WhenAny(browserTask, manualTask);

                ct.ThrowIfCancellationRequested();

                string finalUrl;
                if (completedTask == manualTask) 
                {
                    finalUrl = await manualTask;
                }
                else
                {
                    var browserResult = await browserTask;
                    if (browserResult.ResultType == BrowserResultType.UserCancel)
                        throw new OperationCanceledException();
                    
                    if (browserResult.ResultType != BrowserResultType.Success)
                        throw new Exception($"Browser failed: {browserResult.Error}");
                    
                    finalUrl = browserResult.Response;
                }

                var result = await client.ProcessResponseAsync(finalUrl, state);

                if (result.IsError)
                    throw new Exception($"{result.Error}");

                //Console.WriteLine($"User: {result.User.Identity?.Name}");
                
                return result;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Login operation was cancelled.");
                throw; // Re-throw so your ViewModel knows to reset CanApply
            }
        }
    }

    private static ProviderSecrets? LoadSecretsFromJson(string path)
    {
        // 1. Define the URI.
        // Format: "avares://AssemblyName/Assets/FileName.json"
        var uri = new Uri(path);

        try
        {
            // 2. Open the asset stream
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            
            // 3. Read and Deserialize
            string jsonText = reader.ReadToEnd();
            //Console.WriteLine($"Read following from json: {jsonText}");
            return JsonSerializer.Deserialize<ProviderSecrets>(jsonText);
        }
        catch (Exception ex)
        {
            // Handle file not found or JSON errors
            Console.WriteLine($"Error loading JSON: {ex.Message}");
            return null;
        }
    }
}
