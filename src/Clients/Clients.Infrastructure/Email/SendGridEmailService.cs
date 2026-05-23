using Clients.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Clients.Infrastructure.Email;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _fromEmail = configuration["SendGrid:FromEmail"]
            ?? throw new InvalidOperationException("SendGrid:FromEmail not configured.");
        _fromName = configuration["SendGrid:FromName"] ?? "Banking System";
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var message = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = subject,
            PlainTextContent = body,
            HtmlContent = $"<p>{body}</p>"
        };
        message.AddTo(new EmailAddress(to));

        var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Email sent to {To} | Subject: {Subject}", to, subject);
        else
        {
            var body2 = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid failed for {To}. Status: {Status}. Body: {Body}",
                to, response.StatusCode, body2);
        }
    }
}
