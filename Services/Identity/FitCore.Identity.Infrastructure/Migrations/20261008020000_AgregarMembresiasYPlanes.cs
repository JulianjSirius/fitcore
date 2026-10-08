using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitCore.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMembresiasYPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DuracionMeses = table.Column<int>(type: "integer", nullable: false),
                    MaxBeneficiarios = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Membresias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioTitularId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompananteUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membresias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Membresias_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembresiaId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompananteUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProveedorTransaccionId = table.Column<string>(type: "text", nullable: true),
                    RegistradoPorAdministradorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Membresias_MembresiaId",
                        column: x => x.MembresiaId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Planes",
                columns: new[] { "Id", "Activo", "Codigo", "DuracionMeses", "FechaActualizacion", "MaxBeneficiarios", "Nombre", "Precio" },
                values: new object[,]
                {
                    { new Guid("6f1d2c3a-0001-4a7e-9c11-000000000001"), true, "Mensual", 1, new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Mensual", 120000m },
                    { new Guid("6f1d2c3a-0002-4a7e-9c11-000000000002"), true, "Duo", 1, new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Duo", 200000m },
                    { new Guid("6f1d2c3a-0003-4a7e-9c11-000000000003"), true, "Anual", 12, new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Anual", 1200000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_AcompananteUsuarioId",
                table: "Membresias",
                column: "AcompananteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_Estado_FechaVencimiento",
                table: "Membresias",
                columns: new[] { "Estado", "FechaVencimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_PlanId",
                table: "Membresias",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_UsuarioTitularId",
                table: "Membresias",
                column: "UsuarioTitularId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_MembresiaId",
                table: "Pagos",
                column: "MembresiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_PlanId",
                table: "Pagos",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_Referencia",
                table: "Pagos",
                column: "Referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_UsuarioId",
                table: "Pagos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Planes_Codigo",
                table: "Planes",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Membresias");

            migrationBuilder.DropTable(
                name: "Planes");
        }
    }
}
