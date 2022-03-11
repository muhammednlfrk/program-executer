using System.Net;
using System.Net.Mail;
using System.Text;

namespace ProgramExecuter.BackgroundWorkerService.Mail
{
    internal class SmtpMailService : IMailService
    {
        private readonly MailSettings _settings;
        private readonly ILogger<SmtpMailService> _logger;

        private bool _isMailSended = false;

        public SmtpMailService(IConfiguration configuration, ILogger<SmtpMailService> logger)
        {
            // Get mail configuration
            IConfigurationSection mailConfigSection = configuration.GetSection("MailSettings");
            _settings = new MailSettings
            {
                Mail = mailConfigSection.GetValue<string>("Mail"),
                DisplayName = mailConfigSection.GetValue<string>("DisplayName"),
                Password = mailConfigSection.GetValue<string>("Password"),
                Host = mailConfigSection.GetValue<string>("Host"),
                Port = mailConfigSection.GetValue<int>("Port")
            };

            // Set logger
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default)
        {
            // Set response
            _isMailSended = false;

            // Create SMTP client
            using var smtpClient = new SmtpClient(_settings.Host, _settings.Port);
            smtpClient.EnableSsl = false;
            smtpClient.Credentials = new NetworkCredential(_settings.Mail, _settings.Password);

            // Create sender mail address
            var from = new MailAddress(_settings.Mail, _settings.DisplayName, Encoding.UTF8);

            // Create destination mail address
            var to = new MailAddress(mailRequest.ToMail);

            // Create mail message
            using var mailMessage = new MailMessage(from, to)
            {
                Body = mailRequest.Body,
                BodyEncoding = Encoding.UTF8,
                Subject = mailRequest.Subject,
                SubjectEncoding = Encoding.UTF8
            };

            // Log when send completed
            smtpClient.SendCompleted += (_, e) =>
            {
                if (e.Cancelled)
                    _logger.LogInformation($"Email gönderme işlemi iptal edildi.");
                else if (e.Error != null)
                    _logger.LogError($"Email gönderilemedi! {e.Error.Message}");
                else
                {
                    _isMailSended = true;
                    _logger.LogInformation("Mail gönderildi.");
                }
            };

            // Send email
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            // Return response
            return _isMailSended;
        }
    }
}
