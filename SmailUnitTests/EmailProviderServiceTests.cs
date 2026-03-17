using NUnit.Framework;
using Core.Services;
using Core.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using DnsClient;
using Moq;
using DnsClient.Protocol;

namespace Core.Tests;

[TestFixture]
public class EmailProviderServiceTests
{
    private EmailProviderService _service;
    private AuthSettings _settings;

    [SetUp]
    public void Setup()
    {
        // Mocking the Settings that the constructor expects
        _settings = new AuthSettings
        {
            Google = new ClientIdentification { ClientId = "G-123", ClientSecret = "G-Secret" },
            Microsoft = new ClientIdentification { ClientId = "M-123", ClientSecret = "M-Secret" }
        };

        var options = Options.Create(_settings);
        _service = new EmailProviderService(options);
    }

    [Test]
    [TestCase("test@gmail.com", "Google")]
    [TestCase("user@outlook.com", "Microsoft")]
    [TestCase("hello@hotmail.com", "Microsoft")]
    public async Task GetServerProvider_FastMatch_ReturnsCorrectProvider(string email, string expectedName)
    {
        // Act
        var provider = await _service.GetServerProviderFromEmailAsync(email);

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public async Task GetServerProvider_InvalidEmailFormat_ThrowsFormatException()
    {
        // Act & Assert
        // MailAddress constructor throws FormatException for invalid strings
        Assert.ThrowsAsync<FormatException>(async () => 
            await _service.GetServerProviderFromEmailAsync("not-an-email"));
    }

    [Test]
    public async Task GetServerProvider_UnknownDomain_ReturnsNull()
    {
        // This will attempt a real DNS lookup unless we refactor.
        // Assuming your network is up, a random domain should return null.
        var provider = await _service.GetServerProviderFromEmailAsync("someone@example.xyz");

        Assert.That(provider, Is.Null);
    }

    [Test]
    public void Constructor_SetsIdentificationCorrectly()
    {
        // Since we can't easily see private _providers, 
        // we verify via the return of a known match
        var task = _service.GetServerProviderFromEmailAsync("test@gmail.com");
        var provider = task.Result;

        Assert.That(provider.Identification.ClientId, Is.EqualTo("G-123"));
        Assert.That(provider.Identification.Name, Is.EqualTo("Google"));
    }
}