using System;
using Core.Services;

namespace Core.Models;

public class Session
{
    public required SmsService SmsService { get; init; }

    public MessagePayload Payload { get; private set; } = new();

    public async Task PrepareShutdownAsync()
    {
        await SmsService.DeregisterWebhooksAsync();
    }
}
