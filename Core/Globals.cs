namespace Core;

public static class Globals
{
    public static string WebsocketURL { get; } = "http://127.0.0.1:5005/ws";

    public static string NavigateToSettings { get; } = "settings";
    public static string NavigateToAuthenticationAction { get; } = "authentication";
    public static string NavigateToPayloadSummaryAction { get; } = "summary";
    public static string NavigateToMessageConfigurationAction { get; } = "textconfig";
    public static string NavigateToRecepientConfigurationAction { get; } = "contactConfig";
    public static string NavigateToExecutionAction { get; } = "execution";
    public static string NewSessionAction { get; } = "newSession";
}