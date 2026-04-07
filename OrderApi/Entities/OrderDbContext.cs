using Microsoft.EntityFrameworkCore;

namespace OrderApi.Entities
{
    public class OrderDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        public OrderDbContext(DbContextOptions<OrderDbContext> options)
            : base(options) { }
    }
}
