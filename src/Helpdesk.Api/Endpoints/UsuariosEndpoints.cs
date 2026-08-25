using Microsoft.EntityFrameworkCore;
using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Helpdesk.Api.Endpoints;

public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/usuarios").WithTags("Usuarios").RequireAuthorization();

        //Gets
        group.MapGet("/", GetUsuarios)
            .RequireAuthorization("SoloAdmins");
        group.MapGet("/{id}", GetUsuarioId)
            .RequireAuthorization("SoloAdmins");
        group.MapGet("/asignables", GetUsuariosAsignables)
            .RequireAuthorization("SoloAdmins");
        group.MapGet("/me", GetMiUsuario)
            .RequireAuthorization();
        //Post
        group.MapPost("/", PostUsuario)
            .RequireAuthorization("SoloAdmins");
        //Puts
        group.MapPut("/{id}", PutUsuario)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{id}/email", PutMail)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{id}/status", StatusUser)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{id}/password", ResetUserPassword)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{id}/rol", PutRolUsuario)
            .RequireAuthorization("SoloAdmins");
        //Deletes
        group.MapDelete("/{id}", DeleteUsuario)
            .RequireAuthorization("SoloAdmins");
        

        return app;
    }

    //Get TODOS los usuarios
    private static async Task<IResult> GetUsuarios(HelpdeskDbContext contexto){
        return Results.Ok(await contexto.Usuarios.Select(u => new UsuarioResponseDto(
            u.Id,
            u.Nombre,
            u.Email,
            u.NombrePila,
            u.ApellidoPila,
            u.Rol,
            u.Estado
        )).ToListAsync());
    }
    //Get usuario por ID
    private static async Task<IResult> GetUsuarioId(int id,HelpdeskDbContext contexto)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);

        if (usuario is null)
        {
            return Results.NotFound();
        }
        var respuesta = new UsuarioResponseDto(
            usuario.Id,
            usuario.Nombre,
            usuario.Email,
            usuario.NombrePila,
            usuario.ApellidoPila,
            usuario.Rol,
            usuario.Estado
            );
        
        return Results.Ok(respuesta);
    }

    //Crear usuario
    private static async Task<IResult> PostUsuario(HelpdeskDbContext contexto, CrearUsuarioDto dto)
    {
        //Verifico si tiene rol
        if (dto.Rol is null)
        {
            return Results.BadRequest("Ingrese un rol para el usuario.");
        }
        var hasher = new PasswordHasher<Usuario>();
        //si el email o nombre ya esta en uso, error, sino, dejo crear el objeto
        bool emailisTaken = await contexto.Usuarios.AnyAsync(u => u.Email == dto.Email);
        bool userisTaken = await contexto.Usuarios.AnyAsync(u => u.Nombre == dto.Nombre);

        if (emailisTaken) { return Results.BadRequest("El email ya esta en uso"); }
        if (userisTaken) { return Results.BadRequest("El usuario ya esta en uso"); }

            Usuario nuevo = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                NombrePila = dto.NombrePila,
                ApellidoPila = dto.ApellidoPila,
                Rol = dto.Rol.Value,
            };
            nuevo.PasswordHash = hasher.HashPassword(nuevo, dto.Password);

            contexto.Usuarios.Add(nuevo);
            await contexto.SaveChangesAsync();
            var respuesta = new UsuarioResponseDto(
                nuevo.Id,
                nuevo.Nombre,
                nuevo.Email,
                nuevo.NombrePila,
                nuevo.ApellidoPila,
                nuevo.Rol,
                nuevo.Estado
                );
            return Results.Created($"/usuarios/{nuevo.Id}", respuesta);

    }

    //Modificar usuario
    private static async Task<IResult> PutUsuario(int id, HelpdeskDbContext contexto, ActualizarUsuarioDto dto)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);
        
        if (usuario is null)
        {
            return Results.NotFound();
        }
        
        //Verifico que el usuario no haya sido tomado
        bool isTaken = await contexto.Usuarios.AnyAsync(u => u.Nombre == dto.Nombre && u.Id != id);

        if (!isTaken)
        {
            usuario.Nombre = dto.Nombre;
            usuario.NombrePila = dto.NombrePila;
            usuario.ApellidoPila = dto.ApellidoPila;
            await contexto.SaveChangesAsync();
            return Results.NoContent();
        }
        else
        {
            return Results.BadRequest("El nombre ya esta en uso.");
        }
    }

    //Borrar un usuario
    private static async Task<IResult> DeleteUsuario(int id, HelpdeskDbContext contexto)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return Results.NotFound();
        }
        usuario.Estado = EstadoUsuario.Inactivo;
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    //Actualizar mail (solo admins)
    private static async Task<IResult> PutMail(int id, HelpdeskDbContext contexto, CambiarEmailDto dto)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return Results.NotFound();
        }
        bool isTaken = await contexto.Usuarios.AnyAsync(u => u.Email == dto.NewEmail && u.Id != id);

        if (!isTaken)
        {
            usuario.Email = dto.NewEmail;
            await contexto.SaveChangesAsync();
            return Results.NoContent();
        }
        return Results.BadRequest("El email ya esta en uso.");
    }

    //Actualizar Estado del usuario
    private static async Task<IResult> StatusUser(int id, HelpdeskDbContext contexto, ActualizarEstadoUsuarioDto dto)
    {
        //Verifico si el usuario existe
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return Results.NotFound();
        }
        //Verifico si tiene Estado
        if (dto.EstadoUsuario is null)
        {
            return Results.BadRequest("Ingrese un estado para el usuario.");
        }
        usuario.Estado = dto.EstadoUsuario.Value;

        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ResetUserPassword(int id, HelpdeskDbContext contexto, ResetPasswordDto dto)
    {
        //Busco el usuario, si no existe, not found
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return Results.NotFound();
        }
        //Instancio el hasher
        var hasher = new PasswordHasher<Usuario>();
        //Guardo la contraseña nueva
        usuario.PasswordHash = hasher.HashPassword(usuario, dto.NewPassword);
        usuario.DebeCambiarCredenciales = true;
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> PutRolUsuario(int id, HelpdeskDbContext contexto, ActualizarRolDto dto)
    {
        //Busco el usuario, si no existe, not found
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return Results.NotFound();
        }
        //verifico si el dto no viene nulo
        if(dto.RolUsuario is null) { return Results.BadRequest("Ingrese un rol para el usuario"); }

        //actualizo el rol
        usuario.Rol = dto.RolUsuario.Value;
        await contexto.SaveChangesAsync();
        return Results.NoContent();

    }

    //Filtro los usuarios que sean elegibles para asignar a tickets
    private static async Task<IResult> GetUsuariosAsignables(HelpdeskDbContext contexto)
    {
        var uAsignables = await contexto.Usuarios
            .Where(u => u.Rol == RolUsuario.Agente || u.Rol == RolUsuario.Analista) //Filtro aca y luego selecciono
            .Where(u => u.Estado == EstadoUsuario.Activo) //Si esta bloqueado o inactivo, no es asignable
            .Select(u => new UsuarioResponseDto(
            u.Id,
            u.Nombre,
            u.Email,
            u.NombrePila,
            u.ApellidoPila,
            u.Rol,
            u.Estado
            )).ToListAsync();

        return Results.Ok(uAsignables);
    }

    //Obtengo el usuario logueado
    private static async Task<IResult> GetMiUsuario(ClaimsPrincipal user, HelpdeskDbContext contexto)
    {
        //Obtengo el id atraves del claim
        //Lo paso a entero
        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out int usuarioInt))
        {
            return Results.Unauthorized(); //Si el parse falla, no esta autorizado
        }
        else
        {
            //Busco el usuario en la base
            var usuario = await contexto.Usuarios.FindAsync(usuarioInt);

            //Si no existe, not found
            if (usuario is null)
            {
                return Results.NotFound();
            }

            //Si existe, se mapea al DTO de respuesta y retorna
            var dto = new UsuarioResponseDto
            (
                usuarioInt,
                usuario.Nombre,
                usuario.Email,
                usuario.NombrePila,
                usuario.ApellidoPila,
                usuario.Rol,
                usuario.Estado
                );

            return Results.Ok(dto);
        }
    }

}