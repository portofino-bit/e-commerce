using Ecommerce.Application.DTOs.Products;

namespace Ecommerce.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllAsync(
        string? search,
        Guid? categoryId,
        string? sortBy,
        int page,
        int pageSize);

    Task<ProductResponseDto?> GetByIdAsync(Guid id);

    Task<Guid> CreateAsync(CreateProductDto request);

    Task UpdateAsync(Guid id, CreateProductDto request);

    Task DeleteAsync(Guid id);
}