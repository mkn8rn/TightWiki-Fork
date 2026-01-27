using BLL.Services.Configuration;
using BLL.Services.Exception;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TightWiki.Utils;
using static TightWiki.Contracts.Constants;

namespace BLL.Services.Email
{
    /// <summary>
    /// Business logic service for email operations.
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IExceptionService _exceptionService;

        public EmailService(
            ILogger<EmailService> logger,
            IConfigurationService configurationService,
            IExceptionService exceptionService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _exceptionService = exceptionService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var values = _configurationService.GetConfigurationEntriesByGroupName(ConfigurationGroup.Email);
                var smtpPassword = values.Value<string>("Password");
                var smtpUsername = values.Value<string>("Username");
                var smtpAddress = values.Value<string>("Address");
                var smtpFromDisplayName = values.Value<string>("From Display Name");
                var smtpUseSSL = values.Value<bool>("Use SSL");
                int smtpPort = values.Value<int>("Port");

                if (string.IsNullOrEmpty(smtpAddress) || string.IsNullOrEmpty(smtpUsername))
                {
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(smtpFromDisplayName, smtpUsername));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                using var client = new SmtpClient();
                var options = smtpUseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                await client.ConnectAsync(smtpAddress, smtpPort, options);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                _exceptionService.LogException(ex);
            }
        }
    }
}
