using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using System.IO;
using Newtonsoft.Json;
using Order_API.Models;

public class OrderListening : BackgroundService
{
    private readonly ILogger _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly OrderDBContext _context;
    private readonly IConfiguration env;

    public OrderListening(ILoggerFactory loggerFactory, OrderDBContext context, IConfiguration env)
    {
        this._logger = loggerFactory.CreateLogger<OrderListening>();
        _context = context;
        this.env = env;
        InitRabbitMQ();
    }

    private void InitRabbitMQ()
    {
        var configuration = GetConfiguration();
        var factory = new ConnectionFactory
        {
            UserName = configuration.GetSection("RABBITMQ_UserName").Value,
            Password = configuration.GetSection("RABBITMQ_Password").Value,
            HostName = configuration.GetSection("RABBITMQ_HostName").Value,
            //VirtualHost = configuration.GetSection("RABBITMQ_VirtualHost").Value,
            Port = Convert.ToInt32(configuration.GetSection("RABBITMQ_PORT").Value)
        };

        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        _channel.QueueDeclare("OrderStatus", false, false, false, null);

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            // received message  
            var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

            // handle the received message  
            HandleMessage(content);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        consumer.Shutdown += OnConsumerShutdown;
        consumer.Registered += OnConsumerRegistered;
        consumer.Unregistered += OnConsumerUnregistered;
        consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

        _channel.BasicConsume("OrderStatus", false, consumer);
        return Task.CompletedTask;
    }

    private void HandleMessage(string content)
    {
        // login listening content
        _logger.LogInformation($"OrderStatus received {content}");

        

        Order order = JsonConvert.DeserializeObject<Order>(content);

        // save to DB
        order.Order_Status = "INITIATED";
        _context.Order.Add(order);
        _context.SaveChangesAsync();

    }

    private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
    private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }

    public IConfigurationRoot GetConfiguration()
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        return builder.Build();
    }
}

