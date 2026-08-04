using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;

using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Endpoints;

public static class ConfiguracionEndpoints
{
    public static IEndpointRouteBuilder MapConfiguracionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/configuracion")
            .WithTags("Configuracion")
            .RequireAuthorization();

        return app;
    }

    #region Gets

    public static async Task<IResult> GetConfiguracion(HelpdeskDbContext contexto)
    {
        var config = await contexto.ConfiguracionesEmpresa.FirstAsync();
        return Results.Ok(new ConfiguracionResponseDto(config.Id, config.NombreEmpresa, config.UsarFechaVencimiento));
    }
        

    #endregion

    #region PutNombreEmpresa

    //Actualizo el nombre de la empresa
    public static async Task<IResult> PutNombreEmpresa(HelpdeskDbContext contexto, ActualizarNombreEmpresaDto dto)
    {
        var config = await contexto.ConfiguracionesEmpresa.FirstAsync();
        var nombre = config.NombreEmpresa;
        //Guardo el nombre
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    public static async Task<IResult> PutFechaVencimiento(HelpdeskDbContext contexto, ActualizarFechaVencimientoDto dto)
    {
        var config = await contexto.ConfiguracionesEmpresa.FirstAsync(); 
        
        return Results.NoContent();
    }
    #endregion
}