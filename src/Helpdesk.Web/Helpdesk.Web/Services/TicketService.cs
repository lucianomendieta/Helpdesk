using System.Net.Http.Json;
using Helpdesk.Web.Dtos;
using Helpdesk.Web.Extensions;
using Helpdesk.Web.Json;
using System.Net;

namespace Helpdesk.Web.Services;

public interface ITicketService
{
    Task<TicketDto[]> GetTicketsAsync();
    Task<TicketResponseDto> GetTicketAsync(int ticketid);
    Task<ResultadoApi<TicketDto>> CreateTicketAsync(CrearTicketDto dto);
    Task<bool> DeleteTicketAsync(int id);
    Task<ResultadoApi> UpdateTicketAsync(int id, PutTicketDto dto);
    Task<bool> ChangeTicketStateAsync(int id, PutTicketStateDto dto);
    Task<ResultadoApi> AssignTicketAsync(int ticketId, AsignarTicketDto dto);
    Task<bool> ChangeTicketPriorityAsync(int ticketId, ActualizarPrioridadTicketDto dto);
    Task<TicketsStatsDto?> GetTicketsStatsAsync();
    Task<TicketDto[]?> GetTicketsRecientesAsync();
    Task<bool> ChangeTicketCategoriaAsync(int ticketId, ActualizarCategoriaTicketDto dto);
}

public class TicketService(HttpClient http) : ITicketService
{
    public async Task<TicketDto[]> GetTicketsAsync()
    {
        return await http.GetFromJsonAsync<TicketDto[]>("tickets", JsonConfig.Options) ?? [];
    }

    public async Task<TicketResponseDto> GetTicketAsync(int ticketid)
    {
        var response = await http.GetAsync($"tickets/{ticketid}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new TicketResponseDto(null, EstadoResponse.NoEncontrado);
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TicketResponseDto(null, EstadoResponse.Prohibido);
        }
        else
        {
            var ticket = await response.Content.ReadFromJsonAsync<TicketDto>(JsonConfig.Options);
            
            return new TicketResponseDto(ticket, EstadoResponse.Ok);
        }
    }

    public async Task<ResultadoApi<TicketDto>> CreateTicketAsync(CrearTicketDto dto)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync("tickets",dto);
        if (response.IsSuccessStatusCode) 
        {
            var succesful = await response.Content.ReadFromJsonAsync<TicketDto>(JsonConfig.Options);
            return new ResultadoApi<TicketDto>(true, succesful, null);
        }
        else
        {
            return new ResultadoApi<TicketDto>(false, null, await response.LeerErrorAsync());
        }
    }

    public async Task<bool> DeleteTicketAsync(int id)
    {
        var response = await http.DeleteAsync($"tickets/{id}");
        return response.IsSuccessStatusCode;
    }

    //Actualizar ticket
    public async Task<ResultadoApi> UpdateTicketAsync(int id, PutTicketDto dto)
    {
        var response = await http.PutAsJsonAsync($"tickets/{id}", dto);
        if (response.IsSuccessStatusCode)
        {
            return new ResultadoApi(true, null);
        }
        else
        {
            return new ResultadoApi(false, await response.LeerErrorAsync());
        }
    }

    public async Task<bool> ChangeTicketStateAsync(int id, PutTicketStateDto dto)
    {
        var message = await http.PutAsJsonAsync($"tickets/{id}/status", dto,JsonConfig.Options);
        return message.IsSuccessStatusCode;
    }

    public async Task<ResultadoApi> AssignTicketAsync(int ticketId, AsignarTicketDto dto)
    {
        var message = await http.PutAsJsonAsync($"tickets/{ticketId}/assign", dto);
        if (message.IsSuccessStatusCode)
        {
            return new ResultadoApi(true, null);
        }
        else
        {
            return new ResultadoApi(false, await message.LeerErrorAsync());
        }
    }

    public async Task<bool> ChangeTicketPriorityAsync(int ticketId, ActualizarPrioridadTicketDto dto)
    {
        var message = await http.PutAsJsonAsync($"tickets/{ticketId}/priority", dto, JsonConfig.Options);
        return message.IsSuccessStatusCode;
    }


    //Obtengo las stats de los tickets segun el usuario que este el logueado --- Ver Endpoint
    public async Task<TicketsStatsDto?> GetTicketsStatsAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<TicketsStatsDto?>("tickets/stats", JsonConfig.Options);
        }
        catch
        {
            return null;
        }
    }

    //Obtengo los 5 ultimos tickets del usuario logueado --- Ver endpoint
    public async Task<TicketDto[]?> GetTicketsRecientesAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<TicketDto[]?> ("tickets/recents", JsonConfig.Options);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangeTicketCategoriaAsync(int ticketId, ActualizarCategoriaTicketDto dto)
    {
        var message = await http.PutAsJsonAsync($"tickets/{ticketId}/categoria", dto, JsonConfig.Options);
        return message.IsSuccessStatusCode;
    }
    
}
