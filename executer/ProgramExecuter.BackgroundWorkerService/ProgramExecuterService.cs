using System.Diagnostics;

namespace ProgramExecuter.BackgroundWorkerService;

public class ProgramExecuterService
{
    private readonly List<Process> processBuffer = new List<Process>();

    public Task<bool> IsRunningAsync(string programPath, string programName)
    {
        Process[] processes = Process.GetProcessesByName(programName);
        bool isRunning = processes?.Length > 0;
        return Task.FromResult(isRunning);
    }

    public Task<bool> ExecuteAsync(string programPath, string[] args)
    {
        try
        {
            Process process = Process.Start(new ProcessStartInfo(programPath, string.Join(' ', args))
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            });
            processBuffer.Add(process);
            return Task.FromResult(process != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
