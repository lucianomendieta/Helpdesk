
namespace Helpdesk.Api.Dtos;

public record TicketStatsDto
(
    int Total,
    int Abierto,
    int EnProgreso,
    int Hecho,
    int Pendiente,
    int Cerrado,
    int SinAsignar,
    int VencenHoy

    );