using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Models;
using Duende.IdentityModel.OidcClient.Results;

namespace Core.Services;

public class SecurityVault : IDisposable
{
    private const string FileName = "vault.data";
    private bool _loaded = false;

    // SMS
    private SecureString? _aesPassphrase;
    private SecureString? _whSigningKey;
    private SecureString? _gatewayPassword;
    private readonly string? _pwd = null;

    public string SmsGatewayUsername { get; private set; } = string.Empty;

    // EMAIL
    private List<TokenPackage> _tokenPackages = [];

    public SecurityVault(string? password = null)
    {
        _pwd = password;
    }

    // ── Persistence Logic ──────────────────────────────────────────────────────

    public async Task SaveToFileAsync()
    {
        if (_pwd == null) return;

        var data = new VaultDataDto
        {
            AesPassphrase = SecureStringToString(_aesPassphrase) ?? string.Empty,
            WhSigningKey = SecureStringToString(_whSigningKey) ?? string.Empty,
            GatewayUsername = SmsGatewayUsername,
            GatewayPassword = SecureStringToString(_gatewayPassword) ?? string.Empty,
            TokenPackages = _tokenPackages
        };

        string json = JsonSerializer.Serialize(data);
        string encrypted = AesEncryptor.Encrypt(json, _pwd);
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FileName);
        await File.WriteAllTextAsync(path, encrypted);
    }

    public async Task LoadAsync()
    {
        if (_pwd == null || _loaded) return;
        //if (_aesPassphrase != null || _gatewayPassword != null) return;

        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Vault file missing and no data in memory.");

        try
        {
            string encrypted = await File.ReadAllTextAsync(filePath);
            string? json = AesEncryptor.Decrypt(encrypted, _pwd);
            
            if (string.IsNullOrEmpty(json)) throw new Exception("Decryption failed or data corrupted.");

            var data = JsonSerializer.Deserialize<VaultDataDto>(json);
            if (data != null)
            {
                _aesPassphrase = StringToSecureString(data.AesPassphrase);
                _whSigningKey = StringToSecureString(data.WhSigningKey);
                SmsGatewayUsername = data.GatewayUsername ?? string.Empty;
                _gatewayPassword = StringToSecureString(data.GatewayPassword);
                _tokenPackages = data.TokenPackages;
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            throw new SecurityException("Failed to load secure vault.", ex);
        }
    }

    // ── Public API (Modified to Auto-Save) ──────────────────────────────────────

    public void SetGateWayCredentials(string usr, string? pwd)
    {
        SmsGatewayUsername = usr?.Trim() ?? string.Empty;
        _gatewayPassword = StringToSecureString(pwd);
    }

    public void SetWebhookSigningKey(string whSigningKey)
    {
        _whSigningKey = StringToSecureString(whSigningKey.Trim());
    }

    public void SetGateWayEncryptionPhrase(string passphrase)
    {
        _aesPassphrase = StringToSecureString(passphrase.Trim());
    }

    public void UpdatePackageInList(TokenPackage package)
    {
        var packageInList = _tokenPackages.SingleOrDefault(p => p.Email == package.Email);

        if (packageInList == null) return; 

        packageInList.AccessToken = package.AccessToken;
        packageInList.AccessTokenExpiration = package.AccessTokenExpiration;
        packageInList.RefreshToken = package.RefreshToken;

        Console.WriteLine("Updated TokenPackage!");
    }

    public void UpdatePackageInListViaRefreshTokenResult(string email, RefreshTokenResult result)
    {
        var package = new TokenPackage();

        package.Email = email;
        package.AccessToken = result.AccessToken;
        package.AccessTokenExpiration = result.AccessTokenExpiration;
        package.RefreshToken = result.RefreshToken;

        UpdatePackageInList(package);
    }

    public void AddPackageToList(TokenPackage package)
    {
        if(_tokenPackages.Any(item => item.Email == package.Email)) return;

        _tokenPackages.Add(package);
        Console.WriteLine("New entry into TokenPackage-list!");
    }

    public SecureStringAccessor? GetAesPassphrase() 
    {
        if (_aesPassphrase == null)
            return null;

        return new(_aesPassphrase);
    }

    public SecureStringAccessor? GetGatewayPassword()
    {
        if (_gatewayPassword == null)
            return null;

        return new(_gatewayPassword);
    }

    public SecureStringAccessor? GetWhSigningKey()
    {
        if (_whSigningKey == null)
            return null;
        
        return new(_whSigningKey);
    }

    public TokenPackage? GetPackageForEmail(string email)
    {
        return _tokenPackages.SingleOrDefault(item => item.Email == email);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static SecureString? StringToSecureString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var secure = new SecureString();
        foreach (char c in value)
            secure.AppendChar(c);

        secure.MakeReadOnly();
        return secure;
    }

    private static void ClearSecureString(ref SecureString? field)
    {
        if (field != null)
        {
            field.Clear();
            field.Dispose();
            field = null;
        }
    }

    public static string? SecureStringToString(SecureString? secure)
    {
        if (secure == null || secure.Length == 0)
            return null;

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
            return Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }

    // ── Safe access pattern ─────────────────────────────────────────────────────

    /// <summary>
    /// Disposable wrapper that gives temporary access to plain string
    /// </summary>
    public readonly struct SecureStringAccessor : IDisposable
    {
        private readonly string? _value;

        internal SecureStringAccessor(SecureString? secure)
        {
            _value = SecureStringToString(secure);
        }

        public string? Value => _value;

        public void Dispose()
        {
            // In real world you could overwrite the string chars here,
            // but .NET strings are immutable so we just let it die quickly
        }

        public static implicit operator string?(SecureStringAccessor a) => a._value;
    }

     /// <summary>
    /// Clear all sensitive data from memory
    /// </summary>
    public void Clear()
    {
        ClearSecureString(ref _aesPassphrase);
        ClearSecureString(ref _whSigningKey);
        ClearSecureString(ref _gatewayPassword);
        SmsGatewayUsername = string.Empty;
    }

    public void Dispose()
    {
        Clear();
    } 
}