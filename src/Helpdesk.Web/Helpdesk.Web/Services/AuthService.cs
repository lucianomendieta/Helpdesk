using System.Net.Http.Json;
using System.Text.Json;

using Helpdesk.Web.Dtos;
using Helpdesk.Web.Extensions;

namespace Helpdesk.Web.Services;

public interface IAuthService
{
    Task<string?> LoginAsync(string nombre, string password);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<ResultadoApi> ChangePasswordAsync(CambiarPasswordDto dto);
}

public class AuthService (HttpClient http, ILocalStorageService storage, CustomAuthenticationStateProvider customAuthState) : IAuthService
{
    public async Task<string?> LoginAsync(string nombre, string password)
    {
        var requestPath = http.BaseAddress + "auth/login";
        HttpResponseMessage response = await http.PostAsJsonAsync(
            requestPath, 
            (new{ nombre, password })
         );
        if (response.IsSuccessStatusCode)
        {
            //Guardo el body del json 
            var succesful = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            //leo el token y si es null devuelvo excepcion
            string? token = succesful.GetProperty("token").GetString() ?? throw new Exception("La respuesta devolvio un token no valido. Login Invalido.");

            //Guardo el token en local
            await storage.SetItemAsync("token", token);
            customAuthState.NotifyAuthChanged();
            return token;
        }
        else
        {
            throw new Exception("Login fallido. Intente de nuevo");
        }

    }
    //Elimino el token
    public async Task LogoutAsync() { await storage.RemoveItemAsync("token"); customAuthState.NotifyAuthChanged(); }

    //Leo el token
    public async Task<string?> GetTokenAsync() { return  await storage.GetItemAsync("token"); }
    
    //Cambio la contraseña del usuario
    public async Task<ResultadoApi> ChangePasswordAsync(CambiarPasswordDto dto)
    {
        var hasChanged = await http.PutAsJsonAsync("auth/login/changepwd", dto);
        return hasChanged.IsSuccessStatusCode ? new ResultadoApi(true, null) : new ResultadoApi(false, await hasChanged.LeerErrorAsync());
    }
}
