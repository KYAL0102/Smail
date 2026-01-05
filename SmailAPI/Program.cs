using Core;
using SmailAPI;

var builder = WebApplication.CreateBuilder(args);

var (pfxPath, pfxPwd) = NetworkManager.GenerateCertificateForLocalIp();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSignalR();

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


//app.UseHttpsRedirection();
//app.MapHub<WebsocketHub>("/ws");
//app.MapControllers();

app.Run();
