using System.Text;
using System.Text.Json;
using OnboardingWorker.Domain;
using OnboardingWorker.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OnboardingWorker.Service;

public class RabbitMqConsumerService:IRabbitMqConsumerService
{
    private readonly IConnection _connection;
    private readonly string _queueName;
    private readonly  ISenderEmail _senderEmail;
    private readonly ILogger<Worker> _logger;
    
    public RabbitMqConsumerService(
        IConnection connection,
        IConfiguration configuration,
        ISenderEmail senderEmail,
        ILogger<Worker> logger)
    {
        _logger = logger;
        _connection = connection;
        _senderEmail = senderEmail; 
        _queueName =  configuration["RabbitMQ:QueueName"];
    }
     public Task Consume(CancellationToken ct = default)
    {
        var channel = _connection.CreateModel();

       
        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        
        channel.BasicQos(0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        _logger.LogInformation("teste 1");
        consumer.Received += async (_, ea) =>
        {
            
            try
            {
               
                var msg = Encoding.UTF8.GetString(ea.Body.ToArray());

                var envelope = JsonSerializer.Deserialize<MessageEnvelope<User>>(msg);
                if (envelope?.Payload is null)
                {
                    _logger.LogWarning("Mensagem inválida (payload nulo). DeliveryTag={Tag}", ea.DeliveryTag);
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); 
                    return;
                }

                var userId = envelope.Payload.id;
                var userEmail = envelope.Payload.email;

                await _senderEmail.SendeEmail(userEmail, userId);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Processado com sucesso. UserId={UserId}", userId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Falha ao desserializar mensagem. DeliveryTag={Tag}", ea.DeliveryTag);
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem. DeliveryTag={Tag}", ea.DeliveryTag);
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true); 
            }
        };

        var consumerTag = channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        
        var tcs = new TaskCompletionSource();
        ct.Register(() =>
        {
            try
            {
                channel.BasicCancel(consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao cancelar consumidor");
            }
            finally
            {
                try { channel.Close(); } catch { /* ignore */ }
                channel.Dispose();
                tcs.TrySetResult();
            }
        });

        return tcs.Task;
    }
}