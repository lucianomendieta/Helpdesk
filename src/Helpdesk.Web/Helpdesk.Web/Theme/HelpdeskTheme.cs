using MudBlazor;

namespace Helpdesk.Web.Theme;

public static class HelpdeskTheme
{
    public static readonly MudTheme Default = new ()
    {
        //Paleta de colores para modo claro
        PaletteLight = new PaletteLight()
        {
            Primary = "#EA6A3D",
            Background = "#EDE6D8",
            Surface = "#FFFFFF",
            TextPrimary = "#221F1A",
            TextSecondary = "#756F63",
            Info = "#2F6FED",
            Success = "#1E8E5A",
            Warning = "#8A6D18",
            Error = "#C6302F",
            Divider = "rgba(33,29,24,0.08)",
            LinesDefault = "rgba(33,29,24,0.08)",
            
            DrawerBackground = "#211D18",
            DrawerText = "rgba(255,255,255,0.6)",
            DrawerIcon = "rgba(255,255,255,0.6)",
        },
        
        //Tipografia
        Typography = new Typography()
        {
            Default = new DefaultTypography { FontFamily = new[]{"Inter", "sans-serif"}},
            //Los demas typos en app.css
        },
        
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "8px"
        }
    };
}