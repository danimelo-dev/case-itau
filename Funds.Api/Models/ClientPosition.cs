namespace Funds.Api.Models;

public class ClientPosition
{
    public int IdPosicao { get; set; }
    public int IdCliente { get; set; }
    public int IdFundo { get; set; }
    public int QuantidadeCotas { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}