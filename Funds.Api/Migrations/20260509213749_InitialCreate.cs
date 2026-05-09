using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funds.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    saldo_disponivel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id_cliente);
                });

            migrationBuilder.CreateTable(
                name: "fundos",
                columns: table => new
                {
                    id_fundo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    horario_corte = table.Column<TimeSpan>(type: "time", nullable: false),
                    valor_cota = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    valor_minimo_aporte = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    valor_minimo_permanencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status_captacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundos", x => x.id_fundo);
                });

            migrationBuilder.CreateTable(
                name: "ordens",
                columns: table => new
                {
                    id_ordem = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_cliente = table.Column<int>(type: "int", nullable: false),
                    id_fundo = table.Column<int>(type: "int", nullable: false),
                    tipo_operacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    tipo_execucao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    quantidade_cotas = table.Column<int>(type: "int", nullable: false),
                    valor_cota = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    valor_operacao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    data_agendamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    executado_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    motivo_rejeicao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens", x => x.id_ordem);
                });

            migrationBuilder.CreateTable(
                name: "posicoes_clientes",
                columns: table => new
                {
                    id_posicao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_cliente = table.Column<int>(type: "int", nullable: false),
                    id_fundo = table.Column<int>(type: "int", nullable: false),
                    quantidade_cotas = table.Column<int>(type: "int", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posicoes_clientes", x => x.id_posicao);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_cpf",
                table: "clientes",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ordens_idempotency_key",
                table: "ordens",
                column: "idempotency_key",
                unique: true,
                filter: "[idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_posicoes_clientes_id_cliente_id_fundo",
                table: "posicoes_clientes",
                columns: new[] { "id_cliente", "id_fundo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "fundos");

            migrationBuilder.DropTable(
                name: "ordens");

            migrationBuilder.DropTable(
                name: "posicoes_clientes");
        }
    }
}
