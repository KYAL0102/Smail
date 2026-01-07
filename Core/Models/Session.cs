using System;
using Core.Services;

namespace Core.Models;

public class Session
{
    public required SmsService SmsService { get; init; }

    public async Task PrepareShutdown()
    {
        SmsService.DeregisterWebhooks();
    }
}
