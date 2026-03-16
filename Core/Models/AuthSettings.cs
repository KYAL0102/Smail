namespace Core.Models;

public record AuthSettings
{
    public ClientIdentification Google { get; set; } = new();
    public ClientIdentification Microsoft { get; set; } = new();
}
