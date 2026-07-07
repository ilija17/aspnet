using aspnet.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Admin,Manager")]
public class LogsApiController : ControllerBase
{
    private readonly LogBufferSink _bufferSink;

    public LogsApiController(LogBufferSink bufferSink)
    {
        _bufferSink = bufferSink;
    }

    [HttpGet]
    public IActionResult GetLogs(
        [FromQuery] string? level,
        [FromQuery] string? q,
        [FromQuery] string? source,
        [FromQuery] int? count)
    {
        var entries = _bufferSink.GetEntries().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(level))
            entries = entries.Where(e => e.Level.Equals(level, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(q))
            entries = entries.Where(e =>
                e.Message.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (e.Exception != null && e.Exception.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (e.SourceContext != null && e.SourceContext.Contains(q, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(source))
            entries = entries.Where(e =>
                e.SourceContext != null && e.SourceContext.Contains(source, StringComparison.OrdinalIgnoreCase));

        var result = entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count ?? 500)
            .ToList();

        return Ok(new
        {
            totalCount = result.Count,
            filterLevel = level,
            filterQuery = q,
            filterSource = source,
            entries = result
        });
    }

    [HttpGet("levels")]
    public IActionResult GetLevels()
    {
        var levels = _bufferSink.GetEntries()
            .Select(e => e.Level)
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        return Ok(levels);
    }
}
