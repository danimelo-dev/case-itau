using FluentAssertions;
using Funds.Api.Common;
using Funds.Api.DTOs.Requests;
using Funds.Api.Enums;
using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories;
using Funds.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Funds.Api.Tests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateImmediateOrderAsync_ShouldExecuteInvestment_WhenRequestIsValid()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        var position = new ClientPosition
        {
            IdPosicao = 1,
            IdCliente = 1,
            IdFundo = 1,
            QuantidadeCotas = 10,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        context.ClientPositions.Add(position);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateImmediateOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10
        };

        // Act
        var response = await service.CreateImmediateOrderAsync(
            request,
            CancellationToken.None);

        // Assert
        response.Status.Should().Be(OrderStatus.Executed.ToString());
        response.MotivoRejeicao.Should().BeNull();

        var updatedClient = await context.Clients.FirstAsync(x => x.IdCliente == 1);
        updatedClient.SaldoDisponivel.Should().Be(99000);

        var updatedPosition = await context.ClientPositions
            .FirstAsync(x => x.IdCliente == 1 && x.IdFundo == 1);

        updatedPosition.QuantidadeCotas.Should().Be(20);

        var order = await context.Orders.FirstAsync();

        order.TipoOperacao.Should().Be(OrderType.Aporte);
        order.TipoExecucao.Should().Be(ExecutionType.Imediata);
        order.Status.Should().Be(OrderStatus.Executed);
        order.QuantidadeCotas.Should().Be(10);
        order.ValorOperacao.Should().Be(1000);
        order.ExecutadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateImmediateOrderAsync_ShouldThrowException_WhenClientHasInsufficientBalance()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 500,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateImmediateOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10
        };

        // Act
        var action = async () =>
            await service.CreateImmediateOrderAsync(
                request,
                CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Saldo insuficiente para realizar o aporte.");

        var orders = await context.Orders.ToListAsync();

        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateImmediateOrderAsync_ShouldThrowException_WhenFundIsClosed()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Fechado,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateImmediateOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10
        };

        // Act
        var action = async () =>
            await service.CreateImmediateOrderAsync(request, CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Fundo fechado para captação.");

        var orders = await context.Orders.ToListAsync();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateImmediateOrderAsync_ShouldThrowException_WhenOrderIsOutsideCutOffWindow()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(0, 0, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateImmediateOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10
        };

        // Act
        var action = async () =>
            await service.CreateImmediateOrderAsync(
                request,
                CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Ordem imediata recusada por estar fora da janela de horário de corte.");

        var orders = await context.Orders.ToListAsync();

        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateImmediateOrderAsync_ShouldThrowException_WhenRedemptionLeavesRemainingValueBelowMinimum()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        var position = new ClientPosition
        {
            IdPosicao = 1,
            IdCliente = 1,
            IdFundo = 1,
            QuantidadeCotas = 10,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        context.ClientPositions.Add(position);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateImmediateOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Resgate,
            QuantidadeCotas = 7
        };

        // Act
        var action = async () =>
            await service.CreateImmediateOrderAsync(
                request,
                CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Resgate parcial deixaria saldo remanescente abaixo do mínimo de permanência.");

        var orders = await context.Orders.ToListAsync();

        orders.Should().BeEmpty();

        var currentPosition = await context.ClientPositions
            .FirstAsync(x => x.IdCliente == 1 && x.IdFundo == 1);

        currentPosition.QuantidadeCotas.Should().Be(10);

        var currentClient = await context.Clients
            .FirstAsync(x => x.IdCliente == 1);

        currentClient.SaldoDisponivel.Should().Be(100000);
    }

    [Fact]
    public async Task CreateScheduledOrderAsync_ShouldThrowException_WhenScheduledDateIsTodayOrPast()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateScheduledOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10,
            DataAgendamento = DateTime.Today
        };

        // Act
        var action = async () =>
            await service.CreateScheduledOrderAsync(
                request,
                CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Agendamento deve ser realizado para uma data futura a partir de D+1.");

        var orders = await context.Orders.ToListAsync();

        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateScheduledOrderAsync_ShouldThrowException_WhenScheduledDateIsWeekend()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateScheduledOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10,
            DataAgendamento = new DateTime(2026, 12, 6) // Sunday
        };

        // Act
        var action = async () =>
            await service.CreateScheduledOrderAsync(request, CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Data de agendamento deve ser um dia útil.");

        var orders = await context.Orders.ToListAsync();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateScheduledOrderAsync_ShouldThrowException_WhenClientDoesNotHaveEnoughQuotasForRedemption()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        var position = new ClientPosition
        {
            IdPosicao = 1,
            IdCliente = 1,
            IdFundo = 1,
            QuantidadeCotas = 5,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        context.ClientPositions.Add(position);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateScheduledOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Resgate,
            QuantidadeCotas = 10,
            DataAgendamento = new DateTime(2026, 12, 1)
        };

        // Act
        var action = async () =>
            await service.CreateScheduledOrderAsync(request, CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Cliente não possui cotas suficientes hoje para agendar o resgate.");

        var orders = await context.Orders.ToListAsync();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateScheduledOrderAsync_ShouldCreateScheduledInvestment_WhenRequestIsValid()
    {
        // Arrange
        var context = CreateDbContext();

        var client = new Client
        {
            IdCliente = 1,
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            IdFundo = 1,
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(23, 59, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new CreateScheduledOrderRequest
        {
            IdCliente = 1,
            IdFundo = 1,
            TipoOperacao = OrderType.Aporte,
            QuantidadeCotas = 10,
            DataAgendamento = new DateTime(2026, 12, 1)
        };

        // Act
        var response = await service.CreateScheduledOrderAsync(
            request,
            CancellationToken.None);

        // Assert
        response.Status.Should().Be(OrderStatus.Scheduled.ToString());
        response.MotivoRejeicao.Should().BeNull();

        var order = await context.Orders.FirstAsync();

        order.TipoOperacao.Should().Be(OrderType.Aporte);
        order.TipoExecucao.Should().Be(ExecutionType.Agendada);
        order.Status.Should().Be(OrderStatus.Scheduled);
        order.DataAgendamento.Should().Be(new DateTime(2026, 12, 1));
        order.ExecutadoEm.Should().BeNull();
        order.ValorOperacao.Should().Be(1000);

        var updatedClient = await context.Clients.FirstAsync(x => x.IdCliente == 1);
        updatedClient.SaldoDisponivel.Should().Be(100000);

        var positions = await context.ClientPositions.ToListAsync();
        positions.Should().BeEmpty();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static OrderService CreateService(AppDbContext context)
    {
        var clientRepository = new ClientRepository(context);
        var fundRepository = new FundRepository(context);
        var clientPositionRepository = new ClientPositionRepository(context);
        var orderRepository = new OrderRepository(context);

        return new OrderService(
            context,
            clientRepository,
            fundRepository,
            clientPositionRepository,
            orderRepository,
            new TestDateTimeProvider(),
            NullLogger<OrderService>.Instance);
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 05, 10, 12, 0, 0, DateTimeKind.Utc);

        public DateTime Today => new(2026, 05, 10);

        public TimeSpan CurrentTimeOfDay => new(12, 0, 0);
    }
}