using System.ComponentModel.DataAnnotations;

using Helpdesk.Api.Models;

namespace Helpdesk.Api.Dtos;

public record ActualizarCategoriaDto
(
    [property: Required]
    [property: StringLength(90)]
    string Nombre,

    string? Descripcion,

    [property: Required]
    string Icono,

    PrioridadTicket PrioridadSugerida
    );
