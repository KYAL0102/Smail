using Microsoft.AspNetCore.SignalR;

namespace SmailAPI;

public class WebsocketHub : Hub 
{
    public override async Task OnConnectedAsync()
    {
        //Console.WriteLine($"New client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        //Console.WriteLine($"{Context.ConnectionId} disconnected.");
        await base.OnDisconnectedAsync(e);
    }
}
