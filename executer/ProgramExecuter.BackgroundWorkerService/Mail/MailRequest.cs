using Microsoft.AspNetCore.Http;

namespace ProgramExecuter.BackgroundWorkerService.Mail;

public record MailRequest(string ToMail, string Subject, string Body);