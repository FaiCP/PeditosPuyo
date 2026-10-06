using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/resolve-link")]
[Authorize]
public class ResolveLinkController : ControllerBase
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        ConnectTimeout = TimeSpan.FromSeconds(10),
    })
    {
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" } },
        Timeout = TimeSpan.FromSeconds(15),
    };

    [HttpGet]
    public async Task<IActionResult> Resolve([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            return BadRequest(new { isSuccess = false, error = "URL inválida" });

        if (parsed.Host != "maps.app.goo.gl" && parsed.Host != "g.co" && parsed.Host != "goo.gl")
            return BadRequest(new { isSuccess = false, error = "Solo se resuelven links acortados de Google Maps" });

        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            return Ok(new { finalUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { isSuccess = false, error = $"No se pudo resolver el link: {ex.Message}" });
        }
    }
}
