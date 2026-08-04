using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Api.Dtos;

public record ActualizarFechaVencimientoDto(
    [property: Required]
    bool? UsarFechaVencimiento
    );