using Core.Models;

namespace Core.Services;

public static class PayloadFactory
{
    public static async Task<MessagePayload> CreateMessagePayloadAsync(SecurityVault securityVault)
    {
        var payload = new MessagePayload();

        payload.PrimaryTransmissionType = securityVault.PrimaryTransmissionType;
        payload.StrategyKey = securityVault.StrategyKey;

        var recipientPool = (await RecipientPoolBaseLoader.LoadFromSourceAsync(securityVault.HttpsThumbprint)) ?? [];

        foreach(var recipient in recipientPool)
        {
            payload.ContactPool.Add(recipient, TransmissionType.NONE);
        }

        return payload;
    }
}
