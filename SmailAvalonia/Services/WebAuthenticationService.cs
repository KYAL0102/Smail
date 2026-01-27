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

    public static async Task GetTokenFromUserWebPermissionAsync(Provider provider, string email = "")
    {
        var secrets = LoadSecretsFromJson(provider.SecretsPath);

        var options = new OidcClientOptions
        {
            Authority = $"https://{provider.AuthorityUrl}",
            ClientId = secrets.ClientId,
            ClientSecret = secrets.ClientSecret,
            Scope = "openid profile email",
            RedirectUri = "http://127.0.0.1:45454",
            Browser = new SystemBrowser(port: 45454), 
            Policy = new Policy 
            { 
                Discovery = new DiscoveryPolicy
                {
                    ValidateEndpoints = false,
                    ValidateIssuerName = false
                },
                RequireAccessTokenHash = true 
            }
        };

        var client = new OidcClient(options);

        var loginParams = new Parameters { { "prompt", "consent" } };
        if(!string.IsNullOrEmpty(email)) loginParams.Add("login_hint", email);
        
        var state = await client.PrepareLoginAsync(loginParams);
        _manualUrlTaskSource = new TaskCompletionSource<string>();

        var browserTask = options.Browser.InvokeAsync(new BrowserOptions(state.StartUrl, options.RedirectUri), CancellationToken.None);

        //Process.Start(new ProcessStartInfo(state.StartUrl) { UseShellExecute = true });

        var manualTask = _manualUrlTaskSource.Task;
        var completedTask = await Task.WhenAny(browserTask, manualTask);

        string finalUrl;
        if (completedTask == manualTask) finalUrl = await manualTask;
        else
        {
            var browserResult = await browserTask;
            if (browserResult.ResultType != BrowserResultType.Success)
                throw new Exception($"Browser failed: {browserResult.Error}");
            
            finalUrl = browserResult.Response;
        }

        var result = await client.ProcessResponseAsync(finalUrl, state);

        if (!result.IsError)
        {
            Console.WriteLine($"User: {result.User.Identity?.Name} \nToken: {result.AccessToken}");
        }
        else throw new Exception($"{result.Error}");
    }

    private static GoogleSecrets? LoadSecretsFromJson(string path)
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
            return JsonSerializer.Deserialize<GoogleSecrets>(jsonText);
        }
        catch (Exception ex)
        {
            // Handle file not found or JSON errors
            Console.WriteLine($"Error loading JSON: {ex.Message}");
            return null;
        }
    }
}
