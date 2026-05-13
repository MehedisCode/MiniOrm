using MiniOrm;
using MiniOrm.Models;

var connStr = Environment.GetEnvironmentVariable("MINIORM_CONN");
// Set environment variable
// export MINIORM_CONN="Host=localhost;Port=5432;Database=miniorm;Username=postgres;Password=1234"

if (string.IsNullOrWhiteSpace(connStr))
{
    Console.WriteLine("Connection string not found.");
    return;
}

var db = new AppDbContext(connStr);

var keyboard = new Product
{
    Name = "Keyboard",
    Price = 89.99m,
    Discount = null,
    InStock = true
};

// Insert 
int id = db.Products.Insert(keyboard);
Console.WriteLine($"Inserted ID = {id}");

// Find and update product
var found = db.Products.FindById(id);

if (found != null)
{
    Console.WriteLine($"Found - {found.Name}, Price={found.Price}, Discount={found.Discount?.ToString() ?? "NULL"}");
    found.Price = 79.99m;
    found.Discount = 5.00m;
    db.Products.Update(found);
    Console.WriteLine($"Updated - Price={found.Price}, Discount={found.Discount}");
}

// Get total product count
var all = db.Products.GetAll().ToList();
Console.WriteLine($"Total products: {all.Count}");

// Delete product
db.Products.Delete(id);
Console.WriteLine($"Deleted Id={id} — {db.Products.GetAll().Count()} products remaining");