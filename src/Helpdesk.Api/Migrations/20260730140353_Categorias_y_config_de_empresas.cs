using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class Categorias_y_config_de_empresas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaVencimiento",
                table: "Tickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrioridadSugerida = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    EsDelSistema = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEmpresa = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UsarFechaVencimiento = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesEmpresa", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Activa", "Descripcion", "EsDelSistema", "Icono", "Nombre", "PrioridadSugerida" },
                values: new object[] { 1, true, null, true, "HelpOutline", "Otra cosa", 1 });

            migrationBuilder.InsertData(
                table: "ConfiguracionesEmpresa",
                columns: new[] { "Id", "NombreEmpresa", "UsarFechaVencimiento" },
                values: new object[] { 1, "Mi empresa", false });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategoriaId",
                table: "Tickets",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Categorias_CategoriaId",
                table: "Tickets",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Categorias_CategoriaId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "ConfiguracionesEmpresa");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategoriaId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Tickets");
        }
    }
}
