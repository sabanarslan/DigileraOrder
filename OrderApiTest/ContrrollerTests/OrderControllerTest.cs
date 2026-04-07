using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Constants;
using OrderApi.Controllers;
using OrderApi.Entities;
using OrderApi.Models.Enums;
using OrderApi.Models.Requests;
using OrderApi.Models.Responses;

namespace OrderApiTest.ControllerTests
{
    public class OrderControllerTest
    {
        private IValidator<CreateOrderRequest> GetValidator(bool isValid = true)
        {
            var validator = new InlineValidator<CreateOrderRequest>();
            if (!isValid)
            {
                validator.RuleFor(x => x.ProductName).NotEmpty().WithMessage("ProductName required");
            }
            return validator;
        }

        private OrderDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new OrderDbContext(options);
        }

        [Fact]
        public async Task GetOrder_Should_Return_Order_When_Exists()
        {
            // Arrange
            using var context = GetDbContext();

            Order order = new Order
            {
                Id = Guid.NewGuid(),
                ProductName = "Test Product",
                Price = 200,
                Status = OrderStatus.Created
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new OrderController(context, GetValidator());

            // Act
            var result = await controller.GetOrder(order.Id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<GetOrderResponse>().Subject;

            response.Id.Should().Be(order.Id);
            response.ProductName.Should().Be(order.ProductName);
            response.Price.Should().Be(order.Price);
            response.Status.Should().Be(order.Status);
        }

        [Fact]
        public async Task GetOrder_Should_Return_NotFound_When_Order_NotExists()
        {
            using var context = GetDbContext();
            var controller = new OrderController(context, GetValidator());

            var result = await controller.GetOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task QueryOrders_Should_Return_All_Orders()
        {
            using var context = GetDbContext();

            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), ProductName = "A", Price = 100, Status = OrderStatus.Created },
                new Order { Id = Guid.NewGuid(), ProductName = "B", Price = 200, Status = OrderStatus.Created }
            };
            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            var controller = new OrderController(context, GetValidator());

            var result = await controller.QueryOrders();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<List<QueryOrderResponse>>().Subject;

            response.Should().HaveCount(orders.Count);
        }

        [Fact]
        public async Task CreateOrder_Should_Return_Created_With_Valid_Request()
        {
            using var context = GetDbContext();

            var controller = new OrderController(context, GetValidator());

            var request = new CreateOrderRequest
            {
                ProductName = "New Product",
                Price = 123
            };

            var result = await controller.CreateOrder(request);

            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            var response = createdResult.Value.Should().BeAssignableTo<CreateOrderResponse>().Subject;

            Order order = await context.Orders.FindAsync(response.Id);

            order.Should().NotBeNull();
            order!.ProductName.Should().Be(request.ProductName);
            order.Price.Should().Be(request.Price);
            order.Status.Should().Be(OrderStatus.Created);

            OutboxMessage outbox = context.OutboxMessages.FirstOrDefault(x => x.Body.Contains(response.Id.ToString()));

            outbox.Should().NotBeNull();
            outbox!.QueueName.Should().Be(QueueNameConstants.CreateOrder);
        }

        [Fact]
        public async Task CreateOrder_Should_Return_BadRequest_When_Invalid()
        {
            using var context = GetDbContext();

            var controller = new OrderController(context, GetValidator(isValid: false));

            var request = new CreateOrderRequest
            {
                ProductName = "",
                Price = 123
            };

            var result = await controller.CreateOrder(request);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeAssignableTo<ValidationProblemDetails>();
        }
    }
}