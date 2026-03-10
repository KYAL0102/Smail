using Core.Services;

namespace SmailAPI;

public static class ApiServer
{
    private static readonly TaskCompletionSource _isReady = new();
    public static Task ReadyTask => _isReady.Task;

    public static async Task RunAsync(string[] args, CancellationToken ct = default)
    { 
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = "SmailAPI"
        });

        var (pfxPath, pfxPwd) = NetworkManager.GetCertificateForLocalIp();

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(1));

        builder.WebHost.ConfigureKestrel(options =>
        {
            ///options.ListenAnyIP(5000); // HTTP (optional)
            options.ListenAnyIP(5001, listenOptions =>
            {
                listenOptions.UseHttps(pfxPath, pfxPwd);
            });

            options
                .ListenLocalhost(5005);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseRouting();

        app.Use(async (context, next) =>
        {
            // Only allow /ws on port 5005
            if (context.Request.Path.StartsWithSegments("/ws") &&
                context.Connection.LocalPort != 5005)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("WebSocket available only on localhost:5005");
                return;
            }

            await next();
        });

        app.UseWhen(
            ctx => ctx.Connection.LocalPort == 5005,
            app5005 =>
            {
                app5005.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<WebsocketHub>("/ws");
                });
            }
        );

        app.UseWhen(
            ctx => ctx.Connection.LocalPort == 5001,
            app5001 =>
            {
                app5001.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            }
        );


        app.UseHttpsRedirection();
        //app.MapHub<WebsocketHub>("/ws");
        //app.MapControllers();

        // Start the app without blocking
        await app.StartAsync(ct);
        
        _isReady.TrySetResult();

        try
        {
            // Wait here until the UI signals cancellation
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Force an immediate stop
            await app.StopAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
        }
    }
}
