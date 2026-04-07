using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Constants;
using OrderApi.Entities;
using OrderApi.Events;
using OrderApi.Extensions;
using OrderApi.Models.Enums;
using OrderApi.Models.Requests;
using OrderApi.Models.Responses;
using System.Text.Json;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _dbContext;
        private readonly IValidator<CreateOrderRequest> _validator;

        public OrderController(OrderDbContext dbContext, IValidator<CreateOrderRequest> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder([FromRoute] Guid id)
        {
            Order order = await _dbContext.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            GetOrderResponse response = new GetOrderResponse
            {
                Id = order.Id,
                ProductName = order.ProductName,
                Price = order.Price,
                Status = order.Status
            };

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> QueryOrders()
        {
            List<QueryOrderResponse> orders = await _dbContext.Orders.Select(x => new QueryOrderResponse
            {
                Id = x.Id,
                ProductName = x.ProductName,
                Price = x.Price,
                Status = x.Status
            }).ToListAsync();

            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var validationResult = _validator.Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToProblemDetails());
            }

            Order order = await SaveOrder(request);

            CreateOrderResponse response = new ()
            {
                Id = order.Id
            };

            return Created("", response);
        }

        private async Task<Order> SaveOrder(CreateOrderRequest request)
        {
            Order order = new Order
            {
                Id = Guid.NewGuid(),
                ProductName = request.ProductName,
                Price = request.Price,
                Status = OrderStatus.Created
            };

            _dbContext.Orders.Add(order);

            CreateOrder createOrderEvent = new() { Id = order.Id };
            string body = JsonSerializer.Serialize(createOrderEvent);

            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Body = body,
                QueueName = QueueNameConstants.CreateOrder,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            return order;
        }
    }
}