namespace Funds.Api.DTOs.Responses;

public class OrderResponse
{
    public long IdOrdem { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MotivoRejeicao { get; set; }
}