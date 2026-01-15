using Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace SmailAPI;

public class WebsocketHub : Hub 
{
    public override async Task OnConnectedAsync()
    {
        //Console.WriteLine($"New client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public void UpdateWebsocketSigningKey(string key)
    {
        //Console.WriteLine($"Update was request. Setting signing-key...");
        SecurityVault.Instance.SetWebsocketSigningKey(key);
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        //Console.WriteLine($"{Context.ConnectionId} disconnected.");
        await base.OnDisconnectedAsync(e);
    }
}
