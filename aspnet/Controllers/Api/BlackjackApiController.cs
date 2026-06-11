using aspnet.Models.DTO;
using aspnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

// API za blackjack stol (wwwroot/kocka). Sva pravila igre su u
// BlackjackTableService; svaki poziv vraća pogled personaliziran za clientId.
[Route("api/blackjack")]
[ApiController]
public class BlackjackApiController : ControllerBase
{
    private readonly BlackjackTableService _table;

    public BlackjackApiController(BlackjackTableService table) => _table = table;

    public record BlackjackActionRequest(string? ClientId, int? Seat, int? Amount);

    /// GET /api/blackjack/state?clientId=abc — polling za sinkronizaciju stola
    [HttpGet("state")]
    public ActionResult<BlackjackStateDTO> State(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.GetState(clientId));
    }

    [HttpPost("join")]
    public ActionResult<BlackjackStateDTO> Join([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        if (req.Seat is not (1 or 2)) return BadRequest(new { error = "seat must be 1 or 2." });
        return Ok(_table.Join(req.ClientId, req.Seat.Value));
    }

    [HttpPost("leave")]
    public ActionResult<BlackjackStateDTO> Leave([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Leave(req.ClientId));
    }

    [HttpPost("solo")]
    public ActionResult<BlackjackStateDTO> ToggleSolo([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.ToggleSolo(req.ClientId));
    }

    [HttpPost("reset")]
    public ActionResult<BlackjackStateDTO> Reset([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Reset(req.ClientId));
    }

    [HttpPost("bet")]
    public ActionResult<BlackjackStateDTO> Bet([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        if (req.Amount is not > 0) return BadRequest(new { error = "amount must be positive." });
        return Ok(_table.SetBet(req.ClientId, req.Amount.Value));
    }

    [HttpPost("deal")]
    public ActionResult<BlackjackStateDTO> Deal([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Deal(req.ClientId));
    }

    [HttpPost("hit")]
    public ActionResult<BlackjackStateDTO> Hit([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Hit(req.ClientId));
    }

    [HttpPost("stand")]
    public ActionResult<BlackjackStateDTO> Stand([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Stand(req.ClientId));
    }

    [HttpPost("double")]
    public ActionResult<BlackjackStateDTO> Double([FromBody] BlackjackActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId)) return BadRequest(new { error = "clientId is required." });
        return Ok(_table.Double(req.ClientId));
    }
}
