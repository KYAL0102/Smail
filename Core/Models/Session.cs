using System;
using Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Models;

public class Session
{
    public SmsService? SmsService { get; set; } = null;
    public EmailService? EmailService { get; set; } = null;

    public MessagePayload Payload { get; private set; } = new();

    public async Task PrepareShutdownAsync()
    {
        if(SmsService == null || EmailService == null) return;

        await SmsService.DeregisterWebhooksAsync();
    }
}
