using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Entrenadores",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Entrenadores");
        }
    }
}
