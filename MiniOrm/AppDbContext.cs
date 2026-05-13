using MiniOrm.Data;
using MiniOrm.Models;

namespace MiniOrm;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    public AppDbContext(string connStr) : base(connStr)
    {
        Products = new DbSet<Product>(this);
    }
}