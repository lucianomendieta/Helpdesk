using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Api.Dtos;

public record ActualizarNombreEmpresaDto(
    [property: Required]
    string NombreEmpresa
    );