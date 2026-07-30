using System.ComponentModel.DataAnnotations;

using Helpdesk.Api.Models;

namespace Helpdesk.Api.Dtos;

public record CrearCategoriaDto
 (
    [property: Required]
    [property: StringLength(90)]
    string Nombre,

    string? Descripcion,

    [property: Required]    
    string Icono,

    PrioridadTicket? PrioridadSugerida = PrioridadTicket.Media

    );


