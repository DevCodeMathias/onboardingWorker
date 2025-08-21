using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MailKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnboardingWorker.Domain;
using OnboardingWorker.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OnboardingWorker.Service;

public class RabbitMqConsumerService : IRabbitMqConsumerService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IConnection _connection;
    private readonly string _queueName;
    private readonly ISenderEmail _senderEmail;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger,
        IConnection connection,
        IConfiguration configuration,
        ISenderEmail senderEmail)
    {
        _logger = logger;
        _connection = connection;
        _senderEmail = senderEmail;
        _queueName = configuration["RabbitMQ:QueueName"] ?? "user.created";
    }

    public Task Consume(CancellationToken stoppingToken)
    {
        var channel = _connection.CreateModel();

        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Mensagem recebida: {msg}", msg);
                var envelope = JsonSerializer.Deserialize<MessageEnvelope<User>>(msg);
                var userId = envelope.Payload.Id;
                var userEmail = envelope.Payload.Email; 
                await _senderEmail.SendeEmail(userEmail, userId);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
                channel.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
            await Task.CompletedTask;
        };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        var tcs = new TaskCompletionSource();
        stoppingToken.Register(() =>
        {
            try { channel.Close(); channel.Dispose(); } catch { }
            tcs.TrySetResult();
        });
        return tcs.Task; 
    }
}
