using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Fauzora.Controllers;

[Authorize]
public class RegionController(IHttpClientFactory httpClientFactory, IMemoryCache cache) : Controller
{
    private const string ApiBaseUrl = "https://wilayah.id/api";

    [HttpGet]
    public async Task<IActionResult> List(string level, string? parentCode, CancellationToken cancellationToken)
    {
        var (resource, expectedParentPattern) = level switch
        {
            "province" when string.IsNullOrEmpty(parentCode) => ("provinces.json", (string?)null),
            "city" => ("regencies/", @"^\d{2}$"),
            "district" => ("districts/", @"^\d{2}\.\d{2}$"),
            "village" => ("villages/", @"^\d{2}\.\d{2}\.\d{2}$"),
            _ => (string.Empty, "invalid")
        };

        if (string.IsNullOrEmpty(resource) ||
            (expectedParentPattern is not null &&
             (parentCode is null || !System.Text.RegularExpressions.Regex.IsMatch(parentCode, expectedParentPattern))))
        {
            return BadRequest(new { message = "Permintaan wilayah tidak valid." });
        }

        var path = level == "province" ? resource : $"{resource}{parentCode}.json";
        var cacheKey = $"region-list:{level}:{parentCode ?? "root"}";
        if (cache.TryGetValue(cacheKey, out List<RegionOption>? cached) && cached is not null)
            return Ok(cached);

        try
        {
            using var response = await httpClientFactory.CreateClient().GetAsync(
                $"{ApiBaseUrl}/{path}?lang=id", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Katalog wilayah sedang tidak tersedia." });

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Format katalog wilayah tidak valid." });

            var options = data.EnumerateArray()
                .Where(item => item.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String &&
                               item.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                .Select(item => new RegionOption(
                    item.GetProperty("code").GetString()!,
                    item.GetProperty("name").GetString()!.Trim()))
                .ToList();

            cache.Set(cacheKey, options, TimeSpan.FromHours(24));
            return Ok(options);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Katalog wilayah gagal dihubungi." });
        }
        catch (JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Format katalog wilayah tidak valid." });
        }
    }

    private sealed record RegionOption(string Id, string Name);
}