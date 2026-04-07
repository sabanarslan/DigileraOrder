namespace OrderApi.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string QueueName { get; set; }

        public string Body { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
