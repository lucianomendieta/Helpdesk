using Microsoft.EntityFrameworkCore;
using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", Login);
        group.MapPut("/login/changepwd", CambiarPassword).RequireAuthorization();

        return app;
    }


    private static async Task<IResult> Login(AuthLoginDto dto, HelpdeskDbContext contexto, IConfiguration config)
    {
        var usuario = await contexto.Usuarios.FirstOrDefaultAsync(u => u.Nombre == dto.Nombre);

        if (usuario is null)
        {
            return Results.Unauthorized();
        }
        
        if (usuario.Estado != EstadoUsuario.Activo)
        {
            return Results.Unauthorized();
        }

        var hasher = new PasswordHasher<Usuario>();

        var result = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);

        switch (result)
        {
            //Logueo correcto
            case PasswordVerificationResult.Success:
                var tokenGenerado = GenerarToken(usuario, config);
                return Results.Ok(new { mensaje = $"Bienvenido {usuario.NombrePila}!", token = tokenGenerado }); 

            //Login incorrecto    
            case PasswordVerificationResult.Failed:
                //Sin autorizacion
                return Results.Unauthorized();

            //En caso de que cambie el algoritmo. Re-hash
            case PasswordVerificationResult.SuccessRehashNeeded:
                usuario.PasswordHash = hasher.HashPassword(usuario, dto.Password);
                await contexto.SaveChangesAsync();
                var tokenGeneradoReHashed = GenerarToken(usuario, config);
                return Results.Ok(new { mensaje = $"Bienvenido {usuario.NombrePila}!", token = tokenGeneradoReHashed });

            default: return Results.Unauthorized();
        } 
    }

    //Generamos el token para usarlo
    private static string GenerarToken(Usuario usuario, IConfiguration config)
    {
        //Defino los claims a usar (id, rol de usuario y su nombre real)
        var claimList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim("debe_cambiar_credenciales", $"{usuario.DebeCambiarCredenciales}", ClaimValueTypes.Boolean),
            new Claim(ClaimTypes.Name, usuario.NombrePila)

        };
        
        //Si no existe la key, tira excepcion
        string? base64encoded = config["Jwt:Key"];
        if (string.IsNullOrEmpty(base64encoded))
        {
            throw new InvalidOperationException("No se encontro una clave en el servidor.") ;
        }

        try
        {
            //Convierto a bytes legibles para la SecurityKey
            byte[] rawBytes = Convert.FromBase64String(base64encoded);
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(rawBytes);

            //Armo las credenciales y el tokenDescriptor con los datos del config
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            //Leo la expiracion del config, si no esta o es invalida uso 60 minutos por defecto
            var expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var minutos) ? minutos : 30;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                Subject = new ClaimsIdentity(claimList),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = credentials
            };


            var tokenHandler = new JwtSecurityTokenHandler();
            //Genero el token con los params del Descriptor
            var token = tokenHandler.CreateToken(tokenDescriptor);
            //Exporto el token a string 
            string tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        } catch (Exception ex)
        {
            throw new ArgumentException("Error al generar el token: ", ex);
        }
        
    }

    //Cambio password, nivel cliente.
    private static async Task<IResult> CambiarPassword(CambiarPasswordDto dto, HttpContext http, HelpdeskDbContext contexto)
    {
        //Usuario actual se guarda para buscarlo en base, con sus validaciones
        var usuarioActual = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioActual is null)
        {
            return Results.NotFound();
        }
        var usuarioAModificar = await contexto.Usuarios.FindAsync(int.Parse(usuarioActual));
        if (usuarioAModificar is null)
        {
            return Results.NotFound();
        }
        //Instancio el hasher y verifico la password anterior
        var hasher = new PasswordHasher<Usuario>();
        var result = hasher.VerifyHashedPassword(usuarioAModificar, usuarioAModificar.PasswordHash, dto.CurrentPassword);
        
        switch (result)
        {
            //Si es exitoso, cambio la contraseña y desactivo el bool de cambio de credenciales.
            case PasswordVerificationResult.Success:
                usuarioAModificar.PasswordHash = hasher.HashPassword(usuarioAModificar, dto.NewPassword);
                usuarioAModificar.DebeCambiarCredenciales = false;
                await contexto.SaveChangesAsync();
                return Results.Ok(new { mensaje = $"Se ha modificado tu contraseña {usuarioAModificar.NombrePila}, favor iniciar sesion!"});

            //Sin autorizacion
            case PasswordVerificationResult.Failed:
                return Results.BadRequest("Contraseña incorrecta.");

            case PasswordVerificationResult.SuccessRehashNeeded:
                usuarioAModificar.PasswordHash = hasher.HashPassword(usuarioAModificar, dto.NewPassword);
                await contexto.SaveChangesAsync();
                return Results.Ok(new { mensaje = $"Se ha modificado tu contraseña {usuarioAModificar.NombrePila}, favor iniciar sesion!"});
        }
        return Results.NoContent();
    }
}