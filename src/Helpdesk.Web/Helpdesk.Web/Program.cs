using Helpdesk.Web;
using Helpdesk.Web.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();

builder.Services.AddTransient<AuthHeaderHandler>();

var uriString = builder.Configuration["ApiSettings:BaseUrl"];

if (Uri.TryCreate(uriString, UriKind.Absolute, out var uriSettings))
{
    builder.Services.AddHttpClient("client", c =>
    {
        c.BaseAddress = uriSettings;
    })
        .AddHttpMessageHandler<AuthHeaderHandler>();
}
else
{
    throw new Exception("La Uri base no esta bien configurada. Verifique.");
}

//Servicio de localstorage
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
//servicios de auth
builder.Services.AddScoped<IAuthService, AuthService>();
//Custom state
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>( sp =>
{
    var customService = sp.GetRequiredService<CustomAuthenticationStateProvider>();
    return customService;
});
//factory el servicio de authheader
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("client");
});
//servicio de tickets
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketDetalleService, TicketDetalleService>();
//Servicio de usuarios
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
//Servicio de adjuntos
builder.Services.AddScoped<IAdjuntoService, AdjuntoService>();

//Interop de adjuntos
builder.Services.AddScoped<AdjuntoInterop>();

//Servicio de categorias
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

//Servicio de configuraciones
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();

//Servicio de reportes
builder.Services.AddScoped<IReportesService, ReportesService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
