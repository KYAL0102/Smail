using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Core.Services;

public static class NetworkManager
{
    public static async Task<List<Contact>> FetchFromUriAsync(string uri, string expectedThumbprint)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None) return true;

                return cert?.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256) == expectedThumbprint;
            }
        };

        using var client = new HttpClient(handler);

        try
        {
            var options = new JsonSerializerOptions
            {
                // This handles different casing (e.g., "email" vs "Email")
                PropertyNameCaseInsensitive = true,
                // This allows us to map the string "Whatsapp" directly to an Enum if needed
                Converters = { new JsonStringEnumConverter() }
            };

            // Deserializes whatever properties match; the rest stay null.
            var rawData = await client.GetFromJsonAsync<List<ParticipantDto>>(uri, options);

            if (rawData == null) return [];

            // Apply your domain logic and validation, similar to your ReadFromCsvContentAsync
            return rawData
                .Select(dto => new Contact
                {
                    // Use null-coalescing (??) to handle missing properties
                    Name = dto.Name ?? string.Empty,
                    
                    // Validate if present, otherwise empty
                    MobileNumber = !string.IsNullOrEmpty(dto.MobileNumber) && FormatChecker.IsValidMobile(dto.MobileNumber) 
                                ? dto.MobileNumber 
                                : string.Empty,
                    
                    Email = !string.IsNullOrEmpty(dto.Email) && FormatChecker.IsValidEmail(dto.Email) 
                            ? dto.Email 
                            : string.Empty,
                    
                    SentBy = !string.IsNullOrEmpty(dto.SentBy) ? dto.SentBy : string.Empty,

                    PayedBy = !string.IsNullOrEmpty(dto.PayedBy) ? dto.PayedBy : string.Empty,

                    HomeCountry = dto.HomeCountry ?? "Unknown",

                    HomeRegion = dto.HomeRegion ?? "Unknown",

                    // Handle the Enum conversion for TransmissionType
                    ContactPreference = Enum.TryParse<TransmissionType>(dto.TransmissionType, true, out var pref) 
                                        ? pref 
                                        : TransmissionType.NONE
                })
                // Maintain your requirement: Name must exist
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToList();
        }
        catch (Exception ex)
        {
            // Log the error (consider using a logging service here)
            Console.WriteLine($"Error fetching data: {ex.Message} - {ex.StackTrace}");
            return [];
        }
    }

    /// <summary>
    /// This DTO covers all possible properties. 
    /// If the API returns fewer, the extras remain null.
    /// If the order changes, JSON deserialization doesn't care.
    /// </summary>
    public class ParticipantDto
    {
        public string? Email { get; set; }
        public string? HomeCountry { get; set; }
        public string? HomeRegion { get; set; }
        public string? MobileNumber { get; set; }
        public string? Name { get; set; }
        public string? PayedBy { get; set; }
        public string? SentBy { get; set; }
        public string? TransmissionType { get; set; }
    }

    private static string? _capturedThumbprint = null;
    public static async Task<(bool Success, string Message)> VerifySourceAsync(string path, string? expectedThumbprint = null)
    {
        // Case 1: It's a Web URL
        if (path.StartsWith("http"))
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                    {
                        _capturedThumbprint = cert?.GetCertHashString();

                        if (string.IsNullOrEmpty(expectedThumbprint))
                            return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;

                        var cleanExpected = expectedThumbprint.Replace(" ", "").Replace(":", "").ToUpper();
                        var actualThumbprint = cert?.GetCertHashString()?.ToUpper();

                        return cleanExpected == actualThumbprint;
                    }
                };
                using var client = new HttpClient(handler);
                // Set a short timeout so the UI doesn't hang forever
                client.Timeout = TimeSpan.FromSeconds(5); 

                var request = new HttpRequestMessage(HttpMethod.Head, path);
                var response = await client.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                    return (false, $"Server returned error: {response.StatusCode}");

                // Verify content type (optional but helpful)
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != "application/json" && !path.EndsWith(".csv"))
                {
                    // You can warn them, but maybe proceed anyway
                    return (true, "Warning: Response format might not be standard JSON/CSV.");
                }
                
                return (true, "URL is reachable.");
            }
            catch (HttpRequestException ex) 
            { 
                if(_capturedThumbprint != null)
                {
                    //TODO: Ask user if he wants to trust this thumbprint
                    return (false, $"{ex.Message}");
                }
                else return (false, $"URL unreachable: {ex.Message}"); 
            }
            catch (Exception ex) 
            { 
                return (false, $"URL unreachable: {ex.Message}"); 
            }
        }

        // Case 2: It's a Local File
        else
        {
            if (File.Exists(path))
                return (true, "File found.");
            else
                return (false, "File does not exist at the specified path.");
        }
    }

    // ----------------------------------------------------------------------
    //  PUBLIC ENTRY POINT
    // ----------------------------------------------------------------------
    public static (string PfxPath, string Password) GetCertificateForLocalIp(string encryptionPassword)
    {
        var encryptor = new AesEncryptor(encryptionPassword);
        string ip = GetLocalIPv4();
        string workDir = GetCertWorkDirectory();
        string pfxPath = Path.Combine(workDir, "server.pfx");
        string passwordPath = Path.Combine(workDir, "cert_pwd.txt");

        if (File.Exists(pfxPath) && File.Exists(passwordPath))
        {
            string encryptedResult = File.ReadAllText(passwordPath);
            var savedPassword = encryptor.DecryptSMS(encryptedResult);
            
            using var cert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, savedPassword);
            
            if (DateTime.UtcNow < cert.NotAfter.ToUniversalTime().AddMonths(-1))
            {
                Console.WriteLine("Certificate is still valid (more than 1 month)!");
                return (pfxPath, savedPassword);
            }
            Console.WriteLine("Certificate is in its final month or expired. Regenerating...");
        }

        ForceClearFolder(workDir);

        string newPassword = GenerateRandomPassword();
        
        RunSmsGateCa(ip, workDir);
        ConvertToPfxDotNet(workDir, newPassword);

        var encryptedText = encryptor.EncryptSMS(newPassword);
        File.WriteAllText(passwordPath, encryptedText);

        return (pfxPath, newPassword);
    }

    public static void ForceClearFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return;
        }

        var directory = new DirectoryInfo(folderPath);
        foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    file.Attributes = FileAttributes.Normal;
                }
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    Process.Start("chmod", $"644 \"{file.FullName}\"")?.WaitForExit();
                }
            }
            catch { /* File might have been deleted by a parallel process */ }
        }

        for (int i = 0; i < 3; i++)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(folderPath))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        string tempName = file + ".bak";
                        if (File.Exists(tempName)) File.Delete(tempName);
                        File.Move(file, tempName);
                        File.Delete(tempName);
                    }
                    else
                    {
                        File.Delete(file);
                    }
                }

                foreach (var dir in Directory.EnumerateDirectories(folderPath))
                {
                    Directory.Delete(dir, true);
                }
                
                Console.WriteLine($"Successfully cleared folder {folderPath}!");
                break; // Success
            }
            catch (IOException) when (i < 2)
            {
                Thread.Sleep(500); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not fully clear folder: {ex.Message}");
            }
        }
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

        string fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Could not find smsgate-ca binary at: {fullPath}");

        return fullPath;
    }

    public static string GetWorkDirPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Smail"
        );
    }

    public static string GetCertWorkDirectory()
    {
        string networkId = GetNetworkId();
        string rootDataFolder = GetWorkDirPath();

        string folder = Path.Combine(
            rootDataFolder,
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
        string binaryName = Path.GetFileNameWithoutExtension(binaryPath);

        string crt = Path.Combine(workDir, "server.crt");
        string key = Path.Combine(workDir, "server.key");

        try 
        {
            var existingProcesses = Process.GetProcessesByName(binaryName);
            foreach (var p in existingProcesses)
            {
                Console.WriteLine($"Killing process {p.SessionId}.");
                p.Kill();
                p.WaitForExit(2000);
            }
        }
        catch(Exception ex) { Console.WriteLine($"{ex.Message} - {ex.StackTrace}"); }

        try
        {
            for (int i = 0; i < 3; i++)
            {
                try 
                {
                    if (File.Exists(crt)) 
                    {
                        File.SetAttributes(crt, FileAttributes.Normal);
                        File.Delete(crt);
                    }
                    if (File.Exists(key))
                    {
                        File.SetAttributes(key, FileAttributes.Normal);
                        File.Delete(key);
                    }
                    break; 
                }
                catch (IOException) when (i < 2) 
                {
                    Thread.Sleep(500);
                }
            }
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

    private static string GenerateRandomPassword()
        {
            byte[] randomBytes = new byte[24];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
}
