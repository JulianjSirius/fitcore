using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CambiarTelefonoA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"Telefono\" TYPE bigint USING CASE WHEN btrim(\"Telefono\") ~ '^[0-9]{10}$' THEN \"Telefono\"::bigint ELSE 1000000000 END;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Entrenadores\" ALTER COLUMN \"Telefono\" TYPE bigint USING CASE WHEN btrim(\"Telefono\") ~ '^[0-9]{10}$' THEN \"Telefono\"::bigint ELSE 1000000000 END;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Administradores\" ALTER COLUMN \"Telefono\" TYPE bigint USING CASE WHEN btrim(\"Telefono\") ~ '^[0-9]{10}$' THEN \"Telefono\"::bigint ELSE 1000000000 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"Telefono\" TYPE text USING \"Telefono\"::text;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Entrenadores\" ALTER COLUMN \"Telefono\" TYPE text USING \"Telefono\"::text;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Administradores\" ALTER COLUMN \"Telefono\" TYPE text USING \"Telefono\"::text;");
        }
    }
}
