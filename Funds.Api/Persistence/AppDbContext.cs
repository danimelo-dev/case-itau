using Funds.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Funds.Api.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<ClientPosition> ClientPositions => Set<ClientPosition>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clientes");
            entity.HasKey(x => x.IdCliente);
            entity.Property(x => x.IdCliente).HasColumnName("id_cliente");
            entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(11).IsRequired();
            entity.HasIndex(x => x.Cpf).IsUnique();
            entity.Property(x => x.SaldoDisponivel).HasColumnName("saldo_disponivel").HasColumnType("decimal(18,2)");
            entity.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
            entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
        });

        modelBuilder.Entity<Fund>(entity =>
        {
            entity.ToTable("fundos");
            entity.HasKey(x => x.IdFundo);
            entity.Property(x => x.IdFundo).HasColumnName("id_fundo");
            entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            entity.Property(x => x.HorarioCorte).HasColumnName("horario_corte");
            entity.Property(x => x.ValorCota).HasColumnName("valor_cota").HasColumnType("decimal(18,6)");
            entity.Property(x => x.ValorMinimoAporte).HasColumnName("valor_minimo_aporte").HasColumnType("decimal(18,2)");
            entity.Property(x => x.ValorMinimoPermanencia).HasColumnName("valor_minimo_permanencia").HasColumnType("decimal(18,2)");
            entity.Property(x => x.StatusCaptacao).HasColumnName("status_captacao").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
        });

        modelBuilder.Entity<ClientPosition>(entity =>
        {
            entity.ToTable("posicoes_clientes");
            entity.HasKey(x => x.IdPosicao);
            entity.Property(x => x.IdPosicao).HasColumnName("id_posicao");
            entity.Property(x => x.IdCliente).HasColumnName("id_cliente");
            entity.Property(x => x.IdFundo).HasColumnName("id_fundo");
            entity.Property(x => x.QuantidadeCotas).HasColumnName("quantidade_cotas");
            entity.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
            entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            entity.HasIndex(x => new { x.IdCliente, x.IdFundo }).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("ordens");
            entity.HasKey(x => x.IdOrdem);
            entity.Property(x => x.IdOrdem).HasColumnName("id_ordem");
            entity.Property(x => x.IdCliente).HasColumnName("id_cliente");
            entity.Property(x => x.IdFundo).HasColumnName("id_fundo");
            entity.Property(x => x.TipoOperacao).HasColumnName("tipo_operacao").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.TipoExecucao).HasColumnName("tipo_execucao").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.QuantidadeCotas).HasColumnName("quantidade_cotas");
            entity.Property(x => x.ValorCota).HasColumnName("valor_cota").HasColumnType("decimal(18,6)");
            entity.Property(x => x.ValorOperacao).HasColumnName("valor_operacao").HasColumnType("decimal(18,2)");
            entity.Property(x => x.DataAgendamento).HasColumnName("data_agendamento");
            entity.Property(x => x.ExecutadoEm).HasColumnName("executado_em");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.MotivoRejeicao).HasColumnName("motivo_rejeicao").HasMaxLength(500);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
            entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
        });
    }
}