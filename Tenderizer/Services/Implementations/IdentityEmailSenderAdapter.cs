using IdentityUiEmailSender = Microsoft.AspNetCore.Identity.UI.Services.IEmailSender;
using AppEmailSender = Tenderizer.Services.Interfaces.IEmailSender;

namespace Tenderizer.Services.Implementations;

public sealed class IdentityEmailSenderAdapter : IdentityUiEmailSender
{
    private readonly AppEmailSender _emailSender;

    public IdentityEmailSenderAdapter(AppEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return _emailSender.SendAsync(email, subject, htmlMessage);
    }
}
