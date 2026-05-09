using Funds.Api.Enums;

namespace Funds.Api.DTOs.Requests;

public class CreateImmediateOrderRequest
{
    public int IdCliente { get; set; }
    public int IdFundo { get; set; }
    public OrderType TipoOperacao { get; set; }
    public int QuantidadeCotas { get; set; }
}