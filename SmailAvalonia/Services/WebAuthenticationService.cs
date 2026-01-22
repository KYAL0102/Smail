using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Core.Models;
using Core.Models.EmailAuthentication;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;

namespace SmailAvalonia.Services;

public static class WebAuthenticationService
{
    public static async Task GetTokenFromUserWebPermission(string email = "")
    {
        // 1. Read and Parse
        var secrets = LoadSecretsFromJson("avares://SmailAvalonia/Assets/client_google.json");

        // 3. Build the Request URI
        var options = new OidcClientOptions
        {
            Authority = "https://accounts.google.com",
            ClientId = secrets.ClientId,
            Scope = "openid profile email",
            RedirectUri = secrets.RedirectUris[0], // Must match your Google Console exactly
            Browser = new SystemBrowser(),
            Policy = new Policy { RequireAccessTokenHash = true }
        };

        var client = new OidcClient(options);

        var loginRequest = new LoginRequest
        {
            FrontChannelExtraParameters = new Parameters
            {
                // This tells Google to pre-fill the email field
                { "login_hint", email } 
            }
        };
        if(email == string.Empty) loginRequest = null;

        var result = await client.LoginAsync(loginRequest);

        if (!result.IsError)
        {
            // Success! 
            var user = result.User.Identity?.Name;
            var token = result.AccessToken;

            Console.WriteLine($"User: {user} \nToken: {token}");
        }
        else Console.WriteLine(result.ErrorDescription);
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
