namespace Funds.Api.Models;

public class Client
{
    public int IdCliente { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public decimal SaldoDisponivel { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}