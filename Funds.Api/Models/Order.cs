using Funds.Api.Enums;

namespace Funds.Api.Models;

public class Order
{
    public long IdOrdem { get; set; }
    public int IdCliente { get; set; }
    public int IdFundo { get; set; }
    public OrderType TipoOperacao { get; set; }
    public ExecutionType TipoExecucao { get; set; }
    public int QuantidadeCotas { get; set; }
    public decimal ValorCota { get; set; }
    public decimal ValorOperacao { get; set; }
    public DateTime? DataAgendamento { get; set; }
    public DateTime? ExecutadoEm { get; set; }
    public OrderStatus Status { get; set; }
    public string? MotivoRejeicao { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}