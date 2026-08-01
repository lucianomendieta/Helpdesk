using System.Net.Http.Json;

using Helpdesk.Web.Dtos;
using Helpdesk.Web.Extensions;
using Helpdesk.Web.Json;

namespace Helpdesk.Web.Services;

public interface ICategoriaService
{
    Task<CategoriaResponseDto[]> GetCategoriasAsync();
    Task<CategoriaResponseDto[]> GetCategoriasActivasAsync();
    Task<ResultadoApi<CategoriaResponseDto>> CreateCategoriaAsync(CrearCategoriaDto dto);
    Task<ResultadoApi> UpdateCategoriaAsync(int categoriaId, ActualizarCategoriaDto dto);
    Task<ResultadoApi> UpdateEstadoCategoriaAsync(int categoriaId, ActualizarEstadoCategoriaDto dto);
}
public class CategoriaService (HttpClient http) : ICategoriaService
{

    #region Gets
    //Trato de obtener las categorias y si no tiro un array vacio.
    public async Task<CategoriaResponseDto[]> GetCategoriasAsync()
    {
        return await http.GetFromJsonAsync<CategoriaResponseDto[]>("categorias", JsonConfig.Options) ?? [];
    }
    //Mismo caso, solo activas
    public async Task<CategoriaResponseDto[]> GetCategoriasActivasAsync()
    {
        return await http.GetFromJsonAsync<CategoriaResponseDto[]>("categorias/activas", JsonConfig.Options) ?? [];
    }

    #endregion

    //Creao una categoria con el dto espejo
    public async Task<ResultadoApi<CategoriaResponseDto>> CreateCategoriaAsync(CrearCategoriaDto dto)
    {
        HttpResponseMessage message = await http.PostAsJsonAsync("categorias", dto, JsonConfig.Options);
        //leo la respuesta del post, si es exitosa creo la categoria
        if (message.IsSuccessStatusCode)
        {
            var successful = await message.Content.ReadFromJsonAsync<CategoriaResponseDto>(JsonConfig.Options);
            return new ResultadoApi<CategoriaResponseDto>(true, successful, null );
        }
        else
        {
            return new ResultadoApi<CategoriaResponseDto>(false, null, await message.LeerErrorAsync());
        }
    }

    #region Puts

    //Actualizo info de una categoria, si es exitoso o si es fallido y leo el error
    public async Task<ResultadoApi> UpdateCategoriaAsync(int categoriaId, ActualizarCategoriaDto dto)
    {
        var response = await http.PutAsJsonAsync($"categorias/{categoriaId}", dto);
        return response.IsSuccessStatusCode ? new ResultadoApi(true, null) : new ResultadoApi(false, await response.LeerErrorAsync()); 
    }
    //Mismo caso que el anterior, solo que actualizo el estado de la categoria
    public async Task<ResultadoApi> UpdateEstadoCategoriaAsync(int categoriaId, ActualizarEstadoCategoriaDto dto)
    {
        var response = await http.PutAsJsonAsync($"categorias/{categoriaId}/status", dto);
        return response.IsSuccessStatusCode ? new ResultadoApi(true, null) : new ResultadoApi(false, await response.LeerErrorAsync());
    }

    #endregion
}
