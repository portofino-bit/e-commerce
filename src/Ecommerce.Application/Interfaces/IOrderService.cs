using Ecommerce.Application.DTOs.Orders;

namespace Ecommerce.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CheckoutAsync(Guid userId, CreateOrderDto request);

    Task<List<OrderResponseDto>> GetAllForUserAsync(Guid userId);

    Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId);

    Task CancelAsync(Guid userId, Guid orderId);
}
