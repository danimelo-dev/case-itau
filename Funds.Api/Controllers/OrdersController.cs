using Funds.Api.DTOs.Requests;
using Funds.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Funds.Api.Controllers;

[ApiController]
[Route("ordens")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateImmediateOrder(
        [FromBody] CreateImmediateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _orderService.CreateImmediateOrderAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("agendamento")]
    public async Task<IActionResult> CreateScheduledOrder(
        [FromBody] CreateScheduledOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _orderService.CreateScheduledOrderAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? idCliente,
        CancellationToken cancellationToken)
    {
        var response = await _orderService.GetOrdersAsync(
            idCliente,
            cancellationToken);

        return Ok(response);
    }
}