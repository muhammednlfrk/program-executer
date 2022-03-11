namespace ProgramExecuter.BackgroundWorkerService.Mail;

public interface IMailService
{
    Task<bool> SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default);
}
