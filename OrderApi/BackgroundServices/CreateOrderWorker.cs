using OrderApi.Constants;
using OrderApi.Entities;
using OrderApi.Events;
using OrderApi.Models.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderApi.BackgroundServices
{
    /// <summary>
    /// TO DO Worker yerine MassTransit de kullanılabilir 
    /// </summary>
    public class CreateOrderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private readonly ILogger<OutboxWorker> _logger;

        public CreateOrderWorker(IServiceScopeFactory scopeFactory, ConnectionFactory factory, ILogger<OutboxWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _factory = factory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IConnection connection = _factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.QueueDeclare(QueueNameConstants.CreateOrder, durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var createOrderEvent = JsonSerializer.Deserialize<CreateOrder>(json);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                if (createOrderEvent?.Id == null)
                {
                    _logger.LogWarning($"CreateOrderEvent Bulunamadı Id: {createOrderEvent?.Id}");
                    return;
                }

                var order = await db.Orders.FindAsync(createOrderEvent.Id);
                
                if (order == null)
                {
                    _logger.LogWarning($"Order Bulunamadı Id: {createOrderEvent.Id}");
                    return;
                }

                order.Status = OrderStatus.Processing;
                await db.SaveChangesAsync();

                await Task.Delay(2000); // simulate processing

                order.Status = OrderStatus.Completed;               
                await db.SaveChangesAsync();

                _logger.LogInformation($"Order işlendi: {createOrderEvent.Id}");
            };

            channel.BasicConsume(QueueNameConstants.CreateOrder, autoAck: true, consumer);

            return Task.CompletedTask;
        }
    }
}