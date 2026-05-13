using MiniOrm.Attributes;

namespace MiniOrm.Models;

[Table("orders")]
public class Order
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}