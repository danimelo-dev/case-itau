using Funds.Api.Enums;

namespace Funds.Api.Models;

public class Fund
{
    public int IdFundo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TimeSpan HorarioCorte { get; set; }
    public decimal ValorCota { get; set; }
    public decimal ValorMinimoAporte { get; set; }
    public decimal ValorMinimoPermanencia { get; set; }
    public FundStatus StatusCaptacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}