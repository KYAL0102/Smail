using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Core.Services;

public sealed class SecurityVault : IDisposable
{
    private static readonly Lazy<SecurityVault> _lazyInstance = new(() => new SecurityVault());
    public static SecurityVault Instance => _lazyInstance.Value;

    // Backing fields - private
    private SecureString? _aesPassphrase;
    private SecureString? _whSigningKey;
    private SecureString? _gatewayPassword;
    private SecureString? _emailPassword;

    public string SmsGatewayUsername { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    // ── Public API ──────────────────────────────────────────────────────────────

    public void SetWebsocketSigningKey(string key) => _whSigningKey = StringToSecureString(key);

    public void SetGateWayEncryptionPhrase(string passphrase) => _aesPassphrase = StringToSecureString(passphrase);

    public void SetGateWayCredentials(string usr, string? pwd)
    {
        SmsGatewayUsername = usr?.Trim() ?? string.Empty;
        _gatewayPassword = StringToSecureString(pwd);
    }

    public void SetEmailCredentials(string email, string password)
    {
        Email = email;
        _emailPassword = StringToSecureString(password);
    }

    // Getters return disposable wrapper - forces caller to use using() or Dispose()
    public SecureStringAccessor GetAesPassphrase() => new(_aesPassphrase);
    public SecureStringAccessor GetWhSigningKey() => new(_whSigningKey);
    public SecureStringAccessor GetGatewayPassword() => new(_gatewayPassword);
    public SecureStringAccessor GetEmailPassword() => new(_emailPassword);

    public string GetUsername() => SmsGatewayUsername;
    public string GetEmail() => Email;

    /// <summary>
    /// Clear all sensitive data from memory
    /// </summary>
    public void Clear()
    {
        ClearSecureString(ref _aesPassphrase);
        ClearSecureString(ref _whSigningKey);
        ClearSecureString(ref _gatewayPassword);
        ClearSecureString(ref _emailPassword);
        SmsGatewayUsername = string.Empty;
        Email = string.Empty;
    }

    public void Dispose()
    {
        Clear();
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
}