using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Workouts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPesoYProgresos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PesoKg",
                table: "RutinaEjercicios",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProgresosEjercicio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RutinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreEjercicio = table.Column<string>(type: "text", nullable: false),
                    RepeticionesAnteriores = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesNuevas = table.Column<int>(type: "integer", nullable: false),
                    PesoAnteriorKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    PesoNuevoKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    EsRecordPersonal = table.Column<bool>(type: "boolean", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgresosEjercicio", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgresosEjercicio_UsuarioId_EjercicioId",
                table: "ProgresosEjercicio",
                columns: new[] { "UsuarioId", "EjercicioId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgresosEjercicio");

            migrationBuilder.DropColumn(
                name: "PesoKg",
                table: "RutinaEjercicios");
        }
    }
}
