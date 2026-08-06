using System.Net.Http.Json;

using Helpdesk.Web.Dtos;
using Helpdesk.Web.Extensions;

namespace Helpdesk.Web.Services;

public interface IConfiguracionService
{
    Task<ConfiguracionResponseDto?> GetConfiguracionAsync();
    Task<ResultadoApi> UpdateNombreEmpresaAsync(ActualizarNombreEmpresaDto dto);
    Task<ResultadoApi> UpdateFechaVencimientoAsync(ActualizarFechaVencimientoDto dto);
}

public class ConfiguracionService (HttpClient http) : IConfiguracionService
{
    //Obtengo la configuracion
    public async Task<ConfiguracionResponseDto?> GetConfiguracionAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<ConfiguracionResponseDto>("configuracion");
        }
        catch
        {
            return null;
        }
    }

    //Actualizo el nombre de la empresa
    public async Task<ResultadoApi> UpdateNombreEmpresaAsync(ActualizarNombreEmpresaDto dto)
    {
        var response = await http.PutAsJsonAsync("configuracion/nombre-empresa", dto);
        return response.IsSuccessStatusCode
            ? new ResultadoApi(true, null)
            : new ResultadoApi(false, await response.LeerErrorAsync());
    }
    
    //Actualizo si usa fecha de vencimiento o no
    public async Task<ResultadoApi> UpdateFechaVencimientoAsync(ActualizarFechaVencimientoDto dto)
    {
        var response = await http.PutAsJsonAsync("configuracion/fecha-vencimiento", dto);
        return response.IsSuccessStatusCode
            ? new ResultadoApi(true, null)
            : new ResultadoApi(false, await response.LeerErrorAsync());
    }
}