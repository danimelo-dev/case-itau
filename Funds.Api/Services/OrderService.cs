using Funds.Api.DTOs.Requests;
using Funds.Api.DTOs.Responses;
using Funds.Api.Enums;
using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories.Interfaces;
using Funds.Api.Services.Interfaces;

namespace Funds.Api.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IClientRepository _clientRepository;
    private readonly IFundRepository _fundRepository;
    private readonly IClientPositionRepository _clientPositionRepository;
    private readonly IOrderRepository _orderRepository;

    public OrderService(
        AppDbContext context,
        IClientRepository clientRepository,
        IFundRepository fundRepository,
        IClientPositionRepository clientPositionRepository,
        IOrderRepository orderRepository)
    {
        _context = context;
        _clientRepository = clientRepository;
        _fundRepository = fundRepository;
        _clientPositionRepository = clientPositionRepository;
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> CreateImmediateOrderAsync(
        CreateImmediateOrderRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var client = await GetClientOrThrowAsync(request.IdCliente, cancellationToken);
        var fund = await GetFundOrThrowAsync(request.IdFundo, cancellationToken);

        var valorOperacao = request.QuantidadeCotas * fund.ValorCota;

        ValidateQuantity(request.QuantidadeCotas);
        ValidateCutOff(fund);

        var order = new Order
        {
            IdCliente = request.IdCliente,
            IdFundo = request.IdFundo,
            TipoOperacao = request.TipoOperacao,
            TipoExecucao = ExecutionType.Imediata,
            QuantidadeCotas = request.QuantidadeCotas,
            ValorCota = fund.ValorCota,
            ValorOperacao = valorOperacao,
            Status = OrderStatus.Executed,
            CriadoEm = DateTime.UtcNow,
            ExecutadoEm = DateTime.UtcNow
        };

        if (request.TipoOperacao == OrderType.Aporte)
        {
            await ProcessImmediateInvestmentAsync(
                client,
                fund,
                order,
                request.QuantidadeCotas,
                valorOperacao,
                cancellationToken);
        }
        else if (request.TipoOperacao == OrderType.Resgate)
        {
            await ProcessImmediateRedemptionAsync(
                client,
                fund,
                order,
                request.QuantidadeCotas,
                valorOperacao,
                cancellationToken);
        }
        else
        {
            throw new BusinessException("Tipo de operação inválido.");
        }

        await _orderRepository.AddAsync(order, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new OrderResponse
        {
            IdOrdem = order.IdOrdem,
            Status = order.Status.ToString(),
            MotivoRejeicao = order.MotivoRejeicao
        };
    }

    public async Task<OrderResponse> CreateScheduledOrderAsync(
        CreateScheduledOrderRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(request.IdCliente, cancellationToken);
        var fund = await GetFundOrThrowAsync(request.IdFundo, cancellationToken);

        ValidateQuantity(request.QuantidadeCotas);
        ValidateScheduledDate(request.DataAgendamento);

        var valorOperacao = request.QuantidadeCotas * fund.ValorCota;

        var order = new Order
        {
            IdCliente = client.IdCliente,
            IdFundo = fund.IdFundo,
            TipoOperacao = request.TipoOperacao,
            TipoExecucao = ExecutionType.Agendada,
            QuantidadeCotas = request.QuantidadeCotas,
            ValorCota = fund.ValorCota,
            ValorOperacao = valorOperacao,
            DataAgendamento = request.DataAgendamento.Date,
            Status = OrderStatus.Scheduled,
            CriadoEm = DateTime.UtcNow
        };

        if (request.TipoOperacao == OrderType.Aporte)
        {
            ValidateFundCapacity(fund);

            // Regra do case:
            // Aplicação agendada NÃO valida saldo agora.
        }
        else if (request.TipoOperacao == OrderType.Resgate)
        {
            var position = await _clientPositionRepository.GetByClientAndFundAsync(
                request.IdCliente,
                request.IdFundo,
                cancellationToken);

            if (position is null || position.QuantidadeCotas < request.QuantidadeCotas)
            {
                throw new BusinessException("Cliente não possui cotas suficientes hoje para agendar o resgate.");
            }
        }
        else
        {
            throw new BusinessException("Tipo de operação inválido.");
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new OrderResponse
        {
            IdOrdem = order.IdOrdem,
            Status = order.Status.ToString(),
            MotivoRejeicao = order.MotivoRejeicao
        };
    }

    public async Task<List<OrderResponse>> GetOrdersAsync(
        int? idCliente,
        CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(idCliente, cancellationToken);

        return orders.Select(order => new OrderResponse
        {
            IdOrdem = order.IdOrdem,
            Status = order.Status.ToString(),
            MotivoRejeicao = order.MotivoRejeicao
        }).ToList();
    }

    private async Task ProcessImmediateInvestmentAsync(
        Client client,
        Fund fund,
        Order order,
        int quantidadeCotas,
        decimal valorOperacao,
        CancellationToken cancellationToken)
    {
        ValidateFundCapacity(fund);

        if (valorOperacao < fund.ValorMinimoAporte)
        {
            throw new BusinessException("Valor da aplicação inferior ao valor mínimo de aporte.");
        }

        if (client.SaldoDisponivel < valorOperacao)
        {
            throw new BusinessException("Saldo insuficiente para realizar o aporte.");
        }

        client.SaldoDisponivel -= valorOperacao;
        client.AtualizadoEm = DateTime.UtcNow;

        var position = await _clientPositionRepository.GetByClientAndFundAsync(
            client.IdCliente,
            fund.IdFundo,
            cancellationToken);

        if (position is null)
        {
            position = new ClientPosition
            {
                IdCliente = client.IdCliente,
                IdFundo = fund.IdFundo,
                QuantidadeCotas = quantidadeCotas,
                CriadoEm = DateTime.UtcNow
            };

            await _clientPositionRepository.AddAsync(position, cancellationToken);
        }
        else
        {
            position.QuantidadeCotas += quantidadeCotas;
            position.AtualizadoEm = DateTime.UtcNow;

            await _clientPositionRepository.UpdateAsync(position, cancellationToken);
        }

        await _clientRepository.UpdateAsync(client, cancellationToken);
    }

    private async Task ProcessImmediateRedemptionAsync(
        Client client,
        Fund fund,
        Order order,
        int quantidadeCotas,
        decimal valorOperacao,
        CancellationToken cancellationToken)
    {
        var position = await _clientPositionRepository.GetByClientAndFundAsync(
            client.IdCliente,
            fund.IdFundo,
            cancellationToken);

        if (position is null || position.QuantidadeCotas < quantidadeCotas)
        {
            throw new BusinessException("Cliente não possui cotas suficientes para realizar o resgate.");
        }

        var remainingQuotas = position.QuantidadeCotas - quantidadeCotas;
        var remainingValue = remainingQuotas * fund.ValorCota;

        if (remainingValue > 0 && remainingValue < fund.ValorMinimoPermanencia)
        {
            throw new BusinessException("Resgate parcial deixaria saldo remanescente abaixo do mínimo de permanência.");
        }

        position.QuantidadeCotas -= quantidadeCotas;
        position.AtualizadoEm = DateTime.UtcNow;

        client.SaldoDisponivel += valorOperacao;
        client.AtualizadoEm = DateTime.UtcNow;

        await _clientPositionRepository.UpdateAsync(position, cancellationToken);
        await _clientRepository.UpdateAsync(client, cancellationToken);
    }

    private async Task<Client> GetClientOrThrowAsync(
        int idCliente,
        CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(idCliente, cancellationToken);

        if (client is null)
        {
            throw new BusinessException("Cliente não encontrado.");
        }

        return client;
    }

    private async Task<Fund> GetFundOrThrowAsync(
        int idFundo,
        CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetByIdAsync(idFundo, cancellationToken);

        if (fund is null)
        {
            throw new BusinessException("Fundo não encontrado.");
        }

        return fund;
    }

    private static void ValidateQuantity(int quantidadeCotas)
    {
        if (quantidadeCotas <= 0)
        {
            throw new BusinessException("Quantidade de cotas deve ser maior que zero.");
        }
    }

    private static void ValidateFundCapacity(Fund fund)
    {
        if (fund.StatusCaptacao == FundStatus.Fechado)
        {
            throw new BusinessException("Fundo fechado para captação.");
        }
    }

    private static void ValidateCutOff(Fund fund)
    {
        var currentTime = DateTime.Now.TimeOfDay;

        if (currentTime > fund.HorarioCorte)
        {
            throw new BusinessException("Ordem imediata recusada por estar fora da janela de horário de corte.");
        }
    }

    private static void ValidateScheduledDate(DateTime dataAgendamento)
    {
        var today = DateTime.Today;
        var scheduledDate = dataAgendamento.Date;

        if (scheduledDate <= today)
        {
            throw new BusinessException("Agendamento deve ser realizado para uma data futura a partir de D+1.");
        }

        if (scheduledDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            throw new BusinessException("Data de agendamento deve ser um dia útil.");
        }
    }
}