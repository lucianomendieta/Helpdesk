using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjustesColumnasAdjuntos1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NombreOriginal",
                table: "TicketAdjuntos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NombreAlmacenado",
                table: "TicketAdjuntos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "TicketAdjuntos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAdjuntos_NombreAlmacenado",
                table: "TicketAdjuntos",
                column: "NombreAlmacenado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketAdjuntos_NombreAlmacenado",
                table: "TicketAdjuntos");

            migrationBuilder.AlterColumn<string>(
                name: "NombreOriginal",
                table: "TicketAdjuntos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "NombreAlmacenado",
                table: "TicketAdjuntos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "TicketAdjuntos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
