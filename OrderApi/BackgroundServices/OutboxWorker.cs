using Microsoft.EntityFrameworkCore;
using OrderApi.Entities;
using RabbitMQ.Client;
using System.Text;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(IServiceScopeFactory scopeFactory, ConnectionFactory factory, ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = factory.CreateConnection();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Outbox Worker Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var events = await db.OutboxMessages.Where(x => !x.IsPublished).OrderBy(x => x.CreatedAt).Take(50).ToListAsync(stoppingToken);

        if (!events.Any())
        {
            return;
        }

        foreach (var evt in events)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                PublishEvent(evt);
                evt.IsPublished = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Publish failed: {evt.Id} - {ex.Message}");
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private void PublishEvent(OutboxMessage outboxMessage)
    {
        using var channel = _connection.CreateModel();

        channel.QueueDeclare(queue: outboxMessage.QueueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var json = outboxMessage.Body;
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "",
                             routingKey: outboxMessage.QueueName,
                             basicProperties: null,
                             body: body);
    }
}