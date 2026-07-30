using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Api.Dtos;

public record ActualizarEstadoCategoriaDto
    (
        [property: Required]
        bool? Activa
    );