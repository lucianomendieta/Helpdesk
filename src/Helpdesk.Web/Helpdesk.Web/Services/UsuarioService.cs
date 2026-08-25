
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

using Helpdesk.Web.Dtos;
using Helpdesk.Web.Extensions;
using Helpdesk.Web.Json;

namespace Helpdesk.Web.Services;

public interface IUsuarioService
{
    Task<UsuarioDto[]> GetUsuariosAsync();
    Task<ResultadoApi<UsuarioDto>> CreateUsuarioAsync(CrearUsuarioDto dto);
    Task<ResultadoApi> UpdateUsuarioAsync(int id, PutUsuarioDto dto);
    Task<bool> DeleteUsuarioAsync(int id);
    Task<ResultadoApi> ResetPasswordUsuarioAsync(int id, ResetPasswordDto dto);
    Task<UsuarioDto[]> GetUsuariosAsignablesAsync();
    Task<ResultadoApi> UpdateUsuarioRolAsync(int id, ActualizarRolDto dto);
    Task<UsuarioDto?> GetMiPerfilAsync();
    Task<ResultadoApi> UpdateUsuarioEmailAsync(int id, CambiarEmailDto dto);
}

public class UsuarioService (HttpClient http) : IUsuarioService
{
    //Obtengo todos los usuarios
    public async Task<UsuarioDto[]> GetUsuariosAsync()
    {
        return await http.GetFromJsonAsync<UsuarioDto[]>("usuarios", JsonConfig.Options) ?? [];
    }

    //Crear un usuario
    public async Task<ResultadoApi<UsuarioDto>> CreateUsuarioAsync(CrearUsuarioDto dto)
    {
        //Aguardo el post de usuario y guardo la respuesta
        HttpResponseMessage response = await http.PostAsJsonAsync("usuarios", dto, JsonConfig.Options);
        //Si es exitoso, devuelvo el usuario
        if (response.IsSuccessStatusCode)
        {
            var successful = await response.Content.ReadFromJsonAsync<UsuarioDto>(JsonConfig.Options);
            return new ResultadoApi<UsuarioDto>(true, successful, null);
        }
        else
        {
            return new ResultadoApi<UsuarioDto>(false, null, await response.LeerErrorAsync());
        }
    }

    //Editar usuario, se ejecuta directo en la ruta
    public async Task<ResultadoApi> UpdateUsuarioAsync(int id, PutUsuarioDto dto)
    {
        var response = await http.PutAsJsonAsync($"usuarios/{id}", dto);
        if (response.IsSuccessStatusCode) 
        {
            return new ResultadoApi(true, null);
        }
        else
        {
            return new ResultadoApi(false, await response.LeerErrorAsync());
        }
    }

    //Eliminar usuario, se ejecuta directo en la ruta
    public async Task<bool> DeleteUsuarioAsync(int id)
    {
        var response = await http.DeleteAsync($"usuarios/{id}");
        return response.IsSuccessStatusCode;
    }

    
    public async Task<ResultadoApi> ResetPasswordUsuarioAsync(int id, ResetPasswordDto dto)
    {
        var hasChanged = await http.PutAsJsonAsync($"usuarios/{id}/password", dto);
        if (hasChanged.IsSuccessStatusCode) 
        {
            return new ResultadoApi(true, null);
        }
        else
        {
            return new ResultadoApi(false, await hasChanged.LeerErrorAsync());
        }
    }

    public async Task<UsuarioDto[]> GetUsuariosAsignablesAsync()
    {
        return await http.GetFromJsonAsync<UsuarioDto[]>("usuarios/asignables", JsonConfig.Options) ?? [];
    }

    public async Task<ResultadoApi> UpdateUsuarioRolAsync(int id, ActualizarRolDto dto)
    {
        var updated = await http.PutAsJsonAsync($"usuarios/{id}/rol", dto);
        if (updated.IsSuccessStatusCode)
        {
            return new ResultadoApi(true, null);
        }
        else
        {
            return new ResultadoApi(false, await updated.LeerErrorAsync());
        }
    }

    //Obtengo el usuario logueado
    public async Task<UsuarioDto?> GetMiPerfilAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<UsuarioDto?>("usuarios/me", JsonConfig.Options);
        }
        catch
        {
            return null;
        }

    }
    //Actualizo el correo de la persona
    public async Task<ResultadoApi> UpdateUsuarioEmailAsync(int id, CambiarEmailDto dto)
    {
        var updated = await http.PutAsJsonAsync($"usuarios/{id}/email", dto);
        return updated.IsSuccessStatusCode ? new ResultadoApi(true, null) : new ResultadoApi(false, await updated.LeerErrorAsync());
    }
}
