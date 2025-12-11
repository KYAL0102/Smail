using Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text;  
using System.Security.Cryptography;  
using System.Text.Json;
using Core.ApiResponseClasses;


namespace SmailAPI.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IHubContext<WebsocketHub> _hub;

    public WebhookController(IHubContext<WebsocketHub> hub)
    {
        _hub = hub;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        string signature = HttpContext.Request.Headers["X-Signature"];
        string timestamp = HttpContext.Request.Headers["X-Timestamp"];
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // Compute local HMAC
        var key = Encoding.UTF8.GetBytes("tZSihgTH");
        using var hmac = new HMACSHA256(key);
        byte[] data = Encoding.UTF8.GetBytes(body + timestamp);
        string computed = Convert.ToHexStringLower(hmac.ComputeHash(data)).Replace("-", "").ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature)))
        {
            return Unauthorized(new { status = "Signing Key was false!" });
        }

        var responseObj = JsonSerializer.Deserialize<WebhookResponse>(body);
        Console.WriteLine($"Result of deserialization: {responseObj}");

        if (responseObj != null) 
        {
            responseObj.Payload = responseObj?.Event switch
            {
                "sms:failed" => responseObj.JsonPayload.Deserialize<FailedPayload>(),
                "sms:delivered" => responseObj.JsonPayload.Deserialize<DeliveredPayload>(),
                "email:sent" => responseObj.JsonPayload.Deserialize<SentPayload>(),
                _ => null
            };

            // Forward to Avalonia frontend
            Console.WriteLine($"Sending object via ws...");
            await _hub.Clients.All.SendAsync("WebhookUpdate", responseObj);
        }

        return Ok(new { status = "received" });
    }
}
