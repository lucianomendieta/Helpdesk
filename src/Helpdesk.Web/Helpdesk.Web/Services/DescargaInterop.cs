using Microsoft.JSInterop;

namespace Helpdesk.Web.Services;

public class DescargaInterop(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _objReference = null;

    //LLeno mi objeto 
    private async Task<IJSObjectReference> FillObjRef()
    {
        _objReference ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/descargas.js");
        return _objReference;
    }

    //Descargo por medio del stream
    public async Task DescargarArchivo(Stream stream, string nombreArchivo)
    {
        using DotNetStreamReference streamReference = new(stream);
        var modulo = await FillObjRef();
        await modulo.InvokeVoidAsync("descargarArchivo", streamReference, nombreArchivo);
    }

    //Dispongo del objeto
    public async ValueTask DisposeAsync()
    {
        if (_objReference != null) { await _objReference.DisposeAsync(); }
    }
}