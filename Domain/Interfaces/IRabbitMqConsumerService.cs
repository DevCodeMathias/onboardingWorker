namespace OnboardingWorker.Domain.Interfaces;

public interface IRabbitMqConsumerService
{
    Task Consume(CancellationToken ct = default);
}