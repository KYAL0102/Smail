using NUnit.Framework;
using Core.Services;
using System.Security.Cryptography;

namespace Core.Tests;

[TestFixture]
public class AesEncryptorTests
{
    private const string TestPassphrase = "SuperSecretPassword123!";
    private AesEncryptor _encryptor;

    [SetUp]
    public void Setup()
    {
        _encryptor = new AesEncryptor(TestPassphrase);
    }

    [Test]
    public void Encrypt_Decrypt_ShouldReturnOriginalText()
    {
        // Arrange
        string original = "Hello Avalonia and Velopack!";

        // Act
        string encrypted = _encryptor.Encrypt(original);
        string decrypted = _encryptor.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(original));
    }

    [Test]
    public void Encrypt_ShouldProduceDifferentOutput_ForSameInput()
    {
        // Because of the random salt, encrypting twice should never look the same
        string input = "Consistent Secret";

        string first = _encryptor.Encrypt(input);
        string second = _encryptor.Encrypt(input);

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Decrypt_WithWrongPassphrase_ShouldThrow()
    {
        // Arrange
        string original = "Top Secret Data";
        string encrypted = _encryptor.Encrypt(original);
        
        var wrongEncryptor = new AesEncryptor("WrongPassword");

        // Act & Assert
        // Usually, AES throws a CryptographicException due to padding mismatch 
        // when the key is wrong.
        Assert.Throws<CryptographicException>(() => wrongEncryptor.Decrypt(encrypted));
    }

    [Test]
    public void Encrypt_ShouldNormalizeLineEndings()
    {
        // Arrange
        string input = "Line1\r\nLine2\rLine3";
        string expected = "Line1\nLine2\nLine3";

        // Act
        string encrypted = _encryptor.Encrypt(input);
        string decrypted = _encryptor.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("Very long string with symbols !@#$%^&*()_+")]
    public void Encrypt_HandlesVariousInputs(string input)
    {
        string encrypted = _encryptor.Encrypt(input);
        string decrypted = _encryptor.Decrypt(encrypted);

        Assert.That(decrypted, Is.EqualTo(input.Replace("\r\n", "\n")));
    }

    [Test]
    public void Decrypt_InvalidFormat_ShouldThrowArgumentException()
    {
        string garbage = "$aes-256$short$string";
        
        Assert.Throws<ArgumentException>(() => _encryptor.Decrypt(garbage));
    }
}