using Helpdesk.Api.Models;

namespace Helpdesk.Api.Dtos;

public record CategoriaResponseDto
(
    int Id,
    string Nombre,
    string? Descripcion,
    string Icono,
    PrioridadTicket PrioridadSugerida,
    bool Activa,
    bool EsDelSistema
    );
