using OnboardingWorker.Domain.Interfaces;

namespace OnboardingWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRabbitMqConsumerService  _consumerService;

    public Worker(ILogger<Worker> logger,IRabbitMqConsumerService  consumerService)
    {
        _logger = logger;
        _consumerService = consumerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _consumerService.Consume(stoppingToken);
               _logger.LogInformation("worker stopped");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}