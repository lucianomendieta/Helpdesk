using Helpdesk.Api.Models;


namespace Helpdesk.Api.Dtos;

public record TicketResponseDto
(
    int Id,
    string Titulo,
    string Descripcion,
    DateTimeOffset FechaCreacion,
    EstadoTicket Estado,
    string NombreCreador,
    string? NombreAgente,
    int UsuarioCreo,
    int? AgenteAsignadoId,
    PrioridadTicket Prioridad,
    int? CategoriaId,
    string? CategoriaNombre,
    string? CategoriaIcono
    );
