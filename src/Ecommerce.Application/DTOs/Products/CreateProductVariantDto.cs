namespace Ecommerce.Application.DTOs.Products;

public class CreateProductVariantDto
{
    public string SKU { get; set; } = default!;

    public string Color { get; set; } = default!;

    public string Size { get; set; } = default!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}