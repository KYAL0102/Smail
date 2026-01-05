using Microsoft.AspNetCore.SignalR;

namespace SmailAPI;

public class WebsocketHub : Hub 
{
    public override async Task OnConnectedAsync()
    {
        // Log the connection to the console
        Console.WriteLine($"New client connected: {Context.ConnectionId}");

        // Call the base method to ensure the connection is properly established
        await base.OnConnectedAsync();
    }
}
