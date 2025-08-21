using OnboardingWorker;
using OnboardingWorker.Domain.Interfaces;
using OnboardingWorker.Service;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IConnection>(s  =>
{
    var factory = new ConnectionFactory() { HostName = "localhost" ,DispatchConsumersAsync = true};
    return factory.CreateConnection();
});
builder.Services.AddSingleton<IRabbitMqConsumerService, RabbitMqConsumerService>();
builder.Services.AddSingleton<ISenderEmail, SenderEmail>();

var host = builder.Build();
host.Run();