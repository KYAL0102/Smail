using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;


namespace Core.Services;

public static class NetworkManager
{
    // ----------------------------------------------------------------------
    //  PUBLIC ENTRY POINT
    // ----------------------------------------------------------------------
    public static (string PfxPath, string Password) GetCertificateForLocalIp()
    {
        string ip = GetLocalIPv4();
        string password = "K1Nnay0102"; // TODO: UI solution to set it
        //Console.WriteLine($"Detected LAN IP: {ip}");

        string workDir = GetCertWorkDirectory();

        string pfxPath = Path.Combine(workDir, "server.pfx");

        if(File.Exists(pfxPath)) return (pfxPath, password);

        RunSmsGateCa(ip, workDir);
        string pfx = ConvertToPfxDotNet(workDir, password);

        return (pfx, password);
    }

    // ----------------------------------------------------------------------
    //  PATHS AND DIRECTORIES
    // ----------------------------------------------------------------------
    private static string GetSmsGateBinaryPath()
    {
        string baseDir = AppContext.BaseDirectory;

        string relativePath = OperatingSystem.IsWindows()
            ? @"CALibs/Windows_86x_64/smsgate-ca.exe"
            : @"CALibs/Linux_86x_64/smsgate-ca";

        string fullPath = Path.Combine(baseDir, relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Could not find smsgate-ca binary at: {fullPath}");

        return fullPath;
    }

    public static string GetCertWorkDirectory()
    {
        string networkId = GetNetworkId();

        string folder = Path.Combine(
            AppContext.BaseDirectory,
            "Certificates",
            networkId
        );

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string GetNetworkId()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            if (!ni.GetIPProperties().GatewayAddresses
                .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                continue;

            // Skip loopback & virtual adapters
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                ni.Description.ToLower().Contains("virtual") ||
                ni.Description.ToLower().Contains("vmware") ||
                ni.Description.ToLower().Contains("hyper-v"))
                continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (IPAddress.IsLoopback(ua.Address))
                    continue;

                if (ua.IPv4Mask == null)
                    continue;

                var network = GetNetworkAddress(ua.Address, ua.IPv4Mask);
                var cidr = MaskToCidr(ua.IPv4Mask);

                return $"{network}-{cidr}";
            }
        }

        throw new Exception("No active non-loopback IPv4 network found.");
    }

    private static IPAddress GetNetworkAddress(IPAddress ip, IPAddress mask)
    {
        var ipBytes = ip.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        var networkBytes = new byte[4];

        for (int i = 0; i < 4; i++)
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);

        return new IPAddress(networkBytes);
    }

    private static int MaskToCidr(IPAddress mask)
    {
        return mask.GetAddressBytes().Sum(b => Convert.ToString(b, 2).Count(c => c == '1'));
    }

    // ----------------------------------------------------------------------
    //  LOCAL NETWORK
    // ----------------------------------------------------------------------
    public static string GetLocalIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            if (ni.Description.ToLower().Contains("virtual") ||
                ni.Description.ToLower().Contains("vmware") ||
                ni.Description.ToLower().Contains("hyper-v"))
                continue;

            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address))
                {
                    return addr.Address.ToString();
                }
            }
        }

        throw new Exception("No LAN IPv4 address found.");
    }

    // ----------------------------------------------------------------------
    //  RUN SMSGATE BINARY
    // ----------------------------------------------------------------------
    public static void RunSmsGateCa(string ip, string workDir)
    {
        string binaryPath = GetSmsGateBinaryPath();

        string crt = Path.Combine(workDir, "server.crt");
        string key = Path.Combine(workDir, "server.key");

        try
        {
            if (File.Exists(crt)) File.Delete(crt);
            if (File.Exists(key)) File.Delete(key);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to clean old certificate files: {ex.Message}");
        }

        // Ensure binary is executable on Linux
        if (!OperatingSystem.IsWindows())
        {
            Process.Start("chmod", $"+x \"{binaryPath}\"")?.WaitForExit();
        }

        var psi = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = $"private {ip}",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi)!;

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.WriteLine(output);
            throw new Exception($"smsgate-ca failed:\n{error}");
        }

        // wait for files to be created
        WaitForFiles(new[] { "server.crt", "server.key" }, workDir);
    }

    // ----------------------------------------------------------------------
    //  FILE WAITING LOGIC
    // ----------------------------------------------------------------------
    public static void WaitForFiles(string[] files, string directory, int timeoutMs = 8000)
    {
        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (files.All(f => File.Exists(Path.Combine(directory, f))))
                return;

            Thread.Sleep(100);
        }

        throw new Exception($"Timeout waiting for certificate files in {directory}");
    }

    // ----------------------------------------------------------------------
    //  CONVERSION TO PFX
    // ----------------------------------------------------------------------
    public static string ConvertToPfxDotNet(string directory, string password)
    {
        string crtPath = Path.Combine(directory, "server.crt");
        string keyPath = Path.Combine(directory, "server.key");
        string pfxPath = Path.Combine(directory, "server.pfx");

        if (!File.Exists(crtPath) || !File.Exists(keyPath))
            throw new Exception("Missing server.crt or server.key");

        var certParser = new X509CertificateParser();
        var cert = certParser.ReadCertificate(File.ReadAllBytes(crtPath));

        AsymmetricKeyParameter key;

        using (var reader = new StreamReader(keyPath))
        {
            var pemReader = new PemReader(reader);
            var pemObject = pemReader.ReadObject();

            if (pemObject is AsymmetricCipherKeyPair keyPair)
            {
                key = keyPair.Private;
            }
            else if (pemObject is AsymmetricKeyParameter keyParam)
            {
                key = keyParam;
            }
            else if (pemObject is Org.BouncyCastle.Asn1.Sec.ECPrivateKeyStructure ecStruct)
            {
                // Convert SEC1 EC key -> ECPrivateKeyParameters
                X9ECParameters curve = ECNamedCurveTable.GetByName("prime256v1");

                if (curve == null)
                    throw new Exception("Failed to load EC curve parameters.");

                ECDomainParameters domainParams = new ECDomainParameters(
                    curve.Curve,
                    curve.G,
                    curve.N,
                    curve.H,
                    curve.GetSeed()
                );

                key = new ECPrivateKeyParameters(ecStruct.GetKey(), domainParams);
            }
            else
            {
                throw new Exception($"Unsupported private key format: {pemObject?.GetType().FullName}");
            }
        }

        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry(
            "smsgate",
            new AsymmetricKeyEntry(key),
            new[] { new X509CertificateEntry(cert) }
        );

        using var fs = new FileStream(pfxPath, FileMode.Create, FileAccess.Write);
        store.Save(fs, password.ToCharArray(), new SecureRandom());

        return pfxPath;
    }
}
