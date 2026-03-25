using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Core;

namespace SmailAvalonia.Services;

public class WsClientService
{
    private HubConnection? _connection;

    private static WsClientService? _instance = null;
    public static WsClientService Instance => _instance ??= new();

    public WsClientService(){ }

    public void On<T>(string methodName, Action<T> handler)
    {
        _connection?.On(methodName, handler);
    }

    public void On(string methodName, Action handler)
    {
        _connection?.On(methodName, handler);
    }

    public async Task ConnectToServerWsHub()
    {
        Console.WriteLine("Attempting connection to hub...");
        _connection = new HubConnectionBuilder()
            .WithUrl(Globals.WebsocketURL)
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += async (error) =>
        {
            Console.WriteLine($"Connection closed: {error?.Message}");
            await Task.Delay(5000);
            await _connection.StartAsync();
        };

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("Connected to hub!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("===== SIGNALR CONNECT ERROR =====");
            Exception? e = ex;
            while (e != null)
            {
                Console.WriteLine(e.GetType().FullName);
                Console.WriteLine(e.Message);
                Console.WriteLine();
                e = e.InnerException;
            }
        }
    }
}