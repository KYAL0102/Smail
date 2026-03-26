using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Core.Services;

public class AesEncryptor
{
    private const int NonceBitSize = 96; // 12 bytes is standard for GCM
    private const int MacBitSize = 128;  // 16 bytes for the authentication tag
    private const int KeyBitSize = 256;  // AES-256
    private readonly string _passphrase;
    private readonly int _iterations;

    public AesEncryptor(string? passphrase, int iterations = 75_000)
    {
        _passphrase = passphrase ?? string.Empty;
        _iterations = iterations;
    }

    public string EncryptSMS(string cleartext)
    {
        var safetext = cleartext.Replace("\r\n", "\n").Replace("\r", "\n");
        byte[] salt = GenerateSalt();
        byte[] key = GenerateKeySMS(salt, _iterations);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = salt;

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(safetext);
        byte[] encryptedBytes = aes.CreateEncryptor()
                                   .TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        string saltB64 = Convert.ToBase64String(salt);
        string encryptedB64 = Convert.ToBase64String(encryptedBytes);

        return $"$aes-256-cbc/pbkdf2-sha1$i={_iterations}${saltB64}${encryptedB64}";
    }

    public string DecryptSMS(string encrypted)
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

        byte[] key = GenerateKeySMS(salt, iterations);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = salt;

        byte[] decryptedBytes = aes.CreateDecryptor()
                                   .TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] GenerateSalt(int size = 16)
    {
        return RandomNumberGenerator.GetBytes(size);
    }

    private byte[] GenerateKeySMS(byte[] salt, int iterations)
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

    private const int SALT_SIZE = 16;
    private const int NONCE_SIZE = 12;

    /// <summary>
    /// Encrypts a string using AES-GCM.
    /// </summary>
    /// <param name="plainText">The data to encrypt (e.g., your JSON string).</param>
    /// <param name="password">The Encrypion password</param>
    public static string Encrypt(string plainText, string password)
    {
        byte[] salt = GenerateSalt(SALT_SIZE);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);

        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
        
        byte[] nonce = new byte[NONCE_SIZE]; 
        new SecureRandom().NextBytes(nonce);

        GcmBlockCipher cipher = new (new AesEngine());
        AeadParameters parameters = new (new KeyParameter(key), 128, nonce);
        cipher.Init(true, parameters);
        byte[] outputBytes = new byte[cipher.GetOutputSize(inputBytes.Length)];
        int len = cipher.ProcessBytes(inputBytes, 0, inputBytes.Length, outputBytes, 0);
        cipher.DoFinal(outputBytes, len);

        byte[] combined = new byte[salt.Length + nonce.Length + outputBytes.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(nonce, 0, combined, salt.Length, nonce.Length);
        Buffer.BlockCopy(outputBytes, 0, combined, salt.Length + nonce.Length, outputBytes.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypts a Base64 string using AES-GCM.
    /// </summary>
    public static string? Decrypt(string cipherTextWithMetadata, string password)
    {
        byte[] combined = Convert.FromBase64String(cipherTextWithMetadata);

        byte[] salt = new byte[SALT_SIZE];
        Buffer.BlockCopy(combined, 0, salt, 0, SALT_SIZE);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);

        byte[] nonce = new byte[NONCE_SIZE];
        Buffer.BlockCopy(combined, SALT_SIZE, nonce, 0, NONCE_SIZE);

        int cipherTextOffset = SALT_SIZE + NONCE_SIZE;
        byte[] cipherText = new byte[combined.Length - cipherTextOffset];
        Buffer.BlockCopy(combined, cipherTextOffset, cipherText, 0, cipherText.Length);

        GcmBlockCipher cipher = new (new AesEngine());
        AeadParameters parameters = new (new KeyParameter(key), 128, nonce);
        cipher.Init(false, parameters);
        byte[] outputBytes = new byte[cipher.GetOutputSize(cipherText.Length)];
        int len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, outputBytes, 0);
        
        try {
            cipher.DoFinal(outputBytes, len);
            return Encoding.UTF8.GetString(outputBytes);
        } catch {
            return null;
        }
    }

    public static byte[] DeriveKeyFromPassword(string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes("peopletosillystuff");
        
        // 600,000 iterations is the current OWASP recommendation for SHA-256
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt, 
            600000, 
            HashAlgorithmName.SHA256,
            32
        );
    }
}
