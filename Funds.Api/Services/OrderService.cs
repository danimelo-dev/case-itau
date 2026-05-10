using Funds.Api.Common;
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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext context,
        IClientRepository clientRepository,
        IFundRepository fundRepository,
        IClientPositionRepository clientPositionRepository,
        IOrderRepository orderRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<OrderService> logger)
    {
        _context = context;
        _clientRepository = clientRepository;
        _fundRepository = fundRepository;
        _clientPositionRepository = clientPositionRepository;
        _orderRepository = orderRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateImmediateOrderAsync(
        CreateImmediateOrderRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing immediate order. ClientId: {ClientId}, FundId: {FundId}, Operation: {Operation}, Quantity: {Quantity}",
            request.IdCliente,
            request.IdFundo,
            request.TipoOperacao,
            request.QuantidadeCotas);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var client = await GetClientOrThrowAsync(request.IdCliente, cancellationToken);
        var fund = await GetFundOrThrowAsync(request.IdFundo, cancellationToken);

        ValidateQuantity(request.QuantidadeCotas);
        ValidateCutOff(fund);

        var valorOperacao = request.QuantidadeCotas * fund.ValorCota;
        var now = _dateTimeProvider.UtcNow;

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
            CriadoEm = now,
            ExecutadoEm = now
        };

        if (request.TipoOperacao == OrderType.Aporte)
        {
            await ProcessImmediateInvestmentAsync(
                client,
                fund,
                request.QuantidadeCotas,
                valorOperacao,
                cancellationToken);
        }
        else if (request.TipoOperacao == OrderType.Resgate)
        {
            await ProcessImmediateRedemptionAsync(
                client,
                fund,
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

        _logger.LogInformation(
            "Immediate order processed successfully. OrderId: {OrderId}, ClientId: {ClientId}, FundId: {FundId}, Operation: {Operation}, ExecutionType: {ExecutionType}, Amount: {Amount}, Status: {Status}",
            order.IdOrdem,
            order.IdCliente,
            order.IdFundo,
            order.TipoOperacao,
            order.TipoExecucao,
            order.ValorOperacao,
            order.Status);

        return MapToResponse(order);
    }

    public async Task<OrderResponse> CreateScheduledOrderAsync(
        CreateScheduledOrderRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing scheduled order. ClientId: {ClientId}, FundId: {FundId}, Operation: {Operation}, Quantity: {Quantity}, ScheduledDate: {ScheduledDate}",
            request.IdCliente,
            request.IdFundo,
            request.TipoOperacao,
            request.QuantidadeCotas,
            request.DataAgendamento);

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
            CriadoEm = _dateTimeProvider.UtcNow
        };

        if (request.TipoOperacao == OrderType.Aporte)
        {
            ValidateFundCapacity(fund);
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

        _logger.LogInformation(
            "Scheduled order created successfully. OrderId: {OrderId}, ClientId: {ClientId}, FundId: {FundId}, Operation: {Operation}, ScheduledDate: {ScheduledDate}, Amount: {Amount}, Status: {Status}",
            order.IdOrdem,
            order.IdCliente,
            order.IdFundo,
            order.TipoOperacao,
            order.DataAgendamento,
            order.ValorOperacao,
            order.Status);

        return MapToResponse(order);
    }

    public async Task<List<OrderResponse>> GetOrdersAsync(
        int? idCliente,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting orders. ClientId filter: {ClientId}",
            idCliente);

        var orders = await _orderRepository.GetAllAsync(idCliente, cancellationToken);

        return orders.Select(MapToResponse).ToList();
    }

    private async Task ProcessImmediateInvestmentAsync(
        Client client,
        Fund fund,
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

        var now = _dateTimeProvider.UtcNow;

        client.SaldoDisponivel -= valorOperacao;
        client.AtualizadoEm = now;

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
                CriadoEm = now
            };

            await _clientPositionRepository.AddAsync(position, cancellationToken);
        }
        else
        {
            position.QuantidadeCotas += quantidadeCotas;
            position.AtualizadoEm = now;

            await _clientPositionRepository.UpdateAsync(position, cancellationToken);
        }

        await _clientRepository.UpdateAsync(client, cancellationToken);
    }

    private async Task ProcessImmediateRedemptionAsync(
        Client client,
        Fund fund,
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

        var now = _dateTimeProvider.UtcNow;

        position.QuantidadeCotas -= quantidadeCotas;
        position.AtualizadoEm = now;

        client.SaldoDisponivel += valorOperacao;
        client.AtualizadoEm = now;

        await _clientPositionRepository.UpdateAsync(position, cancellationToken);
        await _clientRepository.UpdateAsync(client, cancellationToken);
    }

    private async Task<Client> GetClientOrThrowAsync(int idCliente, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(idCliente, cancellationToken);

        if (client is null)
            throw new BusinessException("Cliente não encontrado.");

        return client;
    }

    private async Task<Fund> GetFundOrThrowAsync(int idFundo, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetByIdAsync(idFundo, cancellationToken);

        if (fund is null)
            throw new BusinessException("Fundo não encontrado.");

        return fund;
    }

    private static void ValidateQuantity(int quantidadeCotas)
    {
        if (quantidadeCotas <= 0)
            throw new BusinessException("Quantidade de cotas deve ser maior que zero.");
    }

    private static void ValidateFundCapacity(Fund fund)
    {
        if (fund.StatusCaptacao == FundStatus.Fechado)
            throw new BusinessException("Fundo fechado para captação.");
    }

    private void ValidateCutOff(Fund fund)
    {
        if (_dateTimeProvider.CurrentTimeOfDay > fund.HorarioCorte)
            throw new BusinessException("Ordem imediata recusada por estar fora da janela de horário de corte.");
    }

    private void ValidateScheduledDate(DateTime dataAgendamento)
    {
        var scheduledDate = dataAgendamento.Date;

        if (scheduledDate <= _dateTimeProvider.Today)
            throw new BusinessException("Agendamento deve ser realizado para uma data futura a partir de D+1.");

        if (scheduledDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new BusinessException("Data de agendamento deve ser um dia útil.");
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            IdOrdem = order.IdOrdem,
            Status = order.Status.ToString(),
            MotivoRejeicao = order.MotivoRejeicao
        };
    }
}