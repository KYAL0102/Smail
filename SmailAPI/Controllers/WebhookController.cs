using Core;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text;  
using System.Security.Cryptography;
using System.Security;
using Core.Models;


namespace SmailAPI.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController(IHubContext<WebsocketHub> _hub) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        string? signature = HttpContext.Request.Headers["X-Signature"];
        string? timestamp = HttpContext.Request.Headers["X-Timestamp"];
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // Compute local HMAC
        var key = Encoding.UTF8.GetBytes(SecurityVault.Instance.GetWhSigningKey().Value ?? string.Empty);
        using var hmac = new HMACSHA256(key);
        byte[] data = Encoding.UTF8.GetBytes(body + timestamp);
        string computed = Convert.ToHexStringLower(hmac.ComputeHash(data)).Replace("-", "").ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature)))
        {
            Console.WriteLine("Authorization of incoming request failed (signing key was false).");
            return Unauthorized(new { status = "Signing Key was false!" });
        }

        //Console.WriteLine($"Sending body via ws...");
        await _hub.Clients.All.SendAsync("WebhookUpdate", body);

        return Ok(new { status = "received" });
    }
}
