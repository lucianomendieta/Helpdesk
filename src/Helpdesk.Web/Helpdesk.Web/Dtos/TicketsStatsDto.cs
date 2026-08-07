namespace Helpdesk.Web.Dtos;

public record TicketsStatsDto
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
