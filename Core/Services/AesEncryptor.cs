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

    private static byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(16);
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

    /// <summary>
    /// Encrypts a string using AES-GCM.
    /// </summary>
    /// <param name="plainText">The data to encrypt (e.g., your JSON string).</param>
    /// <param name="key">A 32-byte (256-bit) key.</param>
    public static string Encrypt(string plainText, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).");

        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
        
        // 1. Generate a random Nonce (IV)
        byte[] nonce = new byte[NonceBitSize / 8];
        new SecureRandom().NextBytes(nonce);

        // 2. Setup GCM engine
        GcmBlockCipher cipher = new (new AesEngine());
        AeadParameters parameters = new (new KeyParameter(key), MacBitSize, nonce);
        cipher.Init(true, parameters);

        // 3. Process Encryption
        byte[] outputBytes = new byte[cipher.GetOutputSize(inputBytes.Length)];
        int length = cipher.ProcessBytes(inputBytes, 0, inputBytes.Length, outputBytes, 0);
        cipher.DoFinal(outputBytes, length);

        // 4. Combine Nonce + CipherText for storage
        byte[] combined = new byte[nonce.Length + outputBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(outputBytes, 0, combined, nonce.Length, outputBytes.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypts a Base64 string using AES-GCM.
    /// </summary>
    public static string Decrypt(string cipherTextWithNonce, byte[] key)
    {
        byte[] combined = Convert.FromBase64String(cipherTextWithNonce);

        // 1. Extract Nonce
        int nonceSize = NonceBitSize / 8;
        byte[] nonce = new byte[nonceSize];
        byte[] cipherText = new byte[combined.Length - nonceSize];
        
        Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(combined, nonceSize, cipherText, 0, cipherText.Length);

        // 2. Setup GCM engine
        GcmBlockCipher cipher = new (new AesEngine());
        AeadParameters parameters = new (new KeyParameter(key), MacBitSize, nonce);
        cipher.Init(false, parameters);

        // 3. Process Decryption
        byte[] outputBytes = new byte[cipher.GetOutputSize(cipherText.Length)];
        int length = cipher.ProcessBytes(cipherText, 0, cipherText.Length, outputBytes, 0);
        
        try
        {
            cipher.DoFinal(outputBytes, length);
        }
        catch (InvalidCipherTextException)
        {
            // This happens if the data was tampered with or the key is wrong
            return null; 
        }

        return Encoding.UTF8.GetString(outputBytes).TrimEnd('\0');
    }
}
