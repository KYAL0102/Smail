using System;
using System.Threading.Tasks;
using Core.Services;
using Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Models;

public class Session
{
    public SmsService? SmsService { get; set; } = null;
    public EmailService? EmailService { get; set; } = null;

    public MessagePayload? Payload { get; set; } = null;

    public async Task PrepareShutdownAsync()
    {
        if(SmsService != null) await SmsService.DeregisterWebhooksAsync();
    }
}
