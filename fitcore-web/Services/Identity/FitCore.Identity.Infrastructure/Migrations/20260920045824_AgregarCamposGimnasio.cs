using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposGimnasio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "telefono",
                table: "Users",
                newName: "Telefono");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CodigoAccesoQR",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CondicionesMedicas",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaNombre",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaTelefono",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EstadoMembresia",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoMembresia",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMembresia",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Entrenadores",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AniosExperiencia",
                table: "Entrenadores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Biografia",
                table: "Entrenadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EstaActivo",
                table: "Entrenadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaContratacion",
                table: "Entrenadores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilUrl",
                table: "Entrenadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Entrenadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EstaActivo",
                table: "Administradores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SucursalId",
                table: "Administradores",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoAccesoQR",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CondicionesMedicas",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaNombre",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaTelefono",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EstadoMembresia",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FechaVencimientoMembresia",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TipoMembresia",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AniosExperiencia",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "Biografia",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "EstaActivo",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "FechaContratacion",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "FotoPerfilUrl",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "EstaActivo",
                table: "Administradores");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Administradores");

            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "Users",
                newName: "telefono");

            migrationBuilder.AlterColumn<int>(
                name: "telefono",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Telefono",
                table: "Entrenadores",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
