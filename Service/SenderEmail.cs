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
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Invalid recipient (email is null or empty).", nameof(email));

        if (!MimeKit.InternetAddress.TryParse(email, out var toAddress))
            throw new ArgumentException($"Invalid recipient email address: {email}", nameof(email));;

        var baseUri = new Uri(_apiBaseUrl.TrimEnd('/') + "/");
        var verifyUri = new Uri(baseUri, $"api/check/{id}");

        var message = new MimeMessage();

        if (!string.IsNullOrWhiteSpace(_fromName))
            message.From.Add(new MailboxAddress(_fromName, _fromAddress));
        else
            message.From.Add(MailboxAddress.Parse(_fromAddress));

        message.To.Add(toAddress);
        message.Subject = "Verify your account";

        message.Body = new TextPart("html")
        {
            Text = $@"<p>Click the link below to verify your account:</p>
              <p><a href=""{verifyUri}"">Verify</a></p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        if (!string.IsNullOrWhiteSpace(_smtpUser))
            await client.AuthenticateAsync(_smtpUser, _smtpPass);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

}
