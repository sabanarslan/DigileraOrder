using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderApi.BackgroundServices;
using OrderApi.Controllers.Validators;
using OrderApi.Entities;
using OrderApi.Models.Requests;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseInMemoryDatabase("OrderDb"));
builder.Services.AddSingleton(sp =>
{

    return new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMq:HostName"],
        Port = int.Parse(builder.Configuration["RabbitMq:Port"]),
        UserName = builder.Configuration["RabbitMq:UserName"],
        Password = builder.Configuration["RabbitMq:Password"]
    };
});

builder.Services.AddHostedService<CreateOrderWorker>();
builder.Services.AddHostedService<OutboxWorker>();

builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
