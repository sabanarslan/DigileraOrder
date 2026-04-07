namespace OrderApi.Models.Requests
{
    public class CreateOrderRequest
    {
        public string ProductName { get; set; }

        public decimal Price { get; set; }
    }
}
