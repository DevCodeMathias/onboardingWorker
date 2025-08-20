using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace OnboardingWorker.Service;

public class SenderEmail : ISenderEmail
{
    private readonly string _apiBaseUrl;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _fromName;
    private readonly string _fromAddress;

    public SenderEmail(IConfiguration configuration)
    {
        _apiBaseUrl = configuration["api:baseUrl"];
        _smtpHost = configuration["smtp:host"];
        _smtpPort    = int.TryParse(configuration["smtp:port"], out var p) ? p : 587;
        _smtpUser = configuration["smtp:username"];
        _smtpPass = configuration["smtp:password"];
        _fromName = configuration["smtp:fromName"];
        _fromAddress = configuration["smtp:from"];
    }
    
    public async Task SendeEmail(string email, int id)
    {
        var verifyUri = new Uri(new Uri(_apiBaseUrl.TrimEnd('/')), "/api/check/" + id);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Verifique sua conta";

        message.Body = new TextPart("html")
        {
            Text = $@"<p>Clique para verificar sua conta:</p>
                      <p><a href=""{verifyUri}"">Verificar</a></p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtpUser, _smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
