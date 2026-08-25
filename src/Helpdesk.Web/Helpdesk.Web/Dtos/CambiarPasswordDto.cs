namespace Helpdesk.Web.Dtos;

public class CambiarPasswordDto
{
    public string? NewPassword { get; set; }
    public string? CurrentPassword { get; set; }
}