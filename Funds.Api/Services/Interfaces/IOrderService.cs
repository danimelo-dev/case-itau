using Funds.Api.DTOs.Requests;
using Funds.Api.DTOs.Responses;

namespace Funds.Api.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateImmediateOrderAsync(CreateImmediateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderResponse> CreateScheduledOrderAsync(CreateScheduledOrderRequest request, CancellationToken cancellationToken);
    Task<List<OrderResponse>> GetOrdersAsync(int? idCliente, CancellationToken cancellationToken);
}