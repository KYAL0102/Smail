using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Core.Services;

public class AesEncryptor
{
    private readonly string _passphrase;
    private readonly int _iterations;

    public AesEncryptor(string? passphrase, int iterations = 75_000)
    {
        _passphrase = passphrase ?? string.Empty;
        _iterations = iterations;
    }

    public string Encrypt(string cleartext)
    {
        byte[] salt = GenerateSalt();
        byte[] key = GenerateKey(salt, _iterations);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = salt; // ⚠️ salt is IV (matches Python)

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(cleartext);
        byte[] encryptedBytes = aes.CreateEncryptor()
                                   .TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        string saltB64 = Convert.ToBase64String(salt);
        string encryptedB64 = Convert.ToBase64String(encryptedBytes);

        return $"$aes-256-cbc/pbkdf2-sha1$i={_iterations}${saltB64}${encryptedB64}";
    }

    public string Decrypt(string encrypted)
    {
        string[] chunks = encrypted.Split('$');

        if (chunks.Length < 5)
            throw new ArgumentException("Invalid encryption format");

        if (chunks[1] != "aes-256-cbc/pbkdf2-sha1")
            throw new ArgumentException("Unsupported algorithm");

        var parameters = ParseParams(chunks[2]);

        if (!parameters.TryGetValue("i", out string iterStr))
            throw new ArgumentException("Missing iteration count");

        int iterations = int.Parse(iterStr);
        byte[] salt = Convert.FromBase64String(chunks[^2]);
        byte[] encryptedBytes = Convert.FromBase64String(chunks[^1]);

        byte[] key = GenerateKey(salt, iterations);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = salt; // ⚠️ salt is IV

        byte[] decryptedBytes = aes.CreateDecryptor()
                                   .TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(16);
    }

    private byte[] GenerateKey(byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            _passphrase,
            salt,
            iterations,
            HashAlgorithmName.SHA1,
            32);
    }

    private static Dictionary<string, string> ParseParams(string paramString)
    {
        var dict = new Dictionary<string, string>();
        foreach (var pair in paramString.Split(','))
        {
            var kv = pair.Split('=');
            dict[kv[0]] = kv[1];
        }
        return dict;
    }
}
