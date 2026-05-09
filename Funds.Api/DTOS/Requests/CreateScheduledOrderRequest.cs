using Funds.Api.Enums;

namespace Funds.Api.DTOs.Requests;

public class CreateScheduledOrderRequest
{
    public int IdCliente { get; set; }
    public int IdFundo { get; set; }
    public OrderType TipoOperacao { get; set; }
    public int QuantidadeCotas { get; set; }
    public DateTime DataAgendamento { get; set; }
}