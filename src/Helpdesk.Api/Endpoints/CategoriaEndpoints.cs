

using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Endpoints;

public static class CategoriaEndpoints
{
    public static IEndpointRouteBuilder MapCategoriaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categorias")
            .WithTags("Categorias")
            .RequireAuthorization();

        //Gets
        group.MapGet("/", GetCategorias)
            .RequireAuthorization("SoloAdmins");
        group.MapGet("/activas", GetCategoriasActivas);

        //Posts
        group.MapPost("/", PostCategorias)
            .RequireAuthorization("SoloAdmins");

        //Puts
        group.MapPut("/{categoriaId}", PutCategoria)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{categoriaId}/status", PutStatusCategoria)
            .RequireAuthorization("SoloAdmins");



        return app;
    }

    //Gets
    #region Gets
    public static async Task<IResult> GetCategorias(HelpdeskDbContext contexto)
    {
        return Results.Ok(await contexto.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaResponseDto(
            c.Id,
            c.Nombre,
            c.Descripcion,
            c.Icono,
            c.PrioridadSugerida,
            c.Activa,
            c.EsDelSistema,
            c.DiasVencimiento
            )).ToListAsync());
    }

    
    public static async Task<IResult> GetCategoriasActivas(HelpdeskDbContext contexto)
    {
        return Results.Ok(await contexto.Categorias
            .Where(c => c.Activa == true)
            .OrderBy(c => c.Id)
            .Select(c => new CategoriaResponseDto(
            c.Id,
            c.Nombre,
            c.Descripcion,
            c.Icono,
            c.PrioridadSugerida,
            c.Activa,
            c.EsDelSistema,
            c.DiasVencimiento
            )).ToListAsync());
    }
    #endregion

    public static async Task<IResult> PostCategorias(HelpdeskDbContext contexto, CrearCategoriaDto dto)
    {
        //Verifico 
        var categoriaOcupada = await contexto.Categorias.AnyAsync(c => c.Nombre == dto.Nombre);
        if (categoriaOcupada)
        {
            return Results.BadRequest("Ya existe una categoria con el mismo nombre.");
        }

        //Creo la nueva categoria
        Categoria nuevo = new()
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Icono = dto.Icono,
            PrioridadSugerida = dto.PrioridadSugerida ?? PrioridadTicket.Media,
            Activa = true,
            EsDelSistema = false
        };

        //Subo la categoria creada a la base de datos y guardo
        contexto.Categorias.Add(nuevo);
        await contexto.SaveChangesAsync();

        //Cargo la respuesta al endpoint
        var respuesta = new CategoriaResponseDto(
            nuevo.Id,
            nuevo.Nombre,
            nuevo.Descripcion,
            nuevo.Icono,
            nuevo.PrioridadSugerida,
            nuevo.Activa,
            nuevo.EsDelSistema,
            nuevo.DiasVencimiento
            );

        return Results.Created($"/categorias/{nuevo.Id}", respuesta);

    }

    //Puts
    #region puts
    public static async Task<IResult> PutCategoria(int categoriaId, HelpdeskDbContext contexto, ActualizarCategoriaDto dto)
    {
        //Verifico que la categoria exista
        var categoria = await contexto.Categorias.FindAsync(categoriaId);
        if (categoria == null) { return Results.NotFound(); }
        
        //Que la categoria no sea del sistema
        if (categoria.EsDelSistema)
        {
            return Results.BadRequest("No se puede editar una categoria del sistema.");
        }
        
        //Verifico que el nombre sea unico
        bool categoriaExists =
            await contexto.Categorias.AnyAsync(c => c.Nombre == dto.Nombre && c.Id != categoriaId);
        if (!categoriaExists)
        {
            //Actualizo la categoria con lo insertado
            categoria.Nombre = dto.Nombre;
            categoria.Descripcion = dto.Descripcion;
            categoria.Icono = dto.Icono;
            categoria.PrioridadSugerida = dto.PrioridadSugerida;
            categoria.DiasVencimiento = dto.DiasVencimiento;
            
            //Guardo la actualizacion
            await contexto.SaveChangesAsync();
            return Results.NoContent();
        }
        else
        { 
            return Results.BadRequest("El nombre de la categoria ya esta en uso.");
        }
    }

    public static async Task<IResult> PutStatusCategoria(int categoriaId, HelpdeskDbContext contexto, ActualizarEstadoCategoriaDto dto)
    {
        //Verifico que la categoria exista
        var categoria = await contexto.Categorias.FindAsync(categoriaId);
        if (categoria == null) { return Results.NotFound(); }

        //Si por alguna razon en el dto esta null
        if (dto.Activa is null) { return Results.BadRequest("Ingrese un estado para la categoría."); }

        //Que la categoria no sea del sistema
        if (categoria.EsDelSistema && dto.Activa == false)
        {
            return Results.BadRequest("No se puede inhabilitar una categoria del sistema.");
        }

        //Seteo el estado nuevo
        categoria.Activa = dto.Activa.Value;

        //Guardo la actualizacion
        await contexto.SaveChangesAsync();
        return Results.NoContent();

    }
    #endregion
}
