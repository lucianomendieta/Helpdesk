using System.ComponentModel.DataAnnotations;

using Helpdesk.Api.Models;

namespace Helpdesk.Api.Dtos;
public record CrearTicketDto(
    [property: Required(ErrorMessage = "Ingrese un titulo")]
    [property: StringLength(100, MinimumLength = 1)]
    string Titulo,
    [property: Required(ErrorMessage = "Ingrese una descripcion")]
    string Descripcion,

    PrioridadTicket? Prioridad = PrioridadTicket.Media,

    int? CategoriaId = null
    
    );