using ProgramExecuter.BackgroundWorkerService.Mail;

namespace ProgramExecuter.BackgroundWorkerService.Helpers;

internal class ProgramRunErrorMailBuilder
{
    private string _mailBody = string.Empty;
    private int _errorCount = 0;

    public void AddProgramInfoToErrorMail(ProgramToRun program)
    {
        string path = program.Path;
        int pathSubstingIndex = path.LastIndexOf('\\');
        if (pathSubstingIndex < 0)
            pathSubstingIndex = path.LastIndexOf('/');
        if (pathSubstingIndex < 0)
            path = "***";
        else
            path = $"***\\{path.Substring(pathSubstingIndex + 1)}";
        _mailBody += $"\t{DateTime.Now} tarihinde {program.Name} çalışmadı. \n\t\tPath: {path}\n\t\tArguments: {(program.Args?.Length > 0 ? string.Join("\n\t\t\t", program.Args) : "no argument")}\n\n";
        _errorCount++;
    }


    public MailRequest BuildErrorMail(string to, string subject)
    {
        _mailBody = $"{_errorCount} defa çalıştırmayı denedim ama başaramadım.\n\n{_mailBody}";
        return new MailRequest(to, subject, _mailBody);
    }
}
