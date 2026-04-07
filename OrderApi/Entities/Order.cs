using OrderApi.Models.Enums;

namespace OrderApi.Entities
{
    public class Order
    {
        public Guid Id { get; set; } 

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public OrderStatus Status { get; set; }
    }
}
