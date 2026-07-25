using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdjuntosEnTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketAdjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    TicketDetalleId = table.Column<int>(type: "int", nullable: true),
                    NombreOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreAlmacenado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TamanioBytes = table.Column<long>(type: "bigint", nullable: false),
                    SubidoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAdjuntos_TicketDetalles_TicketDetalleId",
                        column: x => x.TicketDetalleId,
                        principalTable: "TicketDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketAdjuntos_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketAdjuntos_Usuarios_SubidoPorId",
                        column: x => x.SubidoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketAdjuntos_SubidoPorId",
                table: "TicketAdjuntos",
                column: "SubidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAdjuntos_TicketDetalleId",
                table: "TicketAdjuntos",
                column: "TicketDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAdjuntos_TicketId",
                table: "TicketAdjuntos",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketAdjuntos");
        }
    }
}
