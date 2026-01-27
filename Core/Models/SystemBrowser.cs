using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Duende.IdentityModel.OidcClient.Browser;
using System.Net.Sockets;
using System.Text;

namespace Core.Models;

public class SystemBrowser : IBrowser
{
    private readonly int _port;

    public SystemBrowser(int port)
    {
        _port = port;
    }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        // 1. Bind to IPv4 loopback explicitly
        TcpListener listener = new TcpListener(IPAddress.Loopback, _port);
        listener.Start();

        try 
        {
            OpenBrowser(options.StartUrl);

            // 2. Accept the browser's connection
            using TcpClient client = await listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();

            // 3. Read the request to get the code/token
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string requestData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Extract the path (e.g., /?code=...)
            var firstLine = requestData.Split('\n')[0];
            var parts = firstLine.Split(' ');
            if (parts.Length < 2) return new BrowserResult { ResultType = BrowserResultType.UnknownError };

            // 4. Send a basic HTTP response so the user sees a "Success" page
            string responseBody = "<html><body><h1>Success</h1><p>You can close this.</p></body></html>";
            string response = "HTTP/1.1 200 OK\r\n" +
                            "Content-Type: text/html\r\n" +
                            "Access-Control-Allow-Origin: *\r\n" + // Allow the browser to see the response
                            "Connection: close\r\n" +
                            $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n\r\n" + 
                            responseBody;
            
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

            return new BrowserResult
            {
                Response = $"http://127.0.0.1:{_port}{parts[1]}",
                ResultType = BrowserResultType.Success
            };
        }
        finally
        {
            listener.Stop();
        }
    }

    private void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }
}
