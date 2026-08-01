using Helpdesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Helpdesk.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Helpdesk.Api.Authorization;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Helpdesk.Api.Models;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Helpdesk.Api.Almacenamiento;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        string? base64encoded = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(base64encoded))
        {
            throw new InvalidOperationException("No se encontro una clave en el servidor.") ;
        }

        //Convierto a bytes legibles para la SecurityKey
            byte[] rawBytes = Convert.FromBase64String(base64encoded);
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(rawBytes);
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = securityKey
        };
    });
builder.Services.AddHelpdeskPolicies();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//agrego el servicio de Almacenamiento en disco
builder.Services.AddSingleton<IAlmacenamientoAdjuntos, AlmacenamientoDisco>();

//verifico si esta configurado el setting
var originString = builder.Configuration["Cors:AllowedOrigin"];
if (originString != null)
{
    //agrego cors para permitir a la api alimentar la web
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "webView", policy =>
        {
            policy.WithOrigins(originString)
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
    });
}
else
{
    throw new Exception("La url de la web no esta configurada. Verifique appsettings.json");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseSqlServer(connectionString));
var app = builder.Build();

app.UseCors("webView");

app.UseAuthentication();
app.Use(async (HttpContext http, RequestDelegate next) => 
{
    if (http.User.Identity?.IsAuthenticated == true)
    {
        var pathPwd = new PathString("/auth/login/changepwd");
        var debeCambiar = http.User.FindFirstValue("debe_cambiar_credenciales");
        if (bool.TryParse(debeCambiar, out bool debeCambiarCredenciales))
        {
            if (!debeCambiarCredenciales)
            {
                await next(http);
            } else if (http.Request.Path.StartsWithSegments(pathPwd)){
                await next(http);
            } else
            {
                http.Response.StatusCode = 403;
                return;
            }
        } else
        {
            await next(http);
        }
    } else
    {
        await next(http);
    }
    });

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.MapGet("/ping", () => new
{
    status = "ok",
    time = DateTimeOffset.UtcNow,
    environment = app.Environment.EnvironmentName
});

using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();
    bool existeAdmin = await contexto.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Administrador);
    if (!existeAdmin)
    {
        string? adminPass = app.Configuration["SeedAdmin:Password"];
        var hasher = new PasswordHasher<Usuario>();
        Usuario nuevo = new Usuario
        {
            Nombre = "admin",
            Email = "admin@helpdesk.com",
            NombrePila = "Sistema",
            ApellidoPila = "MesaPlus",
            Rol = RolUsuario.Administrador,
        };
        if (string.IsNullOrEmpty(adminPass))
        {
            throw new InvalidOperationException("Falta configurar SeedAdmin:Password");
        }
        nuevo.PasswordHash = hasher.HashPassword(nuevo, adminPass);

        contexto.Usuarios.Add(nuevo);
        await contexto.SaveChangesAsync();
    }
}

//Grupos de endpoints
app.MapTicketEndpoints();
app.MapUsuariosEndpoints();
app.MapAuthEndpoints();
app.MapTicketDetalleEndpoints();
app.MapAdjuntoEndpoints();
app.MapCategoriaEndpoints();

app.Run();
