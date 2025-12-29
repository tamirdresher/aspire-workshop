using Microsoft.EntityFrameworkCore;
using Bookstore.Shared;

namespace Bookstore.API.Data;

public class BookstoreDbContext : DbContext
{
    public BookstoreDbContext(DbContextOptions<BookstoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .ToContainer("books")
            .HasPartitionKey(b => b.Id);

        modelBuilder.Entity<Cart>()
            .ToContainer("carts")
            .HasPartitionKey(c => c.Id);

        modelBuilder.Entity<Order>()
            .ToContainer("orders")
            .HasPartitionKey(o => o.Id);
    }
}
