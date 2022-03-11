using ProgramExecuter.BackgroundWorkerService.Helpers;
using ProgramExecuter.BackgroundWorkerService.Mail;

namespace ProgramExecuter.BackgroundWorkerService;

public class ProgramWeorkerbackgroundService : BackgroundService
{
    private readonly ILogger<ProgramWeorkerbackgroundService> _logger;
    private readonly Dictionary<ProgramToRun, bool> _programs;
    private readonly ProgramExecuterService _executer;
    private readonly IMailService _mailService;
    private readonly int _delay;
    private readonly int _maxCheckCount;
    private readonly Dictionary<string, string> _errorEmailSubscribers;

    public ProgramWeorkerbackgroundService(ProgramExecuterService executer, ILogger<ProgramWeorkerbackgroundService> logger, IMailService mailService, IConfiguration configuration)
    {
        // Set services
        _executer = executer;
        _logger = logger;
        _mailService = mailService;

        // Set programs
        List<ProgramToRun> programs = new List<ProgramToRun>();
        IEnumerable<IConfigurationSection> sections = configuration.GetSection("Programs").GetChildren();
        foreach (IConfigurationSection section in sections)
        {
            string name = section.Key;
            string path = section.GetSection("Path").Value;
            string[] args = section.GetSection("Arguments").GetChildren().Select(s => s.Value).ToArray();
            programs.Add(new ProgramToRun(name, path, args));
        }
        _programs = new Dictionary<ProgramToRun, bool>();
        foreach (ProgramToRun program in programs)
            _programs.Add(program, true);

        // Set delay
        _delay = int.Parse(configuration["WorkerOptions:ProgramWorker:Delay"]);

        // Set delay
        _maxCheckCount = int.Parse(configuration["WorkerOptions:ProgramWorker:MaxTryingCount"]);

        // Set reply mail addresses
        _errorEmailSubscribers = new Dictionary<string, string>();
        sections = configuration.GetSection("ReplyMailAddresses").GetChildren();
        foreach (IConfigurationSection section in sections)
        {
            string name = section.Key;
            string email = section.Value;
            _errorEmailSubscribers.Add(email, name);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Error email builder.
            var errorMailBuilder = new ProgramRunErrorMailBuilder();

            // Check all programs in the dictionary.
            foreach ((ProgramToRun program, bool tryExecute) in _programs)
            {
                // Program check counter.
                int checkCounter = 0;

            check: // Checking point.

                // Check program is running and log.
                _logger.LogInformation($"{program.Name} kontrol ediliyor...");
                bool isRunning = await _executer.IsRunningAsync(program.Path, program.Name);

                // Skip next program to check.
                if (isRunning)
                {
                    _programs[program] = true;
                    _logger.LogInformation($"{program.Name} zaten çalýþýyor");
                    continue;
                }
                // If program tried _maxCheckCount times to execute, skip this program.
                else if (!tryExecute)
                    continue;

                // Run program.
                _logger.LogInformation($"{program.Name} çalýþtýrýlýyor...");
                bool isExecuted = await _executer.ExecuteAsync(program.Path, program.Args);

                // if program executing skip the program.
                if (isExecuted)
                {
                    _logger.LogInformation($"{program.Name} çalýþýyor");
                    continue;
                }

                // Raise check counter.
                checkCounter++;

                // Add execution error in email body.
                errorMailBuilder.AddProgramInfoToErrorMail(program);

                // Log error.
                _logger.LogError($"{program.Name} baþlatýlamadý!");

                // If tried over _maxCheckCount times, send email to subscribers.
                if (checkCounter >=
                    _maxCheckCount)
                {
                    _programs[program] = false;
                    foreach ((string email, string name) in _errorEmailSubscribers)
                        _mailService.SendEmailAsync(errorMailBuilder.BuildErrorMail(email, $"Sayýn {name} sanýrým bir sorunumuz var"), stoppingToken);
                    continue;
                }
                // If tried over _maxCheckCount times, try to execute again.
                else
                {
                    _logger.LogInformation($"{program.Name} için tekrar deneniyor...");
                    await Task.Delay(_delay, stoppingToken);
                    goto check;
                }
            }

            // Background service delay
            await Task.Delay(_delay, stoppingToken);
        }
    }
}
